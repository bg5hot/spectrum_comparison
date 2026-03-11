using System;
using System.Collections.Generic;
using System.Linq;

namespace SpectrumComparison
{
    public static class ColumnDesignCalculations
    {
        public class MaterialProperties
        {
            public double Fc { get; set; } // psi
            public double Fy { get; set; } // psi
            public double Es { get; set; } = 29000000; // psi
        }

        public class ColumnSection
        {
            public double B { get; set; } // in
            public double H { get; set; } // in
            public double Cover { get; set; } // in
            public string BarSize { get; set; } = "#8";
            public string TieSize { get; set; } = "#4";
            public int Nx { get; set; } // Bars along width B (top/bottom rows)
            public int Ny { get; set; } // Bars along height H (side rows)
            public int TieLegsX { get; set; } = 4; // Legs parallel to width B
            public int TieLegsY { get; set; } = 4; // Legs parallel to height H

            // Derived properties
            public double Ag => B * H; // gross area（构件毛截面面积）
            public double BarArea => GetBarArea(BarSize);
            public double BarDiameter => GetBarDiameter(BarSize);
            public double TieDiameter => GetBarDiameter(TieSize);
            public int TotalBars => 2 * Nx + 2 * (Ny - 2); // Assuming rectangular arrangement
            public double Ast => TotalBars * BarArea;
            
            public double SpacingX //纵筋中心距
            {
                get 
                {
                    if (Nx < 2) return 0;
                    double coreWidth = B - 2 * Cover - 2 * TieDiameter;
                    return coreWidth/ (Nx - 1);
                }
            }

            public double SpacingY //箍筋中心距
            {
                get 
                {
                    if (Ny < 2) return 0;
                    double coreHeight = H - 2 * Cover - 2 * TieDiameter;
                    return coreHeight/ (Ny - 1);
                }
            }

            // Rebar locations (distance from compression fiber)
            // isXAxisBending: true for Mux (bending about X, layers along H), false for Muy (bending about Y, layers along B)
            public List<(double y, double area)> GetRebarLayers(bool isXAxisBending) 
            {
                var layers = new List<(double y, double area)>();
                double d_prime = Cover + TieDiameter + BarDiameter / 2.0;
                
                double dimension = isXAxisBending ? H : B;
                int n_main = isXAxisBending ? Nx : Ny; // Bars in top/bottom rows (or left/right)
                int n_side = isXAxisBending ? Ny : Nx; // Bars in side rows
                
                // Top/Left layer
                double pos_start = d_prime;
                layers.Add((pos_start, n_main * BarArea));

                // Bottom/Right layer
                double pos_end = dimension - d_prime;
                
                // Intermediate layers
                if (n_side > 2)
                {
                    double spacing = (dimension - 2 * d_prime) / (n_side - 1);
                    for (int i = 1; i < n_side - 1; i++)
                    {
                        double pos = d_prime + i * spacing;
                        // 2 bars per intermediate layer
                        layers.Add((pos, 2 * BarArea));
                    }
                }
                
                layers.Add((pos_end, n_main * BarArea));
                
                return layers;
            }
        }

        public class DesignResult
        {
            public List<InteractionPoint> NominalCurveX { get; set; } = new();
            public List<InteractionPoint> DesignCurveX { get; set; } = new();
            public InteractionPoint UserPointX { get; set; }
            public bool IsSafeX { get; set; }

            public List<InteractionPoint> NominalCurveY { get; set; } = new();
            public List<InteractionPoint> DesignCurveY { get; set; } = new();
            public InteractionPoint UserPointY { get; set; }
            public bool IsSafeY { get; set; }

            public string Report { get; set; } = "";
        }

        public struct InteractionPoint
        {
            public double P { get; set; } // kips
            public double M { get; set; } // kip-ft
        }

        public static double GetBarArea(string size)
        {
            return size switch
            {
                "#3" => 0.11,
                "#4" => 0.20,
                "#5" => 0.31,
                "#6" => 0.44,
                "#7" => 0.60,
                "#8" => 0.79,
                "#9" => 1.00,
                "#10" => 1.27,
                "#11" => 1.56,
                "#14" => 2.25,
                "#18" => 4.00,
                _ => 0
            };
        }

