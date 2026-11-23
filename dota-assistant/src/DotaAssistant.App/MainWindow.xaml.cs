using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using DotaAssistant.App.Services;
using DotaAssistant.Capture;
using DotaAssistant.Vision.Catalog;
using DotaAssistant.Vision.Recognition;

namespace DotaAssistant.App;

public partial class MainWindow : Window
{
    private readonly WindowChromeService _chrome;
    private HotkeyService? _hotkey;
    private IScreenCapture? _capture;
    private readonly DispatcherTimer _statsTimer;
    private bool _contentVisible = true;

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

        await StartCaptureAsync().ConfigureAwait(true);
        _statsTimer.Start();
    }

    private async void OnClosed(object? sender, EventArgs e)
    {
        _statsTimer.Stop();
        _hotkey?.Dispose();
        if (_capture is not null)
        {
            await _capture.DisposeAsync().ConfigureAwait(true);
            _capture = null;
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
            var baseDir = AppContext.BaseDirectory;
            var templates = Path.Combine(baseDir, "assets", "templates");
            var fixture = Path.Combine(baseDir, "assets", "fixtures", "match_hud_1920x1080.png");
            if (!Directory.Exists(templates) || !File.Exists(fixture))
            {
                RecognitionStatusText.Text = "Не найдены assets/templates или фикстура. Пересоберите проект.";
                return;
            }

            var catalog = TemplateCatalog.LoadFromDirectory(templates);
            var result = new FrameRecognizer(catalog, maxInventorySlots: 2).RecognizeFile(fixture);

            RecognitionHeroText.Text = result.Hero is null
                ? "Герой: не уверен"
                : $"Герой: {result.Hero.DisplayNameRu} ({result.Hero.Confidence:P0})";

            if (result.InventoryItems.Count == 0)
            {
                RecognitionItemsText.Text = "Предметы: —";
            }
            else
            {
                var parts = result.InventoryItems.Select(i =>
                    $"{i.DisplayNameRu} ({i.Confidence:P0})");
                RecognitionItemsText.Text = "Предметы: " + string.Join(", ", parts);
            }

            RecognitionStatusText.Text = "Ок: тест на синтетической фикстуре (не кадр живой Dota).";
        }
        catch (Exception ex)
        {
            RecognitionStatusText.Text = $"Ошибка распознавания: {ex.Message}";
        }
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
