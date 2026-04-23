using System.Windows;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using SkiaSharp;

namespace SpectrumComparison
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var chineseFont = SKFontManager.Default.MatchCharacter('中') 
                              ?? SKTypeface.FromFamilyName("Microsoft YaHei")
                              ?? SKTypeface.FromFamilyName("SimSun");

            LiveCharts.Configure(config =>
            {
                config.HasGlobalSKTypeface(chineseFont);
            });
        }
    }
}