        public static double GetBarDiameter(string size)
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
                _ => 0
            };
        }

        public static DesignResult CheckColumn(
            double pu, double mux, double muy, double vux, double vuy, // kips, kip-ft
            MaterialProperties mat, ColumnSection sec)
        {
            var result = new DesignResult();
            var report = new List<string>();
            report.Add("=== ACI 318 矩形柱双向偏压设计 ===");
            report.Add("⚠️Mx表示 绕 X轴/宽度 弯曲 （即弯曲变形发生在 Y 向/高度方向 平面内）");
            report.Add("⚠️My表示 绕 Y轴/高度 弯曲 （即弯曲变形发生在 X 向/宽度方向 平面内）");
            report.Add("⚠️VuX表示 X方向剪力，VuY表示 Y方向剪力");
            report.Add("不考虑弯矩放大系数");
            report.Add("");
            
            // 1. 输入参数与几何信息
            report.Add("【1. 输入参数】");
            report.Add($"设计荷载: Pu = {pu} kips");
            report.Add($"  - X轴弯曲: Mux = {mux} kip-ft, Y向剪力: Vuy = {vuy} kips");
            report.Add($"  - Y轴弯曲: Muy = {muy} kip-ft, X向剪力: Vux = {vux} kips");
            report.Add($"材料强度: f'c = {mat.Fc} psi, fy = {mat.Fy} psi");
            report.Add($"截面尺寸: b = {sec.B:F2} in, h = {sec.H:F2} in");
            report.Add($"保护层厚度: cc = {sec.Cover:F2} in");
            report.Add("");
            
            report.Add("【2. 几何与配筋信息】");
            report.Add($"截面毛面积 Ag = {sec.Ag:F1} in²");
            report.Add($"纵筋配置: {sec.TotalBars} 根 {sec.BarSize}");
            report.Add($"纵筋总面积 Ast = {sec.Ast:F2} in²");
            double rho = sec.Ast / sec.Ag;
            report.Add($"配筋率 ρg = {rho:P2}");
            report.Add("");
            
            // 构造验算-配筋率
            report.Add("【3. 纵筋构造验算】");
            report.Add($"配筋率验算：最小配筋率1%，最大配筋率8%，建议最大配筋率控制在5%以下");
            // ACI 318-19 10.6.1.1: Area of longitudinal reinforcement shall be at least 0.01Ag but not more than 0.08Ag
            if (rho < 0.01) report.Add("⚠️ 配筋率小于 1% (ACI 318 最小配筋率)");
            else if (rho > 0.08) report.Add("⚠️ 配筋率大于 8% (ACI 318 最大配筋率)");
            else report.Add("✅ 配筋率满足要求 (1% - 8%)");
            
            // 构造验算-最小根数
            // ACI 318-19 10.6.1.2: Minimum number of longitudinal bars shall be (b) 4 for bars within rectangular ties
            report.Add($"最小根数验算：矩形柱最少需要 4 根纵筋");
            if (sec.TotalBars < 4) report.Add("⚠️ 矩形柱最少需要 4 根纵筋");
            else report.Add("✅ 纵筋根数满足要求");
            report.Add("");

            //构造验算-纵筋最小间距
            report.Add($"最小纵筋距验算：纵筋最小中心距center to center spacing： s = max(1.5d, 1.5in) (忽略骨料粒径限值)");
            // ACI 318-19 10.6.1.3: Minimum spacing of longitudinal bars shall be at least 1.5d and 1.5in'
            double minSpacing = Math.Max(2 * GetBarDiameter(sec.BarSize), 1.5);
            report.Add($"纵筋最小中心距c s = {minSpacing:F2} in");
            report.Add($"宽度方向纵筋中心距  = {sec.SpacingX:F2} in， 高度方向箍筋中心距 = {sec.SpacingY:F2} in");
            if (sec.SpacingX < minSpacing) report.Add($"⚠️ 宽度方向钢筋净间距 ({sec.SpacingX:F2} in) 小于最小间距");
            if (sec.SpacingY < minSpacing) report.Add($"⚠️ 高度方向箍筋净间距 ({sec.SpacingY:F2} in) 小于最小间距");
            if (sec.SpacingX >= minSpacing && sec.SpacingY >= minSpacing) report.Add($"✅ 钢筋净间距 ({sec.SpacingX:F2} in / {sec.SpacingY:F2} in) 满足要求");
            report.Add("");

            // 4. P-M Interaction Checks (Both Directions)
            report.Add("【4. P-M 承载力验算】");
            
            // --- Direction X (Bending about X-axis) ---
            report.Add("--- 4.1 X轴弯曲 (Mux) ---");
            CalculateInteractionDiagram(mat, sec, true, result.NominalCurveX, result.DesignCurveX);
            result.UserPointX = new InteractionPoint { P = pu, M = mux };
            result.IsSafeX = CheckPointInsideCurve(result.UserPointX, result.DesignCurveX);
            CheckPMCapacity(pu, mux, result.DesignCurveX, mat, sec, report, "X");

            // --- Direction Y (Bending about Y-axis) ---
            report.Add("--- 4.2 Y轴弯曲 (Muy) ---");
            CalculateInteractionDiagram(mat, sec, false, result.NominalCurveY, result.DesignCurveY);
            result.UserPointY = new InteractionPoint { P = pu, M = muy };
            result.IsSafeY = CheckPointInsideCurve(result.UserPointY, result.DesignCurveY);
            CheckPMCapacity(pu, muy, result.DesignCurveY, mat, sec, report, "Y");
            
            report.Add("");

            // 5. Shear Check (Both Directions)
            report.Add("【5. 抗剪验算 (ACI 318-19)】");
            
            // --- Shear Y (Vuy) - Resisted by ties along Y ---
            report.Add("--- 5.1 Y方向剪力 (Vuy) ---");
            // For Vuy (force along H), width is B, depth is derived from H
            double? sReqShearY = CheckShear(vuy, pu, mat, sec, report, true);
            
            // --- Shear X (Vux) - Resisted by ties along X ---
            report.Add("--- 5.2 X方向剪力 (Vux) ---");
            // For Vux (force along B), width is H, depth is derived from B
            double? sReqShearX = CheckShear(vux, pu, mat, sec, report, false);

            report.Add("");

            // 6. Detailing Checks (Ties)
            CheckTieDetailing(mat, sec, report, sReqShearX, sReqShearY);

            result.Report = string.Join(Environment.NewLine, report);
            return result;
        }

        private static void CheckPMCapacity(double pu, double mu, List<InteractionPoint> curve, MaterialProperties mat, ColumnSection sec, List<string> report, string direction)
        {
            // Calculate Po and PnMax (Same for both directions technically, but phiPnMax is what matters)
            double Po = (0.85 * mat.Fc * (sec.Ag - sec.Ast) + mat.Fy * sec.Ast) / 1000.0;
            double phiPnMax = 0.80 * 0.65 * Po;
            
            if (pu > phiPnMax)
            {
                 report.Add($"❌ [{direction}] 轴压力 Pu ({pu}) > φPn,max ({phiPnMax:F1})");
            }
            else
            {
                 report.Add($"✅ [{direction}] 轴压力 Pu ({pu}) ≤ φPn,max ({phiPnMax:F1})");
            }

            double phiMnCapacity = GetMomentCapacityAtP(pu, curve);
            if (phiMnCapacity > 0)
            {
                report.Add($"在 Pu = {pu} kips 下的设计弯矩承载力 φMn{direction} = {phiMnCapacity:F1} kip-ft");
                double ratio = Math.Abs(mu) / phiMnCapacity;
                report.Add($"弯矩比 D/C = {Math.Abs(mu):F1} / {phiMnCapacity:F1} = {ratio:F3}");
                
                if (ratio <= 1.0 && pu <= phiPnMax)
                    report.Add($"✅ [{direction}] 截面承载力满足要求");
                else
                    report.Add($"❌ [{direction}] 截面承载力不足");
            }
            else
            {
                 // Fallback if point check fails or outside range
                 bool safe = CheckPointInsideCurve(new InteractionPoint{P=pu, M=mu}, curve);
                 if (safe) report.Add($"✅ [{direction}] 截面承载力满足要求 (点在曲线内)");
                 else report.Add($"❌ [{direction}] 截面承载力不足 (点在曲线外)");
            }
            report.Add("");
        }

        private static void CalculateInteractionDiagram(
            MaterialProperties mat, ColumnSection sec, bool isXAxis,
            List<InteractionPoint> nominal, List<InteractionPoint> design) 
        {
            double beta1 = CalculateBeta1(mat.Fc); 
            var layers = sec.GetRebarLayers(isXAxis); 
            double d = layers.Last().y; 
            double dimension = isXAxis ? sec.H : sec.B;
            double width = isXAxis ? sec.B : sec.H;

            // Pure compression 
            CalculatePoint(mat, sec, layers, 99999, beta1, nominal, design, dimension, width); 

            // Sweep c from dimension down to 0
            for (double r = 1.2; r >= 0.05; r -= 0.05) 
            {
                CalculatePoint(mat, sec, layers, r * d, beta1, nominal, design, dimension, width);
            }

            // Pure Tension
            CalculatePoint(mat, sec, layers, 0, beta1, nominal, design, dimension, width);
        }

        private static void CalculatePoint(
            MaterialProperties mat, ColumnSection sec, 
            List<(double y, double area)> layers, 
            double c, double beta1,
            List<InteractionPoint> nominal, List<InteractionPoint> design,
            double dimension, double width) 
        {
            double Pn = 0; 
            double Mn = 0; 
            double phi = 0.65;
            double epsilon_t = 0;

            if (c > 50 * dimension) // Pure compression
            {
                Pn = 0.85 * mat.Fc * (sec.Ag - sec.Ast) + mat.Fy * sec.Ast;
                Mn = 0;
                phi = 0.65;
            }
            else if (c <= 0.001) // Pure tension
            {
                Pn = -mat.Fy * sec.Ast;
                Mn = 0;
                phi = 0.90;
            }
            else
            {
                // Concrete contribution
                double a = beta1 * c;
                if (a > dimension) a = dimension;
                double Cc = 0.85 * mat.Fc * width * a;
                Pn += Cc;
                Mn += Cc * (dimension / 2.0 - a / 2.0);

                // Steel contribution
                foreach (var layer in layers)
                {
                    double strain = 0.003 * (c - layer.y) / c;
                    double stress = strain * mat.Es;
                    if (stress > mat.Fy) stress = mat.Fy;
                    if (stress < -mat.Fy) stress = -mat.Fy;

                    // Subtract concrete area displaced by steel in compression zone
                    if (layer.y < a)
                    {
                        double concreteStress = 0.85 * mat.Fc;
                        Pn += (stress * layer.area - concreteStress * layer.area);
                        Mn += (stress * layer.area - concreteStress * layer.area) * (dimension / 2.0 - layer.y);
                    }
                    else
                    {
                        Pn += stress * layer.area;
                        Mn += stress * layer.area * (dimension / 2.0 - layer.y);
                    }

                    if (layer.y > c) 
                    {
                        epsilon_t = Math.Max(epsilon_t, Math.Abs(strain));
                    }
                }

                // Determine Phi
                double ty = mat.Fy / mat.Es;
                if (epsilon_t <= ty)
                    phi = 0.65; 
                else if (epsilon_t >= 0.005)
                    phi = 0.90; 
                else
                    phi = 0.65 + 0.25 * (epsilon_t - ty) / (0.005 - ty); 
            }

            // Max axial load limit
            double Po = 0.85 * mat.Fc * (sec.Ag - sec.Ast) + mat.Fy * sec.Ast;
            double maxPhiPn = 0.80 * 0.65 * Po; 
            
            nominal.Add(new InteractionPoint { P = Pn / 1000.0, M = Mn / 12000.0 });
            
            double phiPn = phi * Pn;
            double phiMn = phi * Mn;

            if (phiPn > maxPhiPn)
            {
               phiPn = maxPhiPn;
            }

            design.Add(new InteractionPoint { P = phiPn / 1000.0, M = phiMn / 12000.0 });
        }

        private static double CalculateBeta1(double fc)
        {
            // ACI 318-19 Table 22.2.2.4.3: Values of beta1 for equivalent rectangular concrete stress distribution
            if (fc <= 4000) return 0.85;
            if (fc >= 8000) return 0.65;
            return 0.85 - 0.05 * (fc - 4000) / 1000.0;
        }

        private static bool CheckPointInsideCurve(InteractionPoint pt, List<InteractionPoint> curve) // 检查一个点是否在交互图中
        {
            var sorted = curve.OrderByDescending(p => p.P).ToList();
            
            if (pt.P > sorted.First().P + 0.1) return false; 
            if (pt.P < sorted.Last().P - 0.1) return false;

            for (int i = 0; i < sorted.Count - 1; i++)
            {
                var p1 = sorted[i];
                var p2 = sorted[i+1];
                
                if (pt.P <= p1.P + 0.001 && pt.P >= p2.P - 0.001)
                {
                    double ratio = (pt.P - p2.P) / (p1.P - p2.P);
                    double maxM = p2.M + ratio * (p1.M - p2.M);
                    
                    if (Math.Abs(pt.M) <= maxM) return true;
                }
            }
            return false;
        }

        private static double GetMomentCapacityAtP(double pu, List<InteractionPoint> curve) // 获取交互图中，在 pu 点的弯矩容量
        {
            var sorted = curve.OrderByDescending(p => p.P).ToList();
            
            if (pu > sorted.First().P + 0.1) return 0; 
            if (pu < sorted.Last().P - 0.1) return 0;

            for (int i = 0; i < sorted.Count - 1; i++)
            {
                var p1 = sorted[i];
                var p2 = sorted[i+1];
                
                if (pu <= p1.P + 0.001 && pu >= p2.P - 0.001)
                {
                    double ratio = (pu - p2.P) / (p1.P - p2.P);
                    double maxM = p2.M + ratio * (p1.M - p2.M);
                    return maxM;
                }
            }
            return 0;
        }

        private static double? CheckShear(double Vu, double Pu, MaterialProperties mat, ColumnSection sec, List<string> report, bool isShearY)
        {
            string dirLabel = isShearY ? "Y向(Vuy)" : "X向(Vux)";
            report.Add($"【5.{ (isShearY ? "1" : "2") } 抗剪验算 - {dirLabel}】");
            
            // For Shear Y (Vuy, along H): Width = B, Depth ~ H
            // For Shear X (Vux, along B): Width = H, Depth ~ B
            double b_shear = isShearY ? sec.B : sec.H; // Width of section perpendicular to shear
            double h_shear = isShearY ? sec.H : sec.B; // Depth of section parallel to shear
            double d = h_shear - sec.Cover - sec.TieDiameter - sec.BarDiameter/2;
            
            report.Add($"计算截面宽 b = {b_shear:F2} in, 有效高度 d = {d:F2} in");

            
            // Vc calculation
            double Nu = Pu * 1000; // lbs
            double lambda = 1.0; // Normal weight
            // ACI 318-19 Table 22.5.5.1: Vc = (2 * lambda * sqrt(fc) + Nu / (6 * Ag)) * bw * d
            double Vc = (2 * lambda * Math.Sqrt(mat.Fc) + Nu / (6 * sec.Ag)) * b_shear * d;
            report.Add($"混凝土抗剪 Vc = [2*lambda*√Fc + Nu/6Ag] * b*d = {Vc/1000:F1} kips");
            
            // ACI 318-19 21.2.1: Strength reduction factor for shear is 0.75
            double phi = 0.75;
            double phiVc = phi * Vc / 1000.0; // kips

            //截面验算 ACI318-19 22.5.1.2: Vu <= phi * Vc => Vc >= Vu/phi
            // Max Vn = Vc + 8*sqrt(fc)*b*d
            double maxVn = (Vc / 1000.0) + (8 * Math.Sqrt(mat.Fc) * b_shear * d / 1000.0);
            
            if (Math.Abs(Vu) <= phi * maxVn)
            {
                report.Add($"✅ Vu ({Math.Abs(Vu):F1}) <= φ(Vc + 8*√fc*b*d) = {phi*maxVn:F1} kips");
                report.Add("截面尺寸满足抗剪要求");
            }
            else
            {
                report.Add($"⚠️ Vu ({Math.Abs(Vu):F1}) > φ(Vc + 8*√fc*b*d) = {phi*maxVn:F1} kips");
                report.Add("截面尺寸不足!");
            }
            
            report.Add($"设计抗剪强度 φVc = {phiVc:F1} kips (φ=0.75)");
            
            if (Math.Abs(Vu) <= phiVc)
            {
                report.Add($"✅ Vu ({Math.Abs(Vu):F1}) <= φVc ({phiVc:F1})");
                report.Add("结论: 不需要抗剪箍筋，仅需按构造配置");
                report.Add("");
                return null;
            }
            else
            {
                report.Add($"Vu ({Math.Abs(Vu):F1}) > φVc ({phiVc:F1})");
                report.Add("结论: 需要抗剪箍筋");
                
                // ACI 318-19 22.5.1.1: Vu <= phi * (Vc + Vs) => Vs >= (Vu/phi) - Vc
                report.Add("");
                report.Add($"抗剪箍筋面积计算: ");
                double VsReq = (Math.Abs(Vu) / phi) - (Vc / 1000.0); // kips
                report.Add($"所需 Vs = Vu/φ - Vc = {Math.Abs(Vu)/phi:F1} kips - {Vc/1000:F1} kips = {VsReq:F1} kips");
                
                // Calculate required spacing for strength
                // ACI 318-19 Eq. 22.5.8.5.3: Vs = Av * fyt * d / s => s = Av * fyt * d / Vs
                // Ties parallel to shear force resist shear.
                // Vuy (along H) -> TieLegsY (legs parallel to H)
                // Vux (along B) -> TieLegsX (legs parallel to B)
                int legs = isShearY ? sec.TieLegsY : sec.TieLegsX;
                double Av = legs * GetBarArea(sec.TieSize); 
                report.Add($"使用箍筋: {sec.TieSize}, {legs}肢 ({ (isShearY ? "Y向" : "X向") }), Av = {Av:F2} in²");
                
                double sReq = Av * mat.Fy * d / (VsReq * 1000);
                report.Add($"所需最大间距 s_req <= Av * Fy * d / Vs = {Av:F2} in² * {mat.Fy:F1} ksi * {d:F2} in / ({VsReq:F1} kips) = {sReq:F2} in (基于强度)");
                
                // ACI 318-19 Table 10.7.6.5.2 Maximum spacing of shear reinforcement
                report.Add("");
                report.Add($"抗剪钢筋最大间距最大间距要求：Table 10.7.6.5.2");
                double vsLimit = 4 * Math.Sqrt(mat.Fc) * b_shear * d / 1000.0; // kips
                double sMaxShear;
                
                if (VsReq <= vsLimit)
                {
                    sMaxShear = Math.Min(d / 2.0, 24.0);
                    report.Add($"Vs ({VsReq:F1}) <= 4√fc*b*d ({vsLimit:F1}) => s_max_shear = min(d/2, 24) = {sMaxShear:F2} in");
                }
                else
                {
                    sMaxShear = Math.Min(d / 4.0, 12.0);
                    report.Add($"Vs ({VsReq:F1}) > 4√fc*b*d ({vsLimit:F1}) => s_max_shear = min(d/4, 12) = {sMaxShear:F2} in");
                }

                double finalS = Math.Min(sReq, sMaxShear);
                report.Add("");
                string sType =(sReq < sMaxShear) ? "sReq < sMaxShear: 间距由计算结果确定" : "sReq > sMaxShear: 间距由最大间距要求确定";
                report.Add($"{sType}");
                
                report.Add($"该方向抗剪控制间距 s = min(s_req, s_max_shear) = {finalS:F2} in");

                report.Add("");
                return finalS;
            }
        }

        private static void CheckTieDetailing(MaterialProperties mat, ColumnSection sec, List<string> report, double? sReqStrengthX, double? sReqStrengthY)
        {
            report.Add("【6. 箍筋构造验算】");
            report.Add($"选用箍筋: {sec.TieSize}");
            
            report.Add("");
            report.Add($"箍筋最小直径验算：");
            report.Add($"纵筋规格：{sec.BarSize}");
            // 1. Min Diameter Check
            // #3 for #10 bars and smaller, #4 for #11 bars and larger
            if (sec.BarDiameter <= 1.270)
            {
                if (sec.TieDiameter >= 0.375)
                {
                    report.Add($"✅ 箍筋规格 {sec.TieSize} >= #3 满足要求");
                }
                else
                {
                    report.Add($"⚠️ 箍筋直径 箍筋规格 {sec.TieSize} 不符合构造要求");
                }
            }
            else if (sec.BarDiameter > 1.270)
            {
                if (sec.TieDiameter >= 0.500)
                {
                    report.Add($"✅ 箍筋规格 {sec.TieSize} >= #4 满足要求");
                }
                else
                {
                    report.Add($"⚠️ 箍筋直径 箍筋规格 {sec.TieSize} 不符合构造要求");
                }
            }
            report.Add("");

            report.Add($"箍筋最大间距验算：");
            // 2. Max Spacing
            // 16 * db_long
            double s1 = 16 * sec.BarDiameter;
            // 48 * db_tie
            double s2 = 48 * sec.TieDiameter;
            // Least dimension
            double s3 = Math.Min(sec.B, sec.H);
            
            // ACI 318-19 25.7.2.1: Vertical spacing of ties shall not exceed the least of (a) 16db (b) 48dt (c) Least dimension
            double sMax = Math.Min(s1, Math.Min(s2, s3));
            report.Add($"构造箍筋最大间距 s_max = min(16倍纵筋直径, 48倍箍筋直径, 柱子最小尺寸)");
            report.Add($"  - 16 * 纵筋直径 ({sec.BarDiameter:F3}\") = {s1:F2} in");
            report.Add($"  - 48 * 箍筋直径 ({sec.TieDiameter:F3}\") = {s2:F2} in");
            report.Add($"  - 截面最小边长 = {s3:F2} in");
            report.Add($"结论: 箍筋最大允许间距 s_max = {sMax:F2} in");

            report.Add("");
            report.Add($"【综合间距要求】");
            
            double sReqX = sReqStrengthX ?? 999; // Default to max if not provided
            double sReqY = sReqStrengthY ?? 999; // Default to max if not provided
            double sReqStrength = Math.Min(sReqX, sReqY);

            if (sReqStrengthX.HasValue || sReqStrengthY.HasValue)
            {
                if (sReqStrengthX.HasValue) 
                    report.Add($"X向抗剪要求间距 s_req_x = {sReqStrengthX.Value:F2} in");
                else
                    report.Add($"X向抗剪要求间距 s_req_x = 无 (由构造控制)");

                if (sReqStrengthY.HasValue) 
                    report.Add($"Y向抗剪要求间距 s_req_y = {sReqStrengthY.Value:F2} in");
                else
                    report.Add($"Y向抗剪要求间距 s_req_y = 无 (由构造控制)");
                
                if (sReqStrength < sMax)
                {
                     report.Add($"抗剪要求间距 ({sReqStrength:F2}) < 构造要求间距 ({sMax:F2})");
                     double final = Math.Floor(sReqStrength * 10) / 10.0; // Round down to nearest 0.1
                     report.Add($"最终设计间距应控制在 s <= {final:F1} in (由抗剪强度控制)");
                }
                else
                {
                     report.Add($"抗剪要求间距 ({sReqStrength:F2}) >= 构造要求间距 ({sMax:F2})");
                     report.Add($"最终设计间距应控制在 s <= {sMax:F2} in (由构造要求控制)");
                }
            }
            else
            {
                 report.Add("抗剪无计算间距要求");
                 report.Add($"最终设计间距应控制在 s <= {sMax:F2} in (由构造要求控制)");
            }
        }
    }
}
