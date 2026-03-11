using System;
using System.Collections.Generic;

namespace SpectrumComparison
{
    /// <summary>
    /// ACI 318 梁截面设计计算
    /// </summary>
    public static class BeamDesignCalculations
    {
        // 材料属性
        public class MaterialProperties
        {
            public double Fc { get; set; } // 混凝土抗压强度 (psi)
            public double Fy { get; set; } // 钢筋屈服强度 (psi)
            public double Es { get; set; } = 29000000; // 钢筋弹性模量 (psi)

            public MaterialProperties(double fc, double fy)
            {
                Fc = fc;
                Fy = fy;
            }
        }

        // 截面尺寸
        public class SectionDimensions
        {
            public double B { get; set; } // 截面宽度 (in)
            public double H { get; set; } // 截面高度 (in)
            public double Cover { get; set; } // 保护层厚度 (in)
            public double StirrupDiameter { get; set; } // 箍筋直径 (in)
            public double MainBarDiameter { get; set; } // 主筋直径 (in)

            public double D // 有效高度
            {
                get { return H - Cover - StirrupDiameter - MainBarDiameter / 2; }
            }

            public SectionDimensions(double b, double h, double cover = 1.5, double stirrupDia = 0.5, double mainBarDia = 1.0)
            {
                B = b;
                H = h;
                Cover = cover;
                StirrupDiameter = stirrupDia;
                MainBarDiameter = mainBarDia;
            }
        }

        // 设计结果
        public class DesignResult
        {
            public double RequiredSteelArea { get; set; } // 所需钢筋面积 (in²)
            public double ProvidedSteelArea { get; set; } // 提供钢筋面积 (in²)
            public int NumberOfBars { get; set; } // 钢筋根数
            public double BarDiameter { get; set; } // 钢筋直径 (in)
            public double NeutralAxisDepth { get; set; } // 中性轴深度 (in)
            public double StrainInSteel { get; set; } // 钢筋应变
            public double Phi { get; set; } // 强度折减系数
            public double PhiMn { get; set; } // 设计弯矩承载力 (lb-in)
            public double MomentRatio { get; set; } // 弯矩比 Mu/φMn
            public string SectionType { get; set; } = ""; // 截面类型 (Tension-controlled/Compression-controlled/Transition)
            public bool IsSufficient { get; set; } // 是否满足要求
            public List<string> DesignProcess { get; set; } = new List<string>(); // 设计过程
        }

        // 钢筋面积表 (in²)
        public static readonly Dictionary<string, double> BarAreas = new Dictionary<string, double>
        {
            ["#3"] = 0.11,
            ["#4"] = 0.20,
            ["#5"] = 0.31,
            ["#6"] = 0.44,
            ["#7"] = 0.60,
            ["#8"] = 0.79,
            ["#9"] = 1.00,
            ["#10"] = 1.27,
            ["#11"] = 1.56,
            ["#14"] = 2.25,
            ["#18"] = 4.00
        };

