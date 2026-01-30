# 2026014：增加风速转换模块，通过左上角的按钮切换到风速转换界面

#  -python version: 3.11.9
# - numpy==2.4.1
# - matplotlib==3.10.8
# - PyQt6==6.10.2
# - PyQt6-Fluent-Widgets==1.10.5
# - PyQt6-Frameless-Window==0.7.5

import sys
import numpy as np
import matplotlib
import matplotlib.pyplot as plt
from matplotlib.backends.backend_qtagg import FigureCanvasQTAgg
from matplotlib.figure import Figure

from PyQt6.QtCore import Qt
from PyQt6.QtWidgets import QApplication, QWidget, QHBoxLayout, QVBoxLayout, QFrame

# 引入 Fluent Widgets 组件
# PyQt-Fluent-Widgets
from qfluentwidgets import (
    FluentWindow, SubtitleLabel, BodyLabel, StrongBodyLabel,
    ComboBox, DoubleSpinBox, PrimaryPushButton,
    CardWidget, ScrollArea, setTheme, Theme, InfoBar, InfoBarPosition,
    FluentIcon as FIF, TextEdit
)

# ==========================================
# 1. 核心计算逻辑 (保持原样不变)
# ==========================================

# 设置matplotlib支持中文
plt.rcParams['font.sans-serif'] = ['SimHei', 'Microsoft YaHei', 'Arial Unicode MS']
plt.rcParams['axes.unicode_minus'] = False

def get_alpha_max(intensity):
    alpha_max_table = {
        "6度(0.05g)": 0.04, "7度(0.10g)": 0.08, "7度(0.15g)": 0.12,
        "8度(0.20g)": 0.16, "8度(0.30g)": 0.24, "9度(0.40g)": 0.32
    }
    return alpha_max_table.get(intensity, 0.08)

def get_Tg(site_category, earthquake_group):
    Tg_table = {
        "第一组": {"I0": 0.20, "I1": 0.25, "II": 0.35, "III": 0.45, "IV": 0.65},
        "第二组": {"I0": 0.25, "I1": 0.30, "II": 0.40, "III": 0.55, "IV": 0.75},
        "第三组": {"I0": 0.30, "I1": 0.35, "II": 0.45, "III": 0.65, "IV": 0.90}
    }
    return Tg_table.get(earthquake_group, {}).get(site_category, 0.35)

def calculate_chinese_spectrum(alpha_max, Tg, damping=0.05):
    gamma = 0.9 + (0.05 - damping) / (0.3 + 6 * damping)
    eta1 = 0.02 + (0.05 - damping) / (4 + 32 * damping)
    if eta1 < 0: eta1 = 0
    eta2 = 1 + (0.05 - damping) / (0.08 + 1.6 * damping)
    if eta2 < 0.55: eta2 = 0.55
    
    periods = np.linspace(0.01, 6.0, 600)
    alpha = np.zeros_like(periods)
    
    for i, T in enumerate(periods):
        if T < 0.1:
            alpha[i] = (0.45 * alpha_max) + (eta2 - 0.45) * alpha_max * (T / 0.1)
        elif T < Tg:
            alpha[i] = eta2 * alpha_max
        elif T < 5 * Tg:
            alpha[i] = eta2 * alpha_max * (Tg / T) ** gamma
        else:
            alpha[i] = (eta2 * 0.2 ** gamma - eta1 * (T - 5 * Tg)) * alpha_max
    return periods, alpha

def get_Fa_Fv(Ss, S1, site_class):
    Fa_table = {
        'A': {0.25: 0.8, 0.5: 0.8, 0.75: 0.8, 1.0: 0.8, 1.25: 0.8, 1.50: 0.8},
        'B': {0.25: 0.9, 0.5: 0.9, 0.75: 0.9, 1.0: 0.9, 1.25: 0.9, 1.50: 0.9},
        'C': {0.25: 1.3, 0.5: 1.3, 0.75: 1.2, 1.0: 1.2, 1.25: 1.2, 1.50: 1.2},
        'D': {0.25: 1.6, 0.5: 1.4, 0.75: 1.2, 1.0: 1.1, 1.25: 1.0, 1.50: 1.0}
    }
    Fv_table = {
        'A': {0.1: 0.8, 0.2: 0.8, 0.3: 0.8, 0.4: 0.8, 0.5: 0.8, 0.6: 0.8},
        'B': {0.1: 0.8, 0.2: 0.8, 0.3: 0.8, 0.4: 0.8, 0.5: 0.8, 0.6: 0.8},
        'C': {0.1: 1.5, 0.2: 1.5, 0.3: 1.5, 0.4: 1.5, 0.5: 1.5, 0.6: 1.4},
        'D': {0.1: 2.4, 0.2: 2.2, 0.3: 2.0, 0.4: 1.9, 0.5: 1.8, 0.6: 1.7}
    }
    def interpolate_value(table, value):
        keys = sorted(table.keys())
        if value <= keys[0]: return table[keys[0]]
        elif value >= keys[-1]: return table[keys[-1]]
        else:
            for i in range(len(keys) - 1):
                if keys[i] <= value < keys[i + 1]:
                    x1, y1 = keys[i], table[keys[i]]
                    x2, y2 = keys[i + 1], table[keys[i + 1]]
                    return y1 + (y2 - y1) * (value - x1) / (x2 - x1)
    Fa = interpolate_value(Fa_table[site_class], Ss)
    Fv = interpolate_value(Fv_table[site_class], S1)
    return Fa, Fv

