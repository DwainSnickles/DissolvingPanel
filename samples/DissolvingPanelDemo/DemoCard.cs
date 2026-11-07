using System.Drawing;
using System.Windows.Forms;
using DissolvingPanels;

namespace DissolvingPanelDemo;

/// <summary>
/// Builds the sample content that lives inside the dissolving panel.
/// Nothing here touches layout at animation time, which is the point: the
/// panel snapshots whatever the designer produced and animates that.
/// </summary>
internal sealed class DemoCard
{
    private static readonly Color Surface = Color.FromArgb(246, 247, 250);
    private static readonly Color Ink = Color.FromArgb(18, 22, 28);
    private static readonly Color Muted = Color.FromArgb(104, 112, 124);
    private static readonly Color Accent = Color.FromArgb(31, 111, 235);

    private readonly Label _title;
    private readonly Label _subtitle;
    private readonly Label[] _keys = new Label[3];
    private readonly Label[] _values = new Label[3];
    private readonly Button _action;
    private readonly bool _compact;

    public DemoCard(bool compact)
    {
        _compact = compact;
        var pad = compact ? 12 : 26;
        var rowHeight = compact ? 24 : 30;

        Panel = new DissolvingPanel
        {
            Dock = DockStyle.Fill,
            BackColor = Surface,
            Effect = DissolveEffect.Dissolve,
            Duration = 420,
            Padding = new Padding(pad),
        };

        var card = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            ColumnCount = 1,
            RowCount = 7,
            Padding = new Padding(pad),
        };
        card.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        card.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        card.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        for (var i = 0; i < 3; i++)
        {
            card.RowStyles.Add(new RowStyle(SizeType.Absolute, rowHeight));
        }
        card.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
        card.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        _title = new Label
        {
            Text = "Product details",
            Font = new Font("Segoe UI", compact ? 12f : 17f, FontStyle.Bold),
            ForeColor = Ink,
            AutoSize = true,
            Margin = new Padding(0),
        };

        _subtitle = new Label
        {
            Text = "Controls stay in the designer; the panel animates them.",
            Font = new Font("Segoe UI", compact ? 7.5f : 9f),
            ForeColor = Muted,
            AutoSize = true,
            Margin = new Padding(0, compact ? 2 : 4, 0, compact ? 8 : 16),
        };

        card.Controls.Add(_title, 0, 0);
        card.Controls.Add(_subtitle, 0, 1);

        for (var i = 0; i < 3; i++)
        {
            var row = new TableLayoutPanel
            {
                ColumnCount = 2,
                Dock = DockStyle.Fill,
                Margin = new Padding(0),
            };
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42f));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58f));
            row.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            _keys[i] = new Label
            {
                Font = new Font("Segoe UI", compact ? 7.5f : 9f),
                ForeColor = Muted,
                AutoSize = true,
                Margin = new Padding(0, 3, 0, 0),
            };
            _values[i] = new Label
            {
                Font = new Font("Segoe UI", compact ? 7.5f : 9f),
                ForeColor = Ink,
                AutoSize = true,
                Margin = new Padding(0, 3, 0, 0),
            };

            row.Controls.Add(_keys[i], 0, 0);
            row.Controls.Add(_values[i], 1, 0);
            card.Controls.Add(row, 0, i + 2);
        }

        _action = new Button
        {
            Text = "Continue",
            Width = compact ? 74 : 96,
            Height = compact ? 24 : 30,
            FlatStyle = FlatStyle.Flat,
            BackColor = Accent,
            ForeColor = Color.White,
            Anchor = AnchorStyles.Right,
            Margin = new Padding(0, compact ? 6 : 12, 0, 0),
            TabStop = false,
        };
        _action.FlatAppearance.BorderSize = 0;
        card.Controls.Add(_action, 0, 6);

        Panel.Controls.Add(card);
        SetPage(0);
    }

    public DissolvingPanel Panel { get; }

    /// <summary>Swaps the sample content between two "pages".</summary>
    public void SetPage(int page)
    {
        var files = page % 2 == 1;

        _title.Text = files ? "Files to include" : "Product details";
        _subtitle.Text = files
            ? "Switching pages replays the transition over new content."
            : "Controls stay in the designer; the panel animates them.";

        var keys = files
            ? new[] { "Selected", "Queue", "Output" }
            : new[] { "Name", "Version", "Target" };
        var values = files
            ? new[] { "6 files", "Ready", "setup.exe" }
            : new[] { "Install Studio", "2.4.0", ".NET 8 · WinForms" };

        for (var i = 0; i < 3; i++)
        {
            _keys[i].Text = keys[i];
            _values[i].Text = values[i];
        }

        _action.Text = files ? "Build" : "Continue";
    }

    /// <summary>True when the demo is rendering a contact sheet cell.</summary>
    public bool IsCompact => _compact;
}
