# 中美规范参数转换 - C# WPF 版本实现文档

本文档详细说明 Python PyQt6 版本转换为 C# WPF 版本的技术细节、接口定义和使用方法。

---

## 技术栈

| 技术 | 版本 | 用途 |
|------|------|------|
| .NET | 8.0 | 运行平台 |
| WPF (Windows Presentation Foundation) | .NET 8 内置 | UI框架 |
| LiveCharts2 | 2.0.0-rc3.3 | 图表绘制（替代matplotlib） |
| SkiaSharp | LiveCharts2 依赖 | 2D图形渲染 |
| MVVM 模式 | - | 数据绑定和界面分离 |

---

## 项目文件结构

```
ASCE7 vs GB50011/
├── SpectrumComparison.csproj      # 项目配置文件
├── CoreCalculations.cs            # 核心计算逻辑
├── SpectrumViewModel.cs           # 反应谱界面ViewModel
├── WindConversionViewModel.cs     # 风速转换界面ViewModel
├── MainWindow.xaml                # 主窗口界面
├── MainWindow.xaml.cs             # 主窗口代码后置
├── SpectrumView.xaml              # 反应谱比较界面
├── SpectrumView.xaml.cs           # 反应谱界面代码后置
├── WindConversionView.xaml        # 风速转换界面
├── WindConversionView.xaml.cs     # 风速转换界面代码后置
├── Styles.xaml                    # Fluent Design样式
├── App.xaml                       # 应用程序资源
└── App.xaml.cs                    # 应用程序入口
```

---

## 核心计算接口 (CoreCalculations.cs)

### 中国规范 GB50011-2010

#### GetAlphaMax
```csharp
public static double GetAlphaMax(string intensity)
```
- **功能**: 根据设防烈度获取Alpha Max值
- **参数**: `intensity` - 设防烈度字符串（如"7度(0.10g)"）
- **返回**: Alpha Max值
- **支持的烈度值**:
  - "6度(0.05g)" -> 0.04
  - "7度(0.10g)" -> 0.08
  - "7度(0.15g)" -> 0.12
  - "8度(0.20g)" -> 0.16
  - "8度(0.30g)" -> 0.24
  - "9度(0.40g)" -> 0.32

#### GetTg
```csharp
public static double GetTg(string siteCategory, string earthquakeGroup)
```
- **功能**: 根据场地类别和地震分组获取特征周期Tg
- **参数**: 
  - `siteCategory` - 场地类别（I0, I1, II, III, IV）
  - `earthquakeGroup` - 地震分组（第一组, 第二组, 第三组）
- **返回**: 特征周期Tg（秒）
- **默认值**: 0.35秒

#### CalculateChineseSpectrum
```csharp
public static (double[] periods, double[] alpha) CalculateChineseSpectrum(
    double alphaMax, double tg, double damping = 0.05)
```
- **功能**: 计算中国规范地震反应谱
- **参数**:
  - `alphaMax` - 最大地震影响系数
  - `tg` - 特征周期
  - `damping` - 结构阻尼比（默认0.05）
- **返回**: 周期数组和地震影响系数数组（600个数据点，周期范围0.01-6.0秒）
- **计算公式**:
  - T < 0.1s: 线性上升段
  - 0.1s <= T < Tg: 平台段
  - Tg <= T < 5Tg: 曲线下降段
  - T >= 5Tg: 直线下降段

### 美国规范 ASCE 7-16

#### GetFaFv
```csharp
public static (double fa, double fv) GetFaFv(double ss, double s1, string siteClass)
```
- **功能**: 根据Ss、S1和场地类别获取Fa和Fv值
- **参数**:
  - `ss` - 短周期谱加速度
  - `s1` - 1秒周期谱加速度
  - `siteClass` - 场地类别（A, B, C, D）
- **返回**: Fa和Fv值元组
- **插值计算**: 支持线性插值

#### CalculateUsSpectrum
```csharp
public static (double[] periods, double[] sa, double sds, double sd1, double fa, double fv) 
    CalculateUsSpectrum(double ss, double s1, string siteClass, double tl, double r, double damping = 0.05)
```
- **功能**: 计算美国规范设计反应谱
- **参数**:
  - `ss` - 短周期谱加速度
  - `s1` - 1秒周期谱加速度
  - `siteClass` - 场地类别（A, B, C, D）
  - `tl` - 长周期过渡周期
  - `r` - 反应修正系数
  - `damping` - 结构阻尼比（默认0.05）
- **返回**: 
  - `periods` - 周期数组
  - `sa` - 谱加速度数组
  - `sds` - 短周期设计谱加速度
  - `sd1` - 1秒周期设计谱加速度
  - `fa` - 短周期场地系数
  - `fv` - 1秒周期场地系数
- **中间计算**:
  - SMS = Fa * Ss
  - SM1 = Fv * S1
  - SDS = (2/3) * SMS
  - SD1 = (2/3) * SM1

