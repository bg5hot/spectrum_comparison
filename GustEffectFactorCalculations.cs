using System;
using System.Collections.Generic;

namespace SpectrumComparison
{
    public static class GustEffectFactorCalculations
    {
        // Table 26.11-1 场地类别常量 (公制 SI Units)
        private static readonly Dictionary<string, ExposureConstants> TerrainConstants = new()
        {
            ["B"] = new ExposureConstants { AlphaBar = 0.25, BBar = 0.45, C = 0.30, LBar = 97.54, EpsilonBar = 0.3333, Zmin = 9.14 },
            ["C"] = new ExposureConstants { AlphaBar = 0.1538, BBar = 0.65, C = 0.20, LBar = 152.40, EpsilonBar = 0.20, Zmin = 4.57 },
            ["D"] = new ExposureConstants { AlphaBar = 0.1111, BBar = 0.80, C = 0.15, LBar = 198.12, EpsilonBar = 0.125, Zmin = 2.13 }
        };

        public static GustEffectResult CalculateGustEffectFactor(
            string exposure, double h, double b, double l, double v, double n1, double beta)
        {
            var result = new GustEffectResult();
            var process = new List<string>();

            process.Add("【ASCE 7-16 第26.11节 阵风响应因子详细计算报告】");
            process.Add($"[输入] 场地:{exposure}, 高度h:{h}m, 宽度B:{b}m, 深度L:{l}m, 风速V:{v}m/s, 频率n₁:{n1}Hz, 阻尼比β:{beta}");
            process.Add("");

            //
            if (!TerrainConstants.TryGetValue(exposure.ToUpper(), out var tc)) //验证场地类别在表格26.11-1中并传给变量tc
            {
                result.ErrorMessage = "错误：无效的场地类别。";
                return result;
            }

            // --- 步骤0: 输入验证 ---
            if (h <= 0 || b <= 0 || l <= 0 || v <= 0 || n1 <= 0 || beta < 0 || beta >= 1.0)
            {
                result.ErrorMessage = "错误：输入参数必须为正数，且阻尼比 β 必须在 0 到 1 之间。";
                return result;
            }

            // --- 步骤1: 判定结构类型 ---
            bool isRigid = n1 >= 1.0;
            result.IsRigid = isRigid;
            process.Add($"【1. 结构类型判定】");
            process.Add($"  基本频率 n₁ = {n1} Hz");
            process.Add($"  判定标准: n₁ ≥ 1.0 Hz 为刚性，否则为柔性。");
            process.Add($"  结果: {(isRigid ? "刚性建筑" : "柔性建筑")}");
            process.Add("");

            // --- 步骤2: 计算场地相关参数 ---
            double zBar = Math.Max(0.6 * h, tc.Zmin); // 等效高度 z̄ = max(0.6h, z_min)
            double iZBar = tc.C * Math.Pow(10.0 / zBar, 1.0 / 6.0); // Eq 26.11-7
            double lZBar = tc.LBar * Math.Pow(zBar / 10.0, tc.EpsilonBar); // Eq 26.11-9

            process.Add($"【2. 计算场地/高度影响参数】");
            process.Add($"  等效高度 z̄ = max(0.6h, z_min) = {zBar:F2} m");
            process.Add($"  湍流强度 I_z̄ = c × (10/z̄)^(1/6) = {tc.C} × (10/{zBar:F2})^(1/6) = {iZBar:F4}");
            process.Add($"  积分尺度 L_z̄ = ℓ × (z̄/10)^ε = {tc.LBar} × ({zBar:F2}/10)^{tc.EpsilonBar:F3} = {lZBar:F2} m");
            process.Add("");

            // --- 步骤3: 计算背景响应 Q ---
            double q = Math.Sqrt(1.0 / (1.0 + 0.63 * Math.Pow((b + h) / lZBar, 0.63))); // Eq 26.11-8
            result.Q = q;
            process.Add($"【3. 计算背景响应 Q】");
            process.Add($"  Q = √[1 / (1 + 0.63 × ((B+h)/L_z̄)^0.63)]");
            process.Add($"  Q = √[1 / (1 + 0.63 × (({b}+{h})/{lZBar:F2})^0.63)] = {q:F3}");
            process.Add("");

            double gQ = 3.4;
            double gV = 3.4;

            if (isRigid)
            {
                // --- 步骤4 (刚性): 计算 G ---
                double num = 1 + 0.7 * gQ * iZBar * q;
                double den = 1 + 0.7 * gV * iZBar;
                double g = 0.925 * (num / den);
                result.G = g;

                process.Add($"【4. 计算刚性建筑阵风因子 G】");
                process.Add($"  G = 0.925 × [(1 + 0.7 × gQ × I_z̄ × Q) / (1 + 0.7 × gv × I_z̄)]");
                process.Add($"  G = 0.925 × [(1 + 0.7×3.4×{iZBar:F4}×{q:F4}) / (1 + 0.7×3.4×{iZBar:F3})]");
                process.Add($"  G = {g:F3}");
            }
            else
            {
                // --- 步骤4 (柔性): 动力计算 ---
                process.Add($"【4. 柔性结构动力计算流程】");

                // 4a. Vz_bar
                double vZBar = tc.BBar * Math.Pow(zBar / 10.0, tc.AlphaBar) * v; 
                process.Add($"  (a) z̄ 高度处的小时平均风速 V̄_z̄ = b̄ × (z̄/10)^ᾱ × V");
                process.Add($"      V̄_z̄ = {tc.BBar} × ({zBar:F2}/10)^{tc.AlphaBar:F3} × {v} = {vZBar:F2} m/s");

                // 4b. Rn
                double n1Reduced = n1 * lZBar / vZBar; // 折减频率 N₁ = n₁ × L_z̄ / V̄_z̄ // Eq 26.11-14
                double rn = 7.47 * n1Reduced / Math.Pow(1 + 10.3 * n1Reduced, 5.0 / 3.0); // Eq 26.11-13 //物理含义 能量谱密度 Rn 是在折减频率 N₁ 下的能量谱密度
                process.Add($"  (b) 能量谱密度 Rn = 7.47 × N₁ / (1 + 10.3 × N₁)^(5/3)");
                process.Add($"      Rn = {rn:F4} (基于折减频率 N₁ = {n1Reduced:F4})");

                // 4c. 分量响应 Rh, Rb, Rl
                double etaH = 4.6 * n1 * h / vZBar; 
                double rh = CalculateRl_Detailed(etaH);
                double etaB = 4.6 * n1 * b / vZBar;
                double rb = CalculateRl_Detailed(etaB);
                double etaL = 15.4 * n1 * l / vZBar; // 注意 15.4
                double rl = CalculateRl_Detailed(etaL);

                process.Add($"  (c) 分量响应 R_l 计算:");
                process.Add($"      Rh (高度项, η={etaH:F3}) = {rh:F3}");
                process.Add($"      Rb (宽度项, η={etaB:F3}) = {rb:F3}");
                process.Add($"      Rl (深度项, η={etaL:F3}) = {rl:F3}");

                // 4d. 共振因子 R
                double rSquared = (1.0 / beta) * rn * rh * rb * (0.53 + 0.47 * rl);
                double r = Math.Sqrt(rSquared);
                result.R = r;
                process.Add($"  (d) 共振因子 R = √[(1/β) × Rn × Rh × Rb × (0.53 + 0.47 × Rl)]");
                process.Add($"      R = √[(1/{beta:F3}) × {rn:F3} × {rh:F3} × {rb:F3} × (0.53 + 0.47×{rl:F3})] = {r:F3}");

                // 4e. gR
                double lnTerm = Math.Log(3600.0 * n1);
                double gR = Math.Sqrt(2.0 * lnTerm) + 0.577 / Math.Sqrt(2.0 * lnTerm);
                result.GR = gR;
                process.Add($"  (e) 峰值因子 gR = √(2ln(3600n₁)) + 0.577/√(2ln(3600n₁)) = {gR:F3}");

                // 4f. Gf
                double totalRMS = Math.Sqrt(gQ * gQ * q * q + gR * gR * rSquared);
                double gf = 0.925 * ((1 + 1.7 * iZBar * totalRMS) / (1 + 1.7 * gV * iZBar));
                result.Gf = gf;

                process.Add($"  (f) 最终合成阵风因子 Gf = 0.925 × [(1 + 1.7 × I_z̄ × √(gQ²Q² + gR²R²)) / (1 + 1.7 × gv × I_z̄)]");
                process.Add($"      分子根号项 = √[({gQ}×{q:F3})² + ({gR:F3}×{r:F3})²] = {totalRMS:F3}");
                process.Add($"      Gf = 0.925 × [(1 + 1.7×{iZBar:F3}×{totalRMS:F3}) / (1 + 1.7×3.4×{iZBar:F3})]");
                process.Add($"      Gf = {gf:F3}");
            }

            process.Add("");
            process.Add("【计算完成】");
            result.Process = process;
            return result;
        }

        private static double CalculateRl_Detailed(double eta)
        {
            if (eta <= 0.0001) return 1.0;
            // 严格执行标准公式: 1/eta - 1/(2*eta^2)*(1-e^-2eta)
            return (1.0 / eta) - (1.0 / (2.0 * eta * eta)) * (1.0 - Math.Exp(-2.0 * eta));
        }
    }

    public class ExposureConstants
    {
        public double AlphaBar, BBar, C, LBar, EpsilonBar, Zmin;
    }

    public class GustEffectResult
    {
        public bool IsRigid;
        public double? G, Gf, Q, R, GR; // G 刚性 Gust Effect Factor, Gf 柔性 Gust Effect Factor, Q  Gust Effect Factor, R 共振因子, GR 峰值因子
        public List<string> Process { get; set; } = new();
        public string? ErrorMessage { get; set; }
    }
}