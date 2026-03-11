using System;
using System.Collections.Generic;
using System.Linq;

namespace SpectrumComparison
{
    public static class CircularColumnDesignCalculations
    {
        public enum TieType
        {
            Tied,   // 绑扎箍筋
            Spiral  // 螺旋箍筋
        }

        public class CircularColumnSection
        {
            public double Diameter { get; set; } // in
            public double Cover { get; set; } // in
            public string BarSize { get; set; } = "#8";
            public string TieSize { get; set; } = "#4";
            public int NumBars { get; set; } = 8; // Min 6
            public TieType TieType { get; set; } = TieType.Spiral;

            // Derived properties
            public double Ag => Math.PI * Math.Pow(Diameter / 2.0, 2);
            public double BarArea => ColumnDesignCalculations.GetBarArea(BarSize);
            public double BarDiameter => ColumnDesignCalculations.GetBarDiameter(BarSize);
            public double TieDiameter => ColumnDesignCalculations.GetBarDiameter(TieSize);
            public double Ast => NumBars * BarArea;
            
            // Rebar locations (distance from center)
            public List<(double y, double area)> GetRebarLayers() 
            {
                var layers = new List<(double y, double area)>();
                double d_core = Diameter - 2 * Cover - 2 * TieDiameter - BarDiameter;
                double r_core = d_core / 2.0;
                
                // Angle between bars
                double dTheta = 2 * Math.PI / NumBars;

                for (int i = 0; i < NumBars; i++)
                {
                    double theta = i * dTheta;
                    // y is distance from centroid (center). 
                    // Let's define y axis positive upwards. 
                    // Compression fiber is at top (y = +R).
                    // So distance from center is R * cos(theta) if theta starts from top.
                    double y = r_core * Math.Cos(theta);
                    layers.Add((y, BarArea));
                }
                
                return layers;
            }
        }

        public static ColumnDesignCalculations.DesignResult CheckColumn(
            double Pu, double Mux, double Muy, double Vux, double Vuy,
            ColumnDesignCalculations.MaterialProperties mat, CircularColumnSection sec)
        {
            var result = new ColumnDesignCalculations.DesignResult();
            var report = new List<string>();

            // 1. Calculate Resultant Forces
            double Mu = Math.Sqrt(Mux * Mux + Muy * Muy);
            double Vu = Math.Sqrt(Vux * Vux + Vuy * Vuy);

            report.Add("【1. 设计参数】");
            report.Add($"圆形柱直径 D = {sec.Diameter:F2} in");
            report.Add($"混凝土强度 f'c = {mat.Fc:F0} psi, 钢筋强度 fy = {mat.Fy:F0} psi");
            report.Add($"纵筋: {sec.NumBars}根 {sec.BarSize}, 箍筋: {sec.TieSize} ({sec.TieType})");
            report.Add($"保护层厚度: {sec.Cover:F2} in");
            report.Add("");

            report.Add("【2. 荷载分析】");
            report.Add($"轴力 Pu = {Pu:F1} kips");
            report.Add($"X向弯矩 Mux = {Mux:F1} kip-ft");
            report.Add($"Y向弯矩 Muy = {Muy:F1} kip-ft");
            report.Add($"合成弯矩 Mu = √(Mux² + Muy²) = {Mu:F1} kip-ft");
            report.Add($"X向剪力 Vux = {Vux:F1} kips");
            report.Add($"Y向剪力 Vuy = {Vuy:F1} kips");
            report.Add($"合成剪力 Vu = √(Vux² + Vuy²) = {Vu:F1} kips");
            report.Add("");

            // 3. Reinforcement Check
            double rho = sec.Ast / sec.Ag;
            report.Add("【3. 配筋率验算】");
            report.Add($"截面面积 Ag = {sec.Ag:F1} in²");
            report.Add($"纵筋总面积 Ast = {sec.NumBars} * {sec.BarArea:F2} = {sec.Ast:F2} in²");
            report.Add($"配筋率 ρ = Ast / Ag = {rho:P2}");

            if (rho >= 0.01 && rho <= 0.08)
                report.Add("✅ 配筋率满足要求 (1% - 8%)");
            else
                report.Add("⚠️ 配筋率超出建议范围 (1% - 8%)");
            report.Add("");

            // 4. Interaction Diagram
            CalculateInteractionDiagram(mat, sec, result.NominalCurveX, result.DesignCurveX);
            
            // Circular column is isotropic, so X and Y curves are identical.
            // Copy X curve to Y curve for UI compatibility if needed, or just use X for everything.
            result.NominalCurveY = new List<ColumnDesignCalculations.InteractionPoint>(result.NominalCurveX);
            result.DesignCurveY = new List<ColumnDesignCalculations.InteractionPoint>(result.DesignCurveX);

            // Check Capacity (using resultant Moment)
            double phiPnMax = GetPhiPnMax(sec, mat);
            report.Add("【4. 截面承载力验算】");
            report.Add($"最大轴力设计值 φPn_max = {phiPnMax:F1} kips");

            // We check (Pu, Mu) against the design curve
            bool isSafe = CheckPointInsideCurve(new ColumnDesignCalculations.InteractionPoint { P = Pu, M = Mu }, result.DesignCurveX, phiPnMax);
            
            double capacityM = GetMomentCapacityAtP(Pu, result.DesignCurveX);
            double ratio = capacityM > 0.001 ? Mu / capacityM : (Pu > phiPnMax ? 999 : 0);

            report.Add($"当前状态点: (P={Pu:F1}, M={Mu:F1})");
            report.Add($"在 P={Pu:F1} kips 时的抗弯承载力 φMn = {capacityM:F1} kip-ft");
            report.Add($"D/C 比 = {ratio:F3}");

            if (Pu > phiPnMax)
            {
                report.Add($"❌ 轴力过大! Pu ({Pu:F1}) > φPn_max ({phiPnMax:F1})");
            }
            else if (isSafe)
            {
                report.Add("✅ 截面承载力满足要求");
            }
            else
            {
                report.Add("❌ 截面承载力不足");
            }
            report.Add("");

            // 5. Shear Check
            CheckShear(Vu, Pu, mat, sec, report);

            result.Report = string.Join(Environment.NewLine, report);
            return result;
        }

