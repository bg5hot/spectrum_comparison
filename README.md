# 中美规范参数转换 - C# WPF 版本

本文档说明 Python PyQt6 版本转换为 C# WPF 版本的技术细节。

---

## 技术栈

| 技术                                    | 版本             | 用途                 |
| ------------------------------------- | -------------- | ------------------ |
| .NET                                  | 8.0            | 运行平台               |
| WPF (Windows Presentation Foundation) | .NET 8 内置      | UI框架               |
| LiveCharts2                           | 2.0.0-rc3.3    | 图表绘制（替代matplotlib） |
| SkiaSharp                             | LiveCharts2 依赖 | 2D图形渲染             |
| MaterialDesignThemes                  | 5.1.0          | UI样式和控件            |
| MathNet.Numerics                      | 5.0.0          | 数值计算（FFT、插值、线性代数） |
| MVVM 模式                               | -              | 数据绑定和界面分离          |

---

## 项目文件结构

```
ASCE7 vs GB50011/
├── UsCodeTools.csproj                 # 项目配置文件
├── CoreCalculations.cs                # 核心计算逻辑（反应谱和风速转换）
├── ArtificialWaveCalculations.cs      # 人工地震波生成逻辑（频域迭代拟合）
├── GustEffectFactorCalculations.cs    # 风振系数计算逻辑（ASCE 7-16 Section 26.11）
├── BeamDesignCalculations.cs          # 梁截面设计计算逻辑（ACI 318-25）
├── ColumnDesignCalculations.cs        # 矩形柱设计计算逻辑（ACI 318-19）
├── CircularColumnDesignCalculations.cs # 圆形柱设计计算逻辑（ACI 318-19）
├── PunchingShearCalculations.cs       # 双向板抗冲切计算逻辑（ACI 318-19）
├── DevelopmentLengthCalculations.cs   # 钢筋锚固长度计算逻辑（ACI 318-19）
├── BooleanToVisibilityConverter.cs    # 布尔值到可见性转换器
├── InverseBoolConverter.cs            # 布尔值取反转换器（含反向可见性）
├── RelayCommand.cs                    # ICommand 实现基类
├── SpectrumViewModel.cs               # 反应谱界面ViewModel
├── ArtificialWaveViewModel.cs         # 人工地震波界面ViewModel
├── WindConversionViewModel.cs         # 风速转换界面ViewModel
├── GustEffectFactorViewModel.cs       # 风振系数界面ViewModel
├── BeamDesignViewModel.cs             # 梁截面设计界面ViewModel
├── ColumnDesignViewModel.cs           # 矩形柱设计界面ViewModel
├── CircularColumnDesignViewModel.cs   # 圆形柱设计界面ViewModel
├── PunchingShearViewModel.cs          # 双向板抗冲切界面ViewModel
├── DevelopmentLengthViewModel.cs      # 钢筋锚固长度界面ViewModel
├── MainWindow.xaml                    # 主窗口界面
├── MainWindow.xaml.cs                 # 主窗口代码后置
├── SpectrumView.xaml                  # 反应谱比较界面
├── SpectrumView.xaml.cs               # 反应谱界面代码后置
├── ArtificialWaveView.xaml            # 人工地震波界面
├── ArtificialWaveView.xaml.cs         # 人工地震波界面代码后置
├── WindConversionView.xaml            # 风速转换界面
├── WindConversionView.xaml.cs         # 风速转换界面代码后置
├── GustEffectFactorView.xaml          # 风振系数界面
├── GustEffectFactorView.xaml.cs       # 风振系数界面代码后置
├── BeamDesignView.xaml                # 梁截面设计界面
├── BeamDesignView.xaml.cs             # 梁截面设计界面代码后置
├── ColumnDesignView.xaml              # 矩形柱设计界面
├── ColumnDesignView.xaml.cs           # 矩形柱设计界面代码后置
├── CircularColumnDesignView.xaml      # 圆形柱设计界面
├── CircularColumnDesignView.xaml.cs   # 圆形柱设计界面代码后置
├── PunchingShearView.xaml             # 双向板抗冲切界面
├── PunchingShearView.xaml.cs          # 双向板抗冲切界面代码后置
├── DevelopmentLengthView.xaml         # 钢筋锚固长度界面
├── DevelopmentLengthView.xaml.cs      # 钢筋锚固长度界面代码后置
├── WeChatView.xaml                    # 关注公众号界面
├── WeChatView.xaml.cs                 # 关注公众号界面代码后置
├── Styles.xaml                        # Fluent Design样式
├── App.xaml                           # 应用程序资源
└── App.xaml.cs                        # 应用程序入口
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

#### GetTg

```csharp
public static double GetTg(string siteCategory, string earthquakeGroup)
```

- **功能**: 根据场地类别和地震分组获取特征周期Tg
- **参数**: 
  - `siteCategory` - 场地类别（I0, I1, II, III, IV）
  - `earthquakeGroup` - 地震分组（第一组, 第二组, 第三组）
- **返回**: 特征周期Tg（秒）

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
- **返回**: 周期数组和地震影响系数数组

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

#### CalculateUsSpectrum

```csharp
public static (double[] periods, double[] sa, double sds, double sd1, double fa, double fv) 
    CalculateUsSpectrum(double ss, double s1, string siteClass, double tl, double r, double damping = 0.05)
