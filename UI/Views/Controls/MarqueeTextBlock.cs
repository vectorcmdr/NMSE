using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;

namespace NMSE.UI.Views.Controls;

/// <summary>
/// A TextBlock that scrolls its text horizontally when the text is wider than the control.
/// </summary>
public class MarqueeTextBlock : Control
{
    public static readonly StyledProperty<string> TextProperty =
        AvaloniaProperty.Register<MarqueeTextBlock, string>(nameof(Text), "");

    public static readonly StyledProperty<double> FontSizeProperty =
        TextBlock.FontSizeProperty.AddOwner<MarqueeTextBlock>();

    public static readonly StyledProperty<IBrush?> ForegroundProperty =
        TextBlock.ForegroundProperty.AddOwner<MarqueeTextBlock>();

    public static readonly StyledProperty<double> ScrollSpeedProperty =
        AvaloniaProperty.Register<MarqueeTextBlock, double>(nameof(ScrollSpeed), 30.0);

    public static readonly StyledProperty<double> PauseSecondsProperty =
        AvaloniaProperty.Register<MarqueeTextBlock, double>(nameof(PauseSeconds), 1.5);

    public string Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public double FontSize
    {
        get => GetValue(FontSizeProperty);
        set => SetValue(FontSizeProperty, value);
    }

    public IBrush? Foreground
    {
        get => GetValue(ForegroundProperty);
        set => SetValue(ForegroundProperty, value);
    }

    public double ScrollSpeed
    {
        get => GetValue(ScrollSpeedProperty);
        set => SetValue(ScrollSpeedProperty, value);
    }

    public double PauseSeconds
    {
        get => GetValue(PauseSecondsProperty);
        set => SetValue(PauseSecondsProperty, value);
    }

    private FormattedText? _formattedText;
    private double _textWidth;
    private double _offset;
    private bool _scrolling;
    private DispatcherTimer? _timer;
    private double _pauseRemaining;

    static MarqueeTextBlock()
    {
        AffectsRender<MarqueeTextBlock>(TextProperty, FontSizeProperty, ForegroundProperty);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == TextProperty || change.Property == FontSizeProperty || change.Property == ForegroundProperty)
        {
            _formattedText = null;
            _offset = 0;
            _pauseRemaining = PauseSeconds;
            UpdateScrollState();
            InvalidateVisual();
        }
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        UpdateScrollState();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        StopTimer();
    }

    private void EnsureFormattedText()
    {
        if (_formattedText != null) return;

        var typeface = new Typeface(FontFamily.Default);
        _formattedText = new FormattedText(
            Text ?? "",
            System.Globalization.CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            typeface,
            FontSize,
            Foreground ?? Brushes.Black);

        _textWidth = _formattedText.Width;
    }

    private void UpdateScrollState()
    {
        EnsureFormattedText();
        bool needsScroll = _textWidth > Bounds.Width && Bounds.Width > 0;

        if (needsScroll && !_scrolling)
        {
            _scrolling = true;
            StartTimer();
        }
        else if (!needsScroll && _scrolling)
        {
            _scrolling = false;
            _offset = 0;
            StopTimer();
        }
    }

    private void StartTimer()
    {
        if (_timer != null) return;
        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(33) };
        _timer.Tick += OnTick;
        _timer.Start();
    }

    private void StopTimer()
    {
        _timer?.Stop();
        _timer = null;
    }

    private void OnTick(object? sender, EventArgs e)
    {
        if (!_scrolling) return;

        if (_pauseRemaining > 0)
        {
            _pauseRemaining -= 0.033;
            return;
        }

        double step = ScrollSpeed * 0.033;
        _offset += step;

        double maxOffset = _textWidth - Bounds.Width;
        if (_offset >= maxOffset + 20)
        {
            _offset = 0;
            _pauseRemaining = PauseSeconds;
        }

        InvalidateVisual();
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        EnsureFormattedText();
        double height = _formattedText?.Height ?? FontSize * 1.2;
        return new Size(0, height);
    }

    public override void Render(DrawingContext context)
    {
        EnsureFormattedText();
        if (_formattedText == null) return;

        using (context.PushClip(new Rect(Bounds.Size)))
        {
            context.DrawText(_formattedText, new Point(-_offset, 0));
        }
    }

    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);
        _offset = 0;
        _pauseRemaining = PauseSeconds;
        UpdateScrollState();
    }
}
