using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using OxyPlot;
using OxyPlot.Series;
using OxyPlot.Axes;
using OxyPlot.Legends;
using Microsoft.Win32;

namespace SpectrumComparison
{
    public class ArtificialWaveViewModel : INotifyPropertyChanged
    {
        // ==========================================
        // 规范选择
        // ==========================================
        private bool _useChineseCode = false;

        public bool UseChineseCode
        {
            get => _useChineseCode;
            set
            {
                _useChineseCode = value;
                OnPropertyChanged();
                UpdateSpectrumParameters();
            }
        }

        // ==========================================
        // 中国规范参数（同SpectrumViewModel）
        // ==========================================
        private double _damping = 0.05;
        private string _chinaIntensity = "8度(0.20g)";
        private string _chinaSiteCategory = "II";
        private string _chinaEarthquakeGroup = "第二组";

        public double Damping
        {
            get => _damping;
            set { _damping = value; OnPropertyChanged(); }
        }

        public string ChinaIntensity
        {
            get => _chinaIntensity;
            set { _chinaIntensity = value; OnPropertyChanged(); }
        }

        public string ChinaSiteCategory
        {
            get => _chinaSiteCategory;
            set { _chinaSiteCategory = value; OnPropertyChanged(); }
        }

        public string ChinaEarthquakeGroup
        {
            get => _chinaEarthquakeGroup;
            set { _chinaEarthquakeGroup = value; OnPropertyChanged(); }
        }

        // ==========================================
        // 美国规范参数（同SpectrumViewModel）
        // ==========================================
        private double _usSs = 1.5;
        private double _usS1 = 0.6;
        private string _usSiteClass = "C";
        private double _usTl = 12.0;
        private double _usR = 8.0;

        public double UsSs
        {
            get => _usSs;
            set { _usSs = value; OnPropertyChanged(); }
        }

        public double UsS1
        {
            get => _usS1;
            set { _usS1 = value; OnPropertyChanged(); }
        }

        public string UsSiteClass
        {
            get => _usSiteClass;
            set { _usSiteClass = value; OnPropertyChanged(); }
        }

        public double UsTl
        {
            get => _usTl;
            set { _usTl = value; OnPropertyChanged(); }
        }

        public double UsR
        {
            get => _usR;
            set { _usR = value; OnPropertyChanged(); }
        }

        // ==========================================
        // 时程参数
        // ==========================================
        private int _numberOfWaves = 5;
        private double _dt = 0.005;
        private double _tTotal = 30.0;
        private int _numberOfIterations = 7;

        public int NumberOfWaves
        {
            get => _numberOfWaves;
            set { _numberOfWaves = value; OnPropertyChanged(); }
        }

        public double Dt
        {
            get => _dt;
            set { _dt = value; OnPropertyChanged(); }
        }

        public double TTotal
        {
            get => _tTotal;
            set { _tTotal = value; OnPropertyChanged(); }
        }

        public int NumberOfIterations
        {
            get => _numberOfIterations;
            set { _numberOfIterations = value; OnPropertyChanged(); }
        }

        // ==========================================
        // 包络线参数
        // ==========================================
        private double _t1 = 3.0;
        private double _t2 = 25.0;
        private double _cDecay = 0.2;

        public double T1
        {
            get => _t1;
            set { _t1 = value; OnPropertyChanged(); }
        }

        public double T2
        {
            get => _t2;
            set { _t2 = value; OnPropertyChanged(); }
        }

        public double CDecay
        {
            get => _cDecay;
            set { _cDecay = value; OnPropertyChanged(); }
        }

        // ==========================================
        // 结果显示
        // ==========================================
        private string _resultText = "等待生成...";
        private string _processText = "";
        private bool _isGenerating = false;
        private int _selectedWaveIndex = 0;

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

        public int SelectedWaveIndex
        {
            get => _selectedWaveIndex;
            set
            {
                _selectedWaveIndex = value;
                OnPropertyChanged();
                UpdateWaveChart();
            }
        }

        public List<int> WaveIndexOptions => _generatedWaves != null
            ? Enumerable.Range(1, _generatedWaves.Count).ToList()
            : new List<int> { 1 };

        // ==========================================
        // 选项列表
        // ==========================================
        public List<string> IntensityOptions => new List<string>
        {
            "6度(0.05g)", "7度(0.10g)", "7度(0.15g)", "8度(0.20g)", "8度(0.30g)", "9度(0.40g)"
        };

        public List<string> SiteCategoryOptions => new List<string>
        {
            "I0", "I1", "II", "III", "IV"
        };

        public List<string> EarthquakeGroupOptions => new List<string>
        {
            "第一组", "第二组", "第三组"
        };

        public List<string> UsSiteClassOptions => new List<string>
        {
            "A", "B", "C", "D"
        };

