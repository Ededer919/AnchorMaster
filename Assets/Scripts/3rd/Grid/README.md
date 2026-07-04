# Grid2D — 2D 网格插件

> 适用于 Unity 2D 项目的轻量网格系统，支持网格创建、坐标转换、单元格高亮、拖拽交互、物体吸附，开箱即用。

## 文件清单

```
Assets/Scripts/
├── 3rd/Grid/
│   ├── GridSystem.cs         ← 核心网格系统（创建/管理/查询/交换/占位检查）
│   ├── GridCell.cs           ← 单元格基类（行列/高亮/选中/点击/占用状态）
│   ├── GridDragHandler.cs    ← 拖拽处理器（拖动/吸附/连线反馈/事件回调）
│   └── README.md             ← 本文档
│
└── GameLogic/
    ├── GridObject.cs         ← 网格物体（吸附/放置/移除/交换/高亮/事件）
    └── GridObjectManager.cs  ← 物体管理器（注册/查询/最近空格/相邻检测）
```

## 功能总览

| 功能 | 所属组件 | 说明 |
|------|---------|------|
| 网格创建 | GridSystem | 传入行列数和 Prefab，自动实例化所有单元格 |
| 坐标转换 | GridSystem | 网格坐标 ↔ 世界坐标互转 |
| 单元格查询 | GridSystem | 按行列号、按世界位置获取单元格 |
| 单元格交换 | GridSystem | 交换两个单元格的位置和数据 |
| 占位检查 | GridSystem | `IsCellOccupied` / `CanPlaceAt` / `GetEmptyCells` |
| 批量高亮 | GridSystem | 一键高亮/取消所有单元格 |
| 高亮/选中 | GridCell | 单个单元格的视觉状态切换 |
| 占用状态 | GridCell | `OccupiedObject` / `IsEmpty` |
| 点击回调 | GridCell | 虚方法 `OnCellClicked()` 供子类重写 |
| 拖拽吸附 | GridDragHandler | 拖动时自动吸附到最近单元格 |
| 连线反馈 | GridDragHandler | 拖动时显示起点到当前位置的连线，距离内变绿/外变红 |
| 事件回调 | GridDragHandler | 拖拽开始/选中/放下/结束四个事件 |
| 物体吸附 | GridObject | 平滑 Lerp 吸附到网格单元格 |
| 占用管理 | GridObject | 一个格子只能放一个物体 |
| 物体交换 | GridObject | 移动到已占用格子时自动交换 |
| 物体注册 | GridObjectManager | 统一注册/查询所有网格物体 |
| 最近空格 | GridObjectManager | 查找距离世界坐标最近的空闲格子 |
| 相邻检测 | GridObjectManager | 4 方向 / 8 方向相邻物体查询 |

---

## 组件说明

### GridSystem（核心网格系统）

管理整个网格的创建、查询、坐标转换。挂载到场景中的任意 GameObject 上。

**职责：**
- `InitializeGrid(rows, cols, prefab, parent)` — 创建网格
- `GridToWorldPosition(row, col)` — 网格坐标 → 世界坐标
- `WorldToGridPosition(worldPos)` — 世界坐标 → 网格坐标
- `GetCell(row, col)` — 按行列号获取单元格
- `GetNearestCell(worldPos)` — 按世界坐标获取最近单元格
- `GetAllCells()` — 获取所有单元格列表
- `SwapCells(r1, c1, r2, c2)` — 交换两个单元格
- `HighlightValidCells(bool)` — 批量高亮/取消
- `IsCellOccupied(row, col)` — 检查格子是否被占用
- `CanPlaceAt(row, col)` — 检查是否可放置（范围内 + 空闲）
- `GetEmptyCells()` — 获取所有空闲格子

> 挂载路径：`Add Component → Grid System`

### GridCell（单元格基类）

每个单元格是一个独立 GameObject，挂载此脚本。可作为基类继承扩展。

**职责：**
- 记录自身行列号（`Row`, `Col`）
- 高亮状态切换（`SetHighlighted(bool)`）
- 选中状态（`IsSelected`）
- 占用状态（`OccupiedObject` / `IsEmpty`）
- 点击回调（`OnCellClicked()` 虚方法）
- 可选的两个 SpriteRenderer：`backgroundRenderer`（背景）+ `highlightRenderer`（高亮层）

**默认颜色：**
| 状态 | 颜色 | RGBA |
|------|------|------|
| 普通 | 深灰半透明 | (0.2, 0.2, 0.3, 0.3) |
| 高亮 | 紫色半透明 | (0.376, 0.329, 0.945, 0.5) |

### GridDragHandler（拖拽处理器）

处理单元格的拖拽交互，支持吸附、视觉反馈和事件回调。

**事件：**
| 事件 | 触发时机 | 参数 |
|------|---------|------|
| `OnDragStarted` | 拖拽开始 | `GridCell` |
| `OnCellSelected` | 拖拽中选中单元格变化 | `GridCell` |
| `OnCellDropped` | 拖拽放下 | `GridCell`（目标） |
| `OnDragStopped` | 拖拽结束 | 无 |

