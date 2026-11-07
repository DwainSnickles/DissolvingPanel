using System;
using System.Windows.Forms;

namespace DissolvingPanelDemo;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();

        if (args.Length > 1 && string.Equals(args[0], "--shots", StringComparison.OrdinalIgnoreCase))
        {
            ShotRenderer.Save(args[1]);
            return;
        }

        Application.Run(new DemoForm());
    }
}
