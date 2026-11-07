using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using DissolvingPanels;

namespace DissolvingPanelDemo;

/// <summary>The whole sample app: a toolbar, one dissolving panel, and two pages.</summary>
internal sealed class DemoForm : Form
{
    private readonly DemoCard _card = new DemoCard(compact: false);
    private readonly ComboBox _effects = new ComboBox
    {
        DropDownStyle = ComboBoxStyle.DropDownList,
        FlatStyle = FlatStyle.Flat,
        DrawMode = DrawMode.OwnerDrawFixed,
        DropDownWidth = 190,
        Width = 152,
        Margin = new Padding(0, 3, 0, 0),
    };

    private readonly NumericUpDown _duration = new NumericUpDown
    {
        Minimum = 100,
        Maximum = 3000,
        Increment = 50,
        Value = 420,
        Width = 62,
        TextAlign = HorizontalAlignment.Right,
        BorderStyle = BorderStyle.FixedSingle,
        Margin = new Padding(0, 3, 0, 0),
    };

    private readonly ComboBox _slide = new ComboBox
    {
        DropDownStyle = ComboBoxStyle.DropDownList,
        FlatStyle = FlatStyle.Flat,
        Width = 118,
        Margin = new Padding(0, 3, 0, 0),
    };

    private readonly CheckBox _fade = new CheckBox
    {
        Text = "Fade with effect",
        AutoSize = true,
        Margin = new Padding(12, 6, 0, 0),
    };

    private readonly Button _play = new Button
    {
        Text = "Dissolve",
        Width = 84,
        Height = 27,
        Margin = new Padding(14, 3, 0, 0),
    };

    private readonly Button _nextPage = new Button
    {
        Text = "Next page",
        Width = 92,
        Height = 27,
        Margin = new Padding(8, 3, 0, 0),
    };

    private int _page;
    private DissolveEffect _currentEffect;
    private Font _groupFont;

    public DemoForm()
    {
        Text = "DissolvingPanel demo";
        Font = new Font("Segoe UI", 9F);
        ClientSize = new Size(880, 540);
        MinimumSize = new Size(700, 420);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.White;

        foreach (var group in DissolveEffectCatalog.Groups)
        {
            _effects.Items.Add(group);
            foreach (var effect in group.Effects)
            {
                _effects.Items.Add(effect);
            }
        }

        _effects.DrawItem += DrawEffectItem;
        _currentEffect = _card.Panel.Effect;
        _effects.SelectedItem = _currentEffect;

        _slide.Items.AddRange(Enum.GetValues<SlideDirection>().Cast<object>().ToArray());
        _slide.SelectedItem = SlideDirection.None;

        _duration.Value = _card.Panel.Duration;

        var bar = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 50,
            Padding = new Padding(14, 11, 14, 8),
            BackColor = Color.White,
            WrapContents = false,
            FlowDirection = FlowDirection.LeftToRight,
        };

        bar.Controls.Add(Caption("Effect"));
        bar.Controls.Add(_effects);
        bar.Controls.Add(Caption("ms"));
        bar.Controls.Add(_duration);
        bar.Controls.Add(Caption("Slide"));
        bar.Controls.Add(_slide);
        bar.Controls.Add(_fade);
        bar.Controls.Add(_play);
        bar.Controls.Add(_nextPage);

        Controls.Add(_card.Panel);
        Controls.Add(bar);

        _effects.SelectedIndexChanged += (_, _) =>
        {
            if (_effects.SelectedItem is not DissolveEffect effect)
            {
                // Category headings are labels, not choices.
                _effects.SelectedItem = _currentEffect;
                return;
            }

            if (effect == _currentEffect)
            {
                return;
            }

            _currentEffect = effect;
            _card.Panel.Effect = effect;
            Text = "DissolvingPanel demo - " + DissolveEffectCatalog.DisplayName(effect);
            Play();
        };
        _slide.SelectedIndexChanged += (_, _) =>
        {
            _card.Panel.SlideFrom = (SlideDirection)_slide.SelectedItem;
            Play();
        };
        _fade.CheckedChanged += (_, _) => _card.Panel.FadeWithEffect = _fade.Checked;
        _duration.ValueChanged += (_, _) => _card.Panel.Duration = (int)_duration.Value;
        _play.Click += (_, _) => _card.Panel.Toggle();
        _nextPage.Click += (_, _) => ShowPage(_page + 1);
    }

    private static Label Caption(string text) => new Label
    {
        Text = text,
        AutoSize = true,
        ForeColor = Color.FromArgb(104, 112, 124),
        Margin = new Padding(10, 7, 6, 0),
    };

    private void DrawEffectItem(object sender, DrawItemEventArgs e)
    {
        if (e.Index < 0)
        {
            return;
        }

        var item = _effects.Items[e.Index];
        var isGroup = item is EffectGroup;

        // The closed combo reuses this handler for its edit portion, where a
        // selection highlight would look wrong - paint it like a plain field.
        var isEdit = (e.State & DrawItemState.ComboBoxEdit) == DrawItemState.ComboBoxEdit;
        if (isEdit)
        {
            e.Graphics.FillRectangle(SystemBrushes.Window, e.Bounds);
        }
        else
        {
            e.DrawBackground();
        }

        var bounds = isGroup
            ? e.Bounds
            : new Rectangle(e.Bounds.X + 10, e.Bounds.Y, e.Bounds.Width - 10, e.Bounds.Height);
        var colour = !isEdit && (e.State & DrawItemState.Selected) == DrawItemState.Selected
            ? SystemColors.HighlightText
            : isGroup ? SystemColors.GrayText : SystemColors.ControlText;

        _groupFont ??= new Font(_effects.Font, FontStyle.Bold);
        TextRenderer.DrawText(
            e.Graphics,
            isGroup ? ((EffectGroup)item).Name : DissolveEffectCatalog.DisplayName((DissolveEffect)item),
            isGroup ? _groupFont : _effects.Font,
            bounds,
            colour,
            TextFormatFlags.VerticalCenter);
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _groupFont?.Dispose();
        base.OnFormClosed(e);
    }

    private void Play()
    {
        if (_card.Panel.Visible && _card.Panel.Width > 0)
        {
            _card.Panel.PlayIn();
        }
    }

    private void ShowPage(int page)
    {
        _page = page;
        _card.SetPage(_page);
        Play();
    }
}