```

- **功能**: 计算美国规范设计反应谱
- **参数**:
  - `ss` - 短周期谱加速度
  - `s1` - 1秒周期谱加速度
  - `siteClass` - 场地类别
  - `tl` - 长周期过渡周期
  - `r` - 反应修正系数
  - `damping` - 结构阻尼比
- **返回**: 周期数组、谱加速度数组、SDS、SD1、Fa、Fv

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
  - `basicWindPressure` - 转换后的基本风压
  - `process` - 转换过程详细步骤列表

---

## 人工地震波生成接口 (ArtificialWaveCalculations.cs)

### 频域迭代拟合方法

#### GenerateArtificialWaves

```csharp
public static WaveGenerationResult GenerateArtificialWaves(WaveGenerationInput input)
```

- **功能**: 批量生成符合目标反应谱的人工地震波
- **参数**: `input` - 输入参数对象
- **返回**: 生成结果对象，包含多条地震波、反应谱对比、详细过程日志

#### WaveGenerationInput 类

| 属性                     | 类型       | 说明                     |
| ---------------------- | -------- | ---------------------- |
| `UseChineseCode`       | `bool`   | 是否使用中国规范（true=GB50011，false=ASCE 7-16） |
| `ChinaIntensity`       | `string` | 中国规范设防烈度               |
| `ChinaSiteCategory`    | `string` | 中国规范场地类别               |
| `ChinaEarthquakeGroup` | `string` | 中国规范地震分组               |
| `UsSs`                 | `double` | 美国规范Ss值                |
| `UsS1`                 | `double` | 美国规范S1值                |
| `UsSiteClass`          | `string` | 美国规范场地类别               |
| `UsTl`                 | `double` | 美国规范TL值                |
| `UsR`                  | `double` | 美国规范R值                 |
| `Damping`              | `double` | 结构阻尼比（默认0.05）          |
| `Dt`                   | `double` | 时间步长（默认0.01 s）        |
| `TTotal`               | `double` | 总时长（默认30.0 s）          |
| `NumberOfWaves`        | `int`    | 生成波数量（默认3）            |
| `NumberOfIterations`   | `int`    | 迭代拟合次数（默认5）           |
| `T1`                   | `double` | 包络线上升段结束时间（默认3.0 s）  |
| `T2`                   | `double` | 包络线平稳段结束时间（默认15.0 s） |
| `CDecay`               | `double` | 包络线衰减系数（默认0.2）        |
| `TMin`                 | `double` | 反应谱计算周期下限（默认0.02 s）  |
| `TMax`                 | `double` | 反应谱计算周期上限（默认6.0 s）   |
| `TStep`                | `double` | 反应谱计算周期步长（默认0.02 s）  |

#### WaveGenerationResult 类

| 属性                | 类型                  | 说明            |
| ----------------- | ------------------- | ------------- |
| `GeneratedWaves`  | `List<double[]>`    | 生成的地震波加速度时程列表 |
| `TimeArray`       | `double[]`          | 时间数组          |
| `TargetPeriods`   | `double[]`          | 目标反应谱周期数组     |
| `TargetSpectrum`  | `double[]`          | 目标反应谱值数组      |
| `CalculatedSpectra` | `List<double[]>`    | 各波计算反应谱列表     |
| `MeanSpectrum`    | `double[]`          | 平均反应谱         |
| `ProcessLog`      | `List<string>`      | 生成过程日志        |
| `Success`         | `bool`              | 是否成功          |
| `ErrorMessage`    | `string?`           | 错误信息          |

#### 核心计算方法

- `GetChineseTargetSpectrum()` - 获取中国规范目标反应谱（复用CoreCalculations）
- `GetUsTargetSpectrum()` - 获取美国规范目标反应谱（复用CoreCalculations）
- `GetEnvelope()` - 生成三段式非平稳包络线（上升段/平稳段/衰减段）
- `CalculateResponseSpectrum()` - 使用双线性变换IIR滤波器计算弹性反应谱
- `ButterworthHighPass()` - 4阶Butterworth高通滤波（基线校正）

#### 生成流程

1. 根据规范参数计算目标反应谱
2. 生成高斯白噪声种子并乘以三段式包络线
3. 迭代拟合：计算当前反应谱 → 计算比值 → 频域缩放 → 逆变换 → 乘包络线 → 高通滤波
4. 批量生成多条波并计算平均谱
5. 输出反应谱对比图

#### SaveWaveToCsv

```csharp
public static string SaveWaveToCsv(double[] time, double[] acceleration, int waveIndex, string directory)
```

- **功能**: 将地震波保存为CSV文件
- **输出格式**: `Time(s),Acceleration(g)`

---

### ArtificialWaveViewModel (人工地震波界面)

#### 属性

| 属性名                   | 类型       | 说明               |
| --------------------- | -------- | ---------------- |
| `UseChineseCode`      | `bool`   | 是否使用中国规范         |
| `Damping`             | `double` | 结构阻尼比             |
| `ChinaIntensity`      | `string` | 中国规范设防烈度          |
| `ChinaSiteCategory`   | `string` | 中国规范场地类别          |
| `ChinaEarthquakeGroup`| `string` | 中国规范地震分组          |
| `UsSs`                | `double` | 美国规范Ss值           |
| `UsS1`                | `double` | 美国规范S1值           |
| `UsSiteClass`         | `string` | 美国规范场地类别          |
| `UsTl`                | `double` | 美国规范TL值           |
| `UsR`                 | `double` | 美国规范R值            |
| `NumberOfWaves`       | `int`    | 生成波数量             |
| `Dt`                  | `double` | 时间步长              |
| `TTotal`              | `double` | 总时长               |
| `NumberOfIterations`  | `int`    | 迭代次数              |
| `T1`                  | `double` | 包络线上升段结束时间        |
| `T2`                  | `double` | 包络线平稳段结束时间        |
| `CDecay`              | `double` | 衰减系数              |
| `ResultText`          | `string` | 结果摘要文本            |
| `ProcessText`         | `string` | 计算过程详细文本          |
| `IsGenerating`        | `bool`   | 是否正在生成（控制进度条和按钮）  |

#### 集合属性

| 属性名                    | 类型             | 说明                    |
| ---------------------- | -------------- | --------------------- |
| `IntensityOptions`     | `List<string>` | 设防烈度选项列表              |
| `SiteCategoryOptions`  | `List<string>` | 场地类别选项列表              |
| `EarthquakeGroupOptions`| `List<string>` | 地震分组选项列表              |
| `UsSiteClassOptions`   | `List<string>` | 美国场地类别选项列表            |
| `NumberOfWavesOptions` | `List<int>`    | 波数量选项（1,3,5,7,10,15,20,30） |
| `DtOptions`            | `List<double>` | 时间步长选项（0.005,0.01,0.02）|
| `TTotalOptions`        | `List<double>` | 总时长选项（20,30,40,60 s）   |
| `IterationsOptions`    | `List<int>`    | 迭代次数选项（3,5,7,10,15）    |

#### 命令

| 命令名             | 类型         | 说明          |
| --------------- | ---------- | ----------- |
| `GenerateCommand`| `ICommand` | 执行人工波生成命令   |
| `SaveCommand`   | `ICommand` | 保存生成的波到CSV文件 |

#### 特性

- **异步生成**: 使用后台线程执行计算，避免UI冻结
- **中美规范切换**: 动态切换中国规范/美国规范参数面板
- **反应谱对比图**: 显示目标规范谱（红色）、平均谱（蓝色）和各条波谱（灰色）
- **CSV导出**: 支持将生成的地震波保存为CSV文件（时间,加速度）

---

## 风振系数计算接口 (GustEffectFactorCalculations.cs)

### ASCE 7-16 风振系数计算

#### CalculateGustEffectFactor

```csharp
public static GustEffectResult CalculateGustEffectFactor(
    string exposure, // 场地类别 (B, C, D)
    double h, // 建筑高度 (m)
    double b, // 建筑宽度 (m)
    double l, // 建筑深度 (m)
    double v, // 基本风速 (m/s)
    double n1) // 建筑基本周期 (Hz)
