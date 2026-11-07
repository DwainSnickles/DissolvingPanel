using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Design;
using System.Text;
using System.Windows.Forms;
using System.Windows.Forms.Design;

namespace DissolvingPanels;

/// <summary>Categories the effects are grouped under in the designer and the preview form.</summary>
public enum EffectCategory
{
    Fades,
    Wipes,
    Pushes,
    ShapeReveals,
}

/// <summary>One category and the effects listed inside it.</summary>
public sealed class EffectGroup
{
    public EffectGroup(EffectCategory category, string name, params DissolveEffect[] effects)
    {
        Category = category;
        Name = name;
        Effects = effects;
    }

    public EffectCategory Category { get; }

    public string Name { get; }

    public DissolveEffect[] Effects { get; }

    public override string ToString() => Name;
}

/// <summary>
/// The grouped view of <see cref="DissolveEffect"/> shared by the property grid, the smart
/// tag and the preview form, so every list shows the same categories in the same order.
/// </summary>
public static class DissolveEffectCatalog
{
    public static readonly IReadOnlyList<EffectGroup> Groups = new[]
    {
        new EffectGroup(
            EffectCategory.Fades,
            "Fades",
            DissolveEffect.Fade,
            DissolveEffect.Dissolve),
        new EffectGroup(
            EffectCategory.Wipes,
            "Wipes",
            DissolveEffect.WipeLeft,
            DissolveEffect.WipeRight,
            DissolveEffect.WipeUp,
            DissolveEffect.WipeDown,
            DissolveEffect.SplitHorizontal,
            DissolveEffect.SplitVertical,
            DissolveEffect.BlindsHorizontal,
            DissolveEffect.BlindsVertical),
        new EffectGroup(
            EffectCategory.Pushes,
            "Pushes",
            DissolveEffect.PushLeft,
            DissolveEffect.PushRight,
            DissolveEffect.PushUp,
            DissolveEffect.PushDown),
        new EffectGroup(
            EffectCategory.ShapeReveals,
            "Shape reveals",
            DissolveEffect.Circle,
            DissolveEffect.Diamond,
            DissolveEffect.Box,
            DissolveEffect.Wheel,
            DissolveEffect.Zoom,
            DissolveEffect.Checkerboard,
            DissolveEffect.RandomBars),
    };

    private static readonly DissolveEffect[] OrderedEffects = BuildOrdered();

    /// <summary>Every effect in category order; used by flat enum lists such as the smart tag.</summary>
    public static DissolveEffect[] Ordered => OrderedEffects;

    /// <summary>"WipeLeft" becomes "Wipe left" for lists that show friendly names.</summary>
    public static string DisplayName(DissolveEffect effect)
    {
        var name = effect.ToString();
        var builder = new StringBuilder(name.Length + 4);

        for (var i = 0; i < name.Length; i++)
        {
            var character = name[i];
            if (i > 0 && char.IsUpper(character))
            {
                builder.Append(' ');
            }

            builder.Append(i == 0 ? character : char.ToLowerInvariant(character));
        }

        return builder.ToString();
    }

    private static DissolveEffect[] BuildOrdered()
    {
        var ordered = new List<DissolveEffect>();
        foreach (var group in Groups)
        {
            ordered.AddRange(group.Effects);
        }

        return ordered.ToArray();
    }
}

/// <summary>Orders the enum values by category wherever a plain list is built from the type.</summary>
public sealed class DissolveEffectConverter : EnumConverter
{
    public DissolveEffectConverter()
        : base(typeof(DissolveEffect))
    {
    }

    public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context) =>
        new(DissolveEffectCatalog.Ordered);
}

/// <summary>Property grid drop-down that lists the effects under their category headings.</summary>
public sealed class DissolveEffectEditor : UITypeEditor
{
    public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context) =>
        UITypeEditorEditStyle.DropDown;

    public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value)
    {
        if (provider?.GetService(typeof(IWindowsFormsEditorService)) is not IWindowsFormsEditorService service)
        {
            return value;
        }

        using var list = new ListBox
        {
            BorderStyle = BorderStyle.None,
            DrawMode = DrawMode.OwnerDrawFixed,
            IntegralHeight = false,
        };

        using var headerFont = new Font(list.Font, FontStyle.Bold);

        foreach (var group in DissolveEffectCatalog.Groups)
        {
            list.Items.Add(group);
            foreach (var effect in group.Effects)
            {
                list.Items.Add(effect);
            }
        }

        list.DrawItem += (_, e) =>
        {
            if (e.Index < 0)
            {
                return;
            }

            var item = list.Items[e.Index];
            var isHeader = item is EffectGroup;
            var text = isHeader
                ? ((EffectGroup)item).Name
                : DissolveEffectCatalog.DisplayName((DissolveEffect)item);

            e.DrawBackground();

            var bounds = isHeader
                ? e.Bounds
                : new Rectangle(e.Bounds.X + 12, e.Bounds.Y, e.Bounds.Width - 12, e.Bounds.Height);
            var colour = (e.State & DrawItemState.Selected) == DrawItemState.Selected
                ? SystemColors.HighlightText
                : isHeader ? SystemColors.GrayText : SystemColors.ControlText;

            TextRenderer.DrawText(
                e.Graphics,
                text,
                isHeader ? headerFont : list.Font,
                bounds,
                colour,
                TextFormatFlags.VerticalCenter);
        };

        list.Height = Math.Min(320, (list.Items.Count * list.ItemHeight) + 4);
        list.SelectedItem = value;
        list.Click += (_, _) =>
        {
            if (list.SelectedItem is DissolveEffect)
            {
                service.CloseDropDown();
            }
        };

        service.DropDownControl(list);

        return list.SelectedItem is DissolveEffect chosen ? chosen : value;
    }
}
