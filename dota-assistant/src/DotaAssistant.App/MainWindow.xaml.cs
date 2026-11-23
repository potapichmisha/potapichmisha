using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using DotaAssistant.App.Services;
using DotaAssistant.Capture;
using DotaAssistant.Tips.Engine;
using DotaAssistant.Tips.Models;
using DotaAssistant.Tips.Rules;
using DotaAssistant.Vision.Catalog;
using DotaAssistant.Vision.Models;
using DotaAssistant.Vision.Recognition;

namespace DotaAssistant.App;

public partial class MainWindow : Window
{
    private readonly WindowChromeService _chrome;
    private readonly PlayerOverrideState _overrides = new();
    private HotkeyService? _hotkey;
    private IScreenCapture? _capture;
    private readonly DispatcherTimer _statsTimer;
    private readonly DispatcherTimer _recognitionTimer;
    private bool _contentVisible = true;
    private TipEngine? _tipEngine;
    private FrameRecognizer? _recognizer;
    private TemplateCatalog? _catalog;
    private long _lastRecognizedPixelFrame = -1;
    private RecognitionResult? _lastResult;
    private string _lastSourceRu = "ожидание…";
    private bool _suppressOverrideEvents;

    public MainWindow()
    {
        InitializeComponent();
        _chrome = new WindowChromeService(this);
        Opacity = OpacitySlider.Value;

        _statsTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(250)
        };
        _statsTimer.Tick += (_, _) => RefreshCaptureStats();

