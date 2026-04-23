using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using MathNet.Numerics;
using MathNet.Numerics.IntegralTransforms;
using MathNet.Numerics.Interpolation;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.Providers.LinearAlgebra;

namespace SpectrumComparison
{
    /// <summary>
    /// 人工地震波生成计算类
    /// 基于频域迭代拟合方法，生成符合目标反应谱的人工地震波
    /// </summary>
    public static class ArtificialWaveCalculations
    {
        #region 目标反应谱计算（复用CoreCalculations）

        /// <summary>
        /// 获取中国规范目标反应谱
        /// </summary>
        public static (double[] periods, double[] sa) GetChineseTargetSpectrum(
            string intensity, string siteCategory, string earthquakeGroup, double damping = 0.05)
        {
            double alphaMax = CoreCalculations.GetAlphaMax(intensity);
            double tg = CoreCalculations.GetTg(siteCategory, earthquakeGroup);
            return CoreCalculations.CalculateChineseSpectrum(alphaMax, tg, damping);
        }

        /// <summary>
        /// 获取美国规范目标反应谱
        /// </summary>
        public static (double[] periods, double[] sa, double sds, double sd1) GetUsTargetSpectrum(
            double ss, double s1, string siteClass, double tl, double r, double damping = 0.05)
        {
            var result = CoreCalculations.CalculateUsSpectrum(ss, s1, siteClass, tl, r, damping);
            return (result.periods, result.sa, result.sds, result.sd1);
        }

        #endregion

        #region 三段式包络线

        /// <summary>
        /// 生成三段式非平稳包络线
        /// </summary>
        /// <param name="timeArray">时间数组（秒）</param>
        /// <param name="t1">上升段结束时间</param>
        /// <param name="t2">平稳段结束时间</param>
        /// <param name="cDecay">衰减系数</param>
        /// <returns>包络线数组</returns>
        public static double[] GetEnvelope(double[] timeArray, double t1, double t2, double cDecay)
        {
            var envelope = new double[timeArray.Length];
            for (int i = 0; i < timeArray.Length; i++)
            {
                double t = timeArray[i];
                if (t < t1)
                {
                    // 上升段: (t/t1)²
                    envelope[i] = Math.Pow(t / t1, 2);
                }
                else if (t <= t2)
                {
                    // 平稳段: 1.0
                    envelope[i] = 1.0;
                }
                else
                {
                    // 衰减段: exp(-c*(t-t2))
                    envelope[i] = Math.Exp(-cDecay * (t - t2));
                }
            }
            return envelope;
        }

        #endregion

        #region 反应谱计算（简化方法）

        /// <summary>
        /// 计算地震波的弹性反应谱
        /// 使用简化方法计算单自由度系统的最大响应
        /// </summary>
        /// <param name="acceleration">加速度时程</param>
        /// <param name="dt">时间步长</param>
        /// <param name="periods">周期数组</param>
        /// <param name="damping">阻尼比</param>
        /// <returns>谱加速度数组</returns>
        public static double[] CalculateResponseSpectrum(
            double[] acceleration, double dt, double[] periods, double damping)
        {
            var sa = new double[periods.Length];

            for (int i = 0; i < periods.Length; i++)
            {
                double T = periods[i];
                if (T < 0.001)
                {
                    // PGA
                    sa[i] = acceleration.Max(Math.Abs);
                    continue;
                }

                // 使用线性插值的数值积分方法计算SDOF响应
                sa[i] = CalculateSDOFResponse(acceleration, dt, T, damping);
            }

            return sa;
        }

