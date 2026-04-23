using System.Windows.Controls;

namespace SpectrumComparison
{
    public partial class WindSimulationView : UserControl
    {
        public WindSimulationView()
        {
            InitializeComponent();
            DataContext = new WindSimulationViewModel();
        }
    }
}
