using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace SpectrumComparison
{
    public class WindConversionViewModel : INotifyPropertyChanged
    {
        private double _windSpeed = 115;
        private string _windUnit = "mph";
        private double _windHeight = 10;
        private string _windTime = "3s";
        private string _returnPeriod = "700y";

        private string _resultWindSpeed = "50年重现期, 10m高度, 10分钟平均风速: - m/s";
        private string _resultWindPressure = "基本风压: - kN/m";
        private string _processText = "";

        public event PropertyChangedEventHandler? PropertyChanged;

        public WindConversionViewModel()
        {
            ConvertCommand = new RelayCommand(ConvertWindSpeed);
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public double WindSpeed
        {
            get => _windSpeed;
            set { _windSpeed = value; OnPropertyChanged(); }
        }

        public string WindUnit
        {
            get => _windUnit;
            set { _windUnit = value; OnPropertyChanged(); }
        }

        public double WindHeight
        {
            get => _windHeight;
            set { _windHeight = value; OnPropertyChanged(); }
        }

        public string WindTime
        {
            get => _windTime;
            set { _windTime = value; OnPropertyChanged(); }
        }

        public string ReturnPeriod
        {
            get => _returnPeriod;
            set { _returnPeriod = value; OnPropertyChanged(); }
        }

        public string ResultWindSpeed
        {
            get => _resultWindSpeed;
            set { _resultWindSpeed = value; OnPropertyChanged(); }
        }

        public string ResultWindPressure
        {
            get => _resultWindPressure;
            set { _resultWindPressure = value; OnPropertyChanged(); }
        }

        public string ProcessText
        {
            get => _processText;
            set { _processText = value; OnPropertyChanged(); }
        }

        public List<string> WindUnitOptions { get; } = new() { "mph", "m/s" };

        public List<string> WindTimeOptions { get; } = new() { "3s", "10s", "60s", "10min", "1h" };

        public List<string> ReturnPeriodOptions { get; } = new() { "300y", "700y", "1700y", "3000y" };

        public ICommand ConvertCommand { get; }

        private void ConvertWindSpeed()
        {
            try
            {
                var (v10m, w0, process) = CoreCalculations.ConvertWindSpeedToChinese(
                    WindSpeed, WindUnit, WindHeight, WindTime, ReturnPeriod);

                ResultWindSpeed = $"50年重现期, 10m高度, 10分钟平均风速: {v10m:F2} m/s";
                ResultWindPressure = $"基本风压: {w0:F3} kN/m";
                ProcessText = string.Join("\n", process);
            }
            catch (Exception ex)
            {
                ProcessText = $"转换错误: {ex.Message}";
            }
        }
    }

    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool>? _canExecute;

        public RelayCommand(Action execute, Func<bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;

        public void Execute(object? parameter) => _execute();
    }
}
