using System.Windows.Controls;

namespace SpectrumComparison
{
    public partial class ArtificialWaveView : UserControl
    {
        public ArtificialWaveView()
        {
            InitializeComponent();
            DataContext = new ArtificialWaveViewModel();
        }
    }
}