### GridObject（网格物体）

挂载到需要吸附到网格的物体上。支持平滑吸附、占用管理、交换、事件回调。

**职责：**
- `PlaceOnGrid(grid, row, col)` — 吸附到指定格子（占用检查，失败返回 false）
- `PlaceAtNearest(grid, worldPos)` — 吸附到最近空格
- `RemoveFromGrid()` — 从当前格子移除
- `MoveTo(grid, row, col)` — 移动到目标格（空格直接放/占用格则交换）
- `SetHighlight(bool)` — 高亮/取消高亮
- 平滑 Lerp 吸附动画（`snapSpeed` 控制速度）
- 拖拽中禁用吸附动画，松手后自动吸附

**属性：**
| 属性 | 类型 | 说明 |
|------|------|------|
| `CurrentCell` | GridCell | 当前所在格子 |
| `IsPlaced` | bool | 是否已吸附到网格 |
| `Row` / `Col` | int | 当前行列号（未吸附返回 -1） |
| `IsDragging` | bool | 是否正在拖拽 |

**事件：**
| 事件 | 触发时机 | 参数 |
|------|---------|------|
| `OnPlaced` | 放置成功 | `GridCell` |
| `OnRemoved` | 从格子移除 | `GridCell`（旧格子） |
| `OnMoved` | 从 A 格移到 B 格 | `GridCell, GridCell`（旧, 新） |

### GridObjectManager（物体管理器）

统一管理场景中所有 GridObject，提供查询、最近空格查找、相邻检测。

**职责：**
- `Register(obj)` / `Unregister(obj)` — 注册/注销物体
- `GetObjectAt(row, col)` — 获取指定格子上的物体
- `FindNearestEmptyCell(worldPos)` — 查找最近空闲格子，返回 `Vector2Int(col, row)`
- `GetAdjacentObjects(obj)` — 4 方向相邻物体
- `GetAdjacentObjects8(obj)` — 8 方向相邻物体
- `ClearAll()` — 移除所有物体

> 挂载路径：`Add Component → Grid Object Manager`

---

## 参数详解

### GridSystem

| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `Cell Size` | float | 1.0 | 单元格尺寸（世界单位） |
| `Grid Origin` | Vector2 | (0, 0) | 网格左下角起始坐标 |

### GridCell

| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `Background Renderer` | SpriteRenderer | null | 背景渲染器（普通状态显示） |
| `Highlight Renderer` | SpriteRenderer | null | 高亮渲染器（高亮时显示） |
| `OccupiedObject` | GridObject | null | 当前占用的物体（运行时设置） |
| `IsEmpty` | bool | true | 是否空闲（只读） |

### GridDragHandler

| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `Snap Distance` | float | 0.5 | 吸附距离，拖拽到此距离内连线变绿 |
| `Drag Speed` | float | 10 | 拖拽速度（预留） |
| `Drag Indicator` | GameObject | null | 拖拽指示器（跟随鼠标） |
| `Connection Line` | LineRenderer | null | 起点到当前位置的连线 |
| `Valid Color` | Color | green | 距离内连线颜色 |
| `Invalid Color` | Color | red | 距离外连线颜色 |

### GridObject

| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `Object ID` | string | "" | 物体唯一标识 |
| `Display Name` | string | "" | 显示名称 |
| `Snap Speed` | float 1~50 | 15 | 吸附动画速度（Lerp 系数） |
| `Snap On Start` | bool | false | 是否在 Start 时自动吸附到最近格子 |
| `Offset` | Vector3 | (0,0,0) | 吸附后相对格子中心的偏移 |
| `Highlight Renderer` | SpriteRenderer | null | 高亮渲染器（可选） |

### GridObjectManager

| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `Grid System` | GridSystem | null | 关联的网格系统引用 |

---

## 坐标系说明

```
世界坐标                        网格坐标 (row, col)
                                  
  Y↑                              row 2:  (2,0) (2,1) (2,2)
  │                               row 1:  (1,0) (1,1) (1,2)
  │                               row 0:  (0,0) (0,1) (0,2)
  └──────────→ X                   col:     0     1     2
```

- `GridToWorldPosition(row, col)` → `(gridOrigin.x + col × cellSize, gridOrigin.y + row × cellSize)`
- `WorldToGridPosition(worldPos)` → 返回 `Vector2Int(col, row)`（注意：x=col, y=row）
- `GetCell(row, col)` 参数顺序为 `(row, col)`
- `FindNearestEmptyCell` 返回 `Vector2Int(col, row)`（x=col, y=row）

> **注意：** `WorldToGridPosition` 和 `FindNearestEmptyCell` 返回的 `Vector2Int` 中 `x = col, y = row`，而 `GetCell` 接收 `(row, col)`。调用时需用 `GetCell(pos.y, pos.x)`。

---

## 使用示例

### 完整：创建网格 + 放置物体

