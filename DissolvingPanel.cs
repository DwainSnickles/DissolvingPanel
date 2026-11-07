using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Design;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Windows.Forms;

namespace DissolvingPanels;

/// <summary>Transitions the panel can use, modelled on PowerPoint slide effects.</summary>
[TypeConverter(typeof(DissolveEffectConverter))]
[Editor(typeof(DissolveEffectEditor), typeof(UITypeEditor))]
public enum DissolveEffect
{
    Fade,
    Dissolve,
    WipeLeft,
    WipeRight,
    WipeUp,
    WipeDown,
    PushLeft,
    PushRight,
    PushUp,
    PushDown,
    SplitHorizontal,
    SplitVertical,
    BlindsHorizontal,
    BlindsVertical,
    Checkerboard,
    RandomBars,
    Circle,
    Diamond,
    Box,
    Wheel,
    Zoom,
}

/// <summary>Edge the contents slide in from while an effect plays.</summary>
public enum SlideDirection
{
    None,
    FromLeft,
    FromRight,
    FromTop,
    FromBottom,
}

/// <summary>Speed curve applied to the transition timeline.</summary>
public enum EasingMode
{
    Linear,
    EaseIn,
    EaseOut,
    EaseInOut,
}

/// <summary>
/// A panel whose contents leave and return with a PowerPoint style transition.
/// Child controls are hidden while the panel is dissolved so they cannot be
/// clicked, focused or tabbed to.
/// </summary>
[ToolboxItem(true)]
[Designer(typeof(DissolvingPanelDesigner))]
[DefaultProperty(nameof(Effect))]
[Description("Panel whose contents animate away (dissolve) and back with a PowerPoint style effect.")]
public class DissolvingPanel : Panel
{
    private const int DissolveColumns = 28;
    private const int DissolveRows = 18;

    private readonly Timer _ticker;
    private readonly Random _random = new Random();
    private Bitmap _snapshot;
    private Control[] _hiddenChildren;
    private float[] _thresholds;
    private int _cellColumns;
    private int _cellRows;
    private double _progress = 1d;
    private double _from = 1d;
    private double _to = 1d;
    private int _elapsed;
    private readonly Stopwatch _clock = new Stopwatch();
    private bool _designTimePreview;

    public DissolvingPanel()
    {
        SetStyle(
            ControlStyles.AllPaintingInWmPaint
            | ControlStyles.OptimizedDoubleBuffer
            | ControlStyles.UserPaint
            | ControlStyles.ResizeRedraw,
            true);

        BackColor = Color.White;
        _ticker = new Timer { Interval = 15 };
        _ticker.Tick += OnTick;
    }

    [DefaultValue(typeof(Color), "White")]
    public override Color BackColor
    {
        get => base.BackColor;
        set => base.BackColor = value;
    }

    [Category("Dissolve")]
    [DefaultValue(DissolveEffect.Dissolve)]
    [Description("Transition used when the panel dissolves away and comes back.")]
    public DissolveEffect Effect { get; set; } = DissolveEffect.Dissolve;

    [Category("Dissolve")]
    [DefaultValue(400)]
    [Description("Length of one transition in milliseconds.")]
    public int Duration { get; set; } = 400;

    [Category("Dissolve")]
    [DefaultValue(false)]
    [Description("Fade the contents as they move, in addition to the effect.")]
    public bool FadeWithEffect { get; set; }

    [Category("Dissolve")]
    [DefaultValue(SlideDirection.None)]
    [Description("Slides the whole contents in from this edge while the effect plays.")]
    public SlideDirection SlideFrom { get; set; }

    [Category("Dissolve")]
    [DefaultValue(0)]
    [Description("Pixels the contents travel while sliding in. 0 travels the full panel, which reads as a sweep rather than a nudge.")]
    public int SlideDistance { get; set; }

    [Category("Dissolve")]
    [DefaultValue(EasingMode.EaseInOut)]
    [Description("Speed curve applied to the transition timeline.")]
    public EasingMode Easing { get; set; } = EasingMode.EaseInOut;

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool IsDissolved { get; private set; }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool IsAnimating => _ticker.Enabled;