### 风速转换

#### ConvertWindSpeedToChinese
```csharp
public static (double windSpeed50y10m10min, double basicWindPressure, List<string> process) 
    ConvertWindSpeedToChinese(double windSpeed, string inputUnit, double inputHeight, string inputTime, string returnPeriod)
```
- **功能**: 将ASCE7风速转换为中国GB50009基本风压
- **参数**:
  - `windSpeed` - 输入风速值
  - `inputUnit` - 输入单位（"mph"或"m/s"）
  - `inputHeight` - 测量高度（米）
  - `inputTime` - 测量时距（"3s", "10s", "60s", "10min", "1h"）
  - `returnPeriod` - 重现期（"300y", "700y", "1700y", "3000y"）
- **返回**: 
  - `windSpeed50y10m10min` - 转换后的风速（50年重现期，10m高度，10分钟平均）
  - `basicWindPressure` - 基本风压（kN/m²）
  - `process` - 转换过程详细步骤列表
- **转换步骤**:
  1. 单位转换（mph -> m/s）
  2. 重现期转换（使用转换系数）
  3. 时距转换（基于Durst曲线）
  4. 高度转换（幂律公式）
  5. 基本风压计算（w0 = 0.5 * ρ * v² / 1000）

---

## ViewModel 接口

### SpectrumViewModel (反应谱界面)

#### 可绑定属性

| 属性名 | 类型 | 说明 | 默认值 |
|--------|------|------|--------|
| `Damping` | `double` | 结构阻尼比（0.01-0.99） | 0.05 |
| `ChinaIntensity` | `string` | 中国规范设防烈度 | "7度(0.10g)" |
| `ChinaSiteCategory` | `string` | 中国规范场地类别 | "II" |
| `ChinaEarthquakeGroup` | `string` | 中国规范地震分组 | "第一组" |
| `UsSs` | `double` | 美国规范Ss值 | 0.51 |
| `UsS1` | `double` | 美国规范S1值 | 0.18 |
| `UsTl` | `double` | 美国规范TL值 | 24.0 |
| `UsR` | `double` | 美国规范R值 | 5.0 |
| `UsSiteClass` | `string` | 美国规范场地类别 | "D" |
| `AlphaMaxText` | `string` | 显示的Alpha Max文本 | "Alpha Max: 0.08" |
| `TgText` | `string` | 显示的Tg文本 | "Tg: 0.35s" |
| `UsFaFvText` | `string` | 显示的Fa Fv文本 | "Fa: - | Fv: -" |
| `UsSdsSd1Text` | `string` | 显示的SDS SD1文本 | "SDS: - | SD1: -" |

#### 选项列表属性

| 属性名 | 类型 | 说明 |
|--------|------|------|
| `IntensityOptions` | `List<string>` | 设防烈度选项列表 |
| `SiteCategoryOptions` | `List<string>` | 场地类别选项列表（I0, I1, II, III, IV） |
| `EarthquakeGroupOptions` | `List<string>` | 地震分组选项列表（第一组, 第二组, 第三组） |
| `UsSiteClassOptions` | `List<string>` | 美国场地类别选项列表（A, B, C, D） |

#### 图表属性（LiveCharts2）

| 属性名 | 类型 | 说明 |
|--------|------|------|
| `Series` | `ISeries[]` | 图表系列数据（中国规范和美国规范两条线） |
| `XAxes` | `Axis[]` | X轴配置（周期T，范围0-6秒） |
| `YAxes` | `Axis[]` | Y轴配置（谱加速度，自动缩放） |

#### 自动更新机制
- 所有输入属性变更时自动触发 `UpdateChart()` 方法
- 图表实时更新显示

### WindConversionViewModel (风速转换界面)

#### 可绑定属性

| 属性名 | 类型 | 说明 | 默认值 |
|--------|------|------|--------|
| `WindSpeed` | `double` | 输入风速值 | 115 |
| `WindUnit` | `string` | 风速单位（mph/m/s） | "mph" |
| `WindHeight` | `double` | 测量高度（米） | 10 |
| `WindTime` | `string` | 测量时距 | "3s" |
| `ReturnPeriod` | `string` | 重现期 | "700y" |
| `ResultWindSpeed` | `string` | 转换结果风速显示文本 | "50年重现期..." |
| `ResultWindPressure` | `string` | 基本风压显示文本 | "基本风压: - kN/m²" |
| `ProcessText` | `string` | 转换过程详细文本 | 空字符串 |

#### 选项列表属性

| 属性名 | 类型 | 说明 |
|--------|------|------|
| `WindUnitOptions` | `List<string>` | 风速单位选项（mph, m/s） |
| `WindTimeOptions` | `List<string>` | 测量时距选项（3s, 10s, 60s, 10min, 1h） |
| `ReturnPeriodOptions` | `List<string>` | 重现期选项（300y, 700y, 1700y, 3000y） |

