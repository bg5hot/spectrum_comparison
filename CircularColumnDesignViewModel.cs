using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.SkiaSharpView.Painting.Effects;
using SkiaSharp;

namespace SpectrumComparison
{
    public class CircularColumnDesignViewModel : INotifyPropertyChanged
    {
        // Inputs
        private double _fc = 4000;
        private double _fy = 60000;
        private double _diameter = 24;
        private double _cover = 1.5;
        
        private string _selectedBarSize = "#8";
        private string _selectedTieSize = "#4";
        private int _numBars = 8;
        private CircularColumnDesignCalculations.TieType _selectedTieType = CircularColumnDesignCalculations.TieType.Spiral;
        
        private double _pu = 800; // kips
        private double _mux = 100; // kip-ft
        private double _muy = 100; // kip-ft
        private double _vux = 50; // kips
        private double _vuy = 50; // kips

        // Outputs
        private string _designProcess = "点击执行设计以查看结果...";
        private ISeries[] _seriesX = Array.Empty<ISeries>();
        private ISeries[] _seriesY = Array.Empty<ISeries>();
        
        // Preview
        public ObservableCollection<RebarVisualItem> RebarLocations { get; } = new();
        private string _rebarInfo = "";
        
        // Helper property for visualization
        private double _barDiameterDisplay;
        public double BarDiameterDisplay { get => _barDiameterDisplay; set { _barDiameterDisplay = value; OnPropertyChanged(); } }

        public ICommand CalculateCommand { get; }

        public event PropertyChangedEventHandler? PropertyChanged;

        public CircularColumnDesignViewModel()
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
        