    [Category("Dissolve")]
    [Description("Raised when a transition finishes.")]
    public event EventHandler DissolveCompleted;

    public void DissolveOut() => AnimateTo(0d);

    public void DissolveIn() => AnimateTo(1d);

    public void Toggle()
    {
        if (IsDissolved || (IsAnimating && _to < 0.5d))
        {
            DissolveIn();
        }
        else
        {
            DissolveOut();
        }
    }

    public void ResetDissolve()
    {
        AbandonTransition();
        Invalidate();
    }

    /// <summary>
    /// Drops whatever the transition was doing and always puts the children back.
    /// Every abort path goes through here, so a half-played transition can never
    /// leave the panel's contents hidden - which matters most at design time,
    /// where a hidden child would be written back into the designer file.
    /// </summary>
    private void AbandonTransition()
    {
        StopTicker();
        ReleaseSnapshot();
        RestoreChildren();
        _progress = 1d;
        _from = 1d;
        _to = 1d;
        IsDissolved = false;
        _designTimePreview = false;
    }

    /// <summary>
    /// Brings the contents in with the current effect, starting from fully dissolved.
    /// Used when a tab becomes visible.
    /// </summary>
    public void PlayIn()
    {
        if (IsDisposed || (DesignMode && !_designTimePreview) || !IsHandleCreated || !Visible || Width <= 0 || Height <= 0)
        {
            return;
        }

        ResetDissolve();
        CaptureSnapshot();
        HideChildren();
        PrepareEffect();

        _progress = 0d;
        _from = 0d;
        _to = 1d;
        _elapsed = 0;
        _clock.Restart();
        IsDissolved = false;
        _ticker.Start();
    }

    /// <summary>
    /// Renders one frame of the current effect at a fixed progress (0 = gone, 1 = fully visible)
    /// without animating. Useful for previews and tooling; call <see cref="ResetDissolve"/> afterwards.
    /// </summary>
    public Bitmap RenderFrame(float progress)
    {
        if (Width <= 0 || Height <= 0)
        {
            return null;
        }

        if (_snapshot is null)
        {
            CaptureSnapshot();
            HideChildren();
        }

        PrepareEffect();
        _progress = Math.Max(0f, Math.Min(1f, progress));
        var bitmap = new Bitmap(Width, Height);
        DrawToBitmap(bitmap, new Rectangle(0, 0, Width, Height));
        return bitmap;
    }

    private void AnimateTo(double target)
    {
        if (IsDisposed || (DesignMode && !_designTimePreview))
        {
            return;
        }

        StopTicker();

        if (Math.Abs(_progress - target) < 0.001d && target > 0.5d)
        {
            return;
        }

        CaptureSnapshot();
        HideChildren();
        PrepareEffect();

        _from = _progress;
        _to = target;
        _elapsed = 0;
        _clock.Restart();
        IsDissolved = false;
        _ticker.Start();
    }

    private void PrepareEffect()
    {
        _thresholds = null;
        _cellColumns = 0;
        _cellRows = 0;

        switch (Effect)
        {
            case DissolveEffect.Dissolve:
                BuildRandomThresholds(DissolveColumns, DissolveRows);
                break;

            case DissolveEffect.Checkerboard:
                BuildCheckerThresholds(8, 6);
                break;

            case DissolveEffect.RandomBars:
                BuildRandomThresholds(1, 12);
                break;
        }
    }

    private void BuildRandomThresholds(int columns, int rows)
    {
        _cellColumns = columns;
        _cellRows = rows;
        _thresholds = new float[columns * rows];

        for (var i = 0; i < _thresholds.Length; i++)
        {
            _thresholds[i] = (float)_random.NextDouble();
        }
    }

    private void BuildCheckerThresholds(int columns, int rows)
    {
        _cellColumns = columns;
        _cellRows = rows;
        _thresholds = new float[columns * rows];

        for (var row = 0; row < rows; row++)
        {
            for (var column = 0; column < columns; column++)
            {
                _thresholds[(row * columns) + column] = (row + column) % 2 == 0 ? 0f : 0.5f;
            }
        }
    }

