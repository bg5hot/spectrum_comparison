using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using MathNet.Numerics.IntegralTransforms;

namespace SpectrumComparison
{
    public static class WindSimulationCalculations
    {
        private static readonly Random random = new();

        #region ASCE 7-16 Table 26.11-1

        public class ExposureParameters
        {
            public double l { get; set; }
            public double z_min { get; set; }
            public double b_hat { get; set; }
            public double alpha_hat { get; set; }
            public double L_const { get; set; }
            public double epsilon_hat { get; set; }
        }

        private static readonly Dictionary<string, ExposureParameters> AsceTable = new()
        {
            ["B"] = new ExposureParameters { l = 0.30, z_min = 30, b_hat = 0.45, alpha_hat = 1/4.0, L_const = 320, epsilon_hat = 1/3.0 },
            ["C"] = new ExposureParameters { l = 0.20, z_min = 15, b_hat = 0.65, alpha_hat = 1/6.5, L_const = 500, epsilon_hat = 1/5.0 },
            ["D"] = new ExposureParameters { l = 0.15, z_min = 7.0, b_hat = 0.80, alpha_hat = 1/9.0, L_const = 650, epsilon_hat = 1/8.0 }
        };

        #endregion

        #region 输入输出

        public class WindSimInput
        {
            public double VRef { get; set; } = 115;
            public string Exposure { get; set; } = "C";
            public double Height { get; set; } = 100;
            public double Duration { get; set; } = 600;
            public double SampleRate { get; set; } = 20;
        }

        public class WindSimResult
        {
            public bool Success { get; set; }
            public string? ErrorMessage { get; set; }

            public List<string> ProcessLog { get; set; } = new();
            
            public string Exposure { get; set; } = "";
            public double HeightFt { get; set; }
            public double HeightEffFt { get; set; }
            public double VAvg { get; set; }
            public double Iz { get; set; }
            public double SigmaU { get; set; }
            public double Lz { get; set; }

            public double[] TimeArray { get; set; } = Array.Empty<double>();
            public double[] FluctuatingWind { get; set; } = Array.Empty<double>();
            public double[] TotalWind { get; set; } = Array.Empty<double>();
            public double[] TargetPSD_Freq { get; set; } = Array.Empty<double>();
            public double[] TargetPSD { get; set; } = Array.Empty<double>();
            public double[] SimulatedPSD_Freq { get; set; } = Array.Empty<double>();
            public double[] SimulatedPSD { get; set; } = Array.Empty<double>();
        }

        #endregion

        #region 核心计算

        public static WindSimResult GenerateWind(WindSimInput input)
        {
            var result = new WindSimResult();
            var log = new List<string>();

            try
            {
                log.Add("=== 美标脉动风模拟 (ASCE 7-16) ===");
                log.Add($"基本风速: {input.VRef} mph");
                log.Add($"场地类别: {input.Exposure}");
                log.Add($"目标高度: {input.Height} ft");
                log.Add($"模拟时长: {input.Duration} s");
                log.Add($"采样频率: {input.SampleRate} Hz");

                if (!AsceTable.ContainsKey(input.Exposure))
                {
                    result.Success = false;
                    result.ErrorMessage = $"无效的场地类别: {input.Exposure}";
                    return result;
                }

                var spec = AsceTable[input.Exposure];
                result.Exposure = input.Exposure;

                double z_eff = Math.Max(input.Height, spec.z_min);
                result.HeightFt = input.Height;
                result.HeightEffFt = z_eff;

                result.VAvg = spec.b_hat * Math.Pow(z_eff / 33, spec.alpha_hat) * input.VRef * (88.0 / 60.0);

                result.Iz = spec.l * Math.Pow(33 / z_eff, 1.0 / 6);
                result.SigmaU = result.Iz * result.VAvg;

                result.Lz = spec.L_const * Math.Pow(z_eff / 33, spec.epsilon_hat);

                log.Add("");
                log.Add("--- 计算结果 ---");
                log.Add($"有效高度: {z_eff:F1} ft");
                log.Add($"平均风速 Vz: {result.VAvg:F2} ft/s");
                log.Add($"湍流强度 Iz: {result.Iz:F4}");
                log.Add($"标准差 σu: {result.SigmaU:F3} ft/s");
                log.Add($"积分尺度 Lz: {result.Lz:F2} ft");

                int N = (int)(input.Duration * input.SampleRate);
                double df = 1.0 / input.Duration;

                int nFreq = N / 2;
                var f = new double[nFreq];
                for (int i = 0; i < nFreq; i++)
                    f[i] = (i + 1) * df;

                var S_target = new double[nFreq];
                for (int i = 0; i < nFreq; i++)
                {
                    double n_val = f[i] * result.Lz / result.VAvg;
                    S_target[i] = (4 * result.SigmaU * result.SigmaU * result.Lz / result.VAvg) / Math.Pow(1 + 6 * n_val, 5.0 / 3);
                }

                var phi = new double[nFreq];
                for (int i = 0; i < nFreq; i++)
                    phi[i] = random.NextDouble() * 2 * Math.PI;

                var A = new double[nFreq];
                for (int i = 0; i < nFreq; i++)
                    A[i] = Math.Sqrt(2 * S_target[i] * df) * N / 2;

                var complexSpectrum = new Complex[N];
                for (int i = 1; i <= nFreq; i++)
                {
                    double phase = random.NextDouble() * 2 * Math.PI;
                    // 能量縮放係數 (MathNet Matlab 模式需要乘以 N/sqrt(2))
                    double amplitude = Math.Sqrt(S_target[i - 1] * df * N * N / 2.0);
                    complexSpectrum[i] = Complex.FromPolarCoordinates(amplitude, phase);
                    
                    if (i < nFreq) 
                        complexSpectrum[N - i] = Complex.Conjugate(complexSpectrum[i]);
                }
                // Nyquist 頻率處理
                complexSpectrum[nFreq] = new Complex(complexSpectrum[nFreq].Magnitude, 0);

                Fourier.Inverse(complexSpectrum, FourierOptions.Matlab);

                var u_t = new double[N];
                for (int i = 0; i < N; i++)
                    u_t[i] = complexSpectrum[i].Real;

                var timeArray = new double[N];
                for (int i = 0; i < N; i++)
                    timeArray[i] = i / input.SampleRate;

                var totalV = new double[N];
                for (int i = 0; i < N; i++)
                    totalV[i] = result.VAvg + u_t[i];

                result.TimeArray = timeArray;
                result.FluctuatingWind = u_t;
                result.TotalWind = totalV;

                result.TargetPSD_Freq = f;
                result.TargetPSD = S_target;

                CalculateWelchPSD(u_t, input.SampleRate, out var welchFreq, out var welchPsd);
                result.SimulatedPSD_Freq = welchFreq;
                result.SimulatedPSD = welchPsd;

                result.ProcessLog = log;
                result.Success = true;

                return result;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
                return result;
            }
        }

