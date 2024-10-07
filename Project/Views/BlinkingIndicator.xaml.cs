using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace APAS.Plugin.KEYTHLEY._2600.Views
{
    /// <summary>
    /// Interaction logic for BlinkingIndicator.xaml
    /// </summary>
    public partial class BlinkingIndicator : UserControl
    {
        public BlinkingIndicator()
        {
            InitializeComponent();
        }

        public void Blink()
        {
            var sb = FindResource("sbdBlinking") as Storyboard;
            Storyboard.SetTarget(sb, brd);
            sb.Begin();
        }
    }
}