    private void OnTick(object sender, EventArgs e)
    {
        _elapsed = (int)_clock.ElapsedMilliseconds;
        var duration = Math.Max(1, Duration);
        var t = Math.Min(1d, _elapsed / (double)duration);
        var eased = ApplyEasing(Easing, t);

        _progress = _from + ((_to - _from) * eased);
        Invalidate();

        if (t >= 1d)
        {
            Complete();
        }
    }

    private static double ApplyEasing(EasingMode mode, double t)
    {
        t = Math.Max(0d, Math.Min(1d, t));

        return mode switch
        {
            EasingMode.Linear => t,
            EasingMode.EaseIn => t * t,
            EasingMode.EaseOut => 1d - ((1d - t) * (1d - t)),
            _ => t < 0.5d ? 2d * t * t : 1d - (Math.Pow((-2d * t) + 2d, 2d) / 2d),
        };
    }

    private void Complete()
    {
        StopTicker();
        _progress = _to;
        IsDissolved = _to < 0.5d;

        if (IsDissolved)
        {
            Invalidate();
        }
        else
        {
            ReleaseSnapshot();
            RestoreChildren();
        }

        var returnToRest = _designTimePreview && IsDissolved;
        if (_designTimePreview && !IsDissolved)
        {
            _designTimePreview = false;
        }

        DissolveCompleted?.Invoke(this, EventArgs.Empty);

        if (returnToRest)
        {
            AnimateTo(1d);
        }
    }

