using System;
using System.Windows.Forms;
using DissolvingPanels;

namespace Panel_Test
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void OnEffectButtonClick(object sender, EventArgs e)
        {
            if (sender is not Button button || button.Tag is not DissolveEffect effect)
            {
                return;
            }

            dissolvingPanel1.Effect = effect;
            statusLabel.Text = "●  PREVIEWING  /  " + effect.ToString().ToUpperInvariant();
            dissolvingPanel1.ResetDissolve();
            dissolvingPanel1.DissolveOut();
        }

        private void OnDissolveCompleted(object sender, EventArgs e)
        {
            if (dissolvingPanel1.IsDissolved)
            {
                dissolvingPanel1.DissolveIn();
            }
            else
            {
                statusLabel.Text = "●  READY  /  21 EFFECTS AVAILABLE";
            }
        }

        private void OnSampleActionClick(object sender, EventArgs e)
        {
            MessageBox.Show(this, "The controls inside the panel are restored after the effect.", "Sample child control");
        }
    }
}