        _recognitionTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(500)
        };
        _recognitionTimer.Tick += (_, _) => TryLiveRecognition();

        Loaded += OnLoaded;
        Closed += OnClosed;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        _hotkey = new HotkeyService(this);
        _hotkey.HotkeyPressed += ToggleContentVisibility;
        _hotkey.Register();
        HotkeyText.Text = _hotkey.IsRegistered
            ? $"Горячая клавиша скрыть/показать: {_hotkey.ShortcutDisplay}"
            : $"Не удалось зарегистрировать {_hotkey.ShortcutDisplay} (занята другой программой). Используйте кнопку «Скрыть».";

        _chrome.Apply(UiDisplayMode.SeparateWindow);
        UpdateModeBadge();
        TryLoadTipEngine();
        TryLoadRecognizer();
        PopulateOverrideCombos();
        RefreshStatusPanel();

        await StartCaptureAsync().ConfigureAwait(true);
        _statsTimer.Start();
        _recognitionTimer.Start();
    }

    private async void OnClosed(object? sender, EventArgs e)
    {
        _statsTimer.Stop();
        _recognitionTimer.Stop();
        _hotkey?.Dispose();
        if (_capture is not null)
        {
            await _capture.DisposeAsync().ConfigureAwait(true);
            _capture = null;
        }
    }

    private void TryLoadTipEngine()
    {
        try
        {
            var tipsDir = Path.Combine(AppContext.BaseDirectory, "assets", "tips");
            if (!Directory.Exists(tipsDir))
            {
                TipsStatusText.Text = "Не найден assets/tips. Пересоберите проект.";
                return;
            }

            _tipEngine = new TipEngine(TipRulesStore.LoadFromTipsDirectory(tipsDir));
            TipsStatusText.Text = "Правила mid загружены.";
        }
        catch (Exception ex)
        {
            _tipEngine = null;
            TipsStatusText.Text = $"Не удалось загрузить советы: {ex.Message}";
        }
    }

    private void TryLoadRecognizer()
    {
        try
        {
            var templates = Path.Combine(AppContext.BaseDirectory, "assets", "templates");
            if (!Directory.Exists(templates))
            {
                StatusSourceText.Text = "Источник: нет assets/templates — пересоберите проект.";
                return;
            }

            _catalog = TemplateCatalog.LoadFromDirectory(templates);
            _recognizer = new FrameRecognizer(_catalog, maxInventorySlots: 6);
            StatusSourceText.Text = "Источник: шаблоны загружены. Включите живое распознавание или тест на фикстуре.";
        }
        catch (Exception ex)
        {
            _catalog = null;
            _recognizer = null;
            StatusSourceText.Text = $"Источник: ошибка шаблонов — {ex.Message}";
        }
    }

    private void PopulateOverrideCombos()
    {
        _suppressOverrideEvents = true;
        try
        {
            HeroOverrideCombo.Items.Clear();
            HeroOverrideCombo.Items.Add(new HeroPickItem(null, "Авто (распознавание)"));
            if (_catalog is not null)
            {
                foreach (var hero in _catalog.Heroes.OrderBy(h => h.DisplayNameRu, StringComparer.OrdinalIgnoreCase))
                {
                    HeroOverrideCombo.Items.Add(new HeroPickItem(hero.Id, hero.DisplayNameRu));
                }
            }

            HeroOverrideCombo.SelectedIndex = 0;

            SideOverrideCombo.Items.Clear();
            SideOverrideCombo.Items.Add(new SidePickItem(null, "Авто (не определена)"));
            SideOverrideCombo.Items.Add(new SidePickItem(PlayerSide.Radiant, "Radiant"));
            SideOverrideCombo.Items.Add(new SidePickItem(PlayerSide.Dire, "Dire"));
            SideOverrideCombo.SelectedIndex = 0;
        }
        finally
        {
            _suppressOverrideEvents = false;
        }
    }

    private async Task StartCaptureAsync()
    {
        if (_capture is not null)
        {
            await _capture.DisposeAsync().ConfigureAwait(true);
            _capture = null;
        }

        CaptureStatusText.Text = "Статус: запуск…";
        _lastRecognizedPixelFrame = -1;
        _capture = await ScreenCaptureFactory.CreateAndStartAsync().ConfigureAwait(true);
        RefreshCaptureStats();
    }

    private void RefreshCaptureStats()
    {
        if (_capture is null)
        {
            return;
        }

        var stats = _capture.GetStats();
        CaptureBackendText.Text = $"Бэкенд: {stats.Backend}";
        CaptureStatusText.Text = $"Статус: {stats.Status}";
        CaptureFpsText.Text = $"FPS: {stats.Fps:0.0}";
        CaptureFrameText.Text = stats.LastFrameAt is null
            ? "Последний кадр: —"
            : $"Последний кадр: {stats.LastFrameAt:HH:mm:ss.fff}";
        CaptureCountText.Text = $"Кадров: {stats.FrameCount}";

        if (_capture.TryCloneLatestPixels(out var pixels) && pixels is not null)
        {
            CapturePixelsText.Text =
                $"Пиксели для vision: {pixels.Width}×{pixels.Height}, кадр #{pixels.FrameIndex}, {pixels.CapturedAt:HH:mm:ss.fff}";
        }
        else
        {
            CapturePixelsText.Text = "Пиксели для vision: нет (заглушка или ещё нет копии DXGI)";
        }
    }

    private void TryLiveRecognition()
    {
        if (LiveRecognitionCheck.IsChecked != true)
        {
            return;
        }

        if (_capture is null || _recognizer is null)
        {
            return;
        }

        if (!_capture.TryCloneLatestPixels(out var pixels) || pixels is null)
        {
            if (_lastResult is null && !_overrides.HasHeroOverride)
            {
                StatusSourceText.Text =
                    "Источник: живое распознавание ждёт DXGI-пиксели. На Linux/удалёнке — только «Тест на фикстуре».";
            }

            return;
        }

        if (pixels.FrameIndex == _lastRecognizedPixelFrame)
        {
            return;
        }

        _lastRecognizedPixelFrame = pixels.FrameIndex;

        try
        {
            var result = _recognizer.RecognizeBgra(
                pixels.Width,
                pixels.Height,
                pixels.BgraPixels,
                pixels.RowPitch);
            ApplyRecognition(result, $"живой DXGI кадр #{pixels.FrameIndex}");
        }
        catch (Exception ex)
        {
            StatusSourceText.Text = $"Источник: ошибка живого кадра — {ex.Message}";
            // Keep previous panel values so flaky frames do not wipe the UI.
        }
    }

    private void UiMode_OnChecked(object sender, RoutedEventArgs e)
    {
        if (!IsLoaded)
        {
            return;
        }

        var mode = ModeOverlayRadio.IsChecked == true
            ? UiDisplayMode.Overlay
            : UiDisplayMode.SeparateWindow;
        _chrome.Apply(mode);
        ClickThroughCheck.IsEnabled = mode == UiDisplayMode.Overlay;
        if (mode != UiDisplayMode.Overlay)
        {
            ClickThroughCheck.IsChecked = false;
        }

        UpdateModeBadge();
    }

    private void UpdateModeBadge()
    {
        ModeBadge.Text = _chrome.Mode == UiDisplayMode.Overlay ? "оверлей" : "окно";
    }

    private void OpacitySlider_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (!IsLoaded)
        {
            return;
        }

        Opacity = e.NewValue;
        OpacityLabel.Text = $"{e.NewValue * 100:0}%";
    }

    private void ClickThrough_OnChanged(object sender, RoutedEventArgs e)
    {
        if (!IsLoaded)
        {
            return;
        }

        _chrome.ClickThrough = ClickThroughCheck.IsChecked == true;
    }

    private void ToggleVisibility_OnClick(object sender, RoutedEventArgs e) => ToggleContentVisibility();

    private void ToggleContentVisibility()
    {
        _contentVisible = !_contentVisible;
        if (_contentVisible)
        {
            Show();
            Activate();
            ToggleVisibilityButton.Content = "Скрыть";
        }
        else
        {
            Hide();
            ToggleVisibilityButton.Content = "Показать";
        }
    }

    private async void RestartCapture_OnClick(object sender, RoutedEventArgs e)
    {
        await StartCaptureAsync().ConfigureAwait(true);
    }

    private void RunFixtureRecognition_OnClick(object sender, RoutedEventArgs e)
    {
        try
        {
            if (_recognizer is null)
            {
                TryLoadRecognizer();
                PopulateOverrideCombos();
            }

            if (_recognizer is null)
            {
                StatusSourceText.Text = "Источник: распознаватель не загружен.";
                return;
            }

            var fixture = Path.Combine(AppContext.BaseDirectory, "assets", "fixtures", "match_hud_1920x1080.png");
            if (!File.Exists(fixture))
            {
                StatusSourceText.Text = "Источник: нет фикстуры assets/fixtures. Пересоберите проект.";
                return;
            }

            var result = _recognizer.RecognizeFile(fixture);
            ApplyRecognition(result, "синтетическая фикстура (не живая Dota)");
        }
        catch (Exception ex)
        {
            StatusSourceText.Text = $"Источник: ошибка фикстуры — {ex.Message}";
        }
    }

    private void HeroOverride_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_suppressOverrideEvents || !IsLoaded)
        {
            return;
        }

        if (HeroOverrideCombo.SelectedItem is HeroPickItem pick)
        {
            if (pick.Id is null)
            {
                _overrides.ClearHero();
            }
            else
            {
                _overrides.HeroId = pick.Id;
                _overrides.HeroDisplayNameRu = pick.DisplayNameRu;
            }
        }

        RefreshStatusPanel();
    }

    private void SideOverride_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_suppressOverrideEvents || !IsLoaded)
        {
            return;
        }

        if (SideOverrideCombo.SelectedItem is SidePickItem pick)
        {
            _overrides.Side = pick.Side;
        }

        RefreshStatusPanel();
    }

    private void ClearOverrides_OnClick(object sender, RoutedEventArgs e)
    {
        _overrides.ClearAll();
        _suppressOverrideEvents = true;
        try
        {
            HeroOverrideCombo.SelectedIndex = 0;
            SideOverrideCombo.SelectedIndex = 0;
        }
        finally
        {
            _suppressOverrideEvents = false;
        }

        RefreshStatusPanel();
    }

    private void ApplyRecognition(RecognitionResult result, string sourceRu)
    {
        _lastResult = result;
        _lastSourceRu = sourceRu;
        RefreshStatusPanel();
    }

    private void RefreshStatusPanel()
    {
        var identity = PlayerIdentityResolver.Resolve(_lastResult, _overrides);

        StatusHeroText.Text = RecognitionStatusFormatter.FormatEffectiveHero(identity);
        StatusSideText.Text = RecognitionStatusFormatter.FormatSide(identity);

        if (_lastResult is not null)
        {
            StatusAlliesText.Text = RecognitionStatusFormatter.FormatAllies(_lastResult.Allies);
            StatusEnemiesText.Text = RecognitionStatusFormatter.FormatEnemies(_lastResult.Enemies);
            StatusItemsText.Text = RecognitionStatusFormatter.FormatItems(_lastResult.InventoryItems);
            StatusConfidenceText.Text = RecognitionStatusFormatter.FormatConfidence(_lastResult, identity);
            StatusSourceText.Text =
                $"Источник: {_lastSourceRu} · layout {_lastResult.LayoutName} · {_lastResult.FrameWidth}×{_lastResult.FrameHeight}";

            var confident = identity.HasUsableHero;
            StatusConfidenceText.Foreground = confident
                ? (Brush)FindResource("AccentBrush")
                : (Brush)FindResource("WarnBrush");
        }
        else
        {
            StatusAlliesText.Text = "Союзники: —";
            StatusEnemiesText.Text = "Противники: —";
            StatusItemsText.Text = "Предметы: —";
            StatusConfidenceText.Text = identity.HeroIsManual
                ? "Уверенность: герой задан вручную (распознавания ещё не было)."
                : "Уверенность: —";
            StatusConfidenceText.Foreground = identity.HeroIsManual
                ? (Brush)FindResource("AccentBrush")
                : (Brush)FindResource("WarnBrush");
            if (_recognizer is not null)
            {
                StatusSourceText.Text = "Источник: ожидание кадра или теста на фикстуре.";
            }
        }

        ApplyTipsFromIdentity(identity);
    }

    private void ApplyTipsFromIdentity(EffectivePlayerIdentity identity)
    {
        if (_tipEngine is null)
        {
            TryLoadTipEngine();
        }

        if (_tipEngine is null)
        {
            StatusTipText.Text = "Совет: движок mid-советов не загружен.";
            TipsContextText.Text = "Контекст: движок советов не загружен.";
            TipsItemsText.Text = "—";
            return;
        }

        if (!identity.HasUsableHero || string.IsNullOrWhiteSpace(identity.HeroId))
        {
            var guess = identity.HeroId is null
                ? "герой не распознан"
                : $"не уверен ({identity.HeroDisplayNameRu} {identity.HeroConfidence:P0})";
            StatusTipText.Text = $"Совет: нет — {guess}. При низкой уверенности советы не даём (или задайте героя вручную).";
            TipsContextText.Text = $"Контекст: {guess}.";
            TipsItemsText.Text = "—";
            TipsStatusText.Text = "Нужен уверенный герой (авто) или ручная подстановка. Смотрите «Ваш герой».";
            return;
        }

        var owned = _lastResult?.InventoryItems
            .Where(i => i.IsConfident)
            .Select(i => i.Id)
            .ToArray() ?? Array.Empty<string>();

        // Prefer a confident enemy as mid opponent when available; else demo default.
        var opponent = _lastResult?.Enemies.FirstOrDefault(e => e.IsConfident)?.Id;

        var sideId = identity.Side switch
        {
            PlayerSide.Radiant => "radiant",
            PlayerSide.Dire => "dire",
            _ => null
        };

        var context = _tipEngine.EnrichWithDemoDefaults(new TipContext
        {
            HeroId = identity.HeroId,
            OpponentId = opponent,
            OwnedItemIds = owned,
            SideId = sideId
        });
        var advice = _tipEngine.Advise(context);

        StatusTipText.Text = advice.Matchup?.PhraseRu
            ?? advice.MatchupStatusRu
            ?? "Матчап: —";

        var modeNote = identity.HeroIsManual ? "вручную" : "авто";
        var sideNote = identity.Side is null
            ? "сторона не задана"
            : $"{RecognitionStatusFormatter.FormatSideName(identity.Side.Value)} ({identity.SideModeRu})";

        TipsContextText.Text = (advice.DemoContextRu
            ?? $"Контекст: {_tipEngine.Store.HeroNameRu(identity.HeroId)}, уровень {context.Level}.")
            + $" · герой {modeNote} · {sideNote}";

        if (advice.NextItems.Count == 0)
        {
            TipsItemsText.Text = advice.BuildStatusRu ?? "—";
        }
        else
        {
            var sb = new StringBuilder();
            for (var i = 0; i < advice.NextItems.Count; i++)
            {
                if (i > 0)
                {
                    sb.AppendLine();
                }

                sb.Append($"{i + 1}. {advice.NextItems[i].PhraseRu}");
            }

            TipsItemsText.Text = sb.ToString();
        }

        TipsStatusText.Text = identity.HeroIsManual
            ? "Локальные JSON-правила mid · герой задан вручную · без LLM."
            : "Локальные JSON-правила mid · шаблонные фразы · без LLM.";
    }

    private void TitleBar_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
        {
            DragMove();
        }
    }

    private void Minimize_OnClick(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

    private void Close_OnClick(object sender, RoutedEventArgs e) => Close();

    private sealed record HeroPickItem(string? Id, string DisplayNameRu)
    {
        public override string ToString() => DisplayNameRu;
    }

    private sealed record SidePickItem(PlayerSide? Side, string DisplayNameRu)
    {
        public override string ToString() => DisplayNameRu;
    }
}