```

- **功能**: 计算ASCE 7-16 Section 26.11风振系数G或Gf
- **参数**:
  - `exposure` - 场地类别（B, C, D）
  - `h` - 建筑高度（米）
  - `b` - 建筑宽度（米）
  - `l` - 建筑深度（米）
  - `v` - 基本风速（米/秒）
  - `n1` - 建筑基本周期（赫兹）
- **返回**: 风振系数计算结果对象，包含：
  - `IsRigid` - 是否为刚性建筑（n₁ ≥ 1 Hz）
  - `BuildingType` - 建筑类型描述
  - `G` - 刚性建筑风振系数
  - `Gf` - 柔性建筑风振系数
  - `ZBar` - 平均高度z̄
  - `VZBar` - z̄高度处的风速
  - `IZBar` - 湍流强度I_z̄
  - `Q` - 背景响应因子
  - `R` - 共振响应因子（仅柔性建筑）
  - `Process` - 详细计算过程列表

#### 计算方法

**刚性建筑** (n₁ ≥ 1 Hz):
```
G = 0.925 × [(1 + 1.7×g_Q×I_z̄×Q)/(1 + 1.7×g_v×I_z̄)]
```

**柔性建筑** (n₁ < 1 Hz):
```
G_f = 0.925 × [(1 + 1.7×g_Q×I_z̄×Q)/(1 + 1.7×g_v×I_z̄)] × [(1 + g_R²×R²)/(1 + g_R²)]^(1/2)
```

其中：
- `g_Q = g_v = 3.4` - 峰值因子
- `g_R` - 峰值因子，取决于风速平均时间
- `I_z̄` - 湍流强度
- `Q` - 背景响应因子
- `R` - 共振响应因子

---

## 梁截面设计计算接口 (BeamDesignCalculations.cs)

### ACI 318-25 梁截面设计

#### CalculateFlexuralReinforcement

```csharp
public static DesignResult CalculateFlexuralReinforcement(
    double mu, // 设计弯矩 (lb-in)
    MaterialProperties material,
    SectionDimensions section,
    string barSize = "#8") // 钢筋规格
```

- **功能**: 计算梁截面受弯配筋
- **参数**:
  - `mu` - 设计弯矩（lb-in）
  - `material` - 材料属性（混凝土强度、钢筋强度）
  - `section` - 截面尺寸（宽度、高度、有效高度等）
  - `barSize` - 钢筋规格（如"#8"）
- **返回**: 设计结果对象，包含所需钢筋面积、提供钢筋面积、钢筋根数、截面类型等

#### CalculateShearReinforcement

```csharp
public static (double avRequired, double avProvided, double sMax, List<string> process) 
    CalculateShearReinforcement(
    double vu, // 设计剪力 (lb)
    double nu, // 设计轴力 (lb，拉力为负)
    MaterialProperties material,
    SectionDimensions section,
    double stirrupArea, // 单肢箍筋面积 (in²)
    int numLegs) // 箍筋肢数