    /// <summary>
    /// Plays the dissolve out and back once so the effect can be judged on the design
    /// surface, without starting the application. Used by the designer verb and smart tag;
    /// the panel is left exactly as it was, with its children restored.
    /// </summary>
    public void PlayDesignPreview()
    {
        if (IsDisposed || IsAnimating || Width <= 0 || Height <= 0)
        {
            return;
        }

        if (!IsHandleCreated)
        {
            // The design surface may not have realised the window yet.
            _ = Handle;
        }

        _designTimePreview = true;

        try
        {
            ResetDissolve();
            _designTimePreview = true;
            AnimateTo(0d);
        }
        catch
        {
            _designTimePreview = false;
            ResetDissolve();
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        if (_snapshot is null || _progress <= 0.001d)
        {
            return;
        }

        var g = e.Graphics;
        var width = (float)_snapshot.Width;
        var height = (float)_snapshot.Height;
        var p = (float)_progress;
        var area = new RectangleF(0f, 0f, width, height);
        var (slideX, slideY) = GetSlideOffset(p, width, height);

        if (slideX != 0f || slideY != 0f)
        {
            g.TranslateTransform(slideX, slideY);
        }

        switch (Effect)
        {
            case DissolveEffect.Fade:
                DrawImage(g, area, p, forceFade: true);
                break;

            case DissolveEffect.Dissolve:
            case DissolveEffect.Checkerboard:
            case DissolveEffect.RandomBars:
                DrawCells(g, area, p);
                break;

            case DissolveEffect.WipeLeft:
                Clip(g, new RectangleF(width * (1f - p), 0f, width * p, height));
                DrawImage(g, area, p);
                break;

            case DissolveEffect.WipeRight:
                Clip(g, new RectangleF(0f, 0f, width * p, height));
                DrawImage(g, area, p);
                break;

            case DissolveEffect.WipeUp:
                Clip(g, new RectangleF(0f, height * (1f - p), width, height * p));
                DrawImage(g, area, p);
                break;

            case DissolveEffect.WipeDown:
                Clip(g, new RectangleF(0f, 0f, width, height * p));
                DrawImage(g, area, p);
                break;

            case DissolveEffect.PushLeft:
                DrawImage(g, Offset(area, -(1f - p) * width, 0f), p);
                break;

            case DissolveEffect.PushRight:
                DrawImage(g, Offset(area, (1f - p) * width, 0f), p);
                break;

            case DissolveEffect.PushUp:
                DrawImage(g, Offset(area, 0f, -(1f - p) * height), p);
                break;

            case DissolveEffect.PushDown:
                DrawImage(g, Offset(area, 0f, (1f - p) * height), p);
                break;

            case DissolveEffect.SplitHorizontal:
            {
                var slice = (height / 2f) * p;
                Clip(g, new RectangleF(0f, 0f, width, slice));
                DrawImage(g, area, p);
                Clip(g, new RectangleF(0f, height - slice, width, slice));
                DrawImage(g, area, p);
                break;
            }

            case DissolveEffect.SplitVertical:
            {
                var slice = (width / 2f) * p;
                Clip(g, new RectangleF(0f, 0f, slice, height));
                DrawImage(g, area, p);
                Clip(g, new RectangleF(width - slice, 0f, slice, height));
                DrawImage(g, area, p);
                break;
            }

            case DissolveEffect.BlindsHorizontal:
            {
                var strip = height / 10f;
                var region = new Region();
                region.MakeEmpty();
                for (var i = 0; i < 10; i++)
                {
                    region.Union(new RectangleF(0f, i * strip, width, strip * p));
                }

                g.SetClip(region, CombineMode.Replace);
                DrawImage(g, area, p);
                region.Dispose();
                break;
            }

            case DissolveEffect.BlindsVertical:
            {
                var strip = width / 10f;
                var region = new Region();
                region.MakeEmpty();
                for (var i = 0; i < 10; i++)
                {
                    region.Union(new RectangleF(i * strip, 0f, strip * p, height));
                }

                g.SetClip(region, CombineMode.Replace);
                DrawImage(g, area, p);
                region.Dispose();
                break;
            }

            case DissolveEffect.Circle:
            {
                var radius = (float)(Math.Sqrt((width * width) + (height * height)) / 2d) * p;
                using var path = new GraphicsPath();
                path.AddEllipse((width / 2f) - radius, (height / 2f) - radius, radius * 2f, radius * 2f);
                g.SetClip(path);
                DrawImage(g, area, p);
                break;
            }

            case DissolveEffect.Diamond:
            {
                var radius = (float)(Math.Sqrt((width * width) + (height * height)) / 2d) * p;
                var cx = width / 2f;
                var cy = height / 2f;
                using var path = new GraphicsPath();
                path.AddPolygon(new[]
                {
                    new PointF(cx, cy - radius),
                    new PointF(cx + radius, cy),
                    new PointF(cx, cy + radius),
                    new PointF(cx - radius, cy),
                });
                g.SetClip(path);
                DrawImage(g, area, p);
                break;
            }

            case DissolveEffect.Box:
            {
                var boxWidth = width * p;
                var boxHeight = height * p;
                Clip(g, new RectangleF((width - boxWidth) / 2f, (height - boxHeight) / 2f, boxWidth, boxHeight));
                DrawImage(g, area, p);
                break;
            }

            case DissolveEffect.Wheel:
            {
                using var path = new GraphicsPath();
                path.AddPie(0f, 0f, width, height, -90f, 360f * p);
                g.SetClip(path);
                DrawImage(g, area, p);
                break;
            }

            case DissolveEffect.Zoom:
            {
                var zoom = 0.15f + (0.85f * p);
                var zoomed = new RectangleF(
                    (width - (width * zoom)) / 2f,
                    (height - (height * zoom)) / 2f,
                    width * zoom,
                    height * zoom);
                DrawImage(g, zoomed, p);
                break;
            }
        }

        g.ResetClip();

        if (slideX != 0f || slideY != 0f)
        {
            g.TranslateTransform(-slideX, -slideY);
        }
    }

    private (float X, float Y) GetSlideOffset(float p, float width, float height)
    {
        var away = 1f - p;

        // SlideDistance keeps the travel short; 0 keeps the original full-panel sweep.
        var slideX = SlideDistance > 0 ? Math.Min(SlideDistance, width) : width;
        var slideY = SlideDistance > 0 ? Math.Min(SlideDistance, height) : height;

        return SlideFrom switch
        {
            SlideDirection.FromLeft => (-slideX * away, 0f),
            SlideDirection.FromRight => (slideX * away, 0f),
            SlideDirection.FromTop => (0f, -slideY * away),
            SlideDirection.FromBottom => (0f, slideY * away),
            _ => (0f, 0f),
        };
    }

    private void DrawCells(Graphics g, RectangleF area, float p)
    {
        if (_thresholds is null || _cellColumns <= 0 || _cellRows <= 0)
        {
            DrawImage(g, area, p);
            return;
        }

        var cellWidth = area.Width / _cellColumns;
        var cellHeight = area.Height / _cellRows;
        var region = new Region();
        region.MakeEmpty();

        for (var row = 0; row < _cellRows; row++)
        {
            for (var column = 0; column < _cellColumns; column++)
            {
                var threshold = _thresholds[(row * _cellColumns) + column];
                if (p >= threshold)
                {
                    region.Union(new RectangleF(
                        column * cellWidth,
                        row * cellHeight,
                        cellWidth + 0.5f,
                        cellHeight + 0.5f));
                }
            }
        }

        g.SetClip(region, CombineMode.Replace);
        DrawImage(g, area, 1f);
        region.Dispose();
    }

    private void DrawImage(Graphics g, RectangleF destination, float p, bool forceFade = false)
    {
        if (_snapshot is null)
        {
            return;
        }

        var alpha = (FadeWithEffect || forceFade) ? Math.Max(0.02f, p) : 1f;
        if (alpha >= 0.999f)
        {
            g.DrawImage(_snapshot, destination);
            return;
        }

        using var attributes = new ImageAttributes();
        attributes.SetColorMatrix(
            new ColorMatrix { Matrix33 = alpha },
            ColorMatrixFlag.Default,
            ColorAdjustType.Bitmap);
        g.DrawImage(
            _snapshot,
            new[]
            {
                new PointF(destination.Left, destination.Top),
                new PointF(destination.Right, destination.Top),
                new PointF(destination.Left, destination.Bottom),
            },
            new RectangleF(0f, 0f, _snapshot.Width, _snapshot.Height),
            GraphicsUnit.Pixel,
            attributes);
    }

    private static void Clip(Graphics g, RectangleF rectangle) => g.SetClip(rectangle);

    private static RectangleF Offset(RectangleF rectangle, float dx, float dy) =>
        new RectangleF(rectangle.X + dx, rectangle.Y + dy, rectangle.Width, rectangle.Height);

    private void HideChildren()
    {
        // Already hidden by the current transition: keep the record of which children
        // these are so they can still be restored when the panel comes back.
        if (_hiddenChildren is not null)
        {
            return;
        }

        var hidden = new List<Control>();
        foreach (Control child in Controls)
        {
            if (child.Visible)
            {
                child.Visible = false;
                hidden.Add(child);
            }
        }

        _hiddenChildren = hidden.ToArray();
    }

    private void RestoreChildren()
    {
        var hidden = _hiddenChildren;
        _hiddenChildren = null;

        if (hidden is null)
        {
            return;
        }

        foreach (var child in hidden)
        {
            if (!child.IsDisposed)
            {
                child.Visible = true;
            }
        }
    }

    private void CaptureSnapshot()
    {
        // While the children are hidden the snapshot is the only copy of how they look,
        // so keep it instead of capturing an empty panel.
        if (_snapshot is not null && _hiddenChildren is not null)
        {
            return;
        }

        ReleaseSnapshot();

        if (Width <= 0 || Height <= 0)
        {
            return;
        }

        var bitmap = new Bitmap(Width, Height);
        try
        {
            DrawToBitmap(bitmap, new Rectangle(0, 0, Width, Height));
            _snapshot = bitmap;
        }
        catch
        {
            bitmap.Dispose();
        }
    }

    private void ReleaseSnapshot()
    {
        _snapshot?.Dispose();
        _snapshot = null;
    }

    private void StopTicker()

    {
        if (_ticker.Enabled)
        {
            _ticker.Stop();
        }
    }

    protected override void OnPaintBackground(PaintEventArgs e) => e.Graphics.Clear(BackColor);

    protected override void OnVisibleChanged(EventArgs e)
    {
        base.OnVisibleChanged(e);

        if (!Visible)
        {
            AbandonTransition();
        }
    }

    protected override void OnHandleDestroyed(EventArgs e)
    {
        // The design surface recreates handles while editing; never keep a
        // transition (or hidden children) across that.
        AbandonTransition();
        base.OnHandleDestroyed(e);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            AbandonTransition();
            _ticker.Tick -= OnTick;
            _ticker.Dispose();
            ReleaseSnapshot();
        }

        base.Dispose(disposing);
    }
}