        private static void CalculateInteractionDiagram(
            ColumnDesignCalculations.MaterialProperties mat, CircularColumnSection sec,
            List<ColumnDesignCalculations.InteractionPoint> nominal, List<ColumnDesignCalculations.InteractionPoint> design)
        {
            double R = sec.Diameter / 2.0;
            var bars = sec.GetRebarLayers(); // y distances from center
            double beta1 = CalculateBeta1(mat.Fc);

            // 3. Sweep c values
            // Range of c: from large (compression) to small (tension)
            // Start with pure compression (c -> infinity)
            CalculatePoint(mat, sec, bars, 99999, beta1, nominal, design, R);

            // Sweep from large c to small c
            // Start from 2*D down to 0.5 inches
            for (double c = sec.Diameter * 2.0; c > 0.5; c -= 0.5)
            {
                CalculatePoint(mat, sec, bars, c, beta1, nominal, design, R);
            }
            
            // Finer steps for small c (transition to tension)
            for (double c = 0.5; c > 0.1; c -= 0.1)
            {
                CalculatePoint(mat, sec, bars, c, beta1, nominal, design, R);
            }

            // End with pure tension (c -> 0)
            CalculatePoint(mat, sec, bars, 0.0001, beta1, nominal, design, R);

        }

        private static void CalculatePoint(
            ColumnDesignCalculations.MaterialProperties mat, CircularColumnSection sec,
            List<(double y, double area)> bars, double c, double beta1,
            List<ColumnDesignCalculations.InteractionPoint> nominal, List<ColumnDesignCalculations.InteractionPoint> design,
            double R)
        {
            double Pn = 0;
            double Mn = 0; // Moment about centroid
            
            // Concrete Contribution
            double a = beta1 * c; // Depth of equivalent stress block

            // Limit a to Diameter (full compression)
            if (a >= 2 * R)
            {
                // Full circle compression
                Pn += 0.85 * mat.Fc * sec.Ag;
                // Mn += 0; // Centroid of full circle is at center
            }
            else if (a > 0)
            {
                // Partial compression
                // Calculate area and moment of circular segment from top (y=R) down to y=R-a
                double concreteForce = 0;
                double concreteMoment = 0; // About center

                if (a <= R)
                {
                    // Case 1: Less than half circle (Segment at top)
                    // theta is half-angle subtended by the chord at the center
                    // cos(theta) = (R - a) / R
                    double cosTheta = (R - a) / R;
                    double theta = Math.Acos(cosTheta); // 0 to pi/2
                    
                    double areaSeg = R * R * (theta - Math.Sin(theta) * cosTheta);
                    double momentSeg = (2.0 / 3.0) * R * R * R * Math.Pow(Math.Sin(theta), 3);
                    
                    concreteForce = 0.85 * mat.Fc * areaSeg;
                    concreteMoment = 0.85 * mat.Fc * momentSeg;
                }
                else
                {
                    // Case 2: More than half circle
                    // Calculate the uncompressed segment at the bottom and subtract from total
                    double h_empty = 2 * R - a;
                    double cosTheta_empty = (R - h_empty) / R;
                    double theta_empty = Math.Acos(cosTheta_empty);
                    
                    double areaEmpty = R * R * (theta_empty - Math.Sin(theta_empty) * cosTheta_empty);
                    double momentEmpty = (2.0 / 3.0) * R * R * R * Math.Pow(Math.Sin(theta_empty), 3);
                    // The moment of the bottom segment is NEGATIVE (below center)
                    // But the formula gives positive magnitude for "segment at top". 
                    // By symmetry, the magnitude is the same, but sign is negative.
                    momentEmpty = -momentEmpty;
                    
                    double areaComp = sec.Ag - areaEmpty;
                    // Moment of full circle is 0. 
                    // Moment_Comp = Moment_Total - Moment_Empty = 0 - Moment_Empty
                    double momentComp = -momentEmpty;
                    
                    concreteForce = 0.85 * mat.Fc * areaComp;
                    concreteMoment = 0.85 * mat.Fc * momentComp;
                }
                
                Pn += concreteForce;
                Mn += concreteMoment;
            }

            // Steel Contribution
            double extremeTensionStrain = 0;

            foreach (var bar in bars)
            {
                // bar.y is distance from center (positive up)
                // Distance from extreme compression fiber: d_i = R - bar.y
                double d_i = R - bar.y;
                double strain = 0.003 * (c - d_i) / c;
                
                double stress = strain * mat.Es;
                if (stress > mat.Fy) stress = mat.Fy;
                if (stress < -mat.Fy) stress = -mat.Fy;

                double force = stress * bar.area;
                
                // Subtract concrete area displaced by compressive steel
                // Check if bar is in compression zone 'a'
                if (d_i < a)
                {
                    force -= 0.85 * mat.Fc * bar.area;
                }

                Pn += force;
                Mn += force * bar.y; // Moment about center

                // Track extreme tension strain (lowest bar, min y)
                // Actually we want strain in extreme tension layer.
                // Lowest bar is at y approx -R.
                if (strain < 0 && Math.Abs(strain) > Math.Abs(extremeTensionStrain))
                {
                    extremeTensionStrain = strain;
                }
            }
            
            // Check pure compression case (c large)
            if (c > 10 * sec.Diameter)
            {
                 // Pure compression formula for circular column
                 // Pn = 0.85 fc (Ag - Ast) + fy Ast
                 Pn = 0.85 * mat.Fc * (sec.Ag - sec.Ast) + mat.Fy * sec.Ast;
                 Mn = 0;
                 extremeTensionStrain = 0; // Compression
            }

            // Phi Calculation
            double phi = GetPhi(sec.TieType, extremeTensionStrain, mat.Fy, mat.Es);
            
            // Max Axial Limit
            double phiPnMax = GetPhiPnMax(sec, mat);

            nominal.Add(new ColumnDesignCalculations.InteractionPoint { P = Pn / 1000.0, M = Mn / 12000.0 });
            
            double phiPn = phi * Pn / 1000.0;
            // Cap at phiPnMax
            if (phiPn > phiPnMax) phiPn = phiPnMax;

            design.Add(new ColumnDesignCalculations.InteractionPoint { P = phiPn, M = phi * Mn / 12000.0 });
        }