        /// <summary>
        /// 计算所需受拉钢筋面积 (单筋矩形截面)
        /// </summary>
        public static DesignResult CalculateFlexuralReinforcement(
            double mu, // 设计弯矩 (lb-in)
            MaterialProperties material,
            SectionDimensions section,
            string barSize = "#8") // 钢筋规格
        {
            var result = new DesignResult();
            var process = new List<string>();

            process.Add("=== ACI 318 梁截面弯矩设计 ===");
            process.Add($"");
            process.Add("【输入参数】");
            process.Add($"设计弯矩 Mu = {mu / 12000:F2} kip-ft = {mu:F0} lb-in");
            process.Add($"混凝土抗压强度 f'c = {material.Fc:F0} psi");
            process.Add($"钢筋屈服强度 fy = {material.Fy:F0} psi");
            process.Add($"截面宽度 b = {section.B:F2} in");
            process.Add($"截面高度 h = {section.H:F2} in");
            process.Add($"有效高度 d = {section.D:F2} in");
            process.Add($"");

            // Step 1: 计算混凝土等效矩形应力块参数
            double beta1;
            if (material.Fc <= 4000)
                beta1 = 0.85;
            else if (material.Fc <= 8000)
                beta1 = 0.85 - 0.05 * (material.Fc - 4000) / 1000;
            else
                beta1 = 0.65;

            process.Add("【步骤1: 确定等效矩形应力块参数】");
            process.Add($"β1 = {beta1:F3}");
            process.Add($"");

            // Step 2: 假设为受拉控制截面 (φ = 0.90)
            double phi = 0.90;
            process.Add("【步骤2: 初步假设】");
            process.Add("假设为受拉控制截面，φ = 0.90");
            process.Add($"所需名义弯矩 Mn = Mu/φ = {mu / phi / 12000:F2} kip-ft"); 
            process.Add($"");

            // Step 3: 计算所需钢筋面积
            // Rn = Mu / (φ * b * d²) //受弯承载力系数 Nominal Strength Coefficient
            double rn = mu / (phi * section.B * section.D * section.D);
            process.Add("【步骤3: 计算钢筋面积】");
            process.Add($"受弯承载力系数Rn = Mu/(φ·b·d²) = {rn:F3} psi");

            // 检查是否超过最大承载力
            double maxrho = 0.85 * material.Fc / material.Fy * 0.31875;
            double maxRn = maxrho * material.Fy * (1 -0.59 * maxrho * material.Fy / material.Fc);
            if (rn > maxRn) {
                process.Add($"警告：设计弯矩过大，超过单筋矩形截面最大承载力");
                process.Add($"最大允许 Rn = {maxRn:F3} psi");
                process.Add($"当前计算 Rn = {rn:F3} psi");
                process.Add($"建议：增大截面尺寸或设置受压钢筋");
                process.Add($"");
                
                result.RequiredSteelArea = double.NaN;
                result.ProvidedSteelArea = 0;
                result.NumberOfBars = 0;
                result.BarDiameter = 0;
                result.NeutralAxisDepth = 0;
                result.StrainInSteel = 0;
                result.Phi = 0;
                result.PhiMn = 0;
                result.MomentRatio = double.PositiveInfinity;
                result.SectionType = "超出承载力范围";
                result.IsSufficient = false;
                result.DesignProcess = process;
                return result;
            }

            // ρ = (0.85·f'c/fy)·[1 - √(1 - 2·Rn/(0.85·f'c))]
            double rho = (0.85 * material.Fc / material.Fy) * 
                        (1 - Math.Sqrt(1 - 2 * rn / (0.85 * material.Fc)));
            process.Add($"配筋率 ρ = {rho:F4}");

            // As = ρ * b * d
            double asRequired = rho * section.B * section.D;
            result.RequiredSteelArea = asRequired;
            process.Add($"所需钢筋面积 As = ρ·b·d = {asRequired:F2} in²");
            process.Add($"");

            // 检查钢筋面积是否合理
            if (double.IsNaN(asRequired) || asRequired < 0) {
                process.Add($"警告：钢筋面积计算异常");
                process.Add($"建议：检查输入参数或增大截面尺寸");
                process.Add($"");
                
                result.RequiredSteelArea = double.NaN;
                result.ProvidedSteelArea = 0;
                result.NumberOfBars = 0;
                result.BarDiameter = 0;
                result.NeutralAxisDepth = 0;
                result.StrainInSteel = 0;
                result.Phi = 0;
                result.PhiMn = 0;
                result.MomentRatio = double.PositiveInfinity;
                result.SectionType = "计算异常";
                result.IsSufficient = false;
                result.DesignProcess = process;
                return result;
            }

            // Step 4: 选择钢筋
            double barArea = BarAreas[barSize];
            int numBars = (int)Math.Ceiling(asRequired / barArea);
            double asProvided = numBars * barArea;
            result.ProvidedSteelArea = asProvided;
            result.NumberOfBars = numBars;
            result.BarDiameter = GetBarDiameter(barSize);

            process.Add("【步骤4: 钢筋选择】");
            process.Add($"选用 {numBars} 根 {barSize} 钢筋");
            process.Add($"单根面积 = {barArea:F2} in²");
            process.Add($"提供总面积 = {asProvided:F2} in²");
            process.Add($"");

            // Step 5: 验算截面承载力
            process.Add("【步骤5: 截面承载力验算】");

            // a = As·fy / (0.85·f'c·b)
            double a = asProvided * material.Fy / (0.85 * material.Fc * section.B);
            process.Add($"等效受压区高度 a = As·fy/(0.85·f'c·b) = {a:F3} in");

            // c = a / β1
            double c = a / beta1;
            result.NeutralAxisDepth = c;
            process.Add($"中性轴深度 c = a/β1 = {c:F3} in");

            // εt = (d - c) / c · 0.003
            double et = (section.D - c) / c * 0.003;
            result.StrainInSteel = et;
            process.Add($"钢筋应变 εt = (d-c)/c · 0.003 = {et:F6}");

            // 确定截面类型和φ值
            if (et >= 0.005)
            {
                result.SectionType = "受拉控制 (Tension-controlled)";
                phi = 0.90;
                process.Add("εt ≥ 0.005，属于受拉控制截面");
            }
            else if (et <= 0.002)
            {
                result.SectionType = "受压控制 (Compression-controlled)";
                phi = 0.65;
                process.Add("εt ≤ 0.002，属于受压控制截面");
            }
            else
            {
                result.SectionType = "过渡区 (Transition)";
                phi = 0.65 + (et - 0.002) * (0.90 - 0.65) / (0.005 - 0.002);
                process.Add("0.002 < εt < 0.005，属于过渡区截面");
            }
            result.Phi = phi;
            process.Add($"强度折减系数 φ = {phi:F3}");

            // φMn = φ · As · fy · (d - a/2)
            double phiMn = phi * asProvided * material.Fy * (section.D - a / 2);
            result.PhiMn = phiMn;
            process.Add($"");
            process.Add("【承载力计算】");
            process.Add($"φMn = φ · As · fy · (d - a/2)");
            process.Add($"φMn = {phi:F3} × {asProvided:F2} × {material.Fy:F0} × ({section.D:F2} - {a:F3}/2)");
            process.Add($"φMn = {phiMn / 12000:F2} kip-ft = {phiMn:F0} lb-in");
            process.Add($"");

            // 验算
            double momentRatio = mu / phiMn;
            result.MomentRatio = momentRatio;
            result.IsSufficient = momentRatio <= 1.0;

            process.Add("【验算结果】");
            process.Add($"Mu/φMn = {momentRatio:F3}");
            if (result.IsSufficient)
            {
                process.Add($"✓ 满足要求 (Mu/φMn ≤ 1.0)");
            }
            else
            {
                process.Add($"✗ 不满足要求 (Mu/φMn > 1.0)，需要增加配筋");
            }
            process.Add($"");

            // Step 6: 最小配筋率验算
            process.Add("【步骤6: 最小配筋率验算】");
            double asMin1 = 3 * Math.Sqrt(material.Fc) / material.Fy * section.B * section.D;
            double asMin2 = 200 * section.B * section.D / material.Fy;
            double asMin = Math.Max(asMin1, asMin2);
            process.Add($"As,min1 = 3·√f'c/fy · b · d = {asMin1:F3} in²");
            process.Add($"As,min2 = 200·b·d/fy = {asMin2:F3} in²");
            process.Add($"As,min = {asMin:F3} in²");
            if (asProvided >= asMin)
            {
                process.Add($"✓ 满足最小配筋率要求 (As = {asProvided:F2} in² ≥ As,min = {asMin:F3} in²)");
            }
            else
            {
                process.Add($"✗ 不满足最小配筋率要求，需要调整");
            }
            process.Add($"");

            // Step 7: 最大配筋率验算 (基于ACI 318 表21.2.2的净拉应变要求)
            process.Add("【步骤7: 最大配筋率验算】");
            process.Add("根据ACI 318表21.2.2，受拉控制截面要求最外层受拉钢筋净拉应变 ≥ 0.005");
            
            // 计算当前配筋下的钢筋应变
            double netTensileStrain = (section.D - c) / c * 0.003;
            process.Add($"当前最外层受拉钢筋净拉应变 εt = {netTensileStrain:F4}");
            
            // 计算受拉控制截面的最大配筋率
            double cMaxTensionControl = 0.003 / (0.003 + 0.005) * section.D;
            double aMaxTensionControl = beta1 * cMaxTensionControl;
            double asMaxTensionControl = 0.85 * material.Fc * aMaxTensionControl * section.B / material.Fy;
            
            process.Add($"受拉控制截面最大中性轴深度 cmax,tension = {cMaxTensionControl:F3} in");
            process.Add($"受拉控制截面最大配筋 As,max,tension = {asMaxTensionControl:F3} in²");
            
            if (netTensileStrain >= 0.005)
            {
                process.Add($"✓ 满足受拉控制截面要求 (εt = {netTensileStrain:F4} ≥ 0.005)");
                if (asProvided <= asMaxTensionControl)
                {
                    process.Add($"✓ 满足最大配筋率要求 (As = {asProvided:F2} in² ≤ As,max = {asMaxTensionControl:F3} in²)");
                }
                else
                {
                    process.Add($"✗ 超过受拉控制截面最大配筋率，需要调整");
                }
            }
            else if (netTensileStrain >= 0.002)
            {
                process.Add($"⚠ 截面处于过渡区 (0.002 ≤ εt = {netTensileStrain:F4} < 0.005)");
                process.Add($"建议增加配筋以达到受拉控制截面要求");
            }
            else
            {
                process.Add($"✗ 截面为受压控制 (εt = {netTensileStrain:F4} < 0.002)");
                process.Add($"需要增大截面尺寸或调整配筋");
            }

            result.DesignProcess = process;
            return result;
        }