        private static double CalculateSDOFResponse(double[] acc, double dt, double T, double damping)
        {
            int n = acc.Length;
            double omega = 2 * Math.PI / T;
            double omega2 = omega * omega;
            
            double c = 2.0 / dt;
            double c2 = c * c;
            
            double b0_c = 2 * damping * omega * c + omega2;
            double b1_c = omega2 - 2 * damping * omega * c;
            
            double A0 = c2 + 2 * damping * omega * c + omega2;
            double A1 = 2 * omega2 - 2 * c2;
            double A2 = c2 - 2 * damping * omega * c + omega2;
            
            double b0_d = b0_c / A0;
            double b1_d = b1_c / A0;
            double a1_d = A1 / A0;
            double a2_d = A2 / A0;
            
            double[] resp = new double[n];
            double x1 = 0, y1 = 0, y2 = 0;
            
            for (int i = 0; i < n; i++)
            {
                double x0 = acc[i];
                double y0 = b0_d * x0 + b1_d * x1 - a1_d * y1 - a2_d * y2;
                
                resp[i] = y0;
                x1 = x0;
                y2 = y1;
                y1 = y0;
            }
            
            double maxAbs = 0;
            for (int i = 0; i < n; i++)
            {
                if (Math.Abs(resp[i]) > maxAbs)
                    maxAbs = Math.Abs(resp[i]);
            }
            
            return maxAbs;
        }

        #endregion

        #region 频域操作

        /// <summary>
        /// 实数FFT（返回单边频谱）
        /// </summary>
        private static Complex[] RFFT(double[] signal)
        {
            int n = signal.Length;
            var complexSignal = signal.Select(x => new Complex(x, 0)).ToArray();
            Fourier.Forward(complexSignal, FourierOptions.Matlab);

            // 返回单边频谱（0到Nyquist频率）
            int nFreq = n / 2 + 1;
            var result = new Complex[nFreq];
            Array.Copy(complexSignal, 0, result, 0, nFreq);
            return result;
        }

        /// <summary>
        /// 实数逆FFT
        /// </summary>
        private static double[] IRFFT(Complex[] spectrum, int n)
        {
            var fullSpectrum = new Complex[n];
            Array.Copy(spectrum, 0, fullSpectrum, 0, spectrum.Length);

            // 填充负频率部分（共轭对称）
            for (int i = spectrum.Length; i < n; i++)
            {
                int mirrorIdx = n - i;
                if (mirrorIdx < spectrum.Length)
                {
                    fullSpectrum[i] = Complex.Conjugate(spectrum[mirrorIdx]);
                }
            }

            Fourier.Inverse(fullSpectrum, FourierOptions.Matlab);
            return fullSpectrum.Select(x => x.Real).ToArray();
        }

        /// <summary>
        /// 获取频率数组（对应RFFT输出）
        /// </summary>
        private static double[] RFFTFreq(int n, double d)
        {
            int nFreq = n / 2 + 1;
            var freqs = new double[nFreq];
            for (int i = 0; i < nFreq; i++)
            {
                freqs[i] = i / (n * d);
            }
            return freqs;
        }

        #endregion

        #region 高通滤波（基线校正）

        #endregion

        #region 人工波生成

        /// <summary>
        /// 人工地震波生成的输入参数
        /// </summary>
        public class WaveGenerationInput
        {
            public bool UseChineseCode { get; set; } = true;

            public string? ChinaIntensity { get; set; }
            public string? ChinaSiteCategory { get; set; }
            public string? ChinaEarthquakeGroup { get; set; }

            public double UsSs { get; set; }
            public double UsS1 { get; set; }
            public string? UsSiteClass { get; set; }
            public double UsTl { get; set; }
            public double UsR { get; set; }

            public double Damping { get; set; } = 0.05;

            public double Dt { get; set; } = 0.01;
            public double TTotal { get; set; } = 30.0;
            public int NumberOfWaves { get; set; } = 3;
            public int NumberOfIterations { get; set; } = 5;

            public double T1 { get; set; } = 3.0;
            public double T2 { get; set; } = 15.0;
            public double CDecay { get; set; } = 0.2;

            public double TMin { get; set; } = 0.02;
            public double TMax { get; set; } = 6.0;
            public double TStep { get; set; } = 0.02;
        }

        /// <summary>
        /// 人工地震波生成结果
        /// </summary>
        public class WaveGenerationResult
        {
            public List<double[]> GeneratedWaves { get; set; } = new();
            public double[] TimeArray { get; set; } = Array.Empty<double>();
            public double[] TargetPeriods { get; set; } = Array.Empty<double>();
            public double[] TargetSpectrum { get; set; } = Array.Empty<double>();
            public List<double[]> CalculatedSpectra { get; set; } = new();
            public double[] MeanSpectrum { get; set; } = Array.Empty<double>();
            public List<string> ProcessLog { get; set; } = new();
            public bool Success { get; set; } = true;
            public string? ErrorMessage { get; set; }
        }

