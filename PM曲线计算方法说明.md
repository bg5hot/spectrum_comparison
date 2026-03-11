# P-M 相互作用曲线绘制算法详解

本文档详细解释了 `ColumnDesignCalculations.cs` 中用于绘制混凝土柱 P-M (轴力-弯矩) 相互作用曲线的核心算法。该算法基于 **ACI 318-19** 规范，采用 **应变协调法 (Strain Compatibility Method)** 进行计算。

## 1. 核心原理

P-M 曲线代表了柱截面在不同偏心率下的破坏包络线。曲线上的每一点都对应截面的一种极限破坏状态。计算的核心思想是假设一系列的中和轴深度 ($c$)，对于每一个 $c$，计算对应的轴力 ($P_n$) 和弯矩 ($M_n$)。

### 基本假设 (ACI 318)
1.  **平截面假定**：截面变形后仍保持平面，应变与距中和轴的距离成正比。
2.  **极限压应变**：混凝土受压边缘达到极限应变 $\epsilon_{cu} = 0.003$ 时发生破坏。
3.  **等效矩形应力块**：混凝土受压区的应力分布简化为等效矩形应力块 (Whitney Stress Block)。
4.  **钢筋本构**：钢筋采用理想弹塑性模型 ($E_s = 29,000$ ksi, 屈服后应力保持 $f_y$)。

---

## 2. 代码逻辑流程

代码主要位于 `ColumnDesignCalculations.cs` 中的 `CalculateInteractionDiagram` 和 `CalculatePoint` 方法。

### 步骤 1: 截面离散化
**方法**: `ColumnSection.GetRebarLayers()`

首先，将纵向钢筋离散化为若干层 (Layers)。每一层包含：
*   `y`: 该层钢筋距离截面受压顶部的距离。
*   `area`: 该层钢筋的总面积。

```csharp
// 示例：获取钢筋层信息
var layers = sec.GetRebarLayers();
double d = layers.Last().y; // 最外层受拉钢筋的有效高度
```

### 步骤 2: 扫描控制点 (主循环)
**方法**: `CalculateInteractionDiagram`

算法通过改变中和轴深度 `c` 来遍历截面的所有破坏状态：

1.  **纯受压点 (Pure Compression)**:
    *   理论上 $c = \infty$，代码中取一个极大值 (e.g., $99999$).
    *   此时全截面受压，应变均匀。
2.  **一般偏压点 (Interaction Region)**:
    *   通过循环改变 $c$ 的值，从 $c = 1.2d$ (大偏心/受压控制) 扫描到 $c = 0.05d$ (小偏心/受拉控制)。
    *   循环步长决定了曲线的光滑程度。
3.  **纯受拉点 (Pure Tension)**:
    *   $c = 0$ (或极小值)。
    *   此时全截面受拉，混凝土不参与工作。

```csharp
// 1. 纯受压 (c 极大)
CalculatePoint(..., 99999, ...);

// 2. 扫描中间状态 (c 从 1.2d 减小到 0.05d)
for (double r = 1.2; r >= 0.05; r -= 0.05)
{
    CalculatePoint(..., r * d, ...);
}

// 3. 纯受拉 (c = 0)
CalculatePoint(..., 0, ...);
```

### 步骤 3: 单点计算逻辑 (核心)
**方法**: `CalculatePoint`

这是计算引擎的核心，输入一个特定的 $c$ 值，输出对应的 $(P_n, M_n)$ 和 $(\phi P_n, \phi M_n)$。

