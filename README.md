本次更新增加了基于ACI318-19的混凝土梁、矩形柱和圆形柱的设计模块，更多细节请参考仓库里的README_CSharp.md，编译完的可执行程序请下载仓库里的UsCodesTools_V20260311.zip。

## 技术栈

| 技术                                    | 版本             | 用途                 |
| ------------------------------------- | -------------- | ------------------ |
| .NET                                  | 8.0            | 运行平台               |
| WPF (Windows Presentation Foundation) | .NET 8 内置      | UI框架               |
| LiveCharts2                           | 2.0.0-rc3.3    | 图表绘制（替代matplotlib） |
| SkiaSharp                             | LiveCharts2 依赖 | 2D图形渲染             |
| MaterialDesignThemes                  | 4.9.0          | UI样式和控件            |
| MVVM 模式                               | -              | 数据绑定和界面分离          |

---

## 项目文件结构

```
ASCE7 vs GB50011/
├── USCodeTools.csproj             # 项目配置文件
├── CoreCalculations.cs            # 核心计算逻辑（反应谱和风速转换）
├── GustEffectFactorCalculations.cs # 风振系数计算逻辑（ASCE 7-16 Section 26.11）
├── BeamDesignCalculations.cs      # 梁截面设计计算逻辑（ACI 318-25）
├── ColumnDesignCalculations.cs      # 柱截面设计计算逻辑（ACI 318-25）
├── CircularColumnDesignCalculations.cs # 圆形柱截面设计计算逻辑（ACI 318-25）
├── SpectrumViewModel.cs           # 反应谱界面ViewModel
├── WindConversionViewModel.cs     # 风速转换界面ViewModel
├── GustEffectFactorViewModel.cs   # 风振系数界面ViewModel
├── BeamDesignViewModel.cs         # 梁截面设计界面ViewModel
├── ColumnDesignViewModel.cs       # 柱截面设计界面ViewModel
├── CircularColumnDesignViewModel.cs # 圆形柱截面设计界面ViewModel
├── MainWindow.xaml                # 主窗口界面
├── MainWindow.xaml.cs             # 主窗口代码后置
├── SpectrumView.xaml              # 反应谱比较界面
├── SpectrumView.xaml.cs           # 反应谱界面代码后置
├── WindConversionView.xaml        # 风速转换界面
├── WindConversionView.xaml.cs     # 风速转换界面代码后置
├── GustEffectFactorView.xaml      # 风振系数界面
├── GustEffectFactorView.xaml.cs   # 风振系数界面代码后置
├── BeamDesignView.xaml            # 梁截面设计界面
├── BeamDesignView.xaml.cs         # 梁截面设计界面代码后置
├── ColumnDesignView.xaml          # 柱截面设计界面
├── ColumnDesignView.xaml.cs       # 柱截面设计界面代码后置
├── CircularColumnDesignView.xaml  # 圆形柱截面设计界面
├── CircularColumnDesignView.xaml.cs # 圆形柱截面设计界面代码后置
├── WeChatView.xaml               # 关注公众号界面
├── WeChatView.xaml.cs            # 关注公众号界面代码后置
├── Styles.xaml                    # Fluent Design样式
├── App.xaml                       # 应用程序资源
└── App.xaml.cs                    # 应用程序入口
```