def calculate_us_spectrum(Ss, S1, site_class, TL, R, damping=0.05):
    Fa, Fv = get_Fa_Fv(Ss, S1, site_class)
    SMS = Fa * Ss
    SM1 = Fv * S1
    SDS = (2/3) * SMS
    SD1 = (2/3) * SM1
    T0 = 0.2 * (SD1 / SDS) if SDS != 0 else 0
    Ts = SD1 / SDS if SDS != 0 else 0
    periods = np.linspace(0.01, 6.0, 600)
    Sa = np.zeros_like(periods)

    # 周期调整系数
    if damping <= 0.02:
        B = 0.8
    elif damping <= 0.05:
        B = 0.8 + 0.2 * (damping - 0.02) / 0.03
    elif damping <= 0.10:
        B = 1.0 + 0.2 * (damping - 0.05) / 0.05
    elif damping <= 0.20:
        B = 1.2 + 0.3 * (damping - 0.10) / 0.10
    else:
        B = 1.5

    for i, T in enumerate(periods):
        if T < T0: sa = SDS * (0.4 + 0.6 * T / T0)
        elif T0 <= T < Ts: sa = SDS
        elif Ts <= T < TL: sa = SD1 / T
        else: sa = SD1 * TL / (T**2)
        Sa[i] = sa / R / B


    return periods, Sa, SDS, SD1, Fa, Fv


def convert_wind_speed_to_chinese(wind_speed, input_unit, input_height, input_time, return_period):
    """
    将ASCE7风速转换为中国GB50009基本风压
    
    参数:
        wind_speed: 输入风速值
        input_unit: 输入单位 ('mph' 或 'm/s')
        input_height: 测量高度
        input_time: 测量时距 ('3s', '10s', '60s', '10min', '1h')
        return_period: 重现期 ('300y', '700y', '1700y', '3000y')
    
    返回:
        dict: 包含转换结果和详细过程的字典
    """
    process = []
    
    # 1. 单位转换：mph -> m/s
    if input_unit == 'mph':
        wind_speed_ms = wind_speed * 0.44704
        process.append(f"单位转换: {wind_speed:.2f} mph = {wind_speed_ms:.2f} m/s")
    else:
        wind_speed_ms = wind_speed
        process.append(f"风速: {wind_speed_ms:.2f} m/s")
    
    # 2. 重现期转换系数
    return_period_factors = {
        '300y': 1.179,
        '700y': 1.264,
        '1700y': 1.352,
        '3000y': 1.409
    }
    rp_factor = return_period_factors.get(return_period, 1.26)
    v_50 = wind_speed_ms / rp_factor
    process.append(f"重现期转换 ({return_period} -> 50y): {wind_speed_ms:.2f} / {rp_factor:.2f} = {v_50:.2f} m/s")
    
    # 3. 时距转换系数（基于Durst曲线）
    time_factors = {
        '3s': 1.52,
        '10s': 1.43,
        '60s': 1.27,
        '10min': 1.06,
        '1h': 1.00
    }
    # 计算到1小时平均风速
    v_1h = v_50 / time_factors.get(input_time, 1.52)
    # 转换到10分钟平均风速
    v_10min = v_1h * time_factors['10min']
    process.append(f"时距转换 ({input_time} -> 10min): {v_50:.2f} / {time_factors.get(input_time, 1.52):.2f} * 1.06 = {v_10min:.2f} m/s")
    
    # 4. 高度转换到10m（使用幂律公式，假设Exposure C/B类地貌）
    # 幂律指数：B类约0.16，C类约0.14
    alpha = 0.15  # 取中间值
    if input_height != 10:
        v_10m = v_10min * (10 / input_height) ** alpha
        process.append(f"高度转换 ({input_height}m -> 10m): {v_10min:.2f} * (10/{input_height})^{alpha:.2f} = {v_10m:.2f} m/s")
    else:
        v_10m = v_10min
        process.append(f"高度已是10m，无需转换: {v_10m:.2f} m/s")
    
    # 5. 计算基本风压（中国规范）
    # w0 = 0.5 * rho * v^2
    rho = 1.25  # 空气密度 kg/m³
    w0 = 0.5 * rho * v_10m ** 2 / 1000  # 转换为kN/m²
    process.append(f"基本风压计算: w0 = 0.5 * {rho} * {v_10m:.2f}² / 1000 = {w0:.3f} kN/m²")
    
    return {
        'wind_speed_50y_10m_10min': v_10m,
        'basic_wind_pressure': w0,
        'process': process
    }