        private static double GetPhi(TieType type, double netTensileStrain, double fy, double Es)
        {
            double et = Math.Abs(netTensileStrain);
            double ety = fy / Es;
            
            double phiComp = (type == TieType.Spiral) ? 0.75 : 0.65;
            double phiTens = 0.90;

            if (et <= ety) return phiComp;
            if (et >= 0.005) return phiTens;
            
            return phiComp + (phiTens - phiComp) * (et - ety) / (0.005 - ety);
        }

        private static double GetPhiPnMax(CircularColumnSection sec, ColumnDesignCalculations.MaterialProperties mat)
        {
            // Po = 0.85 fc (Ag - Ast) + fy Ast
            double Po = 0.85 * mat.Fc * (sec.Ag - sec.Ast) + mat.Fy * sec.Ast;
            double phi = (sec.TieType == TieType.Spiral) ? 0.75 : 0.65;
            double factor = (sec.TieType == TieType.Spiral) ? 0.85 : 0.80;
            
            return factor * phi * Po / 1000.0; // kips
        }

        private static void CheckShear(double Vu, double Pu, ColumnDesignCalculations.MaterialProperties mat, CircularColumnSection sec, List<string> report)
        {
            report.Add("【5. 抗剪验算】");
            
            double D = sec.Diameter;
            double bw = D; // ACI 318-19 22.5.2.2
            double d = 0.8 * D; // ACI 318-19 22.5.2.2
            
            report.Add($"计算截面宽 bw = D = {bw:F2} in");
            report.Add($"有效高度 d = 0.8*D = {d:F2} in");

            double Nu = Pu * 1000;
            double lambda = 1.0;

            // Vc formula
            double Vc = (2 * lambda * Math.Sqrt(mat.Fc) + Nu / (6 * sec.Ag)) * bw * d;
            report.Add($"混凝土抗剪 Vc = {Vc/1000:F1} kips");

            double phi = 0.75;
            double phiVc = phi * Vc / 1000.0;
            
            // Max Vn limit
            double maxVn = (Vc/1000.0) + (8 * Math.Sqrt(mat.Fc) * bw * d / 1000.0);

            report.Add($"设计抗剪强度 φVc = {phiVc:F1} kips");

            if (Math.Abs(Vu) <= phiVc)
            {
                report.Add($"✅ Vu ({Math.Abs(Vu):F1}) <= φVc ({phiVc:F1})");
                report.Add("结论: 不需要抗剪箍筋，仅需按构造配置");
            }
            else
            {
                report.Add($"Vu ({Math.Abs(Vu):F1}) > φVc ({phiVc:F1})");
                report.Add("结论: 需要抗剪箍筋");
                
                double VsReq = (Math.Abs(Vu) / phi) - (Vc / 1000.0);
                report.Add($"所需 Vs = {VsReq:F1} kips");

                // Check Max Vs
                double vsLimit = 4 * Math.Sqrt(mat.Fc) * bw * d / 1000.0;
                double sMaxShear;
                if (VsReq <= vsLimit)
                    sMaxShear = Math.Min(d/2, 24);
                else
                    sMaxShear = Math.Min(d/4, 12);
                
                // Calculate spacing
                // Av = 2 * TieArea (Assuming 2 legs as per user instruction)
                int legs = 2; 
                double Av = legs * sec.TieDiameter * sec.TieDiameter * Math.PI / 4.0; // Area of tie
                // Or use standard bar area
                double AvStd = legs * ColumnDesignCalculations.GetBarArea(sec.TieSize);

                double sReq = AvStd * mat.Fy * d / (VsReq * 1000);
                
                report.Add($"使用箍筋: {sec.TieSize}, 按2肢计算, Av = {AvStd:F2} in²");
                report.Add($"所需最大间距 s_req <= {sReq:F2} in (强度)");
                
                double finalS = Math.Min(sReq, sMaxShear);
                report.Add($"最终抗剪间距 s <= {finalS:F2} in");
            }
        }

        // Helpers
        private static double CalculateBeta1(double fc)
        {
             if (fc <= 4000) return 0.85;
             if (fc >= 8000) return 0.65;
             return 0.85 - 0.05 * (fc - 4000) / 1000;
        }

        private static bool CheckPointInsideCurve(ColumnDesignCalculations.InteractionPoint pt, List<ColumnDesignCalculations.InteractionPoint> curve, double phiPnMax)
        {
            if (pt.P > phiPnMax) return false;
            
            double maxM = GetMomentCapacityAtP(pt.P, curve);
            return Math.Abs(pt.M) <= maxM;
        }

        private static double GetMomentCapacityAtP(double pu, List<ColumnDesignCalculations.InteractionPoint> curve)
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
    }
}
