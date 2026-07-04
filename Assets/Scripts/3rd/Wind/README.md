# Wind2D — 2D 风模拟插件

> 适用于 Unity 2D 项目的轻量风场系统，支持全局风、自然风、定点风、面风四种模式，开箱即用。

## 文件清单

```
Assets/Scripts/3rd/Wind/
├── IWindSource2D.cs      ← 风源接口（自定义风源实现此接口即可接入）
├── WindSystem2D.cs       ← 核心系统（单例，管理全局风/自然风/所有风源）
├── PointWind2D.cs        ← 定点风源（风扇、爆炸、排气口）
├── AreaWind2D.cs         ← 面风/风墙（矩形区域内定时持续风）
├── WindReceiver2D.cs     ← 风力接收器（挂到 Rigidbody2D 物体上）
└── README.md             ← 本文档
```

## 风类型总览

| 类型 | 组件 | 形状 | 持续性 | 典型场景 |
|------|------|------|--------|---------|
| 全局风 | WindSystem2D 内置 | 全场景 | 持续 | 背景风、关卡风向 |
| 自然风 | WindSystem2D 内置 | 全场景 | 持续 | 环境氛围、随机扰动 |
| 定点风 | PointWind2D | 圆形 | 持续 | 风扇、排气口、爆炸 |
| 面风 | AreaWind2D | 矩形 | 定时/循环 | 风墙、阵风、关卡机制 |

> 四种风可同时开启，在 `GetWindForceAt(position)` 中自动叠加。

## 快速开始

只需 2 步：

1. **受风物体**（需有 `Rigidbody2D`）上挂载 `WindReceiver2D` 组件
2. **（可选）** 挂载 `PointWind2D`（点风）或 `AreaWind2D`（面风/风墙）

> **无需手动创建 WindSystem2D** — 如果场景中没有，系统会在运行时自动创建。

运行游戏，物体即受风力影响。

---

## 组件说明

### WindSystem2D（核心系统）

场景中只需一个实例，作为单例管理所有风源。

**职责：**
- 维护全局风（恒定方向 + 强度）
- 维护自然风（Perlin 噪声驱动的随机变化风）
- 自动收集场景中所有 `IWindSource2D` 风源（PointWind2D、AreaWind2D 等）
- 提供 `GetWindForceAt(Vector2 position)` 查询接口

> 挂载路径：`Add Component → Wind2D → Wind System 2D`

### PointWind2D（定点风源）

从一个点向外产生风力，适合风扇、排气口、爆炸等场景。

**职责：**
- 支持两种模式：`Radial`（径向辐射） / `Directional`（定向吹）
- 距离衰减：中心最强，边缘为零（Smoothstep）
- 自然扰动：Perlin 噪声叠加不规则 gust
- 自动注册/注销到 `WindSystem2D`

> 挂载路径：`Add Component → Wind2D → Point Wind 2D`

### AreaWind2D（面风 / 风墙）

在一个矩形区域内产生均匀风力，像一堵风墙。支持定时持续、强度区间变化、淡入淡出。

**职责：**
- 矩形区域（width × height），跟随 GameObject 位置和旋转
- 指定方向吹风，区域内风力均匀（可选边缘平滑衰减）
- 强度在 `[minStrength, maxStrength]` 之间通过 Perlin 噪声变化
- 定时持续 `duration` 秒，到期自动关闭（或循环）
- 淡入 `fadeInTime` / 淡出 `fadeOutTime`，平滑过渡
- 自动注册/注销到 `WindSystem2D`

> 挂载路径：`Add Component → Wind2D → Area Wind 2D`

### WindReceiver2D（风力接收器）

挂载到需要受风的物体上（自动要求 `Rigidbody2D`）。

**职责：**
- 每帧 `FixedUpdate` 中查询当前位置的风力并施加到刚体
- 通过 `windSensitivity` 控制该物体对风的敏感程度
- 支持选择 `ForceMode2D.Force`（持续力）或 `ForceMode2D.Impulse`（瞬时冲量）

> 挂载路径：`Add Component → Wind2D → Wind Receiver 2D`

### IWindSource2D（风源接口）

所有风源组件实现此接口。若需自定义风源类型，实现 `GetForceAt(Vector2, float)` 并在 `OnEnable` 调用 `WindSystem2D.EnsureExists().RegisterWindSource(this)` 即可自动接入系统。

---

## 参数详解

### WindSystem2D

| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `Enable Global Wind` | bool | true | 全局风开关 |
| `Global Wind Direction` | Vector2 | (1, 0) | 全局风向，自动归一化 |
| `Global Wind Strength` | float 0~50 | 2 | 全局风强度 |
| `Enable Natural Wind` | bool | true | 自然风开关 |
| `Natural Wind Strength` | float 0~20 | 1 | 自然风强度 |
| `Natural Wind Frequency` | float 0.01~2 | 0.3 | 噪声频率，越大风变化越快 |
| `Natural Wind Seed` | float | 0 | 噪声种子，改变可得到不同风型 |

