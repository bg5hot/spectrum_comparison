using System;
using System.Collections.Generic;

namespace SpectrumComparison
{
    public static class DevelopmentLengthCalculations
    {
        public enum ConcreteType
        {
            Normalweight,
            Lightweight
        }

        public enum CoatingType
        {
            Uncoated,
            EpoxyCoated,
            ZincAndEpoxy
        }

        public enum CastingPosition
        {
            Other,
            TopBar
        }

        public enum SteelGrade
        {
            Grade40,
            Grade60,
            Grade80,
            Grade100
        }

        public enum CalculationMethod
        {
            Detailed,
            QuickCalc
        }

        public enum SpacingCoverCondition
        {
            Favorable,
            OtherCases
        }

        public class InputParameters
        {
            public double Fc { get; set; } = 4000;
            public ConcreteType ConcreteType { get; set; } = ConcreteType.Normalweight;
            public SteelGrade SteelGrade { get; set; } = SteelGrade.Grade60;
            public string BarSize { get; set; } = "#8";
            public CoatingType Coating { get; set; } = CoatingType.Uncoated;
            public CastingPosition CastingPosition { get; set; } = CastingPosition.Other;

            public CalculationMethod Method { get; set; } = CalculationMethod.QuickCalc;
            public SpacingCoverCondition SpacingCoverCondition { get; set; } = SpacingCoverCondition.OtherCases;

            public double Cc { get; set; } = 2.0;
            public double S { get; set; } = 6.0;
            public double Atr { get; set; } = 0.0;
            public double Str { get; set; } = 12.0;
            public int N { get; set; } = 1;

            public bool IsSFRS { get; set; } = false;
            public bool IsYieldZone { get; set; } = false;
            public double AsRequired { get; set; } = 0.0;
            public double AsProvided { get; set; } = 0.0;
        }

        public class DesignResult
        {
            public double LdBase { get; set; }
            public double LdReduced { get; set; }
            public double LdSeismic { get; set; }
            public double LdFinal { get; set; }

            public double PsiT { get; set; }
            public double PsiE { get; set; }
            public double PsiS { get; set; }
            public double PsiG { get; set; }
            public double Lambda { get; set; }
            public double Cb { get; set; }
            public double Ktr { get; set; }
            public double ConfinementTerm { get; set; }
            public double SqrtFc { get; set; }
            public double Db { get; set; }
            public double Fy { get; set; }
            public double ReductionFactor { get; set; }
            public double SeismicFactor { get; set; }

            public List<string> Process { get; set; } = new List<string>();
        }

        public static readonly Dictionary<string, double> BarAreas = new Dictionary<string, double>
        {
            ["#3"] = 0.11, ["#4"] = 0.20, ["#5"] = 0.31, ["#6"] = 0.44,
            ["#7"] = 0.60, ["#8"] = 0.79, ["#9"] = 1.00, ["#10"] = 1.27,
            ["#11"] = 1.56, ["#14"] = 2.25, ["#18"] = 4.00
        };

        public static readonly Dictionary<string, double> BarDiameters = new Dictionary<string, double>
        {
            ["#3"] = 0.375, ["#4"] = 0.500, ["#5"] = 0.625, ["#6"] = 0.750,
            ["#7"] = 0.875, ["#8"] = 1.000, ["#9"] = 1.128, ["#10"] = 1.270,
            ["#11"] = 1.410, ["#14"] = 1.693, ["#18"] = 2.257
        };

        public static double GetBarArea(string barSize)
        {
            return BarAreas.GetValueOrDefault(barSize, 0.79);
        }

        public static double GetBarDiameter(string barSize)
        {
            return BarDiameters.GetValueOrDefault(barSize, 1.000);
        }

        public static double GetFy(SteelGrade grade)
        {
            return grade switch
            {
                SteelGrade.Grade40 => 40000,
                SteelGrade.Grade60 => 60000,
                SteelGrade.Grade80 => 80000,
                SteelGrade.Grade100 => 100000,
                _ => 60000
            };
        }