        /// <summary>
        /// 生成单条人工地震波（迭代拟合方法）
        /// </summary>
        private static (double[] time, double[] acc) GenerateSingleWave(
            double[] targetPeriods,
            double[] targetSa,
            WaveGenerationInput input,
            Random random)
        {
            int nPts = (int)(input.TTotal / input.Dt);
            var timeArray = new double[nPts];
            for (int i = 0; i < nPts; i++) timeArray[i] = i * input.Dt;

            var envelope = GetEnvelope(timeArray, input.T1, input.T2, input.CDecay);

            var acc = new double[nPts];
            for (int i = 0; i < nPts; i++)
            {
                double u1 = random.NextDouble();
                double u2 = random.NextDouble();
                double z = Math.Sqrt(-2 * Math.Log(u1)) * Math.Cos(2 * Math.PI * u2);
                acc[i] = z * envelope[i];
            }

            var freqs = RFFTFreq(nPts, input.Dt);

            for (int iter = 0; iter < input.NumberOfIterations; iter++)
            {
                var currentSa = CalculateResponseSpectrum(acc, input.Dt, targetPeriods, input.Damping);

                var ratio = new double[targetPeriods.Length];
                for (int i = 0; i < targetPeriods.Length; i++)
                {
                    if (currentSa[i] > 1e-6)
                        ratio[i] = targetSa[i] / currentSa[i];
                    else
                        ratio[i] = 1.0;
                }

                var ratioInterpolation = LinearInterpolationWithFill(targetPeriods, ratio, 1.0);

                var fftAcc = RFFT(acc);
                for (int i = 0; i < fftAcc.Length; i++)
                {
                    double f = freqs[i];
                    if (f < 1e-6)
                    {
                        fftAcc[i] = Complex.Zero;
                        continue;
                    }
                    
                    double T = 1.0 / f;
                    double scaleFactor = ratioInterpolation(T);
                    fftAcc[i] *= scaleFactor;
                }

                acc = IRFFT(fftAcc, nPts);

                for (int i = 0; i < nPts; i++)
                {
                    acc[i] *= envelope[i];
                }

                acc = ButterworthHighPass(acc, input.Dt, 0.05);
            }

            return (timeArray, acc);
        }

        private static Func<double, double> LinearInterpolation(double[] x, double[] y)
        {
            return (double xp) =>
            {
                if (xp <= x[0]) return y[0];
                if (xp >= x[x.Length - 1]) return y[y.Length - 1];

                int i = 0;
                for (int j = 0; j < x.Length - 1; j++)
                {
                    if (xp >= x[j] && xp <= x[j + 1])
                    {
                        i = j;
                        break;
                    }
                }

                double t = (xp - x[i]) / (x[i + 1] - x[i]);
                return y[i] + t * (y[i + 1] - y[i]);
            };
        }

        private static Func<double, double> LinearInterpolationWithFill(double[] x, double[] y, double fillValue)
        {
            return (double xp) =>
            {
                if (xp < x[0] || xp > x[x.Length - 1])
                    return fillValue;

                int i = 0;
                for (int j = 0; j < x.Length - 1; j++)
                {
                    if (xp >= x[j] && xp <= x[j + 1])
                    {
                        i = j;
                        break;
                    }
                }

                double t = (xp - x[i]) / (x[i + 1] - x[i]);
                return y[i] + t * (y[i + 1] - y[i]);
            };
        }

