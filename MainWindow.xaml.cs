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

        private void BtnArtificialWave_Click(object sender, RoutedEventArgs e)
        {
            contentControl.ContentTemplate = (DataTemplate)FindResource("ArtificialWaveViewTemplate");
            contentControl.Content = new ArtificialWaveView();
        }

        private void BtnWind_Click(object sender, RoutedEventArgs e)
        {
            contentControl.ContentTemplate = (DataTemplate)FindResource("WindConversionViewTemplate");
            contentControl.Content = new WindConversionView();
        }

        private void BtnGust_Click(object sender, RoutedEventArgs e)
        {
            contentControl.ContentTemplate = (DataTemplate)FindResource("GustEffectFactorViewTemplate");
            contentControl.Content = new GustEffectFactorView();
        }

        private void BtnWindSim_Click(object sender, RoutedEventArgs e)
        {
            contentControl.ContentTemplate = (DataTemplate)FindResource("WindSimulationViewTemplate");
            contentControl.Content = new WindSimulationView();
        }

        private void BtnBeam_Click(object sender, RoutedEventArgs e)
        {
            contentControl.ContentTemplate = (DataTemplate)FindResource("BeamDesignViewTemplate");
            contentControl.Content = new BeamDesignView();
        }

        private void BtnColumn_Click(object sender, RoutedEventArgs e)
        {
            contentControl.ContentTemplate = (DataTemplate)FindResource("ColumnDesignViewTemplate");
            contentControl.Content = new ColumnDesignView();
        }

        private void BtnCircularColumn_Click(object sender, RoutedEventArgs e)
        {
            contentControl.ContentTemplate = (DataTemplate)FindResource("CircularColumnDesignViewTemplate");
            contentControl.Content = new CircularColumnDesignView();
        }

        private void BtnPunchingShear_Click(object sender, RoutedEventArgs e)
        {
            contentControl.ContentTemplate = (DataTemplate)FindResource("PunchingShearViewTemplate");
            contentControl.Content = new PunchingShearView();
        }

        private void BtnDevelopmentLength_Click(object sender, RoutedEventArgs e)
        {
            contentControl.ContentTemplate = (DataTemplate)FindResource("DevelopmentLengthViewTemplate");
            contentControl.Content = new DevelopmentLengthView();
        }

        private void BtnWeChat_Click(object sender, RoutedEventArgs e)
        {
            contentControl.ContentTemplate = (DataTemplate)FindResource("WeChatViewTemplate");
            contentControl.Content = new WeChatView();
        }
    }
}
