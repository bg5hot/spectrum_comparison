# Claude 协作指南 - 中美规范参数转换工具

一个基于 .NET 8 和 WPF 的结构工程计算工具，用于中美规范（ASCE 7、ACI 318 vs GB50011、GB50009）参数转换和结构构件设计验算。

## 项目概述

本软件是一个 Windows 桌面应用程序，提供以下核心功能：

1. **地震反应谱比较** - 中美规范地震反应谱对比分析
2. **地震人工波模拟** - 基于频域迭代拟合方法生成符合目标反应谱的人工地震波
3. **风速转换** - ASCE 7 风速转换为 GB50009 基本风压
4. **风振系数计算** - ASCE 7-16 Section 26.11 阵风效应因子
5. **风荷载模拟** - ASCE 7-16 脉动风时程模拟（Kaimal 谱 + FFT 方法）
6. **梁截面设计** - ACI 318-25 梁截面受弯受剪设计
7. **矩形柱设计** - ACI 318-19 矩形柱双向偏压设计（P-M交互图）
8. **圆形柱设计** - ACI 318-19 圆形柱双向偏压设计（螺旋/绑扎箍筋）
9. **双向板抗冲切** - ACI 318-19 板柱节点抗冲切验算
10. **钢筋锚固长度** - ACI 318-19 受拉直钢筋锚固长度 ℓd 计算

## 技术栈

- **.NET 8** - 运行平台
- **WPF** - UI 框架（内置 .NET 8）
- **MVVM 模式** - 架构模式，实现界面与逻辑分离
- **LiveChartsCore.SkiaSharpView.WPF 2.0.0-rc3.3** - 图表绘制
- **MaterialDesignThemes 5.1.0** - Material Design UI 样式
- **MathNet.Numerics 5.0.0** - 数值计算（FFT、插值、线性代数）

## 项目架构

### MVVM 架构模式

项目严格遵循 Model-View-ViewModel 架构：

```
┌─────────────┐     ┌──────────────┐     ┌─────────────┐
│    View     │◄────┤  ViewModel   │────►│    Model    │
│  (.xaml)    │     │  (.cs)       │     │ (.cs)       │
└─────────────┘     └──────────────┘     └─────────────┘
     ▲                       │
     │                       │
     └───────DataBinding──────┘
```

- **View**: XAML 文件，负责 UI 布局和数据绑定
- **ViewModel**: C# 类，实现 `INotifyPropertyChanged`，包含界面逻辑和状态
- **Model**: 计算类和静态方法，纯业务逻辑

### 文件命名约定

- **计算类**: `<Module>Calculations.cs`（如 `BeamDesignCalculations.cs`）
- **视图**: `<Module>View.xaml`
- **ViewModel**: `<Module>ViewModel.cs`
- **视图代码后置**: `<Module>View.xaml.cs`（通常只包含 `InitializeComponent`）

### 核心文件结构

```
USCodeTools.csproj          # 项目配置
CoreCalculations.cs         # 核心：反应谱和风速转换
ArtificialWaveCalculations.cs  # 人工地震波生成（频域迭代拟合）
GustEffectFactorCalculations.cs  # 风振系数计算
WindSimulationCalculations.cs  # 风荷载模拟（Kaimal谱+FFT）
BeamDesignCalculations.cs   # 梁设计计算
ColumnDesignCalculations.cs # 矩形柱设计计算
CircularColumnDesignCalculations.cs  # 圆形柱设计计算
PunchingShearCalculations.cs  # 抗冲切计算
DevelopmentLengthCalculations.cs  # 锚固长度计算
BooleanToVisibilityConverter.cs  # WPF 布尔-可见性转换器
InverseBoolConverter.cs     # WPF 布尔取反转换器
RelayCommand.cs             # ICommand 实现基类
MainWindow.xaml/cs          # 主窗口（导航）
Styles.xaml                 # 全局样式
```

## 代码规范

### ViewModel 类模式

所有 ViewModel 类必须遵循以下模式：