        private static double[] ButterworthHighPass(double[] signal, double dt, double cutOffFreq)
        {
            int n = signal.Length;
            double nyquist = 0.5 / dt;
            double normalizedFreq = cutOffFreq / nyquist;
            
            if (normalizedFreq >= 1.0)
                normalizedFreq = 0.99;
            if (normalizedFreq <= 0.0)
                normalizedFreq = 0.01;

            double wc = Math.Tan(Math.PI * normalizedFreq);
            double wc2 = wc * wc;
            
            double sqrt2 = Math.Sqrt(2.0);
            double cosPi8 = Math.Cos(Math.PI / 8);
            double sinPi8 = Math.Sin(Math.PI / 8);
            double cos3Pi8 = Math.Cos(3 * Math.PI / 8);
            double sin3Pi8 = Math.Sin(3 * Math.PI / 8);
            
            double k1 = 2 * sinPi8;
            double k2 = 2 * sin3Pi8;
            
            double b0_1 = 1.0 / (1.0 + k1 * wc + wc2);
            double b1_1 = -2.0 * b0_1;
            double b2_1 = b0_1;
            double a1_1 = 2.0 * (wc2 - 1.0) * b0_1;
            double a2_1 = (1.0 - k1 * wc + wc2) * b0_1;
            
            double b0_2 = 1.0 / (1.0 + k2 * wc + wc2);
            double b1_2 = -2.0 * b0_2;
            double b2_2 = b0_2;
            double a1_2 = 2.0 * (wc2 - 1.0) * b0_2;
            double a2_2 = (1.0 - k2 * wc + wc2) * b0_2;

            double[] temp1 = new double[n];
            double x1 = 0, x2 = 0, y1 = 0, y2 = 0;
            for (int i = 0; i < n; i++)
            {
                double x = signal[i];
                double y = b0_1 * x + b1_1 * x1 + b2_1 * x2 - a1_1 * y1 - a2_1 * y2;
                temp1[i] = y;
                x2 = x1; x1 = x;
                y2 = y1; y1 = y;
            }

            double[] temp2 = new double[n];
            x1 = 0; x2 = 0; y1 = 0; y2 = 0;
            for (int i = 0; i < n; i++)
            {
                double x = temp1[i];
                double y = b0_2 * x + b1_2 * x1 + b2_2 * x2 - a1_2 * y1 - a2_2 * y2;
                temp2[i] = y;
                x2 = x1; x1 = x;
                y2 = y1; y1 = y;
            }

            double[] temp3 = new double[n];
            x1 = 0; x2 = 0; y1 = 0; y2 = 0;
            for (int i = n - 1; i >= 0; i--)
            {
                double x = temp2[i];
                double y = b0_2 * x + b1_2 * x1 + b2_2 * x2 - a1_2 * y1 - a2_2 * y2;
                temp3[i] = y;
                x2 = x1; x1 = x;
                y2 = y1; y1 = y;
            }

            double[] result = new double[n];
            x1 = 0; x2 = 0; y1 = 0; y2 = 0;
            for (int i = n - 1; i >= 0; i--)
            {
                double x = temp3[i];
                double y = b0_1 * x + b1_1 * x1 + b2_1 * x2 - a1_1 * y1 - a2_1 * y2;
                result[i] = y;
                x2 = x1; x1 = x;
                y2 = y1; y1 = y;
            }

            return result;
        }

