using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

namespace SpectrumComparison
{
    public class BeamDesignViewModel : INotifyPropertyChanged
    {
        // 材料参数
        private double _fc = 4000; // psi
        private double _fy = 60000; // psi

        // 截面尺寸
        private double _b = 14; // in
        private double _h = 24; // in
        private double _cover = 1.5; // in
        private double _stirrupDiameter = 0.5; // in (#4)

        // 荷载
        private double _mu = 298.1; // kip-ft
        private double _vu = 49.7; // kips
        private double _nu = 0; // kips

        // 钢筋选择
        private string _selectedBarSize = "#8";
        private string _selectedStirrupSize = "#4";
        private int _stirrupLegs = 2;

        // 结果显示
        private string _requiredSteelArea = "As,req = - in²";
        private string _providedSteelArea = "As,prov = - in²";
        private string _numberOfBars = "钢筋根数: -";
        private string _phiMnResult = "φMn = - kip-ft";
        private string _momentRatio = "Mu/φMn = -";
        private string _sectionType = "截面类型: -";
        private string _designStatus = "设计状态: -";
        private string _shearResult = "";
        private string _designProcess = "";

        // Preview
        public ObservableCollection<Point> RebarLocations { get; } = new();
        private string _rebarInfo = "";
        private int _calculatedBarCount = 0;

        public event PropertyChangedEventHandler? PropertyChanged;

