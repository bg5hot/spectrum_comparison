using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using OxyPlot;
using OxyPlot.Series;
using OxyPlot.Axes;
using OxyPlot.Legends;

namespace SpectrumComparison
{
    public class SpectrumViewModel : INotifyPropertyChanged
    {
        private double _damping = 0.05;
        private string _chinaIntensity = "7度(0.10g)";
        private string _chinaSiteCategory = "II";
        private string _chinaEarthquakeGroup = "第一组";
        private double _usSs = 0.51;
        private double _usS1 = 0.18;
        private double _usTl = 24.0;
        private double _usR = 5.0;
        private string _usSiteClass = "D";

        private string _alphaMaxText = "Alpha Max: 0.08";
        private string _tgText = "Tg: 0.35s";
        private string _usFaFvText = "Fa: - | Fv: -";
        private string _usSdsSd1Text = "SDS: - | SD1: -";

        public event PropertyChangedEventHandler? PropertyChanged;

        public SpectrumViewModel()
        {
            UpdateChart();
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public double Damping
        {
            get => _damping;
            set { _damping = value; OnPropertyChanged(); UpdateChart(); }
        }

        public string ChinaIntensity
        {
            get => _chinaIntensity;
            set { _chinaIntensity = value; OnPropertyChanged(); UpdateChart(); }
        }

        public string ChinaSiteCategory
        {
            get => _chinaSiteCategory;
            set { _chinaSiteCategory = value; OnPropertyChanged(); UpdateChart(); }
        }

        public string ChinaEarthquakeGroup
        {
            get => _chinaEarthquakeGroup;
            set { _chinaEarthquakeGroup = value; OnPropertyChanged(); UpdateChart(); }
        }

        public double UsSs
        {
            get => _usSs;
            set { _usSs = value; OnPropertyChanged(); UpdateChart(); }
        }

        public double UsS1
        {
            get => _usS1;
            set { _usS1 = value; OnPropertyChanged(); UpdateChart(); }
        }

        public double UsTl
        {
            get => _usTl;
            set { _usTl = value; OnPropertyChanged(); UpdateChart(); }
        }

        public double UsR
        {
            get => _usR;
            set { _usR = value; OnPropertyChanged(); UpdateChart(); }
        }

        public string UsSiteClass
        {
            get => _usSiteClass;
            set { _usSiteClass = value; OnPropertyChanged(); UpdateChart(); }
        }

        public string AlphaMaxText
        {
            get => _alphaMaxText;
            set { _alphaMaxText = value; OnPropertyChanged(); }
        }

        public string TgText
        {
            get => _tgText;
            set { _tgText = value; OnPropertyChanged(); }
        }

        public string UsFaFvText
        {
            get => _usFaFvText;
            set { _usFaFvText = value; OnPropertyChanged(); }
        }

        public string UsSdsSd1Text
        {
            get => _usSdsSd1Text;
            set { _usSdsSd1Text = value; OnPropertyChanged(); }
        }

        public List<string> IntensityOptions { get; } = new()
        {
            "6度(0.05g)", "7度(0.10g)", "7度(0.15g)", "8度(0.20g)", "8度(0.30g)", "9度(0.40g)"
        };

        public List<string> SiteCategoryOptions { get; } = new() { "I0", "I1", "II", "III", "IV" };

        public List<string> EarthquakeGroupOptions { get; } = new() { "第一组", "第二组", "第三组" };

        public List<string> UsSiteClassOptions { get; } = new() { "A", "B", "C", "D" };

        private PlotModel _spectrumPlotModel = new PlotModel();

        public PlotModel SpectrumPlotModel
        {
            get => _spectrumPlotModel;
            set { _spectrumPlotModel = value; OnPropertyChanged(); }
        }

        private void UpdateChart()
        {
            try
            {
                double alphaMax = CoreCalculations.GetAlphaMax(ChinaIntensity);
                double tg = CoreCalculations.GetTg(ChinaSiteCategory, ChinaEarthquakeGroup);
                var (chinaPeriods, chinaAlpha) = CoreCalculations.CalculateChineseSpectrum(alphaMax, tg, Damping);

                AlphaMaxText = $"Alpha Max: {alphaMax:F2}";
                TgText = $"Tg: {tg:F2}s";

                var (usPeriods, usSa, sds, sd1, fa, fv) = CoreCalculations.CalculateUsSpectrum(
                    UsSs, UsS1, UsSiteClass, UsTl, UsR, Damping);

                UsFaFvText = $"Fa: {fa:F2}   Fv: {fv:F2}";
                UsSdsSd1Text = $"SDS: {sds:F3}g   SD1: {sd1:F3}g";

                var plotModel = new PlotModel
                {
                    Title = "Response Spectrum Comparison",
                    TitleFontSize = 14,
                    TitleFontWeight = 600,
                    Background = OxyColors.White
                };

                var periodAxis = new LinearAxis
                {
                    Position = AxisPosition.Bottom,
                    Title = "Period T (s)",
                    Minimum = 0,
                    Maximum = 6,
                    MajorGridlineStyle = LineStyle.Solid,
                    MinorGridlineStyle = LineStyle.Dot
                };

                double maxVal = Math.Max(chinaAlpha.Max(), usSa.Max());
                var saAxis = new LinearAxis
                {
                    Position = AxisPosition.Left,
                    Title = "Spectral Acceleration (g)",
                    Minimum = 0,
                    Maximum = maxVal * 1.1,
                    MajorGridlineStyle = LineStyle.Solid,
                    MinorGridlineStyle = LineStyle.Dot
                };

                var chinaSeries = new LineSeries
                {
                    Title = "China GB50011-2010",
                    Color = OxyColor.FromRgb(0, 159, 170),
                    LineStyle = LineStyle.Solid,
                    StrokeThickness = 2,
                    MarkerType = MarkerType.None
                };

                var usSeries = new LineSeries
                {
                    Title = $"US ASCE7-16 (R={UsR})",
                    Color = OxyColor.FromRgb(255, 107, 0),
                    LineStyle = LineStyle.Solid,
                    StrokeThickness = 2,
                    MarkerType = MarkerType.None
                };

                for (int i = 0; i < chinaPeriods.Length; i++)
                {
                    chinaSeries.Points.Add(new DataPoint(chinaPeriods[i], chinaAlpha[i]));
                }

                for (int i = 0; i < usPeriods.Length; i++)
                {
                    usSeries.Points.Add(new DataPoint(usPeriods[i], usSa[i]));
                }

                plotModel.Axes.Add(periodAxis);
                plotModel.Axes.Add(saAxis);
                plotModel.Series.Add(chinaSeries);
                plotModel.Series.Add(usSeries);
                plotModel.Legends.Add(new Legend());

                SpectrumPlotModel = plotModel;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Calculation Error: {ex.Message}");
            }
        }
    }
}