        private static void CalculateWelchPSD(double[] signal, double fs, out double[] freqOut, out double[] psdOut)
        {
            int nperseg = 2048; 
            int noverlap = nperseg / 2;
            int n = signal.Length;
            int step = nperseg - noverlap;
            int numSegments = (n - noverlap) / step;

            var psdAvg = new double[nperseg / 2 + 1];
            double windowSumSquares = 0;
            
            // 漢寧窗與能量補償
            var window = new double[nperseg];
            for (int i = 0; i < nperseg; i++)
            {
                window[i] = 0.5 * (1 - Math.Cos(2 * Math.PI * i / (nperseg - 1)));
                windowSumSquares += window[i] * window[i];
            }

            for (int s = 0; s < numSegments; s++)
            {
                var segment = new Complex[nperseg];
                int offset = s * step;
                for (int i = 0; i < nperseg; i++)
                    segment[i] = new Complex(signal[offset + i] * window[i], 0);

                Fourier.Forward(segment, FourierOptions.Matlab);

                for (int i = 0; i <= nperseg / 2; i++)
                    psdAvg[i] += (segment[i].Magnitude * segment[i].Magnitude);
            }

            freqOut = new double[nperseg / 2 + 1];
            psdOut = new double[nperseg / 2 + 1];
            
            // 單邊譜轉換係數 2.0 / (fs * 窗能量 * 段數)
            double scale = 2.0 / (fs * windowSumSquares * numSegments);

            for (int i = 0; i <= nperseg / 2; i++)
            {
                freqOut[i] = i * (fs / nperseg);
                double val = psdAvg[i] * scale;
                if (i == 0 || i == nperseg / 2) val /= 2.0;
                psdOut[i] = val;
            }
        }

        #endregion

        #region 文件保存

        public static string SaveWindToCsv(double[] timeArray, double[] totalWind, double[] fluctuatingWind, string saveDirectory)
        {
            Directory.CreateDirectory(saveDirectory);
            string filename = Path.Combine(saveDirectory, $"Wind_Simulation_{DateTime.Now:yyyyMMdd_HHmmss}.csv");

            using (var writer = new StreamWriter(filename))
            {
                writer.WriteLine("Time(s),Total_Wind(ft/s),Fluctuating_Wind(ft/s)");
                for (int i = 0; i < timeArray.Length; i++)
                {
                    writer.WriteLine($"{timeArray[i]:F6},{totalWind[i]:F6},{fluctuatingWind[i]:F6}");
                }
            }

            return filename;
        }

        #endregion
    }
}