        public BeamDesignViewModel()
        {
            CalculateCommand = new RelayCommand(CalculateDesign);
            // 初始化时更新箍筋直径
            UpdateStirrupDiameter();
            UpdateRebarPreview();
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #region 属性

        // 材料参数
        public double Fc
        {
            get => _fc;
            set { _fc = value; OnPropertyChanged(); }
        }

        public double Fy
        {
            get => _fy;
            set { _fy = value; OnPropertyChanged(); }
        }

        // 截面尺寸
        public double B
        {
            get => _b;
            set 
            { 
                _b = value; 
                OnPropertyChanged();
                ResetPreview();
            }
        }

        public double H
        {
            get => _h;
            set 
            { 
                _h = value; 
                OnPropertyChanged();
                ResetPreview();
            }
        }

        public double Cover
        {
            get => _cover;
            set 
            { 
                _cover = value; 
                OnPropertyChanged();
                ResetPreview();
            }
        }

        public double StirrupDiameter
        {
            get => _stirrupDiameter;
            set 
            { 
                _stirrupDiameter = value; 
                OnPropertyChanged();
                ResetPreview();
            }
        }

        // 荷载
        public double Mu
        {
            get => _mu;
            set { _mu = value; OnPropertyChanged(); }
        }

        public double Vu
        {
            get => _vu;
            set { _vu = value; OnPropertyChanged(); }
        }

        public double Nu
        {
            get => _nu;
            set { _nu = value; OnPropertyChanged(); }
        }

        // 钢筋选择
        public string SelectedBarSize
        {
            get => _selectedBarSize;
            set 
            { 
                _selectedBarSize = value; 
                OnPropertyChanged();
                ResetPreview();
            }
        }

        public string SelectedStirrupSize
        {
            get => _selectedStirrupSize;
            set 
            { 
                _selectedStirrupSize = value; 
                OnPropertyChanged(); 
                UpdateStirrupDiameter();
                ResetPreview();
            }
        }

        public int StirrupLegs
        {
            get => _stirrupLegs;
            set { _stirrupLegs = value; OnPropertyChanged(); }
        }

        // 结果显示
        public string RequiredSteelArea
        {
            get => _requiredSteelArea;
            set { _requiredSteelArea = value; OnPropertyChanged(); }
        }

        public string ProvidedSteelArea
        {
            get => _providedSteelArea;
            set { _providedSteelArea = value; OnPropertyChanged(); }
        }

        public string NumberOfBars
        {
            get => _numberOfBars;
            set { _numberOfBars = value; OnPropertyChanged(); }
        }

        public string PhiMnResult
        {
            get => _phiMnResult;
            set { _phiMnResult = value; OnPropertyChanged(); }
        }

        public string MomentRatio
        {
            get => _momentRatio;
            set { _momentRatio = value; OnPropertyChanged(); }
        }

        public string SectionType
        {
            get => _sectionType;
            set { _sectionType = value; OnPropertyChanged(); }
        }

        public string DesignStatus
        {
            get => _designStatus;
            set { _designStatus = value; OnPropertyChanged(); }
        }

        public string ShearResult
        {
            get => _shearResult;
            set { _shearResult = value; OnPropertyChanged(); }
        }

        public string DesignProcess
        {
            get => _designProcess;
            set { _designProcess = value; OnPropertyChanged(); }
        }
        
        public string RebarInfo { get => _rebarInfo; set { _rebarInfo = value; OnPropertyChanged(); } }

        #endregion

        #region 选项列表

        public List<string> BarSizeOptions { get; } = new()
        {
            "#3", "#4", "#5", "#6", "#7", "#8", "#9", "#10", "#11"
        };

        public List<string> StirrupSizeOptions { get; } = new()
        {
            "#3", "#4", "#5", "#6", "#7", "#8"
        };

        public List<int> StirrupLegOptions { get; } = new()
        {
            2, 3, 4
        };

        #endregion

        public ICommand CalculateCommand { get; }

        // 钢筋直径映射
        private Dictionary<string, double> BarDiameters = new Dictionary<string, double>
        {
            ["#3"] = 0.375,
            ["#4"] = 0.500,
            ["#5"] = 0.625,
            ["#6"] = 0.750,
            ["#7"] = 0.875,
            ["#8"] = 1.000,
            ["#9"] = 1.128,
            ["#10"] = 1.270,
            ["#11"] = 1.410,
            ["#14"] = 1.693,
            ["#18"] = 2.257
        };

        // 更新箍筋直径
        private void UpdateStirrupDiameter()
        {
            if (BarDiameters.TryGetValue(SelectedStirrupSize, out double diameter))
            {
                StirrupDiameter = diameter;
            }
        }

        private void CalculateDesign()
        {
            try
            {
                // 创建材料属性
                var material = new BeamDesignCalculations.MaterialProperties(Fc, Fy);

                // 创建截面尺寸
                var section = new BeamDesignCalculations.SectionDimensions(B, H, Cover, StirrupDiameter);

                // 计算弯矩配筋
                double muLbIn = Mu * 12000; // 转换为 lb-in
                var flexureResult = BeamDesignCalculations.CalculateFlexuralReinforcement(
                    muLbIn, material, section, SelectedBarSize);

                // 更新弯矩结果显示
                if (double.IsNaN(flexureResult.RequiredSteelArea)) {
                    RequiredSteelArea = "As,req = 超出承载力";
                    ProvidedSteelArea = "As,prov = 0 in²";
                    NumberOfBars = "钢筋根数: 0";
                    PhiMnResult = "φMn = 0 kip-ft";
                    MomentRatio = "Mu/φMn = 超出范围";
                    SectionType = $"截面类型: {flexureResult.SectionType}";
                    DesignStatus = "设计状态: ✗ 不满足要求 - 弯矩过大";
                } else {
                    RequiredSteelArea = $"As,req = {flexureResult.RequiredSteelArea:F2} in²";
                    ProvidedSteelArea = $"As,prov = {flexureResult.ProvidedSteelArea:F2} in²";
                    NumberOfBars = $"钢筋根数: {flexureResult.NumberOfBars} 根 {SelectedBarSize}";
                    PhiMnResult = $"φMn = {flexureResult.PhiMn / 12000:F2} kip-ft";
                    MomentRatio = $"Mu/φMn = {flexureResult.MomentRatio:F3}";
                    SectionType = $"截面类型: {flexureResult.SectionType}";
                    DesignStatus = flexureResult.IsSufficient 
                        ? "设计状态: ✓ 满足要求" 
                        : "设计状态: ✗ 不满足要求";
                }

                // 计算抗剪配筋
                double vuLb = Vu * 1000; // 转换为 lb
                double nuLb = Nu * 1000; // 转换为 lb
                double stirrupArea = BeamDesignCalculations.BarAreas[SelectedStirrupSize];
                var (avReq, avProv, sRequired, sMax, shearProcess) = BeamDesignCalculations.CalculateShearReinforcement(
                    vuLb, nuLb, material, section, stirrupArea, StirrupLegs);

                // 更新抗剪结果显示
                ShearResult = $"箍筋: {SelectedStirrupSize}, {StirrupLegs}肢\n" +
                             $"Av = {avProv:F3} in², 计算间距 sRequired = {sRequired:F1} in, 最大间距 smax = {sMax:F1} in";

                // 合并设计过程
                var allProcess = new List<string>();
                allProcess.AddRange(flexureResult.DesignProcess);
                allProcess.Add("");
                allProcess.Add("=".PadRight(50, '='));
                allProcess.Add("");
                allProcess.AddRange(shearProcess);

                DesignProcess = string.Join("\n", allProcess);
                
                // Update Preview
                _calculatedBarCount = flexureResult.NumberOfBars;
                UpdateRebarPreview();
            }
            catch (Exception ex)
            {
                DesignProcess = $"计算错误: {ex.Message}";
            }
        }

        private void ResetPreview()
        {
            _calculatedBarCount = 0;
            UpdateRebarPreview();
        }

        private void UpdateRebarPreview()
        {
            try
            {
                RebarLocations.Clear();
                
                if (_calculatedBarCount <= 0)
                {
                    RebarInfo = "";
                    return;
                }

                // Simple parsing for display
                string barSizeNum = SelectedBarSize.Replace("#", "");
                RebarInfo = $"{_calculatedBarCount} No. {barSizeNum} Bars";

                if (BarDiameters.TryGetValue(SelectedBarSize, out double barD))
                {
                    double d_prime = Cover + StirrupDiameter + barD / 2.0;
                    
                    // Available width for rebar centers
                    double xLeft = d_prime;
                    double xRight = B - d_prime;
                    double availableWidth = xRight - xLeft;

                    // User Rule: Center-to-center >= Max(1.5 * db, 1.5 inch)
                    double minCenterSpacing = Math.Max(1.5 * barD, 1.5);
                    
                    // Max bars per row
                    // If 1 bar, width is 0. If N bars, width is (N-1)*s
                    // (N_max - 1) * minCenterSpacing <= availableWidth
                    // N_max - 1 <= availableWidth / minCenterSpacing
                    // N_max <= availableWidth / minCenterSpacing + 1
                    int maxBarsPerRow = (int)(availableWidth / minCenterSpacing) + 1;
                    if (maxBarsPerRow < 1) maxBarsPerRow = 1;

                    // Distribute bars
                    int remainingBars = _calculatedBarCount;
                    int currentRow = 1;
                    double currentY = H - d_prime;
                    double verticalSpacing = 1.0 + barD; // 1" clear spacing assumption

                    while (remainingBars > 0)
                    {
                        int barsInThisRow = Math.Min(remainingBars, maxBarsPerRow);
                        
                        // Layout this row
                        if (barsInThisRow == 1)
                        {
                            RebarLocations.Add(new Point((xLeft + xRight) / 2.0, currentY));
                        }
                        else
                        {
                            // Actual spacing for this row
                            double actualSpacing = availableWidth / (barsInThisRow - 1);
                            for (int i = 0; i < barsInThisRow; i++)
                            {
                                RebarLocations.Add(new Point(xLeft + i * actualSpacing, currentY));
                            }
                        }

                        remainingBars -= barsInThisRow;
                        currentRow++;
                        currentY -= verticalSpacing; // Move up for next row
                    }
                }
            }
            catch
            {
                // Ignore preview errors
            }
        }
    }
}