```

- **功能**: 计算梁截面抗剪配筋
- **参数**:
  - `vu` - 设计剪力（lb）
  - `nu` - 设计轴力（lb，拉力为负）
  - `material` - 材料属性
  - `section` - 截面尺寸
  - `stirrupArea` - 单肢箍筋面积（in²）
  - `numLegs` - 箍筋肢数
- **返回**: 元组，包含所需箍筋面积、提供箍筋面积、最大间距、计算过程

---

## ViewModel 接口

### SpectrumViewModel (反应谱界面)

#### 属性

| 属性名                    | 类型       | 说明               |
| ---------------------- | -------- | ---------------- |
| `Damping`              | `double` | 结构阻尼比（0.01-0.99） |
| `ChinaIntensity`       | `string` | 中国规范设防烈度         |
| `ChinaSiteCategory`    | `string` | 中国规范场地类别         |
| `ChinaEarthquakeGroup` | `string` | 中国规范地震分组         |
| `UsSs`                 | `double` | 美国规范Ss值          |
| `UsS1`                 | `double` | 美国规范S1值          |
| `UsTl`                 | `double` | 美国规范TL值          |
| `UsR`                  | `double` | 美国规范R值           |
| `UsSiteClass`          | `string` | 美国规范场地类别         |
| `AlphaMaxText`         | `string` | 显示的Alpha Max文本   |
| `TgText`               | `string` | 显示的Tg文本          |
| `UsFaFvText`           | `string` | 显示的Fa Fv文本       |
| `UsSdsSd1Text`         | `string` | 显示的SDS SD1文本     |

#### 集合属性

| 属性名                      | 类型             | 说明         |
| ------------------------ | -------------- | ---------- |
| `IntensityOptions`       | `List<string>` | 设防烈度选项列表   |
| `SiteCategoryOptions`    | `List<string>` | 场地类别选项列表   |
| `EarthquakeGroupOptions` | `List<string>` | 地震分组选项列表   |
| `UsSiteClassOptions`     | `List<string>` | 美国场地类别选项列表 |

#### 图表属性

| 属性名      | 类型          | 说明               |
| -------- | ----------- | ---------------- |
| `Series` | `ISeries[]` | LiveCharts图表系列数据 |
| `XAxes`  | `Axis[]`    | X轴配置             |
| `YAxes`  | `Axis[]`    | Y轴配置             |

### WindConversionViewModel (风速转换界面)

#### 属性

| 属性名                  | 类型       | 说明            |
| -------------------- | -------- | ------------- |
| `WindSpeed`          | `double` | 输入风速值         |
| `WindUnit`           | `string` | 风速单位（mph/m/s） |
| `WindHeight`         | `double` | 测量高度（米）       |
| `WindTime`           | `string` | 测量时距          |
| `ReturnPeriod`       | `string` | 重现期           |
| `ResultWindSpeed`    | `string` | 转换结果风速显示文本    |
| `ResultWindPressure` | `string` | 基本风压显示文本      |
| `ProcessText`        | `string` | 转换过程详细文本      |

#### 命令

| 命令名              | 类型         | 说明       |
| ---------------- | ---------- | -------- |
| `ConvertCommand` | `ICommand` | 执行风速转换命令 |

### GustEffectFactorViewModel (风振系数界面)

#### 属性

| 属性名                   | 类型       | 说明           |
| --------------------- | -------- | ------------ |
| `Exposure`            | `string` | 场地类别（B, C, D） |
| `BuildingHeight`      | `double` | 建筑高度（m）     |
| `BuildingWidth`       | `double` | 建筑宽度（m）     |
| `BuildingDepth`       | `double` | 建筑深度（m）     |
| `WindSpeed`          | `double` | 基本风速（m/s）    |
| `NaturalPeriod`       | `double` | 建筑基本周期（Hz）   |
| `BuildingType`       | `string` | 建筑类型显示文本     |
| `GustEffectFactor`   | `string` | 风振系数显示文本     |
| `GValue`             | `string` | G值显示文本       |
| `GfValue`            | `string` | Gf值显示文本      |
| `ZBarValue`          | `string` | z̄值显示文本       |
| `VZBarValue`         | `string` | V_z̄值显示文本     |
| `IZBarValue`         | `string` | I_z̄值显示文本     |
| `QValue`             | `string` | Q值显示文本       |
| `RValue`             | `string` | R值显示文本       |
| `ProcessText`         | `string` | 计算过程详细文本     |

#### 集合属性

| 属性名                 | 类型             | 说明        |
| ------------------- | -------------- | --------- |
| `ExposureOptions`  | `List<string>` | 场地类别选项列表 |

#### 命令

| 命令名                | 类型         | 说明         |
| ------------------ | ---------- | ---------- |
| `CalculateCommand` | `ICommand` | 执行风振系数计算命令 |

#### 特性

- **自动建筑类型判断**: 根据建筑基本周期自动判断为刚性建筑（n₁ ≥ 1 Hz）或柔性建筑（n₁ < 1 Hz）
- **国际单位制**: 所有参数和结果均使用国际单位制（m, m/s, Hz）

---

## 风荷载模拟接口 (WindSimulationCalculations.cs)

### ASCE 7-16 脉动风模拟

#### GenerateWind

```csharp
public static WindSimResult GenerateWind(WindSimInput input)
```

- **功能**: 生成符合 ASCE 7-16 Kaimal 谱的脉动风速时程
- **参数**: `input` - 输入参数对象
- **返回**: 风速时程模拟结果对象

#### WindSimInput 类

| 属性          | 类型       | 说明                      | 默认值    |
| ----------- | -------- | ----------------------- | ----- |
| `VRef`      | `double` | 基本风速（mph）              | 115   |
| `Exposure`  | `string` | 场地类别（B/C/D）            | "C"   |
| `Height`    | `double` | 目标高度（ft）               | 100   |
| `Duration`  | `double` | 模拟时长（秒）                | 600   |
| `SampleRate`| `double` | 采样频率（Hz）               | 20    |

#### WindSimResult 类

| 属性                | 类型              | 说明            |
| ----------------- | --------------- | ------------- |
| `Success`         | `bool`          | 是否成功          |
| `ErrorMessage`    | `string?`       | 错误信息          |
| `ProcessLog`      | `List<string>`  | 计算过程日志        |
| `Exposure`        | `string`        | 场地类别          |
| `HeightFt`        | `double`        | 目标高度          |
| `HeightEffFt`     | `double`        | 有效高度          |
| `VAvg`            | `double`        | 平均风速 Vz（ft/s） |
| `Iz`              | `double`        | 湍流强度          |
| `SigmaU`          | `double`        | 风速标准差 σu（ft/s）|
| `Lz`              | `double`        | 积分尺度 Lz（ft）   |
| `TimeArray`       | `double[]`      | 时间数组          |
| `FluctuatingWind` | `double[]`      | 脉动风速时程        |
| `TotalWind`       | `double[]`      | 总风速时程（平均+脉动）  |
| `TargetPSD_Freq`  | `double[]`      | 目标Kaimal谱频率数组 |
| `TargetPSD`       | `double[]`      | 目标Kaimal谱值数组 |
| `SimulatedPSD_Freq`| `double[]`     | 模拟谱频率数组（Welch法）|
| `SimulatedPSD`    | `double[]`      | 模拟谱值数组（Welch法）|

#### 计算方法

**ASCE 7-16 风速剖面** (Eq. 26.11-1):
```
Vz = b̂ × (z / 33)^α̂ × Vref
```

**湍流强度** (Eq. 26.11-8):
```
Iz = l × (33 / z)^(1/6)
```

**Kaimal 谱** (风工程通用模型):
```
S(f) = (4 × σu² × Lz / Vz) / (1 + 6 × n)^(5/3)
其中 n = f × Lz / Vz
```

**FFT 随机相位法**:
1. 根据 Kaimal 谱生成幅值谱
2. 随机相位 0~2π
3. 逆 FFT 生成时程
4. Welch 法计算 PSD 验证

#### SaveWindToCsv

```csharp
public static string SaveWindToCsv(double[] timeArray, double[] totalWind, double[] fluctuatingWind, string saveDirectory)
```

- **功能**: 将风速时程保存为 CSV 文件
- **输出格式**: `Time(s),Total_Wind(ft/s),Fluctuating_Wind(ft/s)`

---

### WindSimulationViewModel (风荷载模拟界面)

#### 属性

| 属性名              | 类型       | 说明              |
| ---------------- | -------- | --------------- |
| `VRef`           | `double` | 基本风速（mph）       |
| `Exposure`       | `string` | 场地类别（B/C/D）     |
| `Height`         | `double` | 目标高度（ft）        |
| `Duration`       | `double` | 模拟时长（秒）         |
| `SampleRate`     | `double` | 采样频率（Hz）        |
| `ResultText`     | `string` | 结果摘要文本          |
| `ProcessText`    | `string` | 计算过程详细文本        |
| `IsGenerating`   | `bool`   | 是否正在生成（控制进度条和按钮）|
| `HasData`        | `bool`   | 是否有数据（控制保存按钮）   |

#### 图表属性（OxyPlot）

| 属性名            | 类型         | 说明        |
| -------------- | ---------- | --------- |
| `WindPlotModel` | `PlotModel` | 风速时程图模型  |
| `PsdPlotModel`  | `PlotModel` | PSD对比图模型 |

#### 集合属性

| 属性名                 | 类型             | 说明                  |
| ------------------- | -------------- | ------------------- |
| `ExposureOptions`  | `List<string>` | 场地类别选项列表             |
| `DurationOptions`  | `List<double>` | 时长选项（300,600,900,1200秒） |
| `SampleRateOptions`| `List<double>` | 采样率选项（10,20,50,100 Hz） |

#### 命令

| 命令名              | 类型         | 说明           |
| ---------------- | ---------- | ------------ |
| `GenerateCommand`| `ICommand` | 执行风模拟生成命令    |
| `SaveCommand`    | `ICommand` | 保存风速时程到CSV文件 |

#### 特性

- **异步生成**: 使用后台线程执行计算，避免UI冻结
- **Kaimal 谱**: 符合 ASCE 7-16 风工程标准
- **PSD 验证**: 使用 Welch 方法计算功率谱密度对比
- **OxyPlot 图表**: 风速时程图和 PSD 对比图（双对数坐标）
- **CSV 导出**: 支持将生成的风速时程保存为 CSV 文件

### BeamDesignViewModel (梁截面设计界面)

#### 属性

| 属性名                   | 类型       | 说明           |
| --------------------- | -------- | ------------ |
| `Fc`                  | `double` | 混凝土抗压强度（psi） |
| `Fy`                  | `double` | 钢筋屈服强度（psi）  |
| `B`                   | `double` | 截面宽度（in）     |
| `H`                   | `double` | 截面高度（in）     |
| `Cover`               | `double` | 保护层厚度（in）    |
| `StirrupDiameter`     | `double` | 箍筋直径（in）     |
| `Mu`                  | `double` | 设计弯矩（kip-ft） |
| `Vu`                  | `double` | 设计剪力（kips）   |
| `Nu`                  | `double` | 设计轴力（kips）   |
| `SelectedBarSize`     | `string` | 受拉钢筋规格       |
| `SelectedStirrupSize` | `string` | 箍筋规格         |
| `StirrupLegs`         | `int`    | 箍筋肢数         |
| `RequiredSteelArea`   | `string` | 所需钢筋面积显示文本   |
| `ProvidedSteelArea`   | `string` | 提供钢筋面积显示文本   |
| `NumberOfBars`        | `string` | 钢筋根数显示文本     |
| `PhiMnResult`         | `string` | 设计弯矩承载力显示文本  |
| `MomentRatio`         | `string` | 弯矩比显示文本      |
| `SectionType`         | `string` | 截面类型显示文本     |
| `DesignStatus`        | `string` | 设计状态显示文本     |
| `ShearResult`         | `string` | 抗剪设计结果显示文本   |
| `DesignProcess`       | `string` | 设计过程详细文本     |

#### 集合属性

| 属性名                  | 类型             | 说明              |
| -------------------- | -------------- | --------------- |
| `BarSizeOptions`     | `List<string>` | 受拉钢筋规格选项列表      |
| `StirrupSizeOptions` | `List<string>` | 箍筋规格选项列表（#3-#8） |
| `StirrupLegOptions`  | `List<int>`    | 箍筋肢数选项列表        |

#### 命令

| 命令名                | 类型         | 说明          |
| ------------------ | ---------- | ----------- |
| `CalculateCommand` | `ICommand` | 执行梁截面设计计算命令 |

#### 特性

- **自动箍筋直径更新**: 当选择箍筋规格时，箍筋直径会自动更新为对应的值
- **箍筋规格范围**: 支持 #3-#8 的箍筋规格选择
- **界面优化**: 输入字段合并布局，节约界面空间

### ColumnDesignViewModel (矩形柱设计界面)

#### 属性

| 属性名                   | 类型       | 说明           |
| --------------------- | -------- | ------------ |
| `Fc`                  | `double` | 混凝土抗压强度（psi） |
| `Fy`                  | `double` | 钢筋屈服强度（psi）  |
| `B`                   | `double` | 截面宽度（in）     |
| `H`                   | `double` | 截面高度（in）     |
| `Cover`               | `double` | 保护层厚度（in）    |
| `SelectedBarSize`     | `string` | 纵筋规格         |
| `SelectedTieSize`     | `string` | 箍筋规格         |
| `TieLegsX`            | `int`    | X方向箍筋肢数      |
| `TieLegsY`            | `int`    | Y方向箍筋肢数      |
| `Nx`                  | `int`    | 宽度方向纵筋根数     |
| `Ny`                  | `int`    | 高度方向纵筋根数     |
| `Pu`                  | `double` | 设计轴力（kips）   |
| `Mux`                 | `double` | X轴弯矩（kip-ft） |
| `Muy`                 | `double` | Y轴弯矩（kip-ft） |
| `Vux`                 | `double` | X方向剪力（kips）  |
| `Vuy`                 | `double` | Y方向剪力（kips）  |
| `DesignProcess`       | `string` | 设计过程详细文本     |
| `SeriesX`             | `ISeries[]` | X轴P-M曲线图表数据 |
| `SeriesY`             | `ISeries[]` | Y轴P-M曲线图表数据 |
| `RebarLocations`      | `ObservableCollection<Point>` | 钢筋位置预览 |
| `RebarInfo`           | `string` | 钢筋信息显示文本     |

#### 集合属性

| 属性名                  | 类型             | 说明              |
| -------------------- | -------------- | --------------- |
| `BarSizeOptions`     | `List<string>` | 纵筋规格选项列表（#3-#18） |
| `TieLegOptions`      | `List<int>`    | 箍筋肢数选项列表（2-12） |
| `BarCountOptions`    | `List<int>`    | 纵筋根数选项列表（2-12） |

#### 命令

| 命令名                | 类型         | 说明          |
| ------------------ | ---------- | ----------- |
| `CalculateCommand` | `ICommand` | 执行柱截面设计计算命令 |

#### 特性

- **双向弯曲验算**: 支持X轴和Y轴双向弯曲承载力验算
- **P-M交互图**: 实时绘制名义曲线和设计曲线
- **钢筋预览**: 可视化显示截面钢筋布置
- **构造验算**: 自动验算配筋率、最小根数、纵筋间距

---

### CircularColumnDesignViewModel (圆形柱设计界面)

#### 属性

| 属性名                   | 类型       | 说明           |
| --------------------- | -------- | ------------ |
| `Fc`                  | `double` | 混凝土抗压强度（psi） |
| `Fy`                  | `double` | 钢筋屈服强度（psi）  |
| `Diameter`            | `double` | 柱直径（in）      |
| `Cover`               | `double` | 保护层厚度（in）    |
| `SelectedBarSize`     | `string` | 纵筋规格         |
| `SelectedTieSize`     | `string` | 箍筋规格         |
| `NumBars`             | `int`    | 纵筋根数（最少6根）   |
| `SelectedTieType`     | `TieType` | 箍筋类型（绑扎/螺旋） |
| `Pu`                  | `double` | 设计轴力（kips）   |
| `Mux`                 | `double` | X轴弯矩（kip-ft） |
| `Muy`                 | `double` | Y轴弯矩（kip-ft） |
| `Vux`                 | `double` | X方向剪力（kips）  |
| `Vuy`                 | `double` | Y方向剪力（kips）  |
| `DesignProcess`       | `string` | 设计过程详细文本     |
| `SeriesX`             | `ISeries[]` | P-M曲线图表数据   |
| `RebarLocations`      | `ObservableCollection<RebarVisualItem>` | 钢筋位置预览 |
| `RebarInfo`           | `string` | 截面信息显示文本     |

#### 集合属性

| 属性名                  | 类型             | 说明              |
| -------------------- | -------------- | --------------- |
| `BarSizes`           | `List<string>` | 纵筋规格选项列表（#3-#18） |
| `TieTypes`           | `List<TieType>` | 箍筋类型选项（绑扎/螺旋） |
| `NumBarsOptions`     | `List<int>`    | 纵筋根数选项列表（6-20） |

#### 命令

| 命令名                | 类型         | 说明          |
| ------------------ | ---------- | ----------- |
| `CalculateCommand` | `ICommand` | 执行圆形柱设计计算命令 |

#### 特性

- **合成弯矩**: 自动计算合成弯矩 Mu = √(Mux² + Muy²)
- **各向同性**: 圆形柱各方向承载力相同
- **箍筋类型**: 支持绑扎箍筋和螺旋箍筋两种类型
- **圆形截面P-M图**: 专门针对圆形截面的交互图计算

---

### PunchingShearViewModel (双向板抗冲切界面)

#### 属性

| 属性名                   | 类型       | 说明           |
| --------------------- | -------- | ------------ |
| `H`                   | `double` | 楼板厚度（in）     |
| `Cover`               | `double` | 保护层厚度（in）    |
| `SelectedBarSize`     | `string` | 板钢筋规格        |
| `C1`                  | `double` | 柱尺寸c1（in）    |
| `C2`                  | `double` | 柱尺寸c2（in）    |
| `ColumnLocation`      | `string` | 柱位置（中柱/边柱/角柱） |
| `Vu`                  | `double` | 设计剪力（kips）   |
| `Msc`                 | `double` | 不平衡弯矩（kip-ft） |
| `Fc`                  | `double` | 混凝土强度（psi）   |
| `Lambda`              | `double` | 轻质混凝土系数      |
| `Fyt`                 | `double` | 箍筋屈服强度（psi）  |
| `StudFyt`             | `double` | 栓钉屈服强度（psi）  |
| `ReinforcementType`   | `string` | 抗剪钢筋类型       |
| `StudSize`            | `string` | 栓钉规格         |
| `StudsPerPerimeter`   | `int`    | 每周栓钉数量       |
| `StudSpacing`         | `double` | 栓钉间距（in）     |
| `ShowReinforcementInputs` | `bool` | 是否显示抗剪钢筋输入 |
| `ResultSummary`       | `string` | 结果摘要         |
| `ReinforcementResult` | `string` | 抗剪钢筋设计结果     |
| `DesignProcess`       | `string` | 设计过程详细文本     |
| `SectionProperties`   | `string` | 临界截面属性显示     |
| `DemandVsCapacity`    | `string` | 需求与承载力显示     |

#### 集合属性

| 属性名                  | 类型             | 说明              |
| -------------------- | -------------- | --------------- |
| `BarSizeOptions`     | `List<string>` | 钢筋规格选项列表（#3-#11） |
| `ColumnLocationOptions` | `List<string>` | 柱位置选项（中柱/边柱/角柱） |
| `ReinforcementTypeOptions` | `List<string>` | 抗剪钢筋类型选项 |
| `StudSizeOptions`    | `List<string>` | 栓钉规格选项列表（#3-#8） |
| `StudsPerPerimeterOptions` | `List<int>` | 每周栓钉数量选项（2-16） |

#### 命令

| 命令名                | 类型         | 说明          |
| ------------------ | ---------- | ----------- |
| `CalculateCommand` | `ICommand` | 执行抗冲切验算命令 |

#### 特性

- **三种柱位置**: 支持中柱、边柱、角柱三种情况
- **尺寸效应系数**: 自动计算ACI 318-19尺寸效应系数λs
- **抗剪钢筋设计**: 支持抗剪栓钉和箍筋两种抗剪钢筋
- **构造要求检查**: 自动检查箍筋构造要求和尺寸效应豁免条件

---

## 柱设计计算接口 (ColumnDesignCalculations.cs)

### ACI 318-19 矩形柱设计

#### CheckColumn

```csharp
public static DesignResult CheckColumn(
    double pu, double mux, double muy, double vux, double vuy,
    MaterialProperties mat, ColumnSection sec)
