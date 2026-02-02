using System;
using System.Collections.Generic;
using System.Linq;

namespace SpectrumComparison
{
    public static class CoreCalculations
    {
        public static double GetAlphaMax(string intensity)
        {
            var alphaMaxTable = new Dictionary<string, double>
            {
                ["6度(0.05g)"] = 0.04,
                ["7度(0.10g)"] = 0.08,
                ["7度(0.15g)"] = 0.12,
                ["8度(0.20g)"] = 0.16,
                ["8度(0.30g)"] = 0.24,
                ["9度(0.40g)"] = 0.32
            };
            return alphaMaxTable.TryGetValue(intensity, out var value) ? value : 0.08;
        }

        public static double GetTg(string siteCategory, string earthquakeGroup)
        {
            var tgTable = new Dictionary<string, Dictionary<string, double>>
            {
                ["第一组"] = new Dictionary<string, double>
                {
                    ["I0"] = 0.20, ["I1"] = 0.25, ["II"] = 0.35, ["III"] = 0.45, ["IV"] = 0.65
                },
                ["第二组"] = new Dictionary<string, double>
                {
                    ["I0"] = 0.25, ["I1"] = 0.30, ["II"] = 0.40, ["III"] = 0.55, ["IV"] = 0.75
                },
                ["第三组"] = new Dictionary<string, double>
                {
                    ["I0"] = 0.30, ["I1"] = 0.35, ["II"] = 0.45, ["III"] = 0.65, ["IV"] = 0.90
                }
            };

            if (tgTable.TryGetValue(earthquakeGroup, out var groupDict))
            {
                if (groupDict.TryGetValue(siteCategory, out var value))
                {
                    return value;
                }
            }
            return 0.35;
        }

        public static (double[] periods, double[] alpha) CalculateChineseSpectrum(double alphaMax, double tg, double damping = 0.05)
        {
            double gamma = 0.9 + (0.05 - damping) / (0.3 + 6 * damping);
            double eta1 = 0.02 + (0.05 - damping) / (4 + 32 * damping);
            if (eta1 < 0) eta1 = 0;
            double eta2 = 1 + (0.05 - damping) / (0.08 + 1.6 * damping);
            if (eta2 < 0.55) eta2 = 0.55;

            int nPoints = 600;
            double[] periods = new double[nPoints];
            double[] alpha = new double[nPoints];

            for (int i = 0; i < nPoints; i++)
            {
                periods[i] = 0.01 + i * (6.0 - 0.01) / (nPoints - 1);
                double T = periods[i];

                if (T < 0.1)
                {
                    alpha[i] = (0.45 * alphaMax) + (eta2 - 0.45) * alphaMax * (T / 0.1);
                }
                else if (T < tg)
                {
                    alpha[i] = eta2 * alphaMax;
                }
                else if (T < 5 * tg)
                {
                    alpha[i] = eta2 * alphaMax * Math.Pow(tg / T, gamma);
                }
                else
                {
                    alpha[i] = (eta2 * Math.Pow(0.2, gamma) - eta1 * (T - 5 * tg)) * alphaMax;
                }
            }

            return (periods, alpha);
        }

        public static (double fa, double fv) GetFaFv(double ss, double s1, string siteClass)
        {
            var faTable = new Dictionary<string, Dictionary<double, double>>
            {
                ["A"] = new Dictionary<double, double> { [0.25] = 0.8, [0.5] = 0.8, [0.75] = 0.8, [1.0] = 0.8, [1.25] = 0.8, [1.50] = 0.8 },
                ["B"] = new Dictionary<double, double> { [0.25] = 0.9, [0.5] = 0.9, [0.75] = 0.9, [1.0] = 0.9, [1.25] = 0.9, [1.50] = 0.9 },
                ["C"] = new Dictionary<double, double> { [0.25] = 1.3, [0.5] = 1.3, [0.75] = 1.2, [1.0] = 1.2, [1.25] = 1.2, [1.50] = 1.2 },
                ["D"] = new Dictionary<double, double> { [0.25] = 1.6, [0.5] = 1.4, [0.75] = 1.2, [1.0] = 1.1, [1.25] = 1.0, [1.50] = 1.0 }
            };

            var fvTable = new Dictionary<string, Dictionary<double, double>>
            {
                ["A"] = new Dictionary<double, double> { [0.1] = 0.8, [0.2] = 0.8, [0.3] = 0.8, [0.4] = 0.8, [0.5] = 0.8, [0.6] = 0.8 },
                ["B"] = new Dictionary<double, double> { [0.1] = 0.8, [0.2] = 0.8, [0.3] = 0.8, [0.4] = 0.8, [0.5] = 0.8, [0.6] = 0.8 },
                ["C"] = new Dictionary<double, double> { [0.1] = 1.5, [0.2] = 1.5, [0.3] = 1.5, [0.4] = 1.5, [0.5] = 1.5, [0.6] = 1.4 },
                ["D"] = new Dictionary<double, double> { [0.1] = 2.4, [0.2] = 2.2, [0.3] = 2.0, [0.4] = 1.9, [0.5] = 1.8, [0.6] = 1.7 }
            };

            double Fa = InterpolateValue(faTable[siteClass], ss);
            double Fv = InterpolateValue(fvTable[siteClass], s1);

            return (Fa, Fv);
        }

