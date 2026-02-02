using System.Windows;

namespace SpectrumComparison
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void BtnSpectrum_Click(object sender, RoutedEventArgs e)
        {
            contentControl.ContentTemplate = (DataTemplate)FindResource("SpectrumViewTemplate");
            contentControl.Content = new SpectrumView();
        }

        private void BtnWind_Click(object sender, RoutedEventArgs e)
        {
            contentControl.ContentTemplate = (DataTemplate)FindResource("WindConversionViewTemplate");
            contentControl.Content = new WindConversionView();
        }
    }
}