# ==========================================
# 2. 自定义Matplotlib控件
# ==========================================
class MplCanvas(FigureCanvasQTAgg):
    def __init__(self, parent=None, width=5, height=4, dpi=100):
        # 使用更美观的样式
        plt.style.use('bmh') # 使用 bmh 样式，比默认好看
        self.fig = Figure(figsize=(width, height), dpi=dpi)
        self.ax = self.fig.add_subplot(111)
        # 调整背景色与Fluent UI融合
        self.fig.patch.set_facecolor('#f9f9f9') 
        self.ax.set_facecolor('#ffffff')
        super().__init__(self.fig)


# ==========================================
# 3. 主窗口界面 (Fluent Design)
# ==========================================
class SpectrumInterface(QWidget):
    def __init__(self):
        super().__init__()
        # 【关键修复】设置对象名称，addSubInterface 需要它
        self.setObjectName("SpectrumInterface")
        
        # 主布局：左侧设置，右侧图表
        self.h_layout = QHBoxLayout(self)
        
        # --- 左侧：设置面板 (使用滚动区域) ---
        self.scroll_area = ScrollArea()
        self.setting_widget = QWidget()
        self.setting_layout = QVBoxLayout(self.setting_widget)
        
        self.init_common_settings()
        self.setting_layout.addSpacing(10)
        self.init_china_settings()
        self.setting_layout.addSpacing(10)
        self.init_us_settings()
        self.setting_layout.addStretch(1) # 底部填充
        
        self.scroll_area.setWidget(self.setting_widget)
        self.scroll_area.setWidgetResizable(True)
        self.scroll_area.setFixedWidth(600) # 固定左侧宽度
        
        # --- 右侧：图表区域 ---
        self.canvas_widget = QWidget()
        self.canvas_layout = QVBoxLayout(self.canvas_widget)
        self.canvas = MplCanvas(self, width=8, height=6, dpi=100)
        self.canvas_layout.addWidget(self.canvas)
        
        # 添加布局
        self.h_layout.addWidget(self.scroll_area)
        self.h_layout.addWidget(self.canvas_widget)
        
        # 初始化图表
        self.update_chart()

    def add_section_title(self, text):
        label = SubtitleLabel(text, self)
        self.setting_layout.addWidget(label)
        
    def add_card(self, widget_layout):
        card = CardWidget(self)
        card.setLayout(widget_layout)
        self.setting_layout.addWidget(card)

    def init_common_settings(self):
        self.add_section_title("结构参数")
        
        card = CardWidget(self)
        layout = QHBoxLayout(card)
        layout.setContentsMargins(16, 16, 16, 16)
        
        layout.addWidget(BodyLabel("结构阻尼比 Damping:"))
        self.damp_spin = DoubleSpinBox()
        self.damp_spin.setRange(0.01, 0.99)
        self.damp_spin.setSingleStep(0.01)
        self.damp_spin.setValue(0.05)
        self.damp_spin.valueChanged.connect(self.update_chart)
        layout.addWidget(self.damp_spin)
        
        self.setting_layout.addWidget(card)

    def init_china_settings(self):
        self.add_section_title("中国规范 (GB50011-2010)")
        
        card = CardWidget(self)
        v_layout = QVBoxLayout(card)
        v_layout.setContentsMargins(16, 16, 16, 16)
        v_layout.setSpacing(15)
        
        # 1. 烈度
        row1 = QHBoxLayout()
        row1.addWidget(BodyLabel("设防烈度:"))
        self.china_intensity_cb = ComboBox()
        self.china_intensity_cb.addItems(["6度(0.05g)", "7度(0.10g)", "7度(0.15g)", "8度(0.20g)", "8度(0.30g)", "9度(0.40g)"])
        self.china_intensity_cb.setCurrentText("7度(0.10g)")
        self.china_intensity_cb.currentTextChanged.connect(self.update_chart)
        row1.addWidget(self.china_intensity_cb)
        v_layout.addLayout(row1)
        
        # 2. 场地类别 & 分组
        row2 = QHBoxLayout()
        row2.addWidget(BodyLabel("场地类别:"))
        self.china_site_cb = ComboBox()
        self.china_site_cb.addItems(["I0", "I1", "II", "III", "IV"])
        self.china_site_cb.setCurrentText("II")
        self.china_site_cb.currentTextChanged.connect(self.update_chart)
        row2.addWidget(self.china_site_cb)
        v_layout.addLayout(row2)
        
        row3 = QHBoxLayout()
        row3.addWidget(BodyLabel("地震分组:"))
        self.china_group_cb = ComboBox()
        self.china_group_cb.addItems(["第一组", "第二组", "第三组"])
        self.china_group_cb.currentTextChanged.connect(self.update_chart)
        row3.addWidget(self.china_group_cb)
        v_layout.addLayout(row3)
        
        # 分割线
        line = QFrame()
        line.setFrameShape(QFrame.Shape.HLine)
        line.setStyleSheet("color: #ccc;")
        v_layout.addWidget(line)
        
        # 3. 输出结果显示
        res_layout = QHBoxLayout()
        self.lbl_alpha = StrongBodyLabel("Alpha Max: 0.08")
        self.lbl_tg = StrongBodyLabel("Tg: 0.35s")
        res_layout.addWidget(self.lbl_alpha)
        res_layout.addStretch()
        res_layout.addWidget(self.lbl_tg)
        v_layout.addLayout(res_layout)
        
        self.setting_layout.addWidget(card)

    def init_us_settings(self):
        self.add_section_title("美国规范 (ASCE 7-16)")
        
        card = CardWidget(self)
        v_layout = QVBoxLayout(card)
        v_layout.setContentsMargins(16, 16, 16, 16)
        v_layout.setSpacing(15)
        
        # Ss, S1
        r1 = QHBoxLayout()
        r1.addWidget(BodyLabel("Ss (g):"))
        self.us_ss_spin = DoubleSpinBox()
        self.us_ss_spin.setValue(0.51)
        self.us_ss_spin.valueChanged.connect(self.update_chart)
        r1.addWidget(self.us_ss_spin)
        
        r1.addWidget(BodyLabel("S1 (g):"))
        self.us_s1_spin = DoubleSpinBox()
        self.us_s1_spin.setValue(0.18)
        self.us_s1_spin.valueChanged.connect(self.update_chart)
        r1.addWidget(self.us_s1_spin)
        v_layout.addLayout(r1)
        
        # Site Class
        r2 = QHBoxLayout()
        r2.addWidget(BodyLabel("Site Class:"))
        self.us_site_cb = ComboBox()
        self.us_site_cb.addItems(['A', 'B', 'C', 'D'])
        self.us_site_cb.setCurrentText('D')
        self.us_site_cb.currentTextChanged.connect(self.update_chart)
        r2.addWidget(self.us_site_cb)
        v_layout.addLayout(r2)
        
        # TL, R
        r3 = QHBoxLayout()
        r3.addWidget(BodyLabel("TL (s):"))
        self.us_tl_spin = DoubleSpinBox()
        self.us_tl_spin.setValue(24.0)
        self.us_tl_spin.valueChanged.connect(self.update_chart)
        r3.addWidget(self.us_tl_spin)
        
        r3.addWidget(BodyLabel("R:"))
        self.us_r_spin = DoubleSpinBox()
        self.us_r_spin.setValue(5.0)
        self.us_r_spin.valueChanged.connect(self.update_chart)
        r3.addWidget(self.us_r_spin)
        v_layout.addLayout(r3)
        
        line = QFrame()
        line.setFrameShape(QFrame.Shape.HLine)
        v_layout.addWidget(line)
        
        # 输出
        self.lbl_us_res1 = BodyLabel("Fa: - | Fv: -")
        self.lbl_us_res2 = StrongBodyLabel("SDS: - | SD1: -")
        v_layout.addWidget(self.lbl_us_res1)
        v_layout.addWidget(self.lbl_us_res2)
        
        self.setting_layout.addWidget(card)

    def update_chart(self):
        try:
            # 1. 获取所有参数
            damp = self.damp_spin.value()
            
            # China
            intensity = self.china_intensity_cb.currentText()
            site_cat = self.china_site_cb.currentText()
            group = self.china_group_cb.currentText()
            
            # US
            ss = self.us_ss_spin.value()
            s1 = self.us_s1_spin.value()
            tl = self.us_tl_spin.value()
            r = self.us_r_spin.value()
            us_site = self.us_site_cb.currentText()
            
            # 2. 计算中国
            alpha_max = get_alpha_max(intensity)
            tg = get_Tg(site_cat, group)
            c_periods, c_alpha = calculate_chinese_spectrum(alpha_max, tg, damp)
            
            # 更新中国标签
            self.lbl_alpha.setText(f"Alpha Max: {alpha_max:.2f}")
            self.lbl_tg.setText(f"Tg: {tg:.2f}s")
            
            # 3. 计算美国
            us_periods, us_sa, sds, sd1, fa, fv = calculate_us_spectrum(ss, s1, us_site, tl, r, damp)
            
            # 更新美国标签
            self.lbl_us_res1.setText(f"Fa: {fa:.2f}   Fv: {fv:.2f}")
            self.lbl_us_res2.setText(f"SDS: {sds:.3f}g   SD1: {sd1:.3f}g")
            
            # 4. 绘图
            self.canvas.ax.clear()
            self.canvas.ax.plot(c_periods, c_alpha, label='China GB50011-2010', linewidth=2, color='#009faa')
            self.canvas.ax.plot(us_periods, us_sa, label=f'US ASCE7-16 (R={r})', linewidth=2, color='#ff6b00')
            
            self.canvas.ax.set_title("Response Spectrum Comparison", fontsize=12)
            self.canvas.ax.set_xlabel("Period T (s)")
            self.canvas.ax.set_ylabel("Spectral Acceleration (g)")
            self.canvas.ax.legend()
            self.canvas.ax.grid(True, linestyle='--', alpha=0.6)
            self.canvas.ax.set_xlim(0, 6)
            
            max_val = max(np.max(c_alpha), np.max(us_sa))
            self.canvas.ax.set_ylim(0, max_val * 1.1)
            
            self.canvas.draw()
            
        except Exception as e:
            print(f"Calculation Error: {e}")