```csharp
public class ModuleViewModel : INotifyPropertyChanged
{
    // 1. 私有字段（使用 _camelCase 前缀）
    private double _fc = 4000;
    private string _resultText = "";

    // 2. 属性（使用 PascalCase）
    public double Fc
    {
        get => _fc;
        set { _fc = value; OnPropertyChanged(); }
    }

    // 3. PropertyChanged 事件
    public event PropertyChangedEventHandler? PropertyChanged;

    // 4. OnPropertyChanged 方法
    protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    // 5. 命令（使用 RelayCommand）
    public ICommand CalculateCommand { get; }

    // 6. 构造函数
    public ModuleViewModel()
    {
        CalculateCommand = new RelayCommand(Calculate);
    }

    // 7. 计算方法
    private void Calculate()
    {
        // 调用 Model 层计算方法
        var result = CalculationsClass.Calculate(...);
        // 更新显示属性
        ResultText = FormatResult(result);
    }
}
```

### 计算类模式

所有计算类使用静态方法：

```csharp
public static class ModuleCalculations
{
    // 输入参数类
    public class InputParameters
    {
        public double Fc { get; set; }
        public double Fy { get; set; }
        // ...
    }

    // 结果类
    public class DesignResult
    {
        public bool IsSafe { get; set; }
        public string Summary { get; set; }
        public List<string> Process { get; set; } = new();
    }

    // 主计算方法
    public static DesignResult Calculate(InputParameters input)
    {
        var result = new DesignResult();
        // 计算逻辑
        // 记录详细过程
        result.Process.Add("步骤1: ...");
        return result;
    }
}
```

### 单位约定

**英制单位（美国规范计算）**:

- 长度: inches (in), feet (ft)
- 力: pounds (lbs), kips (1 kip = 1000 lbs)
- 应力: psi (lbs/in²), ksi (kips/in²)
- 弯矩: kip-ft, lb-in
- 质量: 不直接使用，通过力/重力加速度推导

**国际单位制（中国规范及风振系数）**:

- 长度: 米 (m), 毫米 (mm)
- 速度: 米/秒 (m/s)
- 力: 牛顿 (N), 千牛 (kN)
- 应力: 帕斯卡 (Pa), 兆帕 (MPa)
- 时间: 秒 (s), 分钟

**注意**: ACI 规范相关计算使用英制单位，最终界面显示时保持英制。用户如需国际单位，应自行转换。

### 数值精度

- 工程计算结果保留 2-3 位有效数字
- 中间计算过程保留更高精度，避免累积误差
- 显示格式使用 `F2`、`F3` 或 `N2` 格式说明符

## XAML 绑定规范

### 基本绑定

```xml
<!-- 双向绑定输入框 -->
<TextBox Text="{Binding Fc, UpdateSourceTrigger=PropertyChanged}" />

<!-- 只读结果显示 -->
<TextBlock Text="{Binding ResultText}" />

<!-- 下拉选择 -->
<ComboBox ItemsSource="{Binding Options}" 
          SelectedItem="{Binding SelectedOption}" />
```

### 布局模式

使用 Grid 和 MaterialDesign 卡片：

```xml
<materialDesign:Card Padding="16">
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
        </Grid.RowDefinitions>
        <Grid.ColumnDefinitions>
            <ColumnDefinition Width="*"/>
            <ColumnDefinition Width="*"/>
        </Grid.ColumnDefinitions>
    </Grid>
</materialDesign:Card>
```

### LiveCharts 绑定

```xml
<lvc:CartesianChart Series="{Binding Series}">
    <lvc:CartesianChart.XAxes>
        <lvc:Axis Title="{Binding XAxisTitle}"/>
    </lvc:CartesianChart.XAxes>
</lvc:CartesianChart>
```

## 现有功能模块

### 1. 反应谱比较 (SpectrumView)

**命名空间**: `SpectrumComparison`

**核心计算**:

