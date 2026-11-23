using System.IO;
using System.Text;
using System.Windows;
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
            if (_lastResult is null)
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

    private void ApplyRecognition(RecognitionResult result, string sourceRu)
    {
        _lastResult = result;

        StatusHeroText.Text = RecognitionStatusFormatter.FormatHero(result.Hero);
        StatusAlliesText.Text = RecognitionStatusFormatter.FormatAllies(result.Allies);
        StatusEnemiesText.Text = RecognitionStatusFormatter.FormatEnemies(result.Enemies);
        StatusItemsText.Text = RecognitionStatusFormatter.FormatItems(result.InventoryItems);
        StatusConfidenceText.Text = RecognitionStatusFormatter.FormatConfidence(result);
        StatusSourceText.Text = $"Источник: {sourceRu} · layout {result.LayoutName} · {result.FrameWidth}×{result.FrameHeight}";

        StatusConfidenceText.Foreground = result.HasConfidentHero
            ? (Brush)FindResource("AccentBrush")
            : (Brush)FindResource("WarnBrush");

        ApplyTipsFromRecognition(result);
    }

    private void ApplyTipsFromRecognition(RecognitionResult result)
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

        if (!result.HasConfidentHero || result.Hero is null)
        {
            var guess = result.Hero is null
                ? "герой не распознан"
                : $"не уверен ({result.Hero.DisplayNameRu} {result.Hero.Confidence:P0})";
            StatusTipText.Text = $"Совет: нет — {guess}. При низкой уверенности советы не даём.";
            TipsContextText.Text = $"Контекст: {guess}.";
            TipsItemsText.Text = "—";
            TipsStatusText.Text = "Нужен уверенный герой (порог шаблона). Смотрите строку «Уверенность».";
            return;
        }

        var owned = result.InventoryItems
            .Where(i => i.IsConfident)
            .Select(i => i.Id)
            .ToArray();

        // Prefer a confident enemy as mid opponent when available; else demo default.
        var opponent = result.Enemies.FirstOrDefault(e => e.IsConfident)?.Id;

        var context = _tipEngine.EnrichWithDemoDefaults(new TipContext
        {
            HeroId = result.Hero.Id,
            OpponentId = opponent,
            OwnedItemIds = owned
        });
        var advice = _tipEngine.Advise(context);

        StatusTipText.Text = advice.Matchup?.PhraseRu
            ?? advice.MatchupStatusRu
            ?? "Матчап: —";

        TipsContextText.Text = advice.DemoContextRu
            ?? $"Контекст: {_tipEngine.Store.HeroNameRu(result.Hero.Id)}, уровень {context.Level}.";

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

        TipsStatusText.Text = "Локальные JSON-правила mid · шаблонные фразы · без LLM.";
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
}