#### 命令

| 命令名 | 类型 | 说明 |
|--------|------|------|
| `ConvertCommand` | `ICommand` | 执行风速转换命令（绑定到"执行转换"按钮） |

---

## 界面说明

### 主窗口 (MainWindow)

- **布局**: 左侧导航栏 + 右侧内容区
- **导航项**:
  - 反应谱比较（图标：Home）
  - 风速转换（图标：Sync）
- **尺寸**: 1400 x 800 像素
- **启动位置**: 屏幕居中

### 反应谱比较界面 (SpectrumView)

- **左侧设置面板**（宽度450像素）:
  - 结构参数卡片：阻尼比输入
  - 中国规范卡片：设防烈度、场地类别、地震分组选择，显示Alpha Max和Tg
  - 美国规范卡片：Ss、S1、Site Class、TL、R输入，显示Fa、Fv、SDS、SD1
- **右侧图表区域**:
  - LiveCharts2 绘制的双曲线图
  - 中国规范：青色线 (#009FAA)
  - 美国规范：橙色线 (#FF6B00)
  - 图例显示在右侧

### 风速转换界面 (WindConversionView)

- **输入区域**:
  - 风速值和单位选择
  - 测量高度输入
  - 测量时距选择
  - 重现期选择
  - "执行转换"按钮
- **结果区域**:
  - 转换后风速显示
  - 基本风压显示
  - 转换过程详细步骤（只读文本框）

---

## 样式定义 (Styles.xaml)

### Fluent Design 颜色

| 资源键 | 颜色值 | 用途 |
|--------|--------|------|
| `FluentBlue` | #0078D4 | 主按钮颜色 |
| `FluentLightBlue` | #009FAA | 中国规范曲线颜色 |
| `FluentOrange` | #FF6B00 | 美国规范曲线颜色 |
| `FluentBackground` | #F9F9F9 | 窗口背景色 |
| `FluentCardBackground` | #FFFFFF | 卡片背景色 |
| `FluentBorder` | #E0E0E0 | 边框颜色 |
| `FluentText` | #323130 | 主要文字颜色 |
| `FluentSecondaryText` | #605E5C | 次要文字颜色 |

### 定义的样式

| 样式键 | 目标类型 | 说明 |
|--------|----------|------|
| `CardStyle` | `Border` | 卡片容器样式（圆角、阴影、白色背景） |
| `SectionTitleStyle` | `TextBlock` | 节标题样式（16px，半粗体） |
| `LabelStyle` | `TextBlock` | 普通标签样式（13px，次要色） |
| `StrongLabelStyle` | `TextBlock` | 强调标签样式（13px，半粗体） |
| `PrimaryButtonStyle` | `Button` | 主按钮样式（蓝色背景，白色文字） |
| `FluentComboBoxStyle` | `ComboBox` | 下拉框样式 |
| `FluentNumericStyle` | `TextBox` | 数字输入框样式 |
| `SeparatorStyle` | `Separator` | 分隔线样式 |

---

## 使用方法

### 编译和运行

```bash
# 还原依赖
dotnet restore

# 编译项目
dotnet build

# 运行程序
dotnet run

# 发布程序
dotnet publish -c Release -r win-x64 --self-contained true
```

### 在Visual Studio中打开

1. 打开Visual Studio 2022
2. 选择"打开项目或解决方案"
3. 选择 `SpectrumComparison.csproj` 文件
4. 按 F5 运行

---

## 与原Python版本的差异

| 方面 | Python (PyQt6) | C# (WPF) |
|------|----------------|----------|
| UI框架 | PyQt6 + PyQt-Fluent-Widgets | WPF + MaterialDesign |
| 图表库 | Matplotlib | LiveCharts2 |
| 设计模式 | 直接代码创建UI | MVVM + XAML数据绑定 |
| 样式系统 | QSS样式表 | WPF资源字典 |
| 事件处理 | 信号槽机制 | 命令绑定 + 事件 |
| 平台 | 跨平台 | Windows only |

---

## 注意事项

1. **Windows平台**: WPF仅支持Windows系统
2. **LiveCharts2**: 使用SkiaSharp进行渲染，性能优异
3. **MVVM模式**: 界面逻辑完全分离，便于测试和维护
4. **实时更新**: 反应谱界面参数变化时自动重新计算和绘图
5. **数据验证**: 输入框绑定到double类型，自动进行数值验证

---

## 扩展建议

1. **添加更多规范**: 可在 `CoreCalculations.cs` 中添加欧洲EC8等规范
2. **数据导出**: 可添加导出计算结果为Excel或CSV功能
3. **历史记录**: 可添加保存和加载计算参数功能
4. **打印功能**: 可添加打印图表和计算结果功能
