using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using OxyPlot;
using OxyPlot.Series;
using OxyPlot.Axes;
using OxyPlot.Legends;
using System.IO; // 必须添加，否则找不到 StreamWriter

namespace SpectrumComparison
{
    public class WindSimulationViewModel : INotifyPropertyChanged
    {
        private double _vRef = 115;
        private string _exposure = "C";
        private double _height = 100;
        private double _duration = 600;
        private double _sampleRate = 20;

        public double VRef
        {
            get => _vRef;
            set { _vRef = value; OnPropertyChanged(); }
        }

        public string Exposure
        {
            get => _exposure;
            set { _exposure = value; OnPropertyChanged(); }
        }

        public double Height
        {
            get => _height;
            set { _height = value; OnPropertyChanged(); }
        }

        public double Duration
        {
            get => _duration;
            set { _duration = value; OnPropertyChanged(); }
        }

        public double SampleRate
        {
            get => _sampleRate;
            set { _sampleRate = value; OnPropertyChanged(); }
        }

        public List<string> ExposureOptions => new() { "B", "C", "D" };
        public List<double> DurationOptions => new() { 300, 600, 900, 1200 };
        public List<double> SampleRateOptions => new() { 10, 20, 50, 100 };

        private string _resultText = "等待模拟...";
        private string _processText = "";
        private bool _isGenerating = false;
        private bool _hasData = false;

        public string ResultText
        {
            get => _resultText;
            set { _resultText = value; OnPropertyChanged(); }
        }

        public string ProcessText
        {
            get => _processText;
            set { _processText = value; OnPropertyChanged(); }
        }

        public bool IsGenerating
        {
            get => _isGenerating;
            set { _isGenerating = value; OnPropertyChanged(); }
        }

        public bool HasData
        {
            get => _hasData;
            set { _hasData = value; OnPropertyChanged(); }
        }

        private PlotModel _windPlotModel = new PlotModel();
        private PlotModel _psdPlotModel = new PlotModel();

        public PlotModel WindPlotModel
        {
            get => _windPlotModel;
            set { _windPlotModel = value; OnPropertyChanged(); }
        }

        public PlotModel PsdPlotModel
        {
            get => _psdPlotModel;
            set { _psdPlotModel = value; OnPropertyChanged(); }
        }

        public ICommand GenerateCommand { get; }
        public ICommand SaveCommand { get; }

        private WindSimulationCalculations.WindSimResult? _lastResult;

        public event PropertyChangedEventHandler? PropertyChanged;

