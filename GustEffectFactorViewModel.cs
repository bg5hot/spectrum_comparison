using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace SpectrumComparison
{
    public class GustEffectFactorViewModel : INotifyPropertyChanged
    {
        // 输入参数
        private string _exposure = "C";
        private double _buildingHeight = 120.0;
        private double _buildingWidth = 40.0;
        private double _buildingDepth = 40.0;
        private double _windSpeed = 45.0;
        private double _naturalFrequency = 0.25;
        private double _beta = 0.01;

        // 输出结果
        private string _buildingType = "-";
        private string _gustEffectFactor = "-";
        private string _gValue = "-";
        private string _gfValue = "-";
        private string _zBarValue = "-";
        private string _vZBarValue = "-";
        private string _iZBarValue = "-";
        private string _qValue = "-";
        private string _rValue = "-";
        private string _processText = "";

        public event PropertyChangedEventHandler? PropertyChanged;

        public GustEffectFactorViewModel()
        {
            CalculateCommand = new RelayCommand(CalculateGustEffectFactor);
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // 输入属性
        public string Exposure
        {
            get => _exposure;
            set { _exposure = value; OnPropertyChanged(); }
        }

        public double BuildingHeight
        {
            get => _buildingHeight;
            set { _buildingHeight = value; OnPropertyChanged(); }
        }

        public double BuildingWidth
        {
            get => _buildingWidth;
            set { _buildingWidth = value; OnPropertyChanged(); }
        }

        public double BuildingDepth
        {
            get => _buildingDepth;
            set { _buildingDepth = value; OnPropertyChanged(); }
        }

        public double WindSpeed
        {
            get => _windSpeed;
            set { _windSpeed = value; OnPropertyChanged(); }
        }

        public double NaturalFrequency
        {
            get => _naturalFrequency;
            set { _naturalFrequency = value; OnPropertyChanged(); }
        }

        public double Beta
        {
            get => _beta;
            set { _beta = value; OnPropertyChanged(); }
        }

        // 输出属性
        public string BuildingType
        {
            get => _buildingType;
            set { _buildingType = value; OnPropertyChanged(); }
        }

        public string GustEffectFactor
        {
            get => _gustEffectFactor;
            set { _gustEffectFactor = value; OnPropertyChanged(); }
        }

        public string GValue
        {
            get => _gValue;
            set { _gValue = value; OnPropertyChanged(); }
        }

        public string GfValue
        {
            get => _gfValue;
            set { _gfValue = value; OnPropertyChanged(); }
        }

        public string ZBarValue
        {
            get => _zBarValue;
            set { _zBarValue = value; OnPropertyChanged(); }
        }

        public string VZBarValue
        {
            get => _vZBarValue;
            set { _vZBarValue = value; OnPropertyChanged(); }
        }

        public string IZBarValue
        {
            get => _iZBarValue;
            set { _iZBarValue = value; OnPropertyChanged(); }
        }

        public string QValue
        {
            get => _qValue;
            set { _qValue = value; OnPropertyChanged(); }
        }

        public string RValue
        {
            get => _rValue;
            set { _rValue = value; OnPropertyChanged(); }
        }

        public string ProcessText
        {
            get => _processText;
            set { _processText = value; OnPropertyChanged(); }
        }

        // 选项
        public List<string> ExposureOptions { get; } = new() { "B", "C", "D" };

        public ICommand CalculateCommand { get; }

        private void CalculateGustEffectFactor()
        {
            try
            {
                var result = GustEffectFactorCalculations.CalculateGustEffectFactor(
                    Exposure, BuildingHeight, BuildingWidth, BuildingDepth, WindSpeed, NaturalFrequency, Beta); 

                if (!string.IsNullOrEmpty(result.ErrorMessage))
                {
                    BuildingType = $"错误: {result.ErrorMessage}";
                    GustEffectFactor = "-";
                    ProcessText = result.ErrorMessage;
                    return;
                }

                // 1. 更新建筑类型（手动根据 IsRigid 判定）
                BuildingType = result.IsRigid ? $"刚性结构 (n₁={NaturalFrequency:F3}Hz ≥ 1Hz)" : $"柔性结构 (n₁={NaturalFrequency:F3}Hz < 1Hz)";

                // 2. 更新风振系数显示
                if (result.IsRigid)
                {
                    GustEffectFactor = $"G = {result.G:F3}";
                    GValue = $"G = {result.G:F3}";
                    GfValue = "-";
                    RValue = "R = - (刚性忽略)";
                }
                else
                {
                    GustEffectFactor = $"G_f = {result.Gf:F3}";
                    GValue = "-";
                    GfValue = $"G_f = {result.Gf:F3}";
                    RValue = $"R = {result.R:F3}";
                }

                // 3. 更新中间结果显示
                // 注意：这些属性现在从修改后的 result 对象中获取
                QValue = $"Q = {result.Q:F3}";

                // 4. 更新详细计算过程
                ProcessText = result.Process != null ? string.Join("\r\n", result.Process) : "";
            }
            catch (Exception ex)
            {
                BuildingType = "计算错误";
                GustEffectFactor = "-";
                ProcessText = $"计算过程中发生错误:\n{ex.Message}";
            }
        }
    }
}