- `CoreCalculations.GetAlphaMax()` - 获取中国规范 αmax
- `CoreCalculations.GetTg()` - 获取中国规范特征周期
- `CoreCalculations.CalculateChineseSpectrum()` - 计算中国规范反应谱
- `CoreCalculations.GetFaFv()` - 获取美国规范场地系数
- `CoreCalculations.CalculateUsSpectrum()` - 计算美国规范设计反应谱

**ViewModel**: `SpectrumViewModel`

- 属性: Damping, ChinaIntensity, ChinaSiteCategory, ChinaEarthquakeGroup
- 属性: UsSs, UsS1, UsTl, UsR, UsSiteClass
- 图表: Series (ISeries[]), XAxes, YAxes

### 2. 风速转换 (WindConversionView)

**核心计算**:

- `CoreCalculations.ConvertWindSpeedToChinese()` - ASCE 7 → GB50009 风速/风压转换

**ViewModel**: `WindConversionViewModel`

- 输入: WindSpeed, WindUnit, WindHeight, WindTime, ReturnPeriod
- 输出: ResultWindSpeed, ResultWindPressure, ProcessText
- 命令: ConvertCommand

### 3. 地震人工波模拟 (ArtificialWaveView)

**命名空间**: `SpectrumComparison`

**核心计算**:

- `ArtificialWaveCalculations.GenerateArtificialWaves()` - 频域迭代拟合生成人工地震波
- `ArtificialWaveCalculations.CalculateResponseSpectrum()` - 双线性变换IIR滤波器计算弹性反应谱
- `ArtificialWaveCalculations.GetEnvelope()` - 三段式非平稳包络线（上升/平稳/衰减）
- `ArtificialWaveCalculations.ButterworthHighPass()` - 4阶Butterworth高通滤波（基线校正）
- `ArtificialWaveCalculations.SaveWaveToCsv()` - 导出CSV文件

**ViewModel**: `ArtificialWaveViewModel`

- 规范选择: UseChineseCode（切换中国/美国规范参数面板）
- 中国规范参数: ChinaIntensity, ChinaSiteCategory, ChinaEarthquakeGroup
- 美国规范参数: UsSs, UsS1, UsSiteClass, UsTl, UsR
- 时程参数: NumberOfWaves, Dt, TTotal, NumberOfIterations
- 包络线参数: T1（上升段结束）, T2（平稳段结束）, CDecay（衰减系数）
- 结果: ResultText, ProcessText, Series（反应谱对比图）
- 命令: GenerateCommand（异步生成）, SaveCommand（导出CSV）

**特性**:

- 复用 `CoreCalculations` 获取中美规范目标反应谱
- 使用 MathNet.Numerics 进行 FFT 和数值计算
- 异步生成避免UI冻结
- 反应谱对比图：目标谱（红色）、平均谱（蓝色）、各波谱（灰色）

### 4. 风振系数 (GustEffectFactorView)

**命名空间**: `SpectrumComparison.GustEffectFactor`

**核心计算**:

- `GustEffectFactorCalculations.CalculateGustEffectFactor()` - ASCE 7-16 Section 26.11

**ViewModel**: `GustEffectFactorViewModel`

- 输入: Exposure (B/C/D), BuildingHeight, BuildingWidth, BuildingDepth, WindSpeed, NaturalPeriod
- 输出: BuildingType, GustEffectFactor, GValue, GfValue, ProcessText
- 自动判断刚性建筑 (n₁ ≥ 1 Hz) 或柔性建筑 (n₁ < 1 Hz)

### 5. 风荷载模拟 (WindSimulationView)

**命名空间**: `SpectrumComparison`

**核心计算**:

- `WindSimulationCalculations.GenerateWind()` - ASCE 7-16 脉动风时程模拟
- `WindSimulationCalculations.CalculateWelchPSD()` - Welch 方法功率谱密度计算

**ViewModel**: `WindSimulationViewModel`