```

- **功能**: 矩形柱双向偏压设计验算
- **参数**:
  - `pu` - 设计轴力（kips）
  - `mux` - X轴弯矩（kip-ft）
  - `muy` - Y轴弯矩（kip-ft）
  - `vux` - X方向剪力（kips）
  - `vuy` - Y方向剪力（kips）
  - `mat` - 材料属性
  - `sec` - 截面属性
- **返回**: 设计结果对象，包含P-M曲线、验算结果、详细报告

#### ColumnSection 类

| 属性         | 类型       | 说明               |
| ---------- | -------- | ---------------- |
| `B`        | `double` | 截面宽度（in）         |
| `H`        | `double` | 截面高度（in）         |
| `Cover`    | `double` | 保护层厚度（in）        |
| `BarSize`  | `string` | 纵筋规格             |
| `TieSize`  | `string` | 箍筋规格             |
| `Nx`       | `int`    | 宽度方向纵筋根数         |
| `Ny`       | `int`    | 高度方向纵筋根数         |
| `TieLegsX` | `int`    | X方向箍筋肢数          |
| `TieLegsY` | `int`    | Y方向箍筋肢数          |
| `Ag`       | `double` | 毛截面面积（计算属性）      |
| `Ast`      | `double` | 纵筋总面积（计算属性）      |
| `TotalBars`| `int`    | 总纵筋根数（计算属性）      |

#### DesignResult 类

| 属性              | 类型                        | 说明            |
| --------------- | ------------------------- | ------------- |
| `NominalCurveX` | `List<InteractionPoint>`  | X轴名义P-M曲线     |
| `DesignCurveX`  | `List<InteractionPoint>`  | X轴设计P-M曲线     |
| `UserPointX`    | `InteractionPoint`        | X轴用户荷载点       |
| `IsSafeX`       | `bool`                    | X轴是否安全        |
| `NominalCurveY` | `List<InteractionPoint>`  | Y轴名义P-M曲线     |
| `DesignCurveY`  | `List<InteractionPoint>`  | Y轴设计P-M曲线     |
| `UserPointY`    | `InteractionPoint`        | Y轴用户荷载点       |
| `IsSafeY`       | `bool`                    | Y轴是否安全        |
| `Report`        | `string`                  | 详细设计报告        |

#### 计算内容

1. **配筋率验算**: 验算配筋率是否在1%-8%范围内
2. **最小根数验算**: 矩形柱最少4根纵筋
3. **纵筋间距验算**: 最小中心距 max(1.5d, 1.5in)
4. **P-M承载力验算**: 双向弯曲承载力验算
5. **抗剪验算**: 计算混凝土抗剪承载力和箍筋需求
6. **箍筋构造验算**: 最大间距、最小配箍率等

---

## 圆形柱设计计算接口 (CircularColumnDesignCalculations.cs)

### ACI 318-19 圆形柱设计

#### CheckColumn

```csharp
public static ColumnDesignCalculations.DesignResult CheckColumn(
    double Pu, double Mux, double Muy, double Vux, double Vuy,
    ColumnDesignCalculations.MaterialProperties mat, CircularColumnSection sec)
