using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace SpectrumComparison
{
    public class DevelopmentLengthViewModel : INotifyPropertyChanged
    {
        private double _fc = 4000;
        private string _concreteType = "普通混凝土";
        private string _steelGrade = "Grade 60";
        private string _barSize = "#8";
        private string _coating = "无涂层";
        private string _castingPosition = "其他";
        private string _calculationMethod = "简化查表法";
        private string _spacingCoverCondition = "其他保守情况";

        private double _cc = 2.0;
        private double _s = 6.0;
        private double _atr = 0.0;
        private double _str = 12.0;
        private int _n = 1;

        private bool _isSFRS = false;
        private bool _isYieldZone = false;
        private double _asRequired = 0.0;
        private double _asProvided = 0.0;

        private string _resultSummary = "";
        private string _coefficientsText = "";
        private string _designProcess = "";

        public event PropertyChangedEventHandler? PropertyChanged;

        public DevelopmentLengthViewModel()
        {
            CalculateCommand = new RelayCommand(Calculate);
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #region Properties

        public double Fc
        {
            get => _fc;
            set { _fc = value; OnPropertyChanged(); }
        }

        public string ConcreteType
        {
            get => _concreteType;
            set { _concreteType = value; OnPropertyChanged(); }
        }

        public string SteelGrade
        {
            get => _steelGrade;
            set { _steelGrade = value; OnPropertyChanged(); }
        }

        public string BarSize
        {
            get => _barSize;
            set { _barSize = value; OnPropertyChanged(); }
        }

        public string Coating
        {
            get => _coating;
            set { _coating = value; OnPropertyChanged(); }
        }

        public string CastingPosition
        {
            get => _castingPosition;
            set { _castingPosition = value; OnPropertyChanged(); }
        }

        public string CalculationMethod
        {
            get => _calculationMethod;
            set
            {
                _calculationMethod = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsDetailedMethod));
                OnPropertyChanged(nameof(IsQuickCalcMethod));
            }
        }

        public string SpacingCoverCondition
        {
            get => _spacingCoverCondition;
            set { _spacingCoverCondition = value; OnPropertyChanged(); }
        }

        public double Cc
        {
            get => _cc;
            set { _cc = value; OnPropertyChanged(); }
        }

        public double S
        {
            get => _s;
            set { _s = value; OnPropertyChanged(); }
        }

        public double Atr
        {
            get => _atr;
            set { _atr = value; OnPropertyChanged(); }
        }

        public double Str
        {
            get => _str;
            set { _str = value; OnPropertyChanged(); }
        }

        public int N
        {
            get => _n;
            set { _n = value; OnPropertyChanged(); }
        }

        public bool IsSFRS
        {
            get => _isSFRS;
            set { _isSFRS = value; OnPropertyChanged(); }
        }

        public bool IsYieldZone
        {
            get => _isYieldZone;
            set { _isYieldZone = value; OnPropertyChanged(); }
        }

        public double AsRequired
        {
            get => _asRequired;
            set { _asRequired = value; OnPropertyChanged(); }
        }

        public double AsProvided
        {
            get => _asProvided;
            set { _asProvided = value; OnPropertyChanged(); }
        }

        public bool IsDetailedMethod => CalculationMethod == "精确公式法";

        public bool IsQuickCalcMethod => CalculationMethod == "简化查表法";

        public string ResultSummary
        {
            get => _resultSummary;
            set { _resultSummary = value; OnPropertyChanged(); }
        }

        public string CoefficientsText
        {
            get => _coefficientsText;
            set { _coefficientsText = value; OnPropertyChanged(); }
        }

        public string DesignProcess
        {
            get => _designProcess;
            set { _designProcess = value; OnPropertyChanged(); }
        }

        #endregion

        #region Options

        public List<string> ConcreteTypeOptions { get; } = new()
        {
            "普通混凝土", "轻骨料混凝土"
        };

        public List<string> SteelGradeOptions { get; } = new()
        {
            "Grade 40", "Grade 60", "Grade 80", "Grade 100"
        };

        public List<string> BarSizeOptions { get; } = new()
        {
            "#3", "#4", "#5", "#6", "#7", "#8", "#9", "#10", "#11", "#14", "#18"
        };

        public List<string> CoatingOptions { get; } = new()
        {
            "无涂层", "环氧涂层", "锌+环氧双涂层"
        };

        public List<string> CastingPositionOptions { get; } = new()
        {
            "其他", "顶筋"
        };

        public List<string> CalculationMethodOptions { get; } = new()
        {
            "简化查表法", "精确公式法"
        };

        public List<string> SpacingCoverConditionOptions { get; } = new()
        {
            "条件良好", "其他保守情况"
        };

        #endregion

        public ICommand CalculateCommand { get; }

        private void Calculate()
        {
            try
            {
                var input = new DevelopmentLengthCalculations.InputParameters
                {
                    Fc = Fc,
                    ConcreteType = ConcreteType == "轻骨料混凝土"
                        ? DevelopmentLengthCalculations.ConcreteType.Lightweight
                        : DevelopmentLengthCalculations.ConcreteType.Normalweight,
                    SteelGrade = SteelGrade switch
                    {
                        "Grade 40" => DevelopmentLengthCalculations.SteelGrade.Grade40,
                        "Grade 60" => DevelopmentLengthCalculations.SteelGrade.Grade60,
                        "Grade 80" => DevelopmentLengthCalculations.SteelGrade.Grade80,
                        "Grade 100" => DevelopmentLengthCalculations.SteelGrade.Grade100,
                        _ => DevelopmentLengthCalculations.SteelGrade.Grade60
                    },
                    BarSize = BarSize,
                    Coating = Coating switch
                    {
                        "环氧涂层" => DevelopmentLengthCalculations.CoatingType.EpoxyCoated,
                        "锌+环氧双涂层" => DevelopmentLengthCalculations.CoatingType.ZincAndEpoxy,
                        _ => DevelopmentLengthCalculations.CoatingType.Uncoated
                    },
                    CastingPosition = CastingPosition == "顶筋"
                        ? DevelopmentLengthCalculations.CastingPosition.TopBar
                        : DevelopmentLengthCalculations.CastingPosition.Other,
                    Method = CalculationMethod == "精确公式法"
                        ? DevelopmentLengthCalculations.CalculationMethod.Detailed
                        : DevelopmentLengthCalculations.CalculationMethod.QuickCalc,
                    SpacingCoverCondition = SpacingCoverCondition == "条件良好"
                        ? DevelopmentLengthCalculations.SpacingCoverCondition.Favorable
                        : DevelopmentLengthCalculations.SpacingCoverCondition.OtherCases,
                    Cc = Cc,
                    S = S,
                    Atr = Atr,
                    Str = Str,
                    N = N,
                    IsSFRS = IsSFRS,
                    IsYieldZone = IsYieldZone,
                    AsRequired = AsRequired,
                    AsProvided = AsProvided
                };

                var result = DevelopmentLengthCalculations.Calculate(input);

                ResultSummary = $"ℓd = {Math.Ceiling(result.LdFinal):F0} in. ({Math.Ceiling(result.LdFinal) / 12.0:F1} ft)";

                CoefficientsText = $"ψt = {result.PsiT:F2}\n" +
                                   $"ψe = {result.PsiE:F2}\n" +
                                   $"ψs = {result.PsiS:F2}\n" +
                                   $"ψg = {result.PsiG:F2}\n" +
                                   $"λ  = {result.Lambda:F2}\n" +
                                   $"db = {result.Db:F3} in\n" +
                                   $"fy = {result.Fy:F0} psi";

                if (result.ConfinementTerm > 0)
                {
                    CoefficientsText += $"\n(cb+Ktr)/db = {result.ConfinementTerm:F3}";
                }

                if (result.ReductionFactor < 1.0)
                {
                    CoefficientsText += $"\n折减系数 = {result.ReductionFactor:F3}";
                }

                if (result.SeismicFactor > 1.0)
                {
                    CoefficientsText += $"\n抗震放大 = {result.SeismicFactor:F2}";
                }

                DesignProcess = string.Join(Environment.NewLine, result.Process);
            }
            catch (Exception ex)
            {
                DesignProcess = $"计算错误: {ex.Message}\n\n{ex.StackTrace}";
            }
        }
    }
}