- 输入: VRef (基本风速, mph), Exposure (B/C/D), Height (目标高度, ft), Duration, SampleRate
- 输出: VAvg (平均风速), Iz (湍流强度), Lz (积分尺度), ProcessText
- 图表: WindPlotModel (风速时程图), PsdPlotModel (PSD对比图, OxyPlot)
- 命令: GenerateCommand（异步生成）, SaveCommand（导出CSV）

**特性**:

- Kaimal 谱: ASCE 7-16 标准风工程功率谱模型
- FFT 随机相位法: 频域合成 + 逆FFT生成时程
- Welch 法 PSD 验证: 汉宁窗 + 重叠分段
- OxyPlot 双对数图: 目标谱（红色）vs 模拟谱（黑色）
- 英制单位: mph, ft, ft/s

### 6. 梁截面设计 (BeamDesignView)

**核心计算**:

- `BeamDesignCalculations.CalculateFlexuralReinforcement()` - ACI 318-25 受弯配筋
- `BeamDesignCalculations.CalculateShearReinforcement()` - ACI 318-25 抗剪配筋

**ViewModel**: `BeamDesignViewModel`

- 材料参数: Fc (psi), Fy (psi)
- 截面尺寸: B, H, Cover, StirrupDiameter (in)
- 荷载: Mu (kip-ft), Vu (kips), Nu (kips)
- 钢筋选择: SelectedBarSize, SelectedStirrupSize, StirrupLegs
- 结果: RequiredSteelArea, ProvidedSteelArea, NumberOfBars, PhiMnResult, DesignStatus, ShearResult

**特性**:

- 自动更新箍筋直径
- 支持钢筋根数自动计算
- 实时钢筋预览

### 7. 矩形柱设计 (ColumnDesignView)

**核心计算**:

- `ColumnDesignCalculations.CheckColumn()` - ACI 318-19 双向偏压验算

**ViewModel**: `ColumnDesignViewModel`

- 材料: Fc, Fy
- 截面: B, H, Cover
- 钢筋: SelectedBarSize, SelectedTieSize, TieLegsX, TieLegsY, Nx, Ny
- 荷载: Pu, Mux, Muy, Vux, Vuy
- 图表: SeriesX, SeriesY (P-M 交互图)
- 预览: RebarLocations (钢筋位置可视化)

**计算内容**:

- P-M 承载力验算（双向）
- 抗剪验算
- 配筋率验算 (1%-8%)
- 最小根数验算 (≥4 根)
- 纵筋间距验算

### 8. 圆形柱设计 (CircularColumnDesignView)

**核心计算**:

- `CircularColumnDesignCalculations.CheckColumn()` - ACI 318-19 圆形柱验算

**ViewModel**: `CircularColumnDesignViewModel`

- 截面: Diameter, Cover
- 钢筋: SelectedBarSize, SelectedTieSize, NumBars (≥6), SelectedTieType (Tied/Spiral)
- 弯矩: 自动计算合成弯矩 Mu = √(Mux² + Muy²)
- 各向同性: 各方向承载力相同

**箍筋类型**:

- `TieType.Tied`: φ = 0.65
- `TieType.Spiral`: φ = 0.75

### 9. 双向板抗冲切 (PunchingShearView)

**核心计算**:

- `PunchingShearCalculations.PerformDesign()` - ACI 318-19 抗冲切验算

**ViewModel**: `PunchingShearViewModel`

- 板: H, Cover, SelectedBarSize
- 柱: C1, C2, ColumnLocation (Interior/Edge/Corner)
- 荷载: Vu, Msc
- 材料: Fc, Lambda, Fyt
- 抗剪钢筋: ReinforcementType (None/Stirrups/HeadedStuds), StudSize, StudsPerPerimeter, StudSpacing

**特性**:

- 尺寸效应系数 λs = √(2/(1 + d/10))
- 三种柱位置支持
- 抗剪栓钉/箍筋设计
- 构造要求检查

### 10. 钢筋锚固长度 (DevelopmentLengthView)

**核心计算**:

- `DevelopmentLengthCalculations.Calculate()` - ACI 318-19 ℓd 计算

**ViewModel**: `DevelopmentLengthViewModel`