class WindConversionInterface(QWidget):
    def __init__(self):
        super().__init__()
        self.setObjectName("WindConversionInterface")
        
        # 主布局
        self.v_layout = QVBoxLayout(self)
        
        # 创建滚动区域
        self.scroll_area = ScrollArea()
        self.setting_widget = QWidget()
        self.setting_layout = QVBoxLayout(self.setting_widget)
        
        self.init_wind_conversion_settings()
        self.setting_layout.addStretch(1)
        
        self.scroll_area.setWidget(self.setting_widget)
        self.scroll_area.setWidgetResizable(True)
        self.v_layout.addWidget(self.scroll_area)
    
    def add_section_title(self, text):
        label = SubtitleLabel(text, self)
        self.setting_layout.addWidget(label)
    
    def init_wind_conversion_settings(self):
        self.add_section_title("风速转换 (ASCE7 -> GB50009)")
        
        card = CardWidget(self)
        v_layout = QVBoxLayout(card)
        v_layout.setContentsMargins(16, 16, 16, 16)
        v_layout.setSpacing(15)
        
        # 风速值和单位
        r1 = QHBoxLayout()
        r1.addWidget(BodyLabel("风速:"))
        self.wind_speed_spin = DoubleSpinBox()
        self.wind_speed_spin.setRange(0, 500)
        self.wind_speed_spin.setValue(115)
        self.wind_speed_spin.setSingleStep(1)
        r1.addWidget(self.wind_speed_spin)
        
        self.wind_unit_cb = ComboBox()
        self.wind_unit_cb.addItems(['mph', 'm/s'])
        self.wind_unit_cb.setCurrentText('mph')
        self.wind_unit_cb.setFixedWidth(80)
        r1.addWidget(self.wind_unit_cb)
        v_layout.addLayout(r1)
        
        # 测量高度
        r2 = QHBoxLayout()
        r2.addWidget(BodyLabel("测量高度 (m):"))
        self.wind_height_spin = DoubleSpinBox()
        self.wind_height_spin.setRange(1, 500)
        self.wind_height_spin.setValue(10)
        self.wind_height_spin.setSingleStep(1)
        r2.addWidget(self.wind_height_spin)
        v_layout.addLayout(r2)
        
        # 测量时距
        r3 = QHBoxLayout()
        r3.addWidget(BodyLabel("测量时距:"))
        self.wind_time_cb = ComboBox()
        self.wind_time_cb.addItems(['3s', '10s', '60s', '10min', '1h'])
        self.wind_time_cb.setCurrentText('3s')
        r3.addWidget(self.wind_time_cb)
        v_layout.addLayout(r3)
        
        # 重现期
        r4 = QHBoxLayout()
        r4.addWidget(BodyLabel("重现期:"))
        self.wind_rp_cb = ComboBox()
        self.wind_rp_cb.addItems(['300y', '700y', '1700y', '3000y'])
        self.wind_rp_cb.setCurrentText('700y')
        r4.addWidget(self.wind_rp_cb)
        v_layout.addLayout(r4)
        
        # 转换按钮
        self.convert_btn = PrimaryPushButton("执行转换")
        self.convert_btn.clicked.connect(self.convert_wind_speed)
        v_layout.addWidget(self.convert_btn)
        
        line = QFrame()
        line.setFrameShape(QFrame.Shape.HLine)
        line.setStyleSheet("color: #ccc;")
        v_layout.addWidget(line)
        
        # 输出结果
        self.lbl_wind_result1 = StrongBodyLabel("转换结果")
        v_layout.addWidget(self.lbl_wind_result1)
        
        self.lbl_wind_result2 = BodyLabel("50年重现期, 10m高度, 10分钟平均风速: - m/s")
        v_layout.addWidget(self.lbl_wind_result2)
        
        self.lbl_wind_result3 = StrongBodyLabel("基本风压: - kN/m²")
        v_layout.addWidget(self.lbl_wind_result3)
        
        # 转换过程
        v_layout.addWidget(BodyLabel("转换过程:"))
        self.wind_process_text = TextEdit()
        self.wind_process_text.setReadOnly(True)
        self.wind_process_text.setFixedHeight(150)
        v_layout.addWidget(self.wind_process_text)
        
        self.setting_layout.addWidget(card)
    
    def convert_wind_speed(self):
        try:
            # 获取输入参数
            wind_speed = self.wind_speed_spin.value()
            input_unit = self.wind_unit_cb.currentText()
            input_height = self.wind_height_spin.value()
            input_time = self.wind_time_cb.currentText()
            return_period = self.wind_rp_cb.currentText()
            
            # 执行转换
            result = convert_wind_speed_to_chinese(wind_speed, input_unit, input_height, input_time, return_period)
            
            # 更新结果显示
            self.lbl_wind_result2.setText(f"50年重现期, 10m高度, 10分钟平均风速: {result['wind_speed_50y_10m_10min']:.2f} m/s")
            self.lbl_wind_result3.setText(f"基本风压: {result['basic_wind_pressure']:.3f} kN/m²")
            
            # 显示转换过程
            process_text = "\n".join(result['process'])
            self.wind_process_text.setPlainText(process_text)
            
        except Exception as e:
            self.wind_process_text.setPlainText(f"转换错误: {str(e)}")


class MainWindow(FluentWindow):
    def __init__(self):
        super().__init__()
        self.setWindowTitle("中美规范主要参数转换")
        self.resize(1400, 750)
        
        # 创建反应谱比较界面
        self.spectrum_interface = SpectrumInterface()
        
        # 创建风速转换界面
        self.wind_interface = WindConversionInterface()
        
        # 将界面添加到主窗口
        self.addSubInterface(self.spectrum_interface, FIF.HOME, "反应谱比较")
        self.addSubInterface(self.wind_interface, FIF.SYNC, "风速转换")
        
        # 默认居中
        screen = QApplication.primaryScreen().geometry()
        x = (screen.width() - self.width()) // 2
        y = (screen.height() - self.height()) // 2
        self.move(x, y)

if __name__ == '__main__':
    # 启用高DPI缩放
    QApplication.setHighDpiScaleFactorRoundingPolicy(Qt.HighDpiScaleFactorRoundingPolicy.PassThrough)
    
    app = QApplication(sys.argv)
    
    # 设置主题 (Light / Dark)
    setTheme(Theme.LIGHT)
    
    w = MainWindow()
    w.show()
    
    sys.exit(app.exec())