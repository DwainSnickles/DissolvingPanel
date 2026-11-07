using System.ComponentModel;
using System.ComponentModel.Design;
using System.Windows.Forms.Design;

namespace DissolvingPanels;

/// <summary>
/// Design-time support for <see cref="DissolvingPanel"/>: adds a smart tag with the
/// effect settings so they can be picked without leaving the design surface.
/// </summary>
public class DissolvingPanelDesigner : ControlDesigner
{
    public override DesignerActionListCollection ActionLists
    {
        get
        {
            var actions = new DesignerActionListCollection();
            actions.Add(new DissolvingPanelActionList(Component));
            return actions;
        }
    }
}

/// <summary>Smart tag contents: effect, duration, fade and a reset command.</summary>
public class DissolvingPanelActionList : DesignerActionList
{
    private readonly DissolvingPanel _panel;

    public DissolvingPanelActionList(IComponent component)
        : base(component)
    {
        _panel = (DissolvingPanel)component;
    }

    public DissolveEffect Effect
    {
        get => _panel.Effect;
        set => SetValue(nameof(DissolvingPanel.Effect), value);
    }

    public int Duration
    {
        get => _panel.Duration;
        set => SetValue(nameof(DissolvingPanel.Duration), value);
    }

    public bool FadeWithEffect
    {
        get => _panel.FadeWithEffect;
        set => SetValue(nameof(DissolvingPanel.FadeWithEffect), value);
    }

    public override DesignerActionItemCollection GetSortedActionItems() => new DesignerActionItemCollection
    {
        new DesignerActionHeaderItem("Dissolve"),
        new DesignerActionPropertyItem(
            nameof(Effect),
            "Effect",
            "Dissolve",
            "Transition used when the panel dissolves away and comes back."),
        new DesignerActionPropertyItem(
            nameof(Duration),
            "Duration (ms)",
            "Dissolve",
            "Length of one transition in milliseconds."),
        new DesignerActionPropertyItem(
            nameof(FadeWithEffect),
            "Fade with effect",
            "Dissolve",
            "Fade the contents while the effect runs."),
        new DesignerActionMethodItem(
            this,
            nameof(ResetToDefaults),
            "Reset to defaults",
            "Dissolve",
            "Restore Dissolve, 400 ms and no extra fade.",
            true),
    };

    /// <summary>Restores the effect defaults.</summary>
    public void ResetToDefaults()
    {
        Effect = DissolveEffect.Dissolve;
        Duration = 400;
        FadeWithEffect = false;
    }

    private void SetValue(string propertyName, object value)
    {
        var descriptor = TypeDescriptor.GetProperties(_panel)[propertyName];
        descriptor?.SetValue(_panel, value);
    }
}