### PointWind2D

| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `Radius` | float 0.1~50 | 5 | 影响半径 |
| `Strength` | float 0~50 | 8 | 风力强度 |
| `Mode` | enum | Directional | `Radial`（径向）或 `Directional`（定向） |
| `Fixed Direction` | Vector2 | (1, 0) | Directional 模式下的风向，自动归一化 |
| `Natural Variation` | float 0~1 | 0.2 | 噪声扰动幅度，0 = 完全稳定 |
| `Noise Frequency` | float 0.01~2 | 0.5 | 扰动变化频率 |

### AreaWind2D

| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| **Wind Area** | | | |
| `Width` | float 0.1~50 | 10 | 矩形区域宽度 |
| `Height` | float 0.1~50 | 2 | 矩形区域高度 |
| **Wind Direction** | | | |
| `Direction` | Vector2 | (1, 0) | 风向（世界空间），自动归一化 |
| **Wind Strength** | | | |
| `Min Strength` | float 0~50 | 3 | 最小风力强度 |
| `Max Strength` | float 0~50 | 8 | 最大风力强度 |
| `Variation Frequency` | float 0.01~2 | 0.5 | 强度变化频率，越大波动越快 |
| **Duration** | | | |
| `Duration` | float | 5 | 持续时间（秒），到期后自动关闭 |
| `Fade In Time` | float | 0.5 | 淡入时间（秒），从 0 渐变到满力 |
| `Fade Out Time` | float | 0.5 | 淡出时间（秒），从满力渐变到 0 |
| `Loop` | bool | false | 是否循环（到期后重新开始） |
| **Edge Falloff** | | | |
| `Edge Falloff` | float 0~0.5 | 0.1 | 边缘平滑衰减比例，0 = 硬边 |

### WindReceiver2D

| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `Wind Sensitivity` | float 0~5 | 1 | 风力敏感度（叶子=2, 石头=0.2） |
| `Force Mode` | enum | Force | `Force`（持续力）或 `Impulse`（冲量） |

---

## 风力计算原理

在任意位置 `P`，系统返回的合力为：

```
F(P) = F_global + F_natural + Σ F_source_i(P)
```

| 风源类型 | 公式 | 说明 |
|---------|------|------|
| 全局风 | `dir × strength` | 恒定方向和大小 |
| 自然风 | `PerlinNoise(time × freq) × strength` | X/Y 分别采样，方向和大小随时间变化 |
| 定点风 | `dir × strength × smoothstep(1 - dist/radius) × variation` | 距离衰减 |
| 面风 | `dir × lerp(min, max, noise) × edgeFalloff × fade` | 区域内均匀，强度区间变化 |

**面风的时间轴：**

```
|← fade in →|←── full strength ──→|← fade out →|
0          fadeInTime        duration-fadeOut   duration
```

---

## 使用示例

### 场景搭建

```
WindSystem              ← WindSystem2D（可省略，自动创建）
│                         全局风: 方向(1,0), 强度2
│                         自然风: 强度1, 频率0.3
│
├── Fan                 ← PointWind2D (Directional, radius=5, strength=8)
│                         风扇，持续定向吹
│
├── WindWall            ← AreaWind2D (width=12, height=3, dir=(1,0),
│                         minStr=3, maxStr=8, duration=5, fadeIn=0.5, fadeOut=0.5)
│                         风墙，持续5秒，强度3~8间变化，淡入淡出
│
├── Leaf                ← Rigidbody2D + WindReceiver2D (sensitivity=2)
│                         轻物体，受风明显
│
└── Rock                ← Rigidbody2D + WindReceiver2D (sensitivity=0.2)
                          重物体，几乎不受风影响
```

### 代码调用

#### 查询某位置的风力

```csharp
Vector2 windForce = WindSystem2D.Instance.GetWindForceAt(transform.position);
```

#### 便捷 API

```csharp
// 一键设置全局风（自动开启）
WindSystem2D.Instance.SetGlobalWind(new Vector2(0, 1), 5f);

// 一键设置自然风（自动开启）
WindSystem2D.Instance.SetNaturalWind(2f, 0.5f);

// 一键开关
WindSystem2D.Instance.SetWindEnabled(global: true, natural: false);
```

#### 运行时触发面风（风墙）

```csharp
var wall = GetComponent<AreaWind2D>();

// 重新触发（重置计时）
wall.enabled = false;
wall.enabled = true;

// 动态修改参数
wall.minStrength = 5f;
wall.maxStrength = 15f;
wall.duration = 3f;
wall.direction = new Vector2(0, 1); // 改为向上吹
```