        /// <summary>
        /// 批量生成人工地震波
        /// </summary>
        public static WaveGenerationResult GenerateArtificialWaves(WaveGenerationInput input)
        {
            var result = new WaveGenerationResult();
            var log = new List<string>();

            try
            {
                log.Add("=== 人工地震波生成 ===");
                log.Add($"规范: {(input.UseChineseCode ? "中国规范 GB50011-2010" : "美国规范 ASCE 7-16")}");
                log.Add($"波数量: {input.NumberOfWaves}");
                log.Add($"时间步长: {input.Dt} s");
                log.Add($"总时长: {input.TTotal} s");
                log.Add($"迭代次数: {input.NumberOfIterations}");
                log.Add("");

                // 1. 获取目标反应谱
                double[] targetPeriods = Enumerable.Range(0, (int)((input.TMax - input.TMin) / input.TStep) + 1)
                    .Select(i => input.TMin + i * input.TStep).ToArray();

                double[] targetSa;
                if (input.UseChineseCode)
                {
                    log.Add("中国规范参数:");
                    log.Add($"  设防烈度: {input.ChinaIntensity}");
                    log.Add($"  场地类别: {input.ChinaSiteCategory}");
                    log.Add($"  地震分组: {input.ChinaEarthquakeGroup}");

                    if (string.IsNullOrEmpty(input.ChinaIntensity) ||
                        string.IsNullOrEmpty(input.ChinaSiteCategory) ||
                        string.IsNullOrEmpty(input.ChinaEarthquakeGroup))
                    {
                        result.Success = false;
                        result.ErrorMessage = "请完整填写中国规范参数";
                        return result;
                    }

                    (targetPeriods, targetSa) = GetChineseTargetSpectrum(
                        input.ChinaIntensity,
                        input.ChinaSiteCategory,
                        input.ChinaEarthquakeGroup,
                        input.Damping);
                }
                else
                {
                    log.Add("美国规范参数:");
                    log.Add($"  Ss = {input.UsSs}");
                    log.Add($"  S1 = {input.UsS1}");
                    log.Add($"  场地类别: {input.UsSiteClass}");
                    log.Add($"  Tl = {input.UsTl}");
                    log.Add($"  R = {input.UsR}");

                    if (string.IsNullOrEmpty(input.UsSiteClass))
                    {
                        result.Success = false;
                        result.ErrorMessage = "请填写场地类别";
                        return result;
                    }

                    var (periods, sa, sds, sd1) = GetUsTargetSpectrum(
                        input.UsSs, input.UsS1, input.UsSiteClass,
                        input.UsTl, input.UsR, input.Damping);
                    targetPeriods = periods;
                    targetSa = sa;
                }

                log.Add($"目标反应谱计算完成，周期范围: {targetPeriods.Min():F2}-{targetPeriods.Max():F2} s");
                log.Add("");

                result.TargetPeriods = targetPeriods;
                result.TargetSpectrum = targetSa;

                // 2. 生成多条人工波
                var random = new Random();
                var allSpectra = new List<double[]>();

                for (int waveIdx = 0; waveIdx < input.NumberOfWaves; waveIdx++)
                {
                    log.Add($"正在生成第 {waveIdx + 1}/{input.NumberOfWaves} 条波...");

                    var (time, acc) = GenerateSingleWave(targetPeriods, targetSa, input, random);

                    result.GeneratedWaves.Add(acc);
                    if (waveIdx == 0)
                    {
                        result.TimeArray = time;
                    }

                    // 计算该波的反应谱
                    var calcSa = CalculateResponseSpectrum(acc, input.Dt, targetPeriods, input.Damping);
                    allSpectra.Add(calcSa);
                    result.CalculatedSpectra.Add(calcSa);

                    log.Add($"  峰值加速度: {acc.Max(Math.Abs):F4} g");
                }

                log.Add("");

                // 3. 计算平均谱
                result.MeanSpectrum = new double[targetPeriods.Length];
                for (int i = 0; i < targetPeriods.Length; i++)
                {
                    result.MeanSpectrum[i] = allSpectra.Average(s => s[i]);
                }

                log.Add($"平均谱计算完成");
                log.Add("=== 生成完成 ===");

                result.ProcessLog = log;
                result.Success = true;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
                log.Add($"错误: {ex.Message}");
                result.ProcessLog = log;
            }

            return result;
        }

        #endregion

        #region 文件保存

        /// <summary>
        /// 将地震波保存到CSV文件
        /// </summary>
        /// <param name="time">时间数组</param>
        /// <param name="acceleration">加速度数组 (g)</param>
        /// <param name="waveIndex">波序号</param>
        /// <param name="directory">保存目录</param>
        /// <returns>保存的文件路径</returns>
        public static string SaveWaveToCsv(double[] time, double[] acceleration, int waveIndex, string directory)
        {
            if (!System.IO.Directory.Exists(directory))
            {
                System.IO.Directory.CreateDirectory(directory);
            }

            string filename = System.IO.Path.Combine(directory, $"ArtificialWave_{waveIndex + 1:D3}.csv");

            using (var writer = new System.IO.StreamWriter(filename))
            {
                writer.WriteLine("Time(s),Acceleration(g)");
                for (int i = 0; i < time.Length; i++)
                {
                    writer.WriteLine($"{time[i]:F4},{acceleration[i]:F6}");
                }
            }

            return filename;
        }

        #endregion
    }
}