- 材料: Fc, ConcreteType, SteelGrade
- 钢筋: BarSize, Coating, CastingPosition
- 方法: CalculationMethod (Detailed/QuickCalc)
- 约束参数（精确法）: Cc, S, Atr, Str, N
- 条件选择（简化法）: SpacingCoverCondition
- 抗震: IsSFRS, IsYieldZone, AsRequired, AsProvided

**计算步骤**:

1. 修正系数: ψt, ψe, ψs, ψg, λ
2. 约束参数: cb, Ktr, (cb+Ktr)/db ≤ 2.5
3. 基础锚固长度: 精确公式或简化查表
4. 多余钢筋折减: SFRS 禁止折减
5. 抗震预期屈服区放大: 1.25 倍
6. 最小锚固长度: ≥ 12 in

## 新功能开发指南

### 添加新的计算模块

1. **创建计算类** (`<Module>Calculations.cs`):
   
   ```csharp
   namespace SpectrumComparison
   {
       public static class ModuleCalculations
       {
           public class InputParameters { }
           public class DesignResult { }
           public static DesignResult Calculate(InputParameters input) { }
       }
   }
   ```

2. **创建 ViewModel** (`<Module>ViewModel.cs`):
   
   - 继承 `INotifyPropertyChanged`
   - 添加输入属性
   - 添加输出属性
   - 创建 `CalculateCommand`
   - 在构造函数中初始化命令和默认值

3. **创建 View** (`<Module>View.xaml`):
   
   - 使用 MaterialDesign 卡片布局
   - 绑定 ViewModel 属性
   - 添加输入验证

4. **创建 View 代码后置** (`<Module>View.xaml.cs`):
   
   ```csharp
   public partial class ModuleView : UserControl
   {
       public ModuleView()
       {
           InitializeComponent();
           DataContext = new ModuleViewModel();
       }
   }
   ```

5. **在 MainWindow.xaml 中添加**:
   
   ```xml
   <!-- 添加 DataTemplate -->
   <DataTemplate x:Key="ModuleViewTemplate">
       <local:ModuleView/>
   </DataTemplate>
   
   <!-- 添加导航按钮 -->
   <Button Content="模块名称" Click="BtnModule_Click"/>
   ```

6. **在 MainWindow.xaml.cs 中添加导航事件**:
   
   ```csharp
   private void BtnModule_Click(object sender, RoutedEventArgs e)
   {
       contentControl.ContentTemplate = (DataTemplate)FindResource("ModuleViewTemplate");
       contentControl.Content = new ModuleView();
   }
   ```

7. **更新 MainWindow.xaml 标题和按钮文本**

8. **在 README.md 中添加文档**

### 遵循的原则

- **计算逻辑与 UI 分离**: 所有计算放在 `*Calculations.cs` 中
- **静态方法优先**: 计算类使用静态方法，无状态
- **详细计算过程**: 在 `Process` 列表中记录每个步骤
- **规范条文引用**: 在计算过程输出中包含规范条文号
- **单位一致性**: 明确每个参数的单位（英制/国际单位）
- **错误处理**: 验证输入参数的合理性
- **数据绑定**: 使用 WPF 绑定而非手动更新 UI
- **可测试性**: 计算逻辑独立于 UI，易于单元测试

## 测试

### 项目目前没有单元测试

计算逻辑独立于 UI，易于添加单元测试。推荐使用以下框架：

- **xUnit** - 测试框架
- **FluentAssertions** - 断言库
- **Moq** - Mock 框架（如需要）

测试文件命名: `<Module>CalculationsTests.cs`

## 构建和运行

### 前置要求

- .NET 8 SDK
- Windows 操作系统
- Visual Studio 2022 或 Rider（可选）

### 命令行构建

```bash
# 还原依赖
dotnet restore

# 构建
dotnet build

# 运行
dotnet run
```

### Visual Studio

1. 打开 `UsCodes.sln`
2. 按 F5 运行
3. 选择项目配置: Debug/Release