```

- **功能**: 圆形柱双向偏压设计验算
- **参数**:
  - `Pu` - 设计轴力（kips）
  - `Mux` - X轴弯矩（kip-ft）
  - `Muy` - Y轴弯矩（kip-ft）
  - `Vux` - X方向剪力（kips）
  - `Vuy` - Y方向剪力（kips）
  - `mat` - 材料属性
  - `sec` - 圆形截面属性
- **返回**: 设计结果对象

#### CircularColumnSection 类

| 属性          | 类型        | 说明               |
| ----------- | --------- | ---------------- |
| `Diameter`  | `double`  | 柱直径（in）          |
| `Cover`     | `double`  | 保护层厚度（in）        |
| `BarSize`   | `string`  | 纵筋规格             |
| `TieSize`   | `string`  | 箍筋规格             |
| `NumBars`   | `int`     | 纵筋根数（最少6根）       |
| `TieType`   | `TieType` | 箍筋类型（绑扎/螺旋）      |
| `Ag`        | `double`  | 毛截面面积（计算属性）      |
| `Ast`       | `double`  | 纵筋总面积（计算属性）      |

#### TieType 枚举

| 值         | 说明    |
| --------- | ----- |
| `Tied`    | 绑扎箍筋  |
| `Spiral`  | 螺旋箍筋  |

#### 计算特点

- **合成弯矩**: Mu = √(Mux² + Muy²)
- **各向同性**: 圆形柱各方向承载力相同，只需验算一个方向
- **螺旋箍筋**: 螺旋箍筋柱的φ系数为0.75（绑扎为0.65）
- **圆形截面P-M图**: 采用圆形截面应力分布计算

---

## 双向板抗冲切计算接口 (PunchingShearCalculations.cs)

### ACI 318-19 双向板抗冲切验算

#### PerformDesign

```csharp
public static DesignResult PerformDesign(InputParameters input)
```

- **功能**: 执行双向板抗冲切完整设计
- **参数**: `input` - 输入参数对象
- **返回**: 设计结果对象

#### InputParameters 类

| 属性                    | 类型                     | 说明            |
| --------------------- | ---------------------- | ------------- |
| `H`                   | `double`               | 楼板厚度（in）      |
| `Cover`               | `double`               | 保护层厚度（in）     |
| `BarDiameter`         | `double`               | 钢筋直径（in）      |
| `C1`                  | `double`               | 柱尺寸c1（in）     |
| `C2`                  | `double`               | 柱尺寸c2（in）     |
| `Location`            | `ColumnLocation`       | 柱位置           |
| `Vu`                  | `double`               | 设计剪力（lbs）     |
| `Msc`                 | `double`               | 不平衡弯矩（lb-in）  |
| `Fc`                  | `double`               | 混凝土强度（psi）    |
| `Lambda`              | `double`               | 轻质混凝土系数       |
| `Fyt`                 | `double`               | 抗剪钢筋屈服强度（psi） |
| `ReinforcementType`   | `ShearReinforcementType` | 抗剪钢筋类型     |
| `StudArea`            | `double`               | 单根栓钉面积（in²）   |
| `StudsPerPerimeter`   | `int`                  | 每周栓钉数量        |
| `StudSpacing`         | `double`               | 栓钉间距（in）      |

#### ColumnLocation 枚举

| 值          | 说明  |
| ---------- | --- |
| `Interior` | 中柱  |
| `Edge`     | 边柱  |
| `Corner`   | 角柱  |

#### ShearReinforcementType 枚举

| 值               | 说明     |
| --------------- | ------ |
| `None`          | 无抗剪钢筋  |
| `Stirrups`     | 箍筋     |
| `HeadedStuds`  | 抗剪栓钉   |

#### CriticalSectionProperties 类

| 属性        | 类型       | 说明              |
| --------- | -------- | --------------- |
| `D`       | `double` | 有效高度            |
| `B1`      | `double` | 临界截面尺寸b1       |
| `B2`      | `double` | 临界截面尺寸b2       |
| `Bo`      | `double` | 临界周长            |
| `Ac`      | `double` | 临界面积            |
| `Jc`      | `double` | 极惯性矩            |
| `CAB`     | `double` | 形心位置cAB        |
| `CCD`     | `double` | 形心位置cCD        |
| `GammaV`  | `double` | 剪力分配系数γv       |
| `GammaF`  | `double` | 弯矩分配系数γf       |

#### DesignResult 类

| 属性                   | 类型                        | 说明         |
| -------------------- | ------------------------- | ---------- |
| `IsSectionAdequate`  | `bool`                    | 截面尺寸是否满足  |
| `NeedsReinforcement` | `bool`                    | 是否需要抗剪钢筋  |
| `Vu_demand`          | `double`                  | 设计剪应力需求   |
| `Vc_concrete`        | `double`                  | 混凝土抗剪承载力  |
| `PhiVc`              | `double`                  | 设计抗剪承载力   |
| `Vs_required`        | `double`                  | 所需抗剪钢筋贡献  |
| `Vs_provided`        | `double`                  | 提供抗剪钢筋贡献  |
| `PhiVn`              | `double`                  | 总设计抗剪承载力  |
| `Vmax`               | `double`                  | 最大允许剪应力   |
| `LambdaS`            | `double`                  | 尺寸效应系数    |
| `Section`            | `CriticalSectionProperties` | 临界截面属性   |
| `DesignProcess`      | `List<string>`            | 设计过程步骤    |
| `ResultSummary`      | `string`                  | 结果摘要      |
| `ReinforcementResult`| `string`                  | 抗剪钢筋设计结果  |

#### 计算步骤

1. **临界截面属性计算**: 计算d、bo、Jc、γv等
2. **设计剪应力需求**: vu = Vu/Ac + γv·Msc·cAB/Jc
3. **尺寸效应系数**: λs = √(2/(1 + d/10))
4. **混凝土抗剪承载力**: 取三个公式最小值
5. **截面尺寸验算**: vu ≤ φ·vmax
6. **抗冲切验算**: vu ≤ φ·vc
7. **抗剪钢筋设计**: 计算所需抗剪钢筋

---

## 钢筋锚固长度计算接口 (DevelopmentLengthCalculations.cs)

### ACI 318-19 受拉直钢筋锚固长度 ℓd

#### Calculate

```csharp
public static DesignResult Calculate(InputParameters input)
```

- **功能**: 计算受拉直钢筋锚固长度
- **参数**: `input` - 输入参数对象
- **返回**: 设计结果对象，包含锚固长度、修正系数、详细计算过程

#### InputParameters 类

| 属性                       | 类型                     | 说明               |
| ------------------------ | ---------------------- | ----------------- |
| `Fc`                     | `double`               | 混凝土抗压强度（psi）     |
| `ConcreteType`           | `ConcreteType`         | 混凝土类型（普通/轻骨料）    |
| `SteelGrade`             | `SteelGrade`           | 钢筋等级（40/60/80/100） |
| `BarSize`                | `string`               | 钢筋规格             |
| `Coating`                | `CoatingType`          | 涂层类型             |
| `CastingPosition`        | `CastingPosition`      | 浇筑位置（其他/顶筋）     |
| `Method`                 | `CalculationMethod`    | 计算方法（精确/简化）     |
| `SpacingCoverCondition`  | `SpacingCoverCondition`| 间距保护层条件（简化法）   |
| `Cc`                     | `double`               | 净保护层厚度（in）       |
| `S`                      | `double`               | 钢筋中心距（in）        |
| `Atr`                    | `double`               | 横向钢筋面积（in²）      |
| `Str`                    | `double`               | 横向钢筋间距（in）       |
| `N`                      | `int`                  | 锚固纵筋数量           |
| `IsSFRS`                 | `bool`                 | 是否属于抗震受力体系      |
| `IsYieldZone`            | `bool`                 | 是否位于预期屈服区       |
| `AsRequired`             | `double`               | 所需钢筋面积（in²）      |
| `AsProvided`             | `double`               | 提供钢筋面积（in²）      |

#### 枚举类型

**ConcreteType**

| 值              | 说明   |
| -------------- | ---- |
| `Normalweight` | 普通混凝土 |
| `Lightweight`  | 轻骨料混凝土 |

**SteelGrade**

| 值             | 说明        |
| -------------- | --------- |
| `Grade40`      | 40 ksi    |
| `Grade60`      | 60 ksi    |
| `Grade80`      | 80 ksi    |
| `Grade100`     | 100 ksi   |

**CoatingType**

| 值               | 说明      |
| --------------- | ------- |
| `Uncoated`      | 无涂层     |
| `EpoxyCoated`   | 环氧涂层    |
| `ZincAndEpoxy`  | 锌+环氧双涂层 |

**CalculationMethod**

| 值          | 说明                      |
| ----------- | ----------------------- |
| `Detailed`  | 精确公式法 (Eq. 25.4.2.4a)  |
| `QuickCalc` | 简化查表法 (Table 25.4.2.3) |

**SpacingCoverCondition**

| 值             | 说明     |
| -------------- | ------ |
| `Favorable`    | 条件良好   |
| `OtherCases`   | 其他保守情况 |

#### DesignResult 类

| 属性                | 类型            | 说明              |
| ----------------- | ------------- | --------------- |
| `LdBase`          | `double`      | 基础锚固长度          |
| `LdReduced`       | `double`      | 折减后锚固长度         |
| `LdSeismic`       | `double`      | 抗震放大后锚固长度       |
| `LdFinal`         | `double`      | 最终锚固长度          |
| `PsiT`            | `double`      | 浇筑位置系数 ψt       |
| `PsiE`            | `double`      | 涂层系数 ψe         |
| `PsiS`            | `double`      | 钢筋尺寸系数 ψs       |
| `PsiG`            | `double`      | 钢筋等级系数 ψg       |
| `Lambda`          | `double`      | 轻骨料系数 λ         |
| `Cb`              | `double`      | 约束参数 cb         |
| `Ktr`             | `double`      | 横向钢筋指数 Ktr      |
| `ConfinementTerm` | `double`      | 约束项 (cb+Ktr)/db |
| `SqrtFc`          | `double`      | √f'c (限制≤100)  |
| `Db`              | `double`      | 钢筋直径 db         |
| `Fy`              | `double`      | 屈服强度 fy         |
| `ReductionFactor` | `double`      | 多余钢筋折减系数        |
| `SeismicFactor`   | `double`      | 抗震放大系数          |
| `Process`         | `List<string>`| 详细计算过程          |

#### 计算步骤

1. **数据预处理**: 限制 √f'c ≤ 100 psi (25.4.1.4)
2. **修正系数计算**: ψt, ψe, ψs, ψg, λ (Table 25.4.2.5)
3. **约束参数计算**: cb, Ktr, (cb+Ktr)/db ≤ 2.5 (25.4.2.4)
4. **基础锚固长度**: 精确公式或简化查表法
5. **多余钢筋折减**: 抗震体系禁止折减 (25.4.10)
6. **抗震预期屈服区放大**: 1.25倍 (18.10.2.3b)
7. **最小锚固长度检查**: ≥ 12 in. (25.4.2.1b)

---

### DevelopmentLengthViewModel (钢筋锚固长度界面)

#### 属性

| 属性名                       | 类型       | 说明                    |
| ------------------------- | -------- | --------------------- |
| `Fc`                      | `double` | 混凝土抗压强度（psi）          |
| `ConcreteType`            | `string` | 混凝土类型                 |
| `SteelGrade`              | `string` | 钢筋等级                  |
| `BarSize`                 | `string` | 钢筋规格                  |
| `Coating`                 | `string` | 涂层类型                  |
| `CastingPosition`         | `string` | 浇筑位置                  |
| `CalculationMethod`       | `string` | 计算方法                  |
| `SpacingCoverCondition`   | `string` | 间距保护层条件（简化法）         |
| `Cc`                      | `double` | 净保护层厚度（in）            |
| `S`                       | `double` | 钢筋中心距（in）             |
| `Atr`                     | `double` | 横向钢筋面积（in²）           |
| `Str`                     | `double` | 横向钢筋间距（in）            |
| `N`                       | `int`    | 锚固纵筋数量                |
| `IsSFRS`                  | `bool`   | 是否属于抗震受力体系            |
| `IsYieldZone`             | `bool`   | 是否位于预期屈服区             |
| `AsRequired`              | `double` | 所需钢筋面积（in²）           |
| `AsProvided`              | `double` | 提供钢筋面积（in²）           |
| `IsDetailedMethod`        | `bool`   | 是否为精确公式法（控制界面显隐）     |
| `ResultSummary`           | `string` | 结果摘要                  |
| `CoefficientsText`        | `string` | 修正系数显示文本              |
| `DesignProcess`           | `string` | 设计过程详细文本              |

#### 集合属性

| 属性名                            | 类型             | 说明                     |
| ------------------------------ | -------------- | ---------------------- |
| `ConcreteTypeOptions`          | `List<string>` | 混凝土类型选项                |
| `SteelGradeOptions`            | `List<string>` | 钢筋等级选项（Grade 40-100）   |
| `BarSizeOptions`               | `List<string>` | 钢筋规格选项（#3-#18）         |
| `CoatingOptions`               | `List<string>` | 涂层类型选项                 |
| `CastingPositionOptions`       | `List<string>` | 浇筑位置选项                 |
| `CalculationMethodOptions`     | `List<string>` | 计算方法选项                 |
| `SpacingCoverConditionOptions` | `List<string>` | 间距保护层条件选项              |

#### 命令

| 命令名                | 类型         | 说明        |
| ------------------ | ---------- | --------- |
| `CalculateCommand` | `ICommand` | 执行锚固长度计算命令 |

#### 特性

- **双计算模式**: 支持精确公式法和简化查表法切换
- **动态界面**: 选择精确公式法时显示约束参数输入，选择简化法时显示条件选择
- **抗震逻辑**: 自动处理SFRS禁止折减和预期屈服区1.25倍放大
- **完整计算书**: 输出详细的计算过程和规范条文引用

---

## WeChatView (关注公众号界面)

#### 功能

- **显示公众号二维码**: 展示"美标学习日记"公众号的二维码
- **居中布局**: 二维码和说明文字居中显示
- **版权信息**: 显示软件版权信息

#### 特性

- **独立模块**: 作为软件的最后一个功能模块
- **简洁设计**: 采用卡片式布局，界面简洁美观
- **响应式布局**: 适应不同窗口尺寸

---

## 软件功能模块

软件包含以下功能模块：

1. **反应谱比较** - 中美规范地震反应谱对比分析
2. **地震人工波模拟** - 基于频域迭代拟合方法生成符合目标反应谱的人工地震波
3. **风速转换** - ASCE7风速转换为GB50009基本风压
4. **阵风响应因子G** - ASCE 7-16 Section 26.11风振系数计算
5. **风荷载模拟** - ASCE 7-16脉动风时程模拟（Kaimal谱+FFT方法）
6. **单筋混凝土梁** - ACI 318-25梁截面受弯受剪设计
7. **混凝土矩形柱** - ACI 318-19矩形柱双向偏压设计（P-M交互图、抗剪验算）
8. **混凝土圆形柱** - ACI 318-19圆形柱双向偏压设计（支持螺旋箍筋）
9. **双向板抗冲切** - ACI 318-19板柱节点抗冲切验算（支持抗剪栓钉/箍筋）
10. **钢筋锚固长度** - ACI 318-19受拉直钢筋锚固长度ℓd计算（精确公式/简化查表）
11. **关注公众号** - 显示公众号二维码

---

## 技术特点

- **MVVM架构**: 采用Model-View-ViewModel模式，实现界面与逻辑分离
- **数据绑定**: 使用WPF数据绑定机制，实现界面与数据的自动同步
- **Fluent Design**: 采用Material Design风格，界面美观现代
- **实时计算**: 支持实时参数调整和计算结果更新
- **详细过程**: 提供完整的计算过程说明，便于学习和验证
- **多单位支持**: 支持国际单位制和英制单位