        /// <summary>
        /// 获取钢筋直径
        /// </summary>
        private static double GetBarDiameter(string barSize)
        {
            var diameters = new Dictionary<string, double>
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
            return diameters.GetValueOrDefault(barSize, 1.0);
        }

        /// <summary>
        /// 计算抗剪配筋
        /// </summary>
        public static (double avRequired, double avProvided, double sRequired, double sMax, List<string> process) 
            CalculateShearReinforcement(
            double vu, // 设计剪力 (lb)
            double nu, // 设计轴力 (lb，拉力为负)
            MaterialProperties material,
            SectionDimensions section,
            double stirrupArea, // 单肢箍筋面积 (in²)
            int numLegs) // 箍筋肢数
        {
            var process = new List<string>();

            process.Add("=== ACI 318 抗剪设计 ===");        
            process.Add($"");
            process.Add("【输入参数】");
            process.Add($"设计剪力 Vu = {vu / 1000:F2} kips");
            process.Add($"设计轴力 Nu = {nu / 1000:F2} kips");
            process.Add($"混凝土抗压强度 f'c = {material.Fc:F0} psi");
            process.Add($"钢筋屈服强度 fy = {material.Fy:F0} psi");
            process.Add($"截面宽度 b = {section.B:F2} in");
            process.Add($"有效高度 d = {section.D:F2} in");
            process.Add($"");

            // φv = 0.75 for shear
            double phiV = 0.75;
            process.Add("【强度折减系数】");
            process.Add($"抗剪强度折减系数 φv = {phiV:F2}");
            process.Add($"");

            // Vc计算 (simplified method)
            process.Add("【混凝土抗剪承载力 Vc】");
            double lambda = 1.0; // 普通混凝土
            double vc = 2 * lambda * Math.Sqrt(material.Fc) * section.B * section.D;
            process.Add($"Vc = 2·λ·√f'c·b·d");
            process.Add($"Vc = 2 × {lambda:F1} × √{material.Fc:F0} × {section.B:F2} × {section.D:F2}");
            process.Add($"Vc = {vc / 1000:F2} kips");
            process.Add($"φVc = {phiV * vc / 1000:F2} kips");
            process.Add($"");

            // 判断是否需要配筋 ACI318-19 9.6.3.1
            process.Add("【是否需要抗剪配筋】");
            if (vu <= phiV * vc / 2)
            {
                process.Add($"Vu = {vu / 1000:F2} kips ≤ φVc/2 = {phiV * vc / 2000:F2} kips");
                process.Add("理论上不需要抗剪配筋，但按构造配置");
            }
            else if (vu <= phiV * vc)
            {
                process.Add($"φVc/2 < Vu = {vu / 1000:F2} kips ≤ φVc = {phiV * vc / 1000:F2} kips");
                process.Add("按最小配筋率配置箍筋");
            }
            else
            {
                process.Add($"Vu = {vu / 1000:F2} kips > φVc = {phiV * vc / 1000:F2} kips");
                process.Add("需要计算配置抗剪箍筋");
            }
            process.Add($"");

            // 计算所需箍筋
            double vs = (vu / phiV) - vc;
            if (vs < 0) vs = 0;

            double avTotal = stirrupArea * numLegs;
            double sRequired = avTotal * material.Fy * section.D / vs;
            if (double.IsInfinity(sRequired) || sRequired < 0) sRequired = double.MaxValue;

            process.Add("【箍筋计算】");
            process.Add($"所需钢筋抗剪承载力 Vs = Vu/φv - Vc = {vs / 1000:F2} kips");
            process.Add($"箍筋总面积 Av = {avTotal:F3} in² ({numLegs}肢)");
            if (sRequired != double.MaxValue)
            {
                process.Add($"所需间距 s = Av·fy·d/Vs = {sRequired:F1} in");
            }
            process.Add($"");

            // 箍筋最大间距
            process.Add("【最大间距限制】");
            double sMax = 0;
            if (vs > 4 * Math.Sqrt(material.Fc) * section.B * section.D)
            {
                sMax = Math.Min(section.D / 4,12);
                process.Add("Vs > 4·√f'c·b·d，最大间距限制为 d/4和12in的较小值");
            }
            else
            {
                sMax = Math.Min(section.D / 2, 24);
                process.Add("Vs ≤ 4·√f'c·b·d，最大间距限制为 d/2和24in的较小值");
            }

            process.Add($"最大间距smax = {sMax:F1} in");
            process.Add($"");

            //箍筋肢距复核
            process.Add("【箍筋肢距复核】");
            //箍筋肢距 = (梁宽 - 保护层厚度 * 2 ) / (肢数 - 1)
            double swActual = (section.B - section.Cover * 2) / (numLegs - 1);
            double swMax = 0;

            if (vs > 4 * Math.Sqrt(material.Fc) * section.B * section.D)
            {
                swMax = Math.Min(section.D / 2,12);
                process.Add("Vs > 4·√f'c·b·d，最大肢距限制为 d/2和12in的较小值");
            }
            else
            {
                swMax = Math.Min(section.D , 24);
                process.Add("Vs ≤ 4·√f'c·b·d，最大肢距限制为 d和24in的较小值");
            }

            process.Add($"最大肢距swmax = {swMax:F1} in");
            process.Add($"");

            if (swActual > swMax)
            {
                process.Add($"实际箍筋肢距swActual = {swActual:F1} in > 最大肢距swmax = {swMax:F1} in");
                process.Add($"✗需要增加箍筋肢数");
            }
            else
            {
                process.Add($"实际箍筋肢距swActual = {swActual:F1} in ≤ 最大肢距swmax = {swMax:F1} in");
                process.Add($"√箍筋肢数配置正常");
            }
            process.Add($"");

            // 最小配筋率
            process.Add("【最小配筋率】");
            double avMin1 = 0.75 * Math.Sqrt(material.Fc) * section.B * sMax / material.Fy;
            double avMin2 = 50 * section.B * sMax / material.Fy;
            double avMin = Math.Max(avMin1, avMin2);
            process.Add($"Av,min1 = 0.75·√f'c·b·s/fy = {avMin1:F3} in²");
            process.Add($"Av,min2 = 50·b·s/fy = {avMin2:F3} in²");
            process.Add($"Av,min = {avMin:F3} in²");

            double avProvided = Math.Max(avTotal, avMin);
            process.Add($"");
            process.Add("【结论】");
            if (sRequired != double.MaxValue)
            {
                process.Add($"需要配置箍筋间距 s = {sRequired:F1} in");
            }

            process.Add($"");
            process.Add($"提供箍筋面积 Av = {avProvided:F3} in²");
            process.Add($"最大间距 smax = {sMax:F1} in");

            return (avTotal, avProvided, sRequired, sMax, process);
        }

        // todo
        // 验算钢筋间距是否满足裂缝控制要求
        // 增加跨度参数，和非持续活荷载的百分比，验算挠度

    }
}