        public double Diameter 
        { 
            get => _diameter; 
            set 
            { 
                _diameter = value; 
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
        
        public int NumBars 
        { 
            get => _numBars; 
            set 
            { 
                _numBars = value; 
                if (_numBars < 6) _numBars = 6;
                OnPropertyChanged(); 
                UpdateRebarPreview(); 
            } 
        }
        
        public CircularColumnDesignCalculations.TieType SelectedTieType
        {
            get => _selectedTieType;
            set
            {
                _selectedTieType = value;
                OnPropertyChanged();
            }
        }

        public double Pu { get => _pu; set { _pu = value; OnPropertyChanged(); } }
        public double Mux { get => _mux; set { _mux = value; OnPropertyChanged(); } }
        public double Muy { get => _muy; set { _muy = value; OnPropertyChanged(); } }
        public double Vux { get => _vux; set { _vux = value; OnPropertyChanged(); } }
        public double Vuy { get => _vuy; set { _vuy = value; OnPropertyChanged(); } }

        public string DesignProcess { get => _designProcess; set { _designProcess = value; OnPropertyChanged(); } }
        
        public ISeries[] SeriesX { get => _seriesX; set { _seriesX = value; OnPropertyChanged(); } }
        public ISeries[] SeriesY { get => _seriesY; set { _seriesY = value; OnPropertyChanged(); } } // Circular is symmetric, but we keep structure
        
        public string RebarInfo { get => _rebarInfo; set { _rebarInfo = value; OnPropertyChanged(); } }

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

        public List<string> BarSizes { get; } = new List<string>
        {
            "#3", "#4", "#5", "#6", "#7", "#8", "#9", "#10", "#11", "#14", "#18"
        };
        
        public List<CircularColumnDesignCalculations.TieType> TieTypes { get; } = new List<CircularColumnDesignCalculations.TieType>
        {
            CircularColumnDesignCalculations.TieType.Tied,
            CircularColumnDesignCalculations.TieType.Spiral
        };

        public List<int> NumBarsOptions { get; } = Enumerable.Range(6, 15).ToList(); // 6 to 20

        #endregion

        private void UpdateRebarPreview()
        {
            RebarLocations.Clear();
            
            double diameter = Diameter;
            if (diameter <= 0) return;

            double cover = Cover;
            double tieDia = ColumnDesignCalculations.GetBarDiameter(SelectedTieSize);
            double barDia = ColumnDesignCalculations.GetBarDiameter(SelectedBarSize);
            
            // Set display diameter for visualization binding
            BarDiameterDisplay = barDia;
            
            double d_core = diameter - 2 * cover - 2 * tieDia - barDia;
            double r_core = d_core / 2.0;

            if (r_core <= 0) return;

            // Center of column is (D/2, D/2)
            double cx = diameter / 2.0;
            double cy = diameter / 2.0;

            double dTheta = 2 * Math.PI / NumBars;
            for (int i = 0; i < NumBars; i++)
            {
                // Start from top (Y axis)
                // x = cx + r_core * sin(theta)
                // y = cy - r_core * cos(theta)
                
                double x = cx + r_core * Math.Sin(i * dTheta);
                double y = cy - r_core * Math.Cos(i * dTheta);
                
                RebarLocations.Add(new RebarVisualItem(x, y, barDia));
            }

            double ag = Math.PI * Math.Pow(diameter / 2.0, 2);
            double ast = NumBars * ColumnDesignCalculations.GetBarArea(SelectedBarSize);
            double rho = ast / ag;
            
            RebarInfo = $"Ag={ag:F1} in², Ast={ast:F2} in², ρ={rho:P2}";
        }

        private void CalculateDesign()
        {
            var mat = new ColumnDesignCalculations.MaterialProperties { Fc = Fc, Fy = Fy };
            var sec = new CircularColumnDesignCalculations.CircularColumnSection
            {
                Diameter = Diameter,
                Cover = Cover,
                BarSize = SelectedBarSize,
                TieSize = SelectedTieSize,
                NumBars = NumBars,
                TieType = SelectedTieType
            };

            var result = CircularColumnDesignCalculations.CheckColumn(Pu, Mux, Muy, Vux, Vuy, mat, sec);
            
            DesignProcess = result.Report;

            // Plot X
            var nominalPtsX = result.NominalCurveX.Select(p => new ObservablePoint(p.M, p.P)).ToArray();
            var designPtsX = result.DesignCurveX.Select(p => new ObservablePoint(p.M, p.P)).ToArray();
            
            // Resultant Moment Mu
            double Mu = Math.Sqrt(Mux * Mux + Muy * Muy);
            
            var userPtX = new ObservablePoint(Mu, Pu);

            SeriesX = new ISeries[]
            {
                new LineSeries<ObservablePoint>
                {
                    Values = nominalPtsX,
                    Name = "Nominal (Pn-Mn)",
                    Fill = null,
                    GeometrySize = 0,
                    Stroke = new SolidColorPaint(SKColors.Gray) { StrokeThickness = 2 }
                },
                new LineSeries<ObservablePoint>
                {
                    Values = designPtsX,
                    Name = "Design (φPn-φMn)",
                    Fill = null,
                    GeometrySize = 0,
                    Stroke = new SolidColorPaint(SKColors.Blue) { StrokeThickness = 3 }
                },
                new ScatterSeries<ObservablePoint>
                {
                    Values = new []{ userPtX },
                    Name = "User Point (Pu, Mu)",
                    GeometrySize = 10,
                    Fill = new SolidColorPaint(SKColors.Red)
                }
            };
            
            // Plot Y (Identical to X for Circular, but we use the same structure for consistency)
            
            var nominalPtsY = result.NominalCurveY.Select(p => new ObservablePoint(p.M, p.P)).ToArray();
            var designPtsY = result.DesignCurveY.Select(p => new ObservablePoint(p.M, p.P)).ToArray();
            var userPtY = new ObservablePoint(Muy, Pu);

            SeriesY = new ISeries[]
            {
                new LineSeries<ObservablePoint>
                {
                    Values = nominalPtsY,
                    Name = "Nominal (Pn-Mn)",
                    Fill = null,
                    GeometrySize = 0,
                    Stroke = new SolidColorPaint(SKColors.Gray) { StrokeThickness = 2 }
                },
                new LineSeries<ObservablePoint>
                {
                    Values = designPtsY,
                    Name = "Design (φPn-φMn)",
                    Fill = null,
                    GeometrySize = 0,
                    Stroke = new SolidColorPaint(SKColors.Blue) { StrokeThickness = 3 }
                },
                new ScatterSeries<ObservablePoint>
                {
                    Values = new []{ userPtY },
                    Name = "Component (Pu, Muy)",
                    GeometrySize = 10,
                    Fill = new SolidColorPaint(SKColors.Orange)
                }
            };
        }
    }

    public class RebarVisualItem
    {
        public double X { get; }
        public double Y { get; }
        public double VisualSize { get; }
        
        public double Left => X - VisualSize / 2.0;
        public double Top => Y - VisualSize / 2.0;
        
        public RebarVisualItem(double x, double y, double diameter)
        {
            X = x;
            Y = y;
            VisualSize = diameter;
        }
    }
}