        private static double InterpolateValue(Dictionary<double, double> table, double value)
        {
            var keys = table.Keys.OrderBy(k => k).ToList();
            if (value <= keys[0]) return table[keys[0]];
            if (value >= keys[keys.Count - 1]) return table[keys[keys.Count - 1]];

            for (int i = 0; i < keys.Count - 1; i++)
            {
                if (keys[i] <= value && value < keys[i + 1])
                {
                    double x1 = keys[i], y1 = table[keys[i]];
                    double x2 = keys[i + 1], y2 = table[keys[i + 1]];
                    return y1 + (y2 - y1) * (value - x1) / (x2 - x1);
                }
            }
            return table[keys[0]];
        }

        public static (double[] periods, double[] sa, double sds, double sd1, double fa, double fv) 
            CalculateUsSpectrum(double ss, double s1, string siteClass, double tl, double r, double damping = 0.05)
        {
            var (Fa, Fv) = GetFaFv(ss, s1, siteClass);
            double SMS = Fa * ss;
            double SM1 = Fv * s1;
            double SDS = (2.0 / 3.0) * SMS;
            double SD1 = (2.0 / 3.0) * SM1;
            double T0 = SDS != 0 ? 0.2 * (SD1 / SDS) : 0;
            double Ts = SDS != 0 ? SD1 / SDS : 0;

            double B;
            if (damping <= 0.02)
                B = 0.8;
            else if (damping <= 0.05)
                B = 0.8 + 0.2 * (damping - 0.02) / 0.03;
            else if (damping <= 0.10)
                B = 1.0 + 0.2 * (damping - 0.05) / 0.05;
            else if (damping <= 0.20)
                B = 1.2 + 0.3 * (damping - 0.10) / 0.10;
            else
                B = 1.5;

            int nPoints = 600;
            double[] periods = new double[nPoints];
            double[] sa = new double[nPoints];

            for (int i = 0; i < nPoints; i++)
            {
                periods[i] = 0.01 + i * (6.0 - 0.01) / (nPoints - 1);
                double T = periods[i];
                double s;

                if (T < T0)
                    s = SDS * (0.4 + 0.6 * T / T0);
                else if (T0 <= T && T < Ts)
                    s = SDS;
                else if (Ts <= T && T < tl)
                    s = SD1 / T;
                else
                    s = SD1 * tl / (T * T);

                sa[i] = s / r / B;
            }

            return (periods, sa, SDS, SD1, Fa, Fv);
        }

        public static (double windSpeed50y10m10min, double basicWindPressure, List<string> process) 
            ConvertWindSpeedToChinese(double windSpeed, string inputUnit, double inputHeight, string inputTime, string returnPeriod)
        {
            var process = new List<string>();

            double windSpeedMs;
            if (inputUnit == "mph")
            {
                windSpeedMs = windSpeed * 0.44704;
                process.Add($"单位转换: {windSpeed:F2} mph = {windSpeedMs:F2} m/s");
            }
            else
            {
                windSpeedMs = windSpeed;
                process.Add($"风速: {windSpeedMs:F2} m/s");
            }

            var returnPeriodFactors = new Dictionary<string, double>
            {
                ["300y"] = 1.179,
                ["700y"] = 1.264,
                ["1700y"] = 1.352,
                ["3000y"] = 1.409
            };
            double rpFactor = returnPeriodFactors.GetValueOrDefault(returnPeriod, 1.26);
            double v50 = windSpeedMs / rpFactor;
            process.Add($"重现期转换 ({returnPeriod} -> 50y): {windSpeedMs:F2} / {rpFactor:F2} = {v50:F2} m/s");

            var timeFactors = new Dictionary<string, double>
            {
                ["3s"] = 1.52,
                ["10s"] = 1.43,
                ["60s"] = 1.27,
                ["10min"] = 1.06,
                ["1h"] = 1.00
            };
            double v1h = v50 / timeFactors.GetValueOrDefault(inputTime, 1.52);
            double v10min = v1h * timeFactors["10min"];
            process.Add($"时距转换 ({inputTime} -> 10min): {v50:F2} / {timeFactors.GetValueOrDefault(inputTime, 1.52):F2} * 1.06 = {v10min:F2} m/s");

            double alpha = 0.15;
            double v10m;
            if (inputHeight != 10)
            {
                v10m = v10min * Math.Pow(10 / inputHeight, alpha);
                process.Add($"高度转换 ({inputHeight}m -> 10m): {v10min:F2} * (10/{inputHeight})^{alpha:F2} = {v10m:F2} m/s");
            }
            else
            {
                v10m = v10min;
                process.Add($"高度已是10m，无需转换: {v10m:F2} m/s");
            }

            double rho = 1.25;
            double w0 = 0.5 * rho * v10m * v10m / 1000;
            process.Add($"基本风压计算: w0 = 0.5 * {rho} * {v10m:F2}^2 / 1000 = {w0:F3} kN/m");

            return (v10m, w0, process);
        }
    }
}