### 发布

```bash
# 单文件发布
dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true

# 框架依赖发布
dotnet publish -c Release -r win-x64
```

## 常见任务

### 添加新的钢筋规格

编辑对应的 ViewModel，修改 `BarSizeOptions` 列表。同时确保 `*Calculations.cs` 中的 `GetBarArea()` 方法支持该规格。

### 修改材料强度默认值

在对应 ViewModel 的构造函数或字段初始化中修改默认值：

```csharp
private double _fc = 4000;  // psi
private double _fy = 60000; // psi
```

### 调整图表样式

在 ViewModel 中修改 `Series`、`XAxes`、`YAxes` 的配置。参考 LiveChartsCore 文档。

### 添加新的规范版本

在 `*Calculations.cs` 中添加新方法的参数重载，在 ViewModel 中添加版本选择属性。

## 文档维护

- **README_CSharp.md**: 技术文档和 API 参考（保持更新）
- **CLAUDE.md**: 本文件，Claude 协作指南
- **功能说明文档**: 各模块的详细说明（如 `PM曲线计算方法说明.md`、`锚固长度.md`）

添加新功能时，同步更新 README.md。

## 相关规范

- **ASCE 7-16**: Minimum Design Loads and Associated Criteria for Buildings and Other Structures
- **ACI 318-19**: Building Code Requirements for Structural Concrete
- **ACI 318-25**: Building Code Requirements for Structural Concrete (最新版本，用于梁设计)
- **GB50011-2010**: 建筑抗震设计规范
- **GB50009-2012**: 建筑结构荷载规范

## 注意事项

1. **英制单位**: ACI 规范计算使用英制单位，界面显示也使用英制
2. **数值稳定性**: 注意除零、溢出等数值问题
3. **边界条件**: 检查规范公式的适用范围和边界条件
4. **构造要求**: 不仅要计算承载力，还要检查构造要求
5. **抗震设计**: 注意抗震体系的特殊要求（如禁止折减、放大系数等）
6. **国际化**: 目前界面为中文，代码注释为中文

## 工作流建议

1. **阅读规范**: 在实现前阅读相关规范条文
2. **设计接口**: 先定义 `InputParameters` 和 `DesignResult` 类
3. **实现计算**: 编写纯计算逻辑，记录详细过程
4. **编写 ViewModel**: 实现 UI 逻辑和数据绑定
5. **设计界面**: 创建 XAML 布局
6. **测试验证**: 使用规范例题验证计算结果
7. **更新文档**: 同步更新 README.md
8. **提交代码**: 编写清晰的 commit message

## 示例：添加新功能模块

```csharp
// 1. 计算类
public static class NewModuleCalculations
{
    public class InputParameters
    {
        public double Fc { get; set; }  // psi
        public double Fy { get; set; }  // psi
    }

    public class DesignResult
    {
        public bool IsAdequate { get; set; }
        public List<string> Process { get; set; } = new();
    }

    public static DesignResult Calculate(InputParameters input)
    {
        var result = new DesignResult();
        result.Process.Add($"f'c = {input.Fc} psi");
        // 计算逻辑
        return result;
    }
}

// 2. ViewModel
public class NewModuleViewModel : INotifyPropertyChanged
{
    private double _fc = 4000;
    public double Fc { get => _fc; set { _fc = value; OnPropertyChanged(); } }

    public ICommand CalculateCommand { get; }
    public NewModuleViewModel() { CalculateCommand = new RelayCommand(Calculate); }

    private void Calculate()
    {
        var input = new NewModuleCalculations.InputParameters { Fc = Fc };
        var result = NewModuleCalculations.Calculate(input);
        // 更新 UI
    }
}
```

## 发布和版本管理

- 版本格式: `VYYYYMMDD` (如 `V20260406`)
- 发布文件: `UsCodesTools_VYYYYMMDD.zip`
- 标签和版本号同步更新

---

**最后更新**: 2026-04-23
**维护者**: bg5hot
**许可**: 个人学习和研究使用
