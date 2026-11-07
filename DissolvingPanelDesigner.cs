using System;
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
    private DesignerVerbCollection _verbs;

    public override DesignerActionListCollection ActionLists
    {
        get
        {
            var actions = new DesignerActionListCollection();
            actions.Add(new DissolvingPanelActionList(Component));
            return actions;
        }
    }

    /// <summary>Right-click verb that plays the transition on the design surface.</summary>
    public override DesignerVerbCollection Verbs
    {
        get
        {
            if (_verbs is null)
            {
                _verbs = new DesignerVerbCollection
                {
                    new DesignerVerb("Play dissolve preview", OnPlayDissolvePreview),
                };
            }

            return _verbs;
        }
    }

    private void OnPlayDissolvePreview(object sender, EventArgs e)
    {
        if (Component is DissolvingPanel panel)
        {
            panel.PlayDesignPreview();
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

    public EasingMode Easing
    {
        get => _panel.Easing;
        set => SetValue(nameof(DissolvingPanel.Easing), value);
    }

    public bool FadeWithEffect
    {
        get => _panel.FadeWithEffect;
        set => SetValue(nameof(DissolvingPanel.FadeWithEffect), value);
    }

    public SlideDirection SlideFrom
    {
        get => _panel.SlideFrom;
        set => SetValue(nameof(DissolvingPanel.SlideFrom), value);
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
            nameof(Easing),
            "Easing",
            "Dissolve",
            "Speed curve applied to the transition timeline."),
        new DesignerActionPropertyItem(
            nameof(SlideFrom),
            "Slide in",
            "Dissolve",
            "Slide the whole contents in from this edge while the effect plays."),
        new DesignerActionPropertyItem(
            nameof(FadeWithEffect),
            "Fade with effect",
            "Dissolve",
            "Fade the contents while the effect runs."),
        new DesignerActionMethodItem(
            this,
            nameof(PlayPreview),
            "Play preview",
            "Dissolve",
            "Runs the dissolve out and back on the design surface.",
            false),
        new DesignerActionMethodItem(
            this,
            nameof(ResetToDefaults),
            "Reset to defaults",
            "Dissolve",
            "Restore Dissolve, 400 ms, ease in-out, no slide and no extra fade.",
            true),
    };

    /// <summary>Plays the transition once on the design surface.</summary>
    public void PlayPreview() => _panel.PlayDesignPreview();

    /// <summary>Restores the effect defaults.</summary>
    public void ResetToDefaults()
    {
        Effect = DissolveEffect.Dissolve;
        Duration = 400;
        Easing = EasingMode.EaseInOut;
        SlideFrom = SlideDirection.None;
        FadeWithEffect = false;
    }

    private void SetValue(string propertyName, object value)
    {
        var descriptor = TypeDescriptor.GetProperties(_panel)[propertyName];
        descriptor?.SetValue(_panel, value);
    }
}
