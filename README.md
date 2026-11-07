# DissolvingPanel

[![NuGet](https://img.shields.io/nuget/v/DissolvingPanel.svg)](https://www.nuget.org/packages/DissolvingPanel)
[![NuGet downloads](https://img.shields.io/nuget/dt/DissolvingPanel.svg)](https://www.nuget.org/packages/DissolvingPanel)
[![build](https://github.com/DwainSnickles/DissolvingPanel/actions/workflows/ci.yml/badge.svg)](https://github.com/DwainSnickles/DissolvingPanel/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-8.0--windows-512BD4.svg)](https://dotnet.microsoft.com)

A WinForms `Panel` whose contents animate away and come back with PowerPoint-style
transitions. The panel snapshots what it contains, hides the children while the
transition runs (so nothing can be clicked or tabbed to mid-animation), then
reveals the snapshot frame by frame and restores the real controls at the end.

```csharp
var panel = new DissolvingPanel
{
    Dock = DockStyle.Fill,
    Effect = DissolveEffect.WipeRight,
    Duration = 400,
    SlideFrom = SlideDirection.FromBottom,
};

// animate away, then back
panel.DissolveOut();
panel.DissolveIn();
```

## Install

```
dotnet add package DissolvingPanel
```

Targets `net8.0-windows` and later, using `UseWindowsForms`.

## Effects

`Fade`, `Dissolve`, `WipeLeft`, `WipeRight`, `WipeUp`, `WipeDown`,
`PushLeft`, `PushRight`, `PushUp`, `PushDown`, `SplitHorizontal`, `SplitVertical`,
`BlindsHorizontal`, `BlindsVertical`, `Checkerboard`, `RandomBars`, `Circle`,
`Diamond`, `Box`, `Wheel`, `Zoom`.

## API

| Member | Purpose |
| --- | --- |
| `Effect` | Transition used in both directions. |
| `Duration` | Length of one transition, in milliseconds. |
| `FadeWithEffect` | Also ramp opacity while the shape effect runs. |
| `SlideFrom` | Slide the whole contents in from an edge while the effect plays. |
| `DissolveOut()` / `DissolveIn()` / `Toggle()` | Run a transition. |
| `PlayIn()` | Reset and animate from dissolved to visible — handy when a tab page appears. |
| `ResetDissolve()` | Stop any running transition and restore the children. |
| `IsAnimating` / `IsDissolved` | Current state. |
| `DissolveCompleted` | Raised when a transition finishes. |
| `RenderFrame(float)` | Render one frame at a fixed progress without animating (previews, docs, tests). |

Selecting the control in the Visual Studio designer shows a **Dissolve** smart tag
with the effect, duration, fade option and a reset command.

## Notes

- `BackColor` defaults to white; set it to match the host surface.
- If the panel is inside a `TabPage`, give the page `Padding = 0` so the panel
  fills it exactly.
- `PlayIn()` is a no-op while the control is invisible or undisposed handles do not
  exist yet, so it is safe to call from a `SelectedIndexChanged` handler.

## License

MIT — see [LICENSE](LICENSE).
