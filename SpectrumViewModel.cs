using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;

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

        public ISeries[] Series { get; private set; } = Array.Empty<ISeries>();

        public Axis[] XAxes { get; } = new[]
        {
            new Axis
            {
                Name = "Period T (s)",
                NamePaint = new SolidColorPaint(SKColors.Black),
                LabelsPaint = new SolidColorPaint(SKColors.Black),
                TextSize = 12,
                MinLimit = 0,
                MaxLimit = 6
            }
        };

        public Axis[] YAxes { get; } = new[]
        {
            new Axis
            {
                Name = "Spectral Acceleration (g)",
                NamePaint = new SolidColorPaint(SKColors.Black),
                LabelsPaint = new SolidColorPaint(SKColors.Black),
                TextSize = 12,
                MinLimit = 0
            }
        };

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

                var chinaValues = chinaPeriods.Select((t, i) => new LiveChartsCore.Defaults.ObservablePoint(t, chinaAlpha[i])).ToList();
                var usValues = usPeriods.Select((t, i) => new LiveChartsCore.Defaults.ObservablePoint(t, usSa[i])).ToList();

                double maxVal = Math.Max(chinaAlpha.Max(), usSa.Max());
                YAxes[0].MaxLimit = maxVal * 1.1;

                Series = new ISeries[]
                {
                    new LineSeries<LiveChartsCore.Defaults.ObservablePoint>
                    {
                        Name = "China GB50011-2010",
                        Values = chinaValues,
                        Stroke = new SolidColorPaint(new SKColor(0, 159, 170)) { StrokeThickness = 2 },
                        Fill = null,
                        GeometrySize = 0
                    },
                    new LineSeries<LiveChartsCore.Defaults.ObservablePoint>
                    {
                        Name = $"US ASCE7-16 (R={UsR})",
                        Values = usValues,
                        Stroke = new SolidColorPaint(new SKColor(255, 107, 0)) { StrokeThickness = 2 },
                        Fill = null,
                        GeometrySize = 0
                    }
                };

                OnPropertyChanged(nameof(Series));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Calculation Error: {ex.Message}");
            }
        }
    }
}
