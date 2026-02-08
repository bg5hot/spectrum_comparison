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
| MaterialDesignThemes                  | 4.9.0          | UI样式和控件            |
| MVVM 模式                               | -              | 数据绑定和界面分离          |

---

## 项目文件结构

```
ASCE7 vs GB50011/
├── UsCodeTools.csproj             # 项目配置文件
├── CoreCalculations.cs            # 核心计算逻辑（反应谱和风速转换）
├── GustEffectFactorCalculations.cs # 风振系数计算逻辑（ASCE 7-16 Section 26.11）
├── BeamDesignCalculations.cs      # 梁截面设计计算逻辑（ACI 318-25）
├── SpectrumViewModel.cs           # 反应谱界面ViewModel
├── WindConversionViewModel.cs     # 风速转换界面ViewModel
├── GustEffectFactorViewModel.cs   # 风振系数界面ViewModel
├── BeamDesignViewModel.cs         # 梁截面设计界面ViewModel
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
├── WeChatView.xaml               # 关注公众号界面
├── WeChatView.xaml.cs            # 关注公众号界面代码后置
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

### WeChatView (关注公众号界面)

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
2. **风速转换** - ASCE7风速转换为GB50009基本风压
3. **风振系数** - ASCE 7-16 Section 26.11风振系数计算
4. **单筋混凝土梁** - ACI 318-25梁截面设计
5. **关注公众号** - 显示公众号二维码

---

## 技术特点

- **MVVM架构**: 采用Model-View-ViewModel模式，实现界面与逻辑分离
- **数据绑定**: 使用WPF数据绑定机制，实现界面与数据的自动同步
- **Fluent Design**: 采用Material Design风格，界面美观现代
- **实时计算**: 支持实时参数调整和计算结果更新
- **详细过程**: 提供完整的计算过程说明，便于学习和验证
- **多单位支持**: 支持国际单位制和英制单位