#### 运行时控制定点风源

```csharp
PointWind2D fan = GetComponent<PointWind2D>();

// 调整风力
fan.strength = 20f;

// 临时关闭（自动从系统注销）
fan.enabled = false;

// 重新启用（自动注册回系统）
fan.enabled = true;
```

#### 自定义风源（实现 IWindSource2D）

```csharp
using UnityEngine;

public class CustomWindSource : MonoBehaviour, IWindSource2D
{
    private void OnEnable()
    {
        WindSystem2D.EnsureExists().RegisterWindSource(this);
    }

    private void OnDisable()
    {
        WindSystem2D.Instance?.UnregisterWindSource(this);
    }

    public Vector2 GetForceAt(Vector2 position, float time)
    {
        // 自定义风力计算逻辑
        return Vector2.right * 5f;
    }
}
```

### 面风实战：触发一堵持续 3 秒的风墙

**场景：** 玩家踩到机关时，从左向右刮起一堵风墙，持续 3 秒，风力在 5~12 之间波动。

**步骤：**

1. 场景中放置一个空 GameObject，命名 `WindWall`，调整位置和旋转
2. 挂载 `AreaWind2D`，Inspector 中设置：

| 参数 | 值 | 说明 |
|------|-----|------|
| Width | 12 | 风墙宽度 |
| Height | 4 | 风墙高度 |
| Direction | (1, 0) | 向右吹 |
| Min Strength | 5 | 最小风力 |
| Max Strength | 12 | 最大风力 |
| Variation Frequency | 0.5 | 波动频率 |
| Duration | 3 | 持续 3 秒 |
| Fade In Time | 0.3 | 0.3 秒淡入 |
| Fade Out Time | 0.5 | 0.5 秒淡出 |
| Loop | false | 不循环 |

3. 默认 `enabled = true` 会在场景启动时立刻触发。如需由机关触发：

```csharp
public class WindWallTrigger : MonoBehaviour
{
    public AreaWind2D windWall;

    private void Start()
    {
        // 初始关闭，等待触发
        if (windWall != null) windWall.enabled = false;
    }

    // 玩家踩到机关时调用
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && windWall != null)
        {
            // 重新启用 = 重置计时 + 开始吹风
            windWall.enabled = true;
        }
    }
}
```

> 面风到期后自动 `enabled = false`，再次设为 `true` 即可重新触发。

---

## Gizmos 调试

在 Scene 视图中选中挂载组件的物体，可看到可视化辅助：

| 组件 | Gizmo | 颜色 |
|------|-------|------|
| `WindSystem2D` | 全局风方向箭头 + 自然风标记圆圈 | 青色 / 浅绿 |
| `PointWind2D` | 影响范围圆圈 + 风向箭头 | 橙色圆圈 / 黄色箭头 |
| `AreaWind2D` | 矩形区域填充 + 边框 + 风向箭头 | 蓝色半透明 / 黄色箭头 |

---

## 常见问题

### Q: 物体不受风力影响？

检查：
1. 物体上是否有 `Rigidbody2D` 组件
2. `WindReceiver2D` 的 `Wind Sensitivity` 是否大于 0
3. 场景中是否有启用的风源（全局风/自然风/PointWind2D/AreaWind2D）

> WindSystem2D 会在运行时自动创建，无需手动添加。

### Q: 面风没有效果？

检查：
1. 物体是否在矩形区域内（选中 AreaWind2D 物体查看 Gizmo）
2. `Duration` 是否 > 0，组件是否仍在持续时间内
3. `Min Strength` 和 `Max Strength` 是否都大于 0
4. `Direction` 是否非零

### Q: 面风到期后不恢复？

- 若 `Loop = false`，组件到期后会自动 `enabled = false`，重新启用即可再次触发
- 若 `Loop = true`，会自动循环

### Q: 风力太强/太弱？

- 调整 `WindSystem2D` 上的 `Global/Natural Wind Strength`
- 调整 `WindReceiver2D` 的 `Wind Sensitivity`
- 调整风源的 `Strength`（PointWind2D）或 `Min/Max Strength`（AreaWind2D）
- 检查 `Rigidbody2D` 的 `Mass`，质量越大受同等力的加速度越小

### Q: 多个 WindSystem2D 会冲突吗？

系统使用单例模式，第二个实例会在 `Awake` 中自我销毁。整个场景只需一个。

---

## 版本记录

| 版本 | 内容 |
|------|------|
| v1.0 | 初始版本：WindSystem2D + PointWind2D + WindReceiver2D |
| v1.1 | 优化：EnsureExists 自动创建单例、便捷 API、默认参数调优、AddComponentMenu |
| v1.2 | 新增：IWindSource2D 接口、AreaWind2D 面风组件，系统支持任意风源扩展 |