        public WindSimulationViewModel()
        {
            GenerateCommand = new RelayCommand(GenerateWind, CanGenerate);
            SaveCommand = new RelayCommand(SaveWind, CanSave);
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private bool CanGenerate() => !IsGenerating;
        private bool CanSave() => _lastResult != null && _lastResult.Success && !IsGenerating;

        private async void GenerateWind()
        {
            if (IsGenerating) return;
            IsGenerating = true;
            ResultText = "正在模拟...";

            await System.Threading.Tasks.Task.Run(() =>
            {
                try
                {
                    var input = new WindSimulationCalculations.WindSimInput
                    {
                        VRef = VRef,
                        Exposure = Exposure,
                        Height = Height,
                        Duration = Duration,
                        SampleRate = SampleRate
                    };

                    var result = WindSimulationCalculations.GenerateWind(input);

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        _lastResult = result;
                        // 先清空，再更新数据，最后通知属性变更
                        // UpdateWindChart(result);
                        // UpdatePsdChart(result); 
                        
                        // 确保通知是在所有 Axis 和 Series 都准备好后发出的
                        // OnPropertyChanged(nameof(PsdSeries));
                        // OnPropertyChanged(nameof(PsdXAxes));

                        if (result.Success)
                        {
                            ResultText = $"模拟完成！Vz={result.VAvg:F1} ft/s, Iz={result.Iz:F4}, Lz={result.Lz:F0} ft";
                            ProcessText = string.Join("\n", result.ProcessLog);
                            UpdateWindChart(result);
                            UpdatePsdChart(result);
                            HasData = true;
                        }
                        else
                        {
                            ResultText = "模拟失败";
                            ProcessText = result.ErrorMessage ?? "未知错误";
                        }

                        IsGenerating = false;
                    });
                }
                catch (Exception ex)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        ResultText = "模拟失败";
                        ProcessText = $"错误: {ex.Message}";
                        IsGenerating = false;
                    });
                }
            });
        }

        private void UpdateWindChart(WindSimulationCalculations.WindSimResult result)
        {
            var plotModel = new PlotModel
            {
                Title = "脉动风速时程",
                TitleFontSize = 14,
                TitleFontWeight = 600,
                Background = OxyColors.White
            };

            var timeAxis = new LinearAxis
            {
                Position = AxisPosition.Bottom,
                Title = "时间 (s)",
                Minimum = 0,
                Maximum = result.TimeArray.Last(),
                MajorGridlineStyle = LineStyle.Solid,
                MinorGridlineStyle = LineStyle.Dot
            };

            var windAxis = new LinearAxis
            {
                Position = AxisPosition.Left,
                Title = "风速 (ft/s)",
                Minimum = result.TotalWind.Min() * 1.05,
                Maximum = result.TotalWind.Max() * 1.05,
                MajorGridlineStyle = LineStyle.Solid,
                MinorGridlineStyle = LineStyle.Dot
            };

            var lineSeries = new LineSeries
            {
                Title = "脉动风速",
                Color = OxyColors.Teal,
                LineStyle = LineStyle.Solid,
                StrokeThickness = 1
            };

            int step = Math.Max(1, result.TimeArray.Length / 2000);
            for (int i = 0; i < result.TimeArray.Length; i += step)
            {
                lineSeries.Points.Add(new DataPoint(result.TimeArray[i], result.TotalWind[i]));
            }

            plotModel.Axes.Add(timeAxis);
            plotModel.Axes.Add(windAxis);
            plotModel.Series.Add(lineSeries);

            WindPlotModel = plotModel;
        }

        private void UpdatePsdChart(WindSimulationCalculations.WindSimResult result)
        {
            var plotModel = new PlotModel
            {
                Title = "功率谱密度对比",
                TitleFontSize = 14,
                TitleFontWeight = 600,
                Background = OxyColors.White
            };

            var freqAxis = new LogarithmicAxis
            {
                Position = AxisPosition.Bottom,
                Title = "频率 (Hz)",
                Minimum = 0.01,
                Maximum = 10,
                MajorGridlineStyle = LineStyle.Solid,
                MinorGridlineStyle = LineStyle.Dot
            };

            var psdAxis = new LogarithmicAxis
            {
                Position = AxisPosition.Left,
                Title = "PSD (ft²/s²/Hz)",
                MajorGridlineStyle = LineStyle.Solid,
                MinorGridlineStyle = LineStyle.Dot
            };

            var targetSeries = new LineSeries
            {
                Title = "目标 Kaimal 谱",
                Color = OxyColors.Red,
                LineStyle = LineStyle.Solid,
                StrokeThickness = 2
            };

            var simSeries = new LineSeries
            {
                Title = "模拟谱 (Welch)",
                Color = OxyColors.Black,
                LineStyle = LineStyle.Solid,
                StrokeThickness = 1.5
            };

            for (int i = 1; i < result.TargetPSD_Freq.Length; i++)
            {
                if (result.TargetPSD_Freq[i] >= 0.01 && result.TargetPSD_Freq[i] <= 10 && result.TargetPSD[i] > 0)
                {
                    targetSeries.Points.Add(new DataPoint(result.TargetPSD_Freq[i], result.TargetPSD[i]));
                }
            }

            for (int i = 1; i < result.SimulatedPSD_Freq.Length; i++)
            {
                if (result.SimulatedPSD_Freq[i] >= 0.01 && result.SimulatedPSD_Freq[i] <= 10 && result.SimulatedPSD[i] > 0)
                {
                    simSeries.Points.Add(new DataPoint(result.SimulatedPSD_Freq[i], result.SimulatedPSD[i]));
                }
            }

            // 保存调试数据
            using (var writer = new StreamWriter("debug.log"))
            {
                foreach (var point in targetSeries.Points)
                {
                    writer.WriteLine($"目标 Kaimal 谱: 频率: {point.X}, PSD: {point.Y}");
                }
                foreach (var point in simSeries.Points)
                {
                    writer.WriteLine($"模拟谱 (Welch): 频率: {point.X}, PSD: {point.Y}");
                }
            }

            plotModel.Axes.Add(freqAxis);
            plotModel.Axes.Add(psdAxis);
            plotModel.Series.Add(targetSeries);
            plotModel.Series.Add(simSeries);
            plotModel.Legends.Add(new Legend());

            PsdPlotModel = plotModel;
        }

        private void SaveWind()
        {
            if (_lastResult == null || !_lastResult.Success) return;

            try
            {
                string appDirectory = AppDomain.CurrentDomain.BaseDirectory;
                string saveDirectory = System.IO.Path.Combine(appDirectory, "Wind_simulate");

                string filename = WindSimulationCalculations.SaveWindToCsv(
                    _lastResult.TimeArray, _lastResult.TotalWind, _lastResult.FluctuatingWind, saveDirectory);

                ProcessText += $"\n\n已保存风速时程到:\n{filename}";
                ResultText = "已保存风速时程";

                MessageBox.Show(
                    $"成功保存风速时程到:\n{filename}",
                    "保存成功",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"保存失败: {ex.Message}",
                    "错误",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }
}
