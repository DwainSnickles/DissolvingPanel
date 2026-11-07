using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;
using System.Text;
using System.Windows.Forms;
using DissolvingPanels;

namespace DissolvingPanelDemo;

/// <summary>
/// Renders the README images offscreen: the demo window itself, a contact sheet
/// of effects mid-transition, and a frame-by-frame sequence. Everything comes
/// from <see cref="DissolvingPanel.RenderFrame"/> so the output is deterministic
/// apart from the deliberately randomised effects.
/// </summary>
internal static class ShotRenderer
{
    private const int CellWidth = 320;
    private const int CellHeight = 210;
    private const int Gutter = 14;
    private const int CaptionHeight = 26;

    private static readonly Color SheetBackground = Color.FromArgb(247, 248, 250);
    private static readonly Color CellBorder = Color.FromArgb(224, 227, 233);
    private static readonly Color CaptionInk = Color.FromArgb(72, 80, 92);

    private static readonly DissolveEffect[] SheetEffects =
    {
        DissolveEffect.Dissolve,
        DissolveEffect.WipeRight,
        DissolveEffect.Circle,
        DissolveEffect.Checkerboard,
        DissolveEffect.BlindsVertical,
        DissolveEffect.SplitHorizontal,
        DissolveEffect.Wheel,
        DissolveEffect.Zoom,
        DissolveEffect.PushLeft,
    };

    public static void Save(string directory)
    {
        Directory.CreateDirectory(directory);

        using (var form = new DemoForm())
        {
            form.StartPosition = FormStartPosition.Manual;
            form.Location = new Point(-4000, -4000);
            form.ShowInTaskbar = false;
            form.Show();
            Application.DoEvents();

            // DrawToBitmap paints the frame as well, so capture the whole window
            // bounds or the client area is cropped by the height of the title bar.
            using var window = new Bitmap(form.Width, form.Height);
            form.DrawToBitmap(window, new Rectangle(0, 0, form.Width, form.Height));
            window.Save(Path.Combine(directory, "demo.png"), ImageFormat.Png);
            form.Close();
        }

        using var stage = Stage.Create(CellWidth, CellHeight);
        SaveEffects(stage, Path.Combine(directory, "effects.png"));
        SaveSequence(stage, Path.Combine(directory, "sequence.png"));

        Console.WriteLine("Saved demo.png, effects.png and sequence.png to " + Path.GetFullPath(directory));
    }

    private static void SaveEffects(Stage stage, string path)
    {
        const int columns = 3;
        var rows = (SheetEffects.Length + columns - 1) / columns;
        var width = (columns * CellWidth) + ((columns + 1) * Gutter);
        var height = Gutter + (rows * (CaptionHeight + CellHeight)) + ((rows - 1) * Gutter) + Gutter;

        using var sheet = new Bitmap(width, height);
        using (var graphics = Graphics.FromImage(sheet))
        {
            Prepare(graphics);
            using var font = new Font("Segoe UI", 9F, FontStyle.Bold);
            using var brush = new SolidBrush(CaptionInk);
            using var border = new Pen(CellBorder);

            for (var index = 0; index < SheetEffects.Length; index++)
            {
                var effect = SheetEffects[index];
                var x = Gutter + ((index % columns) * (CellWidth + Gutter));
                var y = Gutter + ((index / columns) * (CaptionHeight + CellHeight + Gutter));

                graphics.DrawString(CaptionFor(effect), font, brush, x, y);

                var cell = new Rectangle(x, y + CaptionHeight - 6, CellWidth, CellHeight);
                using var frame = stage.Frame(effect, 0.45f);
                if (frame is not null)
                {
                    graphics.DrawImage(frame, cell);
                }

                graphics.DrawRectangle(border, cell.X, cell.Y, cell.Width - 1, cell.Height - 1);
            }
        }

        sheet.Save(path, ImageFormat.Png);
    }

    private static void SaveSequence(Stage stage, string path)
    {
        float[] steps = { 0.2f, 0.45f, 0.7f, 0.95f };
        var width = (steps.Length * CellWidth) + ((steps.Length + 1) * Gutter);
        var height = Gutter + CaptionHeight + CellHeight + Gutter;

        using var sheet = new Bitmap(width, height);
        using (var graphics = Graphics.FromImage(sheet))
        {
            Prepare(graphics);
            using var font = new Font("Segoe UI", 9F, FontStyle.Bold);
            using var brush = new SolidBrush(CaptionInk);
            using var border = new Pen(CellBorder);

            for (var index = 0; index < steps.Length; index++)
            {
                var step = steps[index];
                var x = Gutter + (index * (CellWidth + Gutter));

                graphics.DrawString("Circle at " + (int)Math.Round(step * 100) + " %", font, brush, x, Gutter);

                var cell = new Rectangle(x, Gutter + CaptionHeight - 6, CellWidth, CellHeight);
                using var frame = stage.Frame(DissolveEffect.Circle, step);
                if (frame is not null)
                {
                    graphics.DrawImage(frame, cell);
                }

                graphics.DrawRectangle(border, cell.X, cell.Y, cell.Width - 1, cell.Height - 1);
            }
        }

        sheet.Save(path, ImageFormat.Png);
    }

    private static void Prepare(Graphics graphics)
    {
        graphics.Clear(SheetBackground);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
        graphics.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
    }

    private static string CaptionFor(DissolveEffect effect)
    {
        var name = effect.ToString();
        var builder = new StringBuilder(name.Length + 4);

        for (var i = 0; i < name.Length; i++)
        {
            if (i > 0 && char.IsUpper(name[i]) && !char.IsUpper(name[i - 1]))
            {
                builder.Append(' ');
            }

            builder.Append(name[i]);
        }

        return builder.ToString();
    }

    /// <summary>An offscreen window holding a compact demo card we can render frames from.</summary>
    private sealed class Stage : IDisposable
    {
        private readonly Form _host;

        private Stage(Form host, DemoCard card)
        {
            _host = host;
            Card = card;
        }

        public DemoCard Card { get; }

        public DissolvingPanel Panel => Card.Panel;

        public static Stage Create(int width, int height)
        {
            var card = new DemoCard(compact: true);
            var host = new Form
            {
                ClientSize = new Size(width, height),
                FormBorderStyle = FormBorderStyle.None,
                StartPosition = FormStartPosition.Manual,
                Location = new Point(-4000, -4000),
                ShowInTaskbar = false,
                BackColor = Color.White,
            };

            host.Controls.Add(card.Panel);
            host.Show();
            Application.DoEvents();

            return new Stage(host, card);
        }

        public Bitmap Frame(DissolveEffect effect, float progress)
        {
            Panel.ResetDissolve();
            Panel.Effect = effect;
            Panel.SlideFrom = SlideDirection.None;
            Panel.FadeWithEffect = false;
            return Panel.RenderFrame(progress);
        }

        public void Dispose()
        {
            Panel.ResetDissolve();
            _host.Close();
            _host.Dispose();
        }
    }
}