```csharp
public class GridExample : MonoBehaviour
{
    public GridSystem gridSystem;
    public GameObject cellPrefab;
    public GameObject objectPrefab;
    public Transform parent;

    private void Start()
    {
        // 1. 创建 5×5 网格
        gridSystem.InitializeGrid(5, 5, cellPrefab, parent);

        // 2. 创建管理器
        var managerObj = new GameObject("Manager");
        var manager = managerObj.AddComponent<GridObjectManager>();
        manager.gridSystem = gridSystem;

        // 3. 创建物体并放置到 (2, 3)
        GameObject obj = Instantiate(objectPrefab, parent);
        var gridObj = obj.AddComponent<GridObject>();
        gridObj.objectID = "item_1";
        gridObj.displayName = "道具1";
        manager.Register(gridObj);

        gridObj.PlaceOnGrid(gridSystem, 2, 3);
    }
}
```

### 物体操作

```csharp
// 放置到指定格子（失败返回 false — 格子被占用）
gridObj.PlaceOnGrid(gridSystem, 1, 2);

// 放置到最近空格
gridObj.PlaceAtNearest(gridSystem, transform.position);

// 从网格移除
gridObj.RemoveFromGrid();

// 移动到目标格（如果被占用则自动交换）
gridObj.MoveTo(gridSystem, 3, 4);
```

### 查询

```csharp
// 格子是否被占用
bool occupied = gridSystem.IsCellOccupied(1, 2);

// 是否可放置
bool canPlace = gridSystem.CanPlaceAt(1, 2);

// 获取所有空格
List<GridCell> empty = gridSystem.GetEmptyCells();

// 获取指定格子上的物体
GridObject obj = manager.GetObjectAt(1, 2);

// 查找最近空格
Vector2Int pos = manager.FindNearestEmptyCell(worldPos);
if (pos.x >= 0)
    gridObj.PlaceOnGrid(gridSystem, pos.y, pos.x);
```

### 相邻检测

```csharp
// 4 方向相邻物体
List<GridObject> adjacent4 = manager.GetAdjacentObjects(gridObj);

// 8 方向相邻物体
List<GridObject> adjacent8 = manager.GetAdjacentObjects8(gridObj);
```

### 拖拽 + 吸附

```csharp
// 拖拽开始时
gridObj.IsDragging = true;
gridObj.RemoveFromGrid();  // 拖拽中暂时脱离网格

// 拖拽中跟随鼠标
transform.position = mouseWorldPos;

// 松手时吸附到最近空格
Vector2Int cell = manager.FindNearestEmptyCell(transform.position);
if (cell.x >= 0)
{
    gridObj.PlaceOnGrid(gridSystem, cell.y, cell.x);
    gridObj.IsDragging = false;
}
```

### 事件订阅

```csharp
gridObj.OnPlaced += (cell) => Debug.Log($"放到 ({cell.Row}, {cell.Col})");
gridObj.OnRemoved += (cell) => Debug.Log($"从 ({cell.Row}, {cell.Col}) 移除");
gridObj.OnMoved += (from, to) => Debug.Log($"从 ({from.Row},{from.Col}) 移到 ({to.Row},{to.Col})");
```

### 继承 GridCell 扩展

```csharp
public class PuzzleTile : GridCell
{
    public int tileId;

    public override void Initialize(int row, int col)
    {
        base.Initialize(row, col);
    }

    public override void OnCellClicked()
    {
        Debug.Log($"Tile {tileId} at ({Row}, {Col}), Empty={IsEmpty}");
        SetHighlighted(!IsHighlighted);
    }
}
```

---

## 常见问题

### Q: PlaceOnGrid 返回 false？

检查：
1. 格子是否在网格范围内
2. 格子是否已被其他物体占用（`cell.IsEmpty`）
3. 传入的 grid 参数是否为 null

### Q: FindNearestEmptyCell 返回 (-1, -1)？

网格已满，没有空格。检查 `gridSystem.GetEmptyCells().Count`。

### Q: 吸附动画不流畅？

调大 `Snap Speed`（如 25~30），值越大吸附越快。拖拽中动画自动暂停。

### Q: MoveTo 到已占用格子后两个物体位置不对？

MoveTo 会自动交换双方位置：当前物体移到目标格，原占用物体移到当前物体的旧位置。双方都会触发 `OnMoved` 事件。

### Q: 物体销毁后格子还显示被占用？

GridObject 在 `OnDestroy` 中会自动清理格子占用。如果手动 Destroy GameObject，占用会自动释放。

### Q: WorldToGridPosition 返回的坐标和 GetCell 参数对不上？

`WorldToGridPosition` 返回 `Vector2Int(x=col, y=row)`，而 `GetCell` 接收 `(row, col)`。调用时用 `GetCell(pos.y, pos.x)`。

### Q: 如何在运行时动态修改网格？

- 调用 `InitializeGrid` 会先销毁旧单元格再创建新的
- 使用 `SetCell(row, col, newCell)` 替换单个单元格
- 使用 `SwapCells(r1, c1, r2, c2)` 交换两个单元格位置