        public static DesignResult Calculate(InputParameters input)
        {
            var result = new DesignResult();
            var process = new List<string>();

            process.Add("=== ACI 318-19 受拉直钢筋锚固长度计算 ===");
            process.Add("");

            double db = GetBarDiameter(input.BarSize);
            double fy = GetFy(input.SteelGrade);
            result.Db = db;
            result.Fy = fy;

            process.Add("【1. 基本参数】");
            process.Add($"钢筋型号: {input.BarSize}, db = {db:F3} in, Ab = {GetBarArea(input.BarSize):F2} in²");
            process.Add($"钢筋等级: {input.SteelGrade}, fy = {fy:F0} psi");
            process.Add($"混凝土强度 f'c = {input.Fc:F0} psi");
            process.Add($"混凝土类型: {input.ConcreteType}");
            process.Add($"涂层类型: {input.Coating}");
            process.Add($"浇筑位置: {(input.CastingPosition == CastingPosition.TopBar ? "顶筋 (下方混凝土>12in)" : "其他")}");
            process.Add($"计算方法: {(input.Method == CalculationMethod.Detailed ? "精确公式法" : "简化查表法 (Quick Calc)")}");
            process.Add("");

            double sqrtFc = Math.Sqrt(input.Fc);
            if (sqrtFc > 100)
            {
                sqrtFc = 100;
                process.Add($"⚠️ √f'c = {Math.Sqrt(input.Fc):F1} > 100 psi, 已限制为 100 psi (ACI 318-19 25.4.1.4)");
            }
            else
            {
                process.Add($"√f'c = {sqrtFc:F1} psi");
            }
            result.SqrtFc = sqrtFc;
            process.Add("");

            process.Add("【2. 修正系数计算 (Table 25.4.2.5)】");

            double lambda = input.ConcreteType == ConcreteType.Lightweight ? 0.75 : 1.0;
            result.Lambda = lambda;
            process.Add($"轻骨料系数 λ = {lambda:F2} ({(input.ConcreteType == ConcreteType.Lightweight ? "轻骨料" : "普通混凝土")})");

            double psiT = input.CastingPosition == CastingPosition.TopBar ? 1.3 : 1.0;
            result.PsiT = psiT;
            process.Add($"浇筑位置系数 ψt = {psiT:F1} {(psiT > 1.0 ? "(顶筋)" : "")}");

            double psiE;
            if (input.Coating == CoatingType.Uncoated)
            {
                psiE = 1.0;
            }
            else
            {
                double clearCover = input.Cc;
                double clearSpacing = input.S - db;
                if (clearCover < 3 * db || clearSpacing < 6 * db)
                {
                    psiE = 1.5;
                }
                else
                {
                    psiE = 1.2;
                }
            }
            result.PsiE = psiE;
            process.Add($"涂层系数 ψe = {psiE:F1} ({input.Coating})");

            double psiTPsiE = psiT * psiE;
            if (psiTPsiE > 1.7)
            {
                process.Add($"⚠️ ψt × ψe = {psiTPsiE:F2} > 1.7, 乘积限制为 1.7 (Table 25.4.2.5)");
                psiTPsiE = 1.7;
            }
            else
            {
                process.Add($"ψt × ψe = {psiTPsiE:F2} ≤ 1.7 (OK)");
            }

            double psiS = db <= 0.750 ? 0.8 : 1.0;
            result.PsiS = psiS;
            process.Add($"钢筋尺寸系数 ψs = {psiS:F1} ({(db <= 0.750 ? "≤ No.6" : "≥ No.7")})");

            double psiG = input.SteelGrade switch
            {
                SteelGrade.Grade40 => 1.0,
                SteelGrade.Grade60 => 1.0,
                SteelGrade.Grade80 => 1.15,
                SteelGrade.Grade100 => 1.3,
                _ => 1.0
            };
            result.PsiG = psiG;
            process.Add($"钢筋等级系数 ψg = {psiG:F2} ({input.SteelGrade})");
            process.Add("");

            double ldBase;

            if (input.Method == CalculationMethod.Detailed)
            {
                process.Add("【3. 约束参数计算 (25.4.2.4)】");

                double cb = Math.Min(input.Cc + db / 2.0, input.S / 2.0);
                result.Cb = cb;
                process.Add($"cb = min(cc + db/2, s/2) = min({input.Cc:F2} + {db / 2:F3}, {input.S / 2:F2}) = {cb:F2} in");

                double ktr;
                if (input.Atr <= 0 || input.Str <= 0 || input.N <= 0)
                {
                    ktr = 0;
                    process.Add($"Ktr = 0 (简化假设或无横向钢筋)");
                }
                else
                {
                    ktr = (40 * input.Atr) / (input.Str * input.N);
                    process.Add($"Ktr = 40×Atr / (s_tr×n) = 40×{input.Atr:F2} / ({input.Str:F1}×{input.N}) = {ktr:F3}");
                }
                result.Ktr = ktr;

                double confinementTerm = (cb + ktr) / db;
                if (confinementTerm > 2.5)
                {
                    process.Add($"(cb + Ktr)/db = ({cb:F2} + {ktr:F3})/{db:F3} = {confinementTerm:F3} > 2.5, 取 2.5");
                    confinementTerm = 2.5;
                }
                else
                {
                    process.Add($"(cb + Ktr)/db = ({cb:F2} + {ktr:F3})/{db:F3} = {confinementTerm:F3} ≤ 2.5 (OK)");
                }
                result.ConfinementTerm = confinementTerm;
                process.Add("");

                if (fy >= 80000 && input.S < 6 * db)
                {
                    double minKtr = 0.5 * db;
                    if (ktr < minKtr)
                    {
                        process.Add($"⚠️⚠️⚠️ 高强钢筋横向钢筋检查 (ACI 318-19 25.4.2.2) ⚠️⚠️⚠️");
                        process.Add($"fy = {fy:F0} psi ≥ 80000 psi, 钢筋中心距 s = {input.S:F2} in < 6×db = {6 * db:F3} in");
                        process.Add($"要求 Ktr ≥ 0.5×db = {minKtr:F3}, 实际 Ktr = {ktr:F3} < {minKtr:F3}");
                        process.Add($"⚠️ 必须提供横向钢筋使 Ktr ≥ {minKtr:F3}，否则计算结果无效！");
                        process.Add("");
                    }
                    else
                    {
                        process.Add($"高强钢筋横向钢筋检查 (ACI 318-19 25.4.2.2): Ktr = {ktr:F3} ≥ 0.5×db = {minKtr:F3} (OK)");
                        process.Add("");
                    }
                }

                process.Add("【4. 精确公式计算 (Eq. 25.4.2.4a)】");
                ldBase = (3.0 / 40.0) * (fy / (lambda * sqrtFc)) * (psiTPsiE * psiS * psiG / confinementTerm) * db;
                process.Add($"ℓd = (3/40) × (fy / (λ√f'c)) × (ψt·ψe·ψs·ψg / ((cb+Ktr)/db)) × db");
                process.Add($"ℓd = (3/40) × ({fy:F0} / ({lambda:F2}×{sqrtFc:F1})) × ({psiTPsiE:F2}×{psiS:F2}×{psiG:F2} / {confinementTerm:F3}) × {db:F3}");
                process.Add($"ℓd = {ldBase:F1} in.");
            }
            else
            {
                process.Add("【3. 简化查表法 (Table 25.4.2.3)】");

                if (input.SpacingCoverCondition == SpacingCoverCondition.Favorable)
                {
                    process.Add($"间距与保护层条件: 条件良好 (净距≥db, 保护层≥db且配箍筋; 或净距≥2db且保护层≥db)");
                    if (db <= 0.750)
                    {
                        ldBase = (fy * psiTPsiE * psiG) / (25 * lambda * sqrtFc) * db;
                        process.Add($"ℓd = (fy·ψt·ψe·ψg) / (25·λ·√f'c) × db");
                    }
                    else
                    {
                        ldBase = (fy * psiTPsiE * psiG) / (20 * lambda * sqrtFc) * db;
                        process.Add($"ℓd = (fy·ψt·ψe·ψg) / (20·λ·√f'c) × db");
                    }
                }
                else
                {
                    process.Add($"间距与保护层条件: 其他保守情况");
                    if (db <= 0.750)
                    {
                        ldBase = (3 * fy * psiTPsiE * psiG) / (50 * lambda * sqrtFc) * db;
                        process.Add($"ℓd = (3·fy·ψt·ψe·ψg) / (50·λ·√f'c) × db");
                    }
                    else
                    {
                        ldBase = (3 * fy * psiTPsiE * psiG) / (40 * lambda * sqrtFc) * db;
                        process.Add($"ℓd = (3·fy·ψt·ψe·ψg) / (40·λ·√f'c) × db");
                    }
                }

                process.Add($"ℓd = {ldBase:F1} in.");
                result.ConfinementTerm = 0;
                result.Cb = 0;
                result.Ktr = 0;
            }

            result.LdBase = ldBase;
            process.Add("");

            process.Add("【5. 多余钢筋折减 (25.4.10)】");
            double reductionFactor = 1.0;

            if (input.AsRequired > 0 && input.AsProvided > input.AsRequired)
            {
                reductionFactor = input.AsRequired / input.AsProvided;
                process.Add($"As,required = {input.AsRequired:F2} in², As,provided = {input.AsProvided:F2} in²");
                process.Add($"折减系数 = As,req / As,prov = {reductionFactor:F3}");

                if (input.IsSFRS || input.IsYieldZone)
                {
                    reductionFactor = 1.0;
                    string reason = input.IsSFRS ? "属于抗震受力体系(SFRS)" : "位于预期屈服区(要求达到fy)";
                    process.Add($"⚠️ {reason}, 禁止折减 (ACI 318-19 25.4.10.2)");
                }
            }
            else
            {
                process.Add($"未输入所需/提供钢筋面积, 或 As,provided ≤ As,required, 不折减");
                if (input.IsSFRS || input.IsYieldZone)
                {
                    string reason = input.IsSFRS ? "属于抗震受力体系(SFRS)" : "位于预期屈服区(要求达到fy)";
                    process.Add($"{reason}, 禁止折减 (ACI 318-19 25.4.10.2)");
                }
            }

            result.ReductionFactor = reductionFactor;
            double ldReduced = ldBase * reductionFactor;
            result.LdReduced = ldReduced;
            process.Add($"ℓd,reduced = {ldBase:F1} × {reductionFactor:F3} = {ldReduced:F1} in.");
            process.Add("");

            process.Add("【6. 抗震预期屈服区放大】");
            double seismicFactor = 1.0;
            if (input.IsYieldZone)
            {
                seismicFactor = 1.25;
                process.Add($"位于预期屈服区, 乘以 1.25 放大系数 (ACI 318-19 18.10.2.3b)");
            }
            else
            {
                process.Add($"非预期屈服区, 无需放大");
            }

            result.SeismicFactor = seismicFactor;
            double ldSeismic = ldReduced * seismicFactor;
            result.LdSeismic = ldSeismic;
            process.Add($"ℓd,seismic = {ldReduced:F1} × {seismicFactor:F2} = {ldSeismic:F1} in.");
            process.Add("");

            process.Add("【7. 最小锚固长度检查 (25.4.2.1b)】");
            double ldFinal = Math.Max(ldSeismic, 12.0);
            if (ldSeismic < 12.0)
            {
                process.Add($"ℓd = {ldSeismic:F1} < 12 in., 取最小值 12 in.");
            }
            else
            {
                process.Add($"ℓd = {ldSeismic:F1} ≥ 12 in. (OK)");
            }
            result.LdFinal = ldFinal;
            process.Add("");

            process.Add("═══════════════════════════════════════");
            process.Add($"最终锚固长度 ℓd = {Math.Ceiling(ldFinal):F0} in. (向上取整)");
            process.Add($"                   = {Math.Ceiling(ldFinal) / 12.0:F1} ft");
            process.Add("═══════════════════════════════════════");

            result.Process = process;
            return result;
        }
    }
}