        public List<int> NumberOfWavesOptions => new List<int> { 1, 3, 5, 7, 10, 15, 20, 30 };
        public List<double> DtOptions => new List<double> { 0.005, 0.01, 0.02 };
        public List<double> TTotalOptions => new List<double> { 20.0, 30.0, 40.0, 60.0 };
        public List<int> IterationsOptions => new List<int> { 3, 5, 7, 10, 15 };

        // ==========================================
        // 图表数据
        // ==========================================
        private PlotModel _spectrumPlotModel = new PlotModel();
        private PlotModel _wavePlotModel = new PlotModel();

        public PlotModel SpectrumPlotModel
        {
            get => _spectrumPlotModel;
            set { _spectrumPlotModel = value; OnPropertyChanged(); }
        }

        public PlotModel WavePlotModel
        {
            get => _wavePlotModel;
            set { _wavePlotModel = value; OnPropertyChanged(); }
        }

        // ==========================================
        // 命令
        // ==========================================
        public ICommand GenerateCommand { get; }
        public ICommand SaveCommand { get; }

        // ==========================================
        // 生成结果（用于保存）
        // ==========================================
        private List<double[]>? _generatedWaves;
        private double[]? _timeArray;

        public event PropertyChangedEventHandler? PropertyChanged;

        public ArtificialWaveViewModel()
        {
            GenerateCommand = new RelayCommand(GenerateWaves, CanGenerate);
            SaveCommand = new RelayCommand(SaveWaves, CanSave);
            UpdateSpectrumParameters();
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void UpdateSpectrumParameters()
        {
            // 当切换规范时，可以在这里更新UI显示
        }

        private bool CanGenerate()
        {
            return !IsGenerating;
        }

        private bool CanSave()
        {
            return _generatedWaves != null && _generatedWaves.Count > 0 && !IsGenerating;
        }

        /// <summary>
        /// 生成人工地震波
        /// </summary>
        private async void GenerateWaves()
        {
            if (IsGenerating) return;

            IsGenerating = true;
            ResultText = "正在生成...";
            ProcessText = "准备生成参数...\n";

            // 在后台线程执行计算
            await System.Threading.Tasks.Task.Run(() =>
            {
                try
                {
                    var input = new ArtificialWaveCalculations.WaveGenerationInput
                    {
                        UseChineseCode = UseChineseCode,
                        Damping = Damping,
                        Dt = Dt,
                        TTotal = TTotal,
                        NumberOfWaves = NumberOfWaves,
                        NumberOfIterations = NumberOfIterations,
                        T1 = T1,
                        T2 = T2,
                        CDecay = CDecay,
                        TMin = 0.02,
                        TMax = 6.0,
                        TStep = 0.02
                    };

                    if (UseChineseCode)
                    {
                        input.ChinaIntensity = ChinaIntensity;
                        input.ChinaSiteCategory = ChinaSiteCategory;
                        input.ChinaEarthquakeGroup = ChinaEarthquakeGroup;
                    }
                    else
                    {
                        input.UsSs = UsSs;
                        input.UsS1 = UsS1;
                        input.UsSiteClass = UsSiteClass;
                        input.UsTl = UsTl;
                        input.UsR = UsR;
                    }

                    var result = ArtificialWaveCalculations.GenerateArtificialWaves(input);

                    // 在UI线程更新结果
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        if (result.Success)
                        {
                            _generatedWaves = result.GeneratedWaves;
                            _timeArray = result.TimeArray;

                            // 更新文本显示
                            ResultText = $"生成完成！共 {result.GeneratedWaves.Count} 条波";
                            ProcessText = string.Join("\n", result.ProcessLog);

                            // 更新图表
                            UpdateChart(result);
                            UpdateWaveChart();
                            OnPropertyChanged(nameof(WaveIndexOptions));
                        }
                        else
                        {
                            ResultText = "生成失败";
                            ProcessText = result.ErrorMessage ?? "未知错误";
                        }

                        IsGenerating = false;
                    });
                }
                catch (Exception ex)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        ResultText = "生成失败";
                        ProcessText = $"错误: {ex.Message}\n\n{ex.StackTrace}";
                        IsGenerating = false;
                    });
                }
            });
        }

        private void UpdateChart(ArtificialWaveCalculations.WaveGenerationResult result)
        {
            var plotModel = new PlotModel
            {
                Title = "反应谱对比",
                TitleFontSize = 14,
                TitleFontWeight = 600,
                Background = OxyColors.White
            };

            var periodAxis = new LinearAxis
            {
                Position = AxisPosition.Bottom,
                Title = "周期 T (s)",
                Minimum = 0,
                Maximum = 6,
                MajorGridlineStyle = LineStyle.Solid,
                MinorGridlineStyle = LineStyle.Dot
            };

            double maxY = Math.Max(result.TargetSpectrum.Max(), result.MeanSpectrum.Max()) * 1.1;
            var saAxis = new LinearAxis
            {
                Position = AxisPosition.Left,
                Title = "谱加速度 / 地震影响系数",
                Minimum = 0,
                Maximum = maxY,
                MajorGridlineStyle = LineStyle.Solid,
                MinorGridlineStyle = LineStyle.Dot
            };

            var targetSeries = new LineSeries
            {
                Title = "目标规范谱",
                Color = OxyColors.Red,
                LineStyle = LineStyle.Solid,
                StrokeThickness = 3
            };

            var meanSeries = new LineSeries
            {
                Title = $"平均谱 ({result.CalculatedSpectra.Count}条波)",
                Color = OxyColors.Blue,
                LineStyle = LineStyle.Solid,
                StrokeThickness = 2
            };

            var periods = result.TargetPeriods;
            for (int i = 0; i < periods.Length; i++)
            {
                targetSeries.Points.Add(new DataPoint(periods[i], result.TargetSpectrum[i]));
                meanSeries.Points.Add(new DataPoint(periods[i], result.MeanSpectrum[i]));
            }

            plotModel.Series.Add(targetSeries);
            plotModel.Series.Add(meanSeries);

            foreach (var calcSa in result.CalculatedSpectra)
            {
                var waveSeries = new LineSeries
                {
                    Color = OxyColors.LightGray,
                    LineStyle = LineStyle.Solid,
                    StrokeThickness = 1,
                    Title = null
                };

                for (int i = 0; i < periods.Length; i++)
                {
                    waveSeries.Points.Add(new DataPoint(periods[i], calcSa[i]));
                }

                plotModel.Series.Add(waveSeries);
            }

            plotModel.Axes.Add(periodAxis);
            plotModel.Axes.Add(saAxis);
            plotModel.Legends.Add(new Legend());

            SpectrumPlotModel = plotModel;
        }

        private void UpdateWaveChart()
        {
            if (_generatedWaves == null || _timeArray == null || _generatedWaves.Count == 0)
                return;

            int index = Math.Max(0, Math.Min(SelectedWaveIndex - 1, _generatedWaves.Count - 1));
            var wave = _generatedWaves[index];
            var time = _timeArray;

            var plotModel = new PlotModel
            {
                Title = $"地震波加速度时程 - 第 {index + 1} 条波",
                TitleFontSize = 14,
                TitleFontWeight = 600,
                Background = OxyColors.White
            };

            var timeAxis = new LinearAxis
            {
                Position = AxisPosition.Bottom,
                Title = "时间 (s)",
                Minimum = 0,
                Maximum = time.Last(),
                MajorGridlineStyle = LineStyle.Solid,
                MinorGridlineStyle = LineStyle.Dot
            };

            double maxY = wave.Max(Math.Abs) * 1.1;
            var accelAxis = new LinearAxis
            {
                Position = AxisPosition.Left,
                Title = "加速度 (g)",
                Minimum = -maxY,
                Maximum = maxY,
                MajorGridlineStyle = LineStyle.Solid,
                MinorGridlineStyle = LineStyle.Dot
            };

            var lineSeries = new LineSeries
            {
                Title = $"第 {index + 1} 条波",
                Color = OxyColors.DodgerBlue,
                LineStyle = LineStyle.Solid,
                StrokeThickness = 1
            };

            for (int i = 0; i < time.Length; i++)
            {
                lineSeries.Points.Add(new DataPoint(time[i], wave[i]));
            }

            plotModel.Axes.Add(timeAxis);
            plotModel.Axes.Add(accelAxis);
            plotModel.Series.Add(lineSeries);

            WavePlotModel = plotModel;
        }

        /// <summary>
        /// 保存生成的地震波到文件
        /// </summary>
        private void SaveWaves()
        {
            if (_generatedWaves == null || _timeArray == null)
                return;

            try
            {
                // 获取程序所在目录
                string appDirectory = AppDomain.CurrentDomain.BaseDirectory;
                string saveDirectory = System.IO.Path.Combine(appDirectory, "Artificial_Waves");

                // 保存所有波
                var savedFiles = new List<string>();
                for (int i = 0; i < _generatedWaves.Count; i++)
                {
                    string filename = ArtificialWaveCalculations.SaveWaveToCsv(
                        _timeArray, _generatedWaves[i], i, saveDirectory);
                    savedFiles.Add(filename);
                }

                // 更新显示
                ProcessText += $"\n\n已保存 {savedFiles.Count} 条波到目录:\n{saveDirectory}";
                ResultText = $"已保存 {savedFiles.Count} 条地震波";

                MessageBox.Show(
                    $"成功保存 {savedFiles.Count} 条人工地震波到:\n{saveDirectory}",
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
