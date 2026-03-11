using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;

namespace SpectrumComparison
{
    public class ColumnDesignViewModel : INotifyPropertyChanged
    {
        // Inputs
        private double _fc = 4000;
        private double _fy = 60000;
        private double _b = 30;
        private double _h = 24;
        private double _cover = 1.5;
        
        private string _selectedBarSize = "#8";
        private string _selectedTieSize = "#4";
        private int _tieLegsX = 4;
        private int _tieLegsY = 4;
        private int _nx = 6;
        private int _ny = 4;
        
        private double _pu = 1200; // kips
        private double _mux = 250; // kip-ft
        private double _muy = 350; // kip-ft
        private double _vux = 300; // kips
        private double _vuy = 120; // kips

        // Outputs
        private string _designProcess = "点击执行设计以查看结果...";
        private ISeries[] _seriesX = Array.Empty<ISeries>();
        private ISeries[] _seriesY = Array.Empty<ISeries>();
        
        // Preview
        public ObservableCollection<Point> RebarLocations { get; } = new();
        private string _rebarInfo = "";

        public ICommand CalculateCommand { get; }

        public event PropertyChangedEventHandler? PropertyChanged;

        public ColumnDesignViewModel()
        {
            CalculateCommand = new RelayCommand(CalculateDesign);
            UpdateRebarPreview();
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #region Properties

        public double Fc { get => _fc; set { _fc = value; OnPropertyChanged(); } }
        public double Fy { get => _fy; set { _fy = value; OnPropertyChanged(); } }
        
        public double B 
        { 
            get => _b; 
            set 
            { 
                _b = value; 
                OnPropertyChanged(); 
                UpdateRebarPreview(); 
            } 
        }
        
        public double H 
        { 
            get => _h; 
            set 
            { 
                _h = value; 
                OnPropertyChanged(); 
                UpdateRebarPreview(); 
            } 
        }
        
        public double Cover 
        { 
            get => _cover; 
            set 
            { 
                _cover = value; 
                OnPropertyChanged(); 
                UpdateRebarPreview(); 
            } 
        }
        
        public string SelectedBarSize 
        { 
            get => _selectedBarSize; 
            set 
            { 
                _selectedBarSize = value; 
                OnPropertyChanged(); 
                UpdateRebarPreview(); 
            } 
        }
        
        public string SelectedTieSize 
        { 
            get => _selectedTieSize; 
            set 
            { 
                _selectedTieSize = value; 
                OnPropertyChanged(); 
                UpdateRebarPreview(); 
            } 
        }
        
        public int TieLegsX
        {
            get => _tieLegsX;
            set
            {
                _tieLegsX = value;
                OnPropertyChanged();
            }
        }

        public int TieLegsY
        {
            get => _tieLegsY;
            set
            {
                _tieLegsY = value;
                OnPropertyChanged();
            }
        }

        public int Nx 
        { 
            get => _nx; 
            set 
            { 
                _nx = value; 
                OnPropertyChanged(); 
                UpdateRebarPreview(); 
            } 
        }
        
        public int Ny 
        { 
            get => _ny; 
            set 
            { 
                _ny = value; 
                OnPropertyChanged(); 
                UpdateRebarPreview(); 
            } 
        }
        
        public double Pu { get => _pu; set { _pu = value; OnPropertyChanged(); } }
        public double Mux { get => _mux; set { _mux = value; OnPropertyChanged(); } }
        public double Muy { get => _muy; set { _muy = value; OnPropertyChanged(); } }
        public double Vux { get => _vux; set { _vux = value; OnPropertyChanged(); } }
        public double Vuy { get => _vuy; set { _vuy = value; OnPropertyChanged(); } }

        public string DesignProcess { get => _designProcess; set { _designProcess = value; OnPropertyChanged(); } }
        public ISeries[] SeriesX { get => _seriesX; set { _seriesX = value; OnPropertyChanged(); } }
        public ISeries[] SeriesY { get => _seriesY; set { _seriesY = value; OnPropertyChanged(); } }
        public string RebarInfo { get => _rebarInfo; set { _rebarInfo = value; OnPropertyChanged(); } }

        public List<string> BarSizeOptions { get; } = new()
        {
            "#3", "#4", "#5", "#6", "#7", "#8", "#9", "#10", "#11", "#14", "#18"
        };

        public List<int> TieLegOptions { get; } = Enumerable.Range(2, 11).ToList();
        
        public List<int> BarCountOptions { get; } = Enumerable.Range(2, 11).ToList();

        public Axis[] XAxesX { get; } = new[]
        {
            new Axis
            {
                Name = "Moment Mx (kip-ft)",
                NamePaint = new SolidColorPaint(SKColors.Black),
                LabelsPaint = new SolidColorPaint(SKColors.Black),
                TextSize = 12,
                Labeler = value => value.ToString("F0"),
                MinStep = 1,
                SeparatorsPaint = new SolidColorPaint(SKColors.LightGray) { StrokeThickness = 1 },
                ZeroPaint = new SolidColorPaint(SKColors.Black) { StrokeThickness = 3 }
            }
        };

        public Axis[] YAxesX { get; } = new[]
        {
            new Axis
            {
                Name = "Axial Load P (kips)",
                NamePaint = new SolidColorPaint(SKColors.Black),
                LabelsPaint = new SolidColorPaint(SKColors.Black),
                TextSize = 12,
                Labeler = value => value.ToString("F0"),
                MinStep = 1,
                SeparatorsPaint = new SolidColorPaint(SKColors.LightGray) { StrokeThickness = 1 },
                ZeroPaint = new SolidColorPaint(SKColors.Black) { StrokeThickness = 3 }
            }
        };

        public Axis[] XAxesY { get; } = new[]
        {
            new Axis
            {
                Name = "Moment My (kip-ft)",
                NamePaint = new SolidColorPaint(SKColors.Black),
                LabelsPaint = new SolidColorPaint(SKColors.Black),
                TextSize = 12,
                Labeler = value => value.ToString("F0"),
                MinStep = 1,
                SeparatorsPaint = new SolidColorPaint(SKColors.LightGray) { StrokeThickness = 1 },
                ZeroPaint = new SolidColorPaint(SKColors.Black) { StrokeThickness = 3 }
            }
        };

        public Axis[] YAxesY { get; } = new[]
        {
            new Axis
            {
                Name = "Axial Load P (kips)",
                NamePaint = new SolidColorPaint(SKColors.Black),
                LabelsPaint = new SolidColorPaint(SKColors.Black),
                TextSize = 12,
                Labeler = value => value.ToString("F0"),
                MinStep = 1,
                SeparatorsPaint = new SolidColorPaint(SKColors.LightGray) { StrokeThickness = 1 },
                ZeroPaint = new SolidColorPaint(SKColors.Black) { StrokeThickness = 3 }
            }
        };

        #endregion

        private void CalculateDesign()
        {
            try
            {
                var mat = new ColumnDesignCalculations.MaterialProperties { Fc = Fc, Fy = Fy };
                var sec = new ColumnDesignCalculations.ColumnSection 
                { 
                    B = B, H = H, Cover = Cover, 
                    BarSize = SelectedBarSize, TieSize = SelectedTieSize,
                    Nx = Nx, Ny = Ny,
                    TieLegsX = TieLegsX, TieLegsY = TieLegsY
                };

                var result = ColumnDesignCalculations.CheckColumn(Pu, Mux, Muy, Vux, Vuy, mat, sec);

                DesignProcess = result.Report;

                // Update Chart X
                var nominalPointsX = result.NominalCurveX.Select(p => new LiveChartsCore.Defaults.ObservablePoint(p.M, p.P)).ToList();
                var designPointsX = result.DesignCurveX.Select(p => new LiveChartsCore.Defaults.ObservablePoint(p.M, p.P)).ToList();
                var userPointX = new List<LiveChartsCore.Defaults.ObservablePoint> 
                { 
                    new LiveChartsCore.Defaults.ObservablePoint(result.UserPointX.M, result.UserPointX.P) 
                };

                SeriesX = new ISeries[]
                {
                    new LineSeries<LiveChartsCore.Defaults.ObservablePoint>
                    {
                        Name = "Nominal (Pn-Mn)",
                        Values = nominalPointsX,
                        Stroke = new SolidColorPaint(SKColors.Gray) { StrokeThickness = 1 },
                        Fill = null,
                        GeometrySize = 0,
                        GeometryStroke = new SolidColorPaint(SKColors.Gray) { StrokeThickness = 1 },
                        GeometryFill = new SolidColorPaint(SKColors.Gray)
                    },
                    new LineSeries<LiveChartsCore.Defaults.ObservablePoint>
                    {
                        Name = "Design (φPn-φMn)",
                        Values = designPointsX,
                        Stroke = new SolidColorPaint(SKColors.Blue) { StrokeThickness = 3 },
                        Fill = null,
                        GeometrySize = 0,
                        GeometryStroke = new SolidColorPaint(SKColors.Blue) { StrokeThickness = 3 },
                        GeometryFill = new SolidColorPaint(SKColors.Blue)
                    },
                    new ScatterSeries<LiveChartsCore.Defaults.ObservablePoint>
                    {
                        Name = "User Load (Pu-Mux)",
                        Values = userPointX,
                        Fill = new SolidColorPaint(result.IsSafeX ? SKColors.Green : SKColors.Red),
                        GeometrySize = 15
                    }
                };

                // Update Chart Y
                var nominalPointsY = result.NominalCurveY.Select(p => new LiveChartsCore.Defaults.ObservablePoint(p.M, p.P)).ToList();
                var designPointsY = result.DesignCurveY.Select(p => new LiveChartsCore.Defaults.ObservablePoint(p.M, p.P)).ToList();
                var userPointY = new List<LiveChartsCore.Defaults.ObservablePoint> 
                { 
                    new LiveChartsCore.Defaults.ObservablePoint(result.UserPointY.M, result.UserPointY.P) 
                };

                SeriesY = new ISeries[]
                {
                    new LineSeries<LiveChartsCore.Defaults.ObservablePoint>
                    {
                        Name = "Nominal (Pn-Mn)",
                        Values = nominalPointsY,
                        Stroke = new SolidColorPaint(SKColors.Gray) { StrokeThickness = 1 },
                        Fill = null,
                        GeometrySize = 0,
                        GeometryStroke = new SolidColorPaint(SKColors.Gray) { StrokeThickness = 1 },
                        GeometryFill = new SolidColorPaint(SKColors.Gray)
                    },
                    new LineSeries<LiveChartsCore.Defaults.ObservablePoint>
                    {
                        Name = "Design (φPn-φMn)",
                        Values = designPointsY,
                        Stroke = new SolidColorPaint(SKColors.Blue) { StrokeThickness = 3 },
                        Fill = null,
                        GeometrySize = 0,
                        GeometryStroke = new SolidColorPaint(SKColors.Blue) { StrokeThickness = 3 },
                        GeometryFill = new SolidColorPaint(SKColors.Blue)
                    },
                    new ScatterSeries<LiveChartsCore.Defaults.ObservablePoint>
                    {
                        Name = "User Load (Pu-Muy)",
                        Values = userPointY,
                        Fill = new SolidColorPaint(result.IsSafeY ? SKColors.Green : SKColors.Red),
                        GeometrySize = 15
                    }
                };
            }
            catch (Exception ex)
            {
                DesignProcess = $"Error: {ex.Message}\n{ex.StackTrace}";
            }
        }

        private void UpdateRebarPreview()
        {
            try
            {
                RebarLocations.Clear();
                int totalBars = 2 * (Nx + Ny - 2);
                // Simple parsing for display
                string barSizeNum = SelectedBarSize.Replace("#", "");
                RebarInfo = $"{totalBars} No. {barSizeNum} Bars";

                if (Nx < 2 || Ny < 2) return;

                double tieD = GetBarDiameter(SelectedTieSize);
                double barD = GetBarDiameter(SelectedBarSize);
                
                // Effective depth to center of bar
                double d_prime = Cover + tieD + barD / 2.0;
                
                // Ensure section is large enough
                if (B <= 2 * d_prime || H <= 2 * d_prime) return;

                // Top and Bottom Rows
                if (Nx >= 2)
                {
                    double spacingX = (B - 2 * d_prime) / (Nx - 1);
                    double yTop = d_prime; // Y is down in WPF Canvas
                    double yBot = H - d_prime;

                    for (int i = 0; i < Nx; i++)
                    {
                        double x = d_prime + i * spacingX;
                        RebarLocations.Add(new Point(x, yTop));
                        RebarLocations.Add(new Point(x, yBot));
                    }
                }

                // Side Rows (excluding corners which are already added)
                if (Ny > 2)
                {
                    double spacingY = (H - 2 * d_prime) / (Ny - 1);
                    double xLeft = d_prime;
                    double xRight = B - d_prime;

                    for (int j = 1; j < Ny - 1; j++)
                    {
                        double y = d_prime + j * spacingY;
                        RebarLocations.Add(new Point(xLeft, y));
                        RebarLocations.Add(new Point(xRight, y));
                    }
                }
            }
            catch
            {
                // Ignore calculation errors during preview update
            }
        }

        private double GetBarDiameter(string size)
        {
            return size switch
            {
                "#3" => 0.375,
                "#4" => 0.500,
                "#5" => 0.625,
                "#6" => 0.750,
                "#7" => 0.875,
                "#8" => 1.000,
                "#9" => 1.128,
                "#10" => 1.270,
                "#11" => 1.410,
                "#14" => 1.693,
                "#18" => 2.257,
                _ => 1.000
            };
        }
    }
}