#### A. 计算混凝土贡献 ($C_c$)
利用等效矩形应力块参数 $\beta_1$ (根据 $f'_c$ 计算)：
*   受压区高度 $a = \beta_1 \times c$ (且 $a \le h$)。
*   混凝土压力 $C_c = 0.85 f'_c \times b \times a$。
*   $C_c$ 作用点位于 $a/2$ 处，对截面几何中心 ($h/2$) 取矩。

```csharp
double a = beta1 * c;
if (a > sec.H) a = sec.H;
double Cc = 0.85 * mat.Fc * sec.B * a;
Pn += Cc;
Mn += Cc * (sec.H / 2.0 - a / 2.0); // 对几何中心的力矩
```

#### B. 计算钢筋贡献 ($F_{si}$)
对每一层钢筋进行遍历：
1.  **计算应变**: 根据线性应变分布 (相似三角形)。
    $$ \epsilon_s = 0.003 \times \frac{c - y}{c} $$
2.  **计算应力**:
    $$ f_s = \epsilon_s \times E_s $$
    限制在 $[-f_y, f_y]$ 范围内。
3.  **修正混凝土重叠**:
    *   如果在受压区 ($y < a$)，钢筋占据了混凝土的体积，需要扣除这部分混凝土的力 ($0.85 f'_c$)。
4.  **累加力和力矩**:
    *   $P_n += F_s$
    *   $M_n += F_s \times (h/2 - y)$

```csharp
foreach (var layer in layers)
{
    // 1. 应变协调
    double strain = 0.003 * (c - layer.y) / c;
    
    // 2. 本构关系 (Hooke's Law + Yield limit)
    double stress = strain * mat.Es;
    if (stress > mat.Fy) stress = mat.Fy;
    else if (stress < -mat.Fy) stress = -mat.Fy;

    // 3. 累加贡献 (含受压区混凝土扣除逻辑)
    if (layer.y < a) { ... }
    else { ... }
    
    // 4. 记录最外层受拉应变 epsilon_t 用于计算 phi
    if (layer.y > c) epsilon_t = Math.Max(epsilon_t, Math.Abs(strain));
}
```

#### C. 计算强度折减系数 ($\phi$)
根据最外层受拉钢筋的净拉应变 $\epsilon_t$ 确定破坏模式 (ACI 318 Table 21.2.2)：
*   **受压控制 (Compression-controlled)**: $\epsilon_t \le \epsilon_{ty}$ (通常 0.002), $\phi = 0.65$ (对于箍筋柱)。
*   **受拉控制 (Tension-controlled)**: $\epsilon_t \ge 0.005$, $\phi = 0.90$。
*   **过渡区 (Transition)**: 线性插值。

```csharp
double ty = mat.Fy / mat.Es;
if (epsilon_t <= ty)
    phi = 0.65;
else if (epsilon_t >= 0.005)
    phi = 0.90;
else
    phi = 0.65 + 0.25 * (epsilon_t - ty) / (0.005 - ty);
```

#### D. 最大轴压限制
规范规定设计轴力不能超过特定限值 (ACI 318 Eq. 22.4.2.1)：
$$ \phi P_{n,max} = 0.80 \times \phi \times [0.85 f'_c (A_g - A_{st}) + f_y A_{st}] $$
代码中会对计算出的 $\phi P_n$ 进行截断处理，这就形成了 P-M 曲线顶部的"平头"部分。

```csharp
if (phiPn > maxPhiPn)
{
   phiPn = maxPhiPn;
}
```

## 3. 结果输出

最终生成两条曲线：
1.  **Nominal Curve ($P_n, M_n$)**: 名义承载力曲线，不考虑 $\phi$ 系数。
2.  **Design Curve ($\phi P_n, \phi M_n$)**: 设计承载力曲线，用于与设计荷载 ($P_u, M_u$) 进行比对。

如果用户的荷载点 ($P_u, M_u$) 位于 Design Curve 内部，则截面承载力满足要求。

### 步骤 4: 结果验证与承载力校核

为了判断用户的设计荷载 ($P_u, M_u$) 是否安全，以及计算当前的承载力利用率 (D/C Ratio)，程序实现了以下两个关键辅助算法。

#### A. 判断点是否在曲线内 (`CheckPointInsideCurve`)
该方法用于快速判断一个给定点是否在 P-M 包络线内部。

**算法逻辑**:
1.  **预处理**: 将 P-M 曲线上的点按轴力 $P$ 从大到小排序。
2.  **边界检查**:
    *   如果 $P_u$ 大于曲线最大轴力 ($P_{max} + 容差$)，则在曲线外 (不安全)。
    *   如果 $P_u$ 小于曲线最小轴力 ($P_{min} - 容差$)，则在曲线外 (不安全)。
3.  **区间查找与插值**:
    *   遍历排序后的曲线点，找到包含 $P_u$ 的线段区间 $[P_{i+1}, P_i]$，即满足 $P_{i+1} \le P_u \le P_i$。
    *   利用线性插值计算在该轴力 $P_u$ 下曲线对应的最大允许弯矩 $M_{max}$：
        $$ Ratio = \frac{P_u - P_{i+1}}{P_i - P_{i+1}} $$
        $$ M_{max} = M_{i+1} + Ratio \times (M_i - M_{i+1}) $$
4.  **判定**:
    *   如果用户弯矩 $|M_u| \le M_{max}$，则点在曲线内 (安全)。
    *   否则，点在曲线外 (不安全)。

```csharp
// 核心逻辑简述
for (int i = 0; i < sorted.Count - 1; i++)
{
    if (pt.P <= p1.P && pt.P >= p2.P)
    {
        // 线性插值计算当前P对应的包络线弯矩
        double maxM = Interpolate(p1, p2, pt.P);
        if (Math.Abs(pt.M) <= maxM) return true;
    }
}
```

#### B. 计算特定轴力下的弯矩承载力 (`GetMomentCapacityAtP`)
该方法用于计算在用户给定轴力 $P_u$ 下，截面能够承受的最大弯矩 $\phi M_n$，从而计算 D/C 比。

**算法逻辑**:
与 `CheckPointInsideCurve` 类似，该方法也是基于插值：
1.  **排序与边界检查**: 同样对曲线按 $P$ 降序排序。如果 $P_u$ 超出范围，返回 0。
2.  **定位与计算**:
    *   找到包含 $P_u$ 的曲线区间。
    *   通过线性插值直接返回该位置的弯矩值 $M_{capacity}$。
3.  **应用**:
    *   返回值即为 $\phi M_n$ (在给定 $P_u$ 下)。
    *   D/C Ratio (弯矩比) = $M_u / \phi M_n$。

---

## 4. 总结

该模块完整实现了 ACI 318-19 关于柱截面承载力的计算要求，主要特点包括：
*   精确的**应变协调分析**，而非查表法。
*   考虑了**任意排布的钢筋层** (不仅是两边配筋)。
*   动态计算 **$\phi$ 系数**，反映了从脆性破坏到延性破坏的转变。
*   包含了规范规定的**最大轴压限制**。

---

## 5. 双向抗剪验算 (Biaxial Shear Design)

除了 P-M 弯曲承载力，模块还实现了基于 **ACI 318-19** 的双向抗剪验算。

### A. 双向独立验算
柱子在两个主轴方向 ($X$ 和 $Y$) 上分别承受剪力 $V_{ux}$ 和 $V_{uy}$。程序对这两个方向分别进行独立的抗剪验算。

1.  **Y向剪力 ($V_{uy}$)**:
    *   剪力平行于截面高度 $H$ 方向。
    *   抗剪宽度 $b_w = B$。
    *   抗剪深度 $h = H$ (用于计算有效高度 $d$).
    *   由平行于 $H$ 的箍筋肢 ($TieLegsY$) 提供抗剪贡献。

2.  **X向剪力 ($V_{ux}$)**:
    *   剪力平行于截面宽度 $B$ 方向。
    *   抗剪宽度 $b_w = H$。
    *   抗剪深度 $h = B$ (用于计算有效高度 $d$).
    *   由平行于 $B$ 的箍筋肢 ($TieLegsX$) 提供抗剪贡献。

### B. 混凝土抗剪承载力 ($V_c$)
根据 ACI 318-19 Table 22.5.5.1，考虑轴压力 $N_u$ 对抗剪强度的有利影响：

$$ V_c = \left( 2\lambda\sqrt{f'_c} + \frac{N_u}{6A_g} \right) b_w d $$

其中：
*   $N_u = P_u$ (轴压力，lbs)
*   $\lambda = 1.0$ (普通混凝土)
*   $b_w$ 和 $d$ 根据剪力方向取对应值。

### C. 箍筋抗剪需求 ($V_s$)
如果 $V_u > \phi V_c$ (其中 $\phi = 0.75$)，则需要配置抗剪箍筋。所需箍筋提供的抗剪强度为：

$$ V_{s,req} = \frac{V_u}{\phi} - V_c $$

所需箍筋间距 $s$ 计算公式 (ACI 318-19 Eq. 22.5.8.5.3)：

$$ s \le \frac{A_v f_{yt} d}{V_s} $$

其中 $A_v$ 为对应方向箍筋的总截面面积 (单肢面积 $\times$ 肢数)。

### D. 综合间距控制
程序分别计算 $X$ 向和 $Y$ 向所需的强度间距 $s_{req,x}$ 和 $s_{req,y}$，并结合构造要求 (最大间距 $s_{max}$) 确定最终设计间距：

$$ s_{design} = \min(s_{req,x}, s_{req,y}, s_{max}) $$

构造最大间距 $s_{max}$ 取以下最小值 (ACI 318-19 25.7.2.1)：
1.  $16 \times$ 纵筋直径
2.  $48 \times$ 箍筋直径
3.  截面最小边长
