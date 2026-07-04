# Grid2D — 2D 网格插件

> 适用于 Unity 2D 项目的轻量网格系统，支持网格创建、坐标转换、单元格高亮、拖拽交互，开箱即用。

## 文件清单

```
Assets/Scripts/3rd/Grid/
├── GridSystem.cs         ← 核心网格系统（创建/管理/查询/交换）
├── GridCell.cs           ← 单元格基类（行列/高亮/选中/点击）
├── GridDragHandler.cs    ← 拖拽处理器（拖动/吸附/连线反馈/事件回调）
└── README.md             ← 本文档
```

## 功能总览

| 功能 | 所属组件 | 说明 |
|------|---------|------|
| 网格创建 | GridSystem | 传入行列数和 Prefab，自动实例化所有单元格 |
| 坐标转换 | GridSystem | 网格坐标 ↔ 世界坐标互转 |
| 单元格查询 | GridSystem | 按行列号、按世界位置获取单元格 |
| 单元格交换 | GridSystem | 交换两个单元格的位置和数据 |
| 批量高亮 | GridSystem | 一键高亮/取消所有单元格 |
| 高亮/选中 | GridCell | 单个单元格的视觉状态切换 |
| 点击回调 | GridCell | 虚方法 `OnCellClicked()` 供子类重写 |
| 拖拽吸附 | GridDragHandler | 拖动时自动吸附到最近单元格 |
| 连线反馈 | GridDragHandler | 拖动时显示起点到当前位置的连线，距离内变绿/外变红 |
| 事件回调 | GridDragHandler | 拖拽开始/选中/放下/结束四个事件 |

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

> 挂载路径：`Add Component → Grid System`

### GridCell（单元格基类）

每个单元格是一个独立 GameObject，挂载此脚本。可作为基类继承扩展。

**职责：**
- 记录自身行列号（`Row`, `Col`）
- 高亮状态切换（`SetHighlighted(bool)`）
- 选中状态（`IsSelected`）
- 点击回调（`OnCellClicked()` 虚方法）
- 可选的两个 SpriteRenderer：`backgroundRenderer`（背景）+ `highlightRenderer`（高亮层）

**默认颜色：**
| 状态 | 颜色 | RGBA |
|------|------|------|
| 普通 | 深灰半透明 | (0.2, 0.2, 0.3, 0.3) |
| 高亮 | 紫色半透明 | (0.376, 0.329, 0.945, 0.5) |

### GridDragHandler（拖拽处理器）

处理单元格的拖拽交互，支持吸附、视觉反馈和事件回调。

**职责：**
- `StartDrag(cell, worldPos)` — 开始拖拽某个单元格
- `UpdateDrag(worldPos)` — 拖拽中更新位置和连线
- `EndDrag(worldPos)` — 结束拖拽，触发放置回调
- `SetNearestCell(cell)` — 外部设置当前最近单元格
- 拖拽指示器（`dragIndicator`）跟随鼠标
- 连线（`connectionLine`）从起点到当前位置，距离内变绿/外变红

**事件：**
| 事件 | 触发时机 | 参数 |
|------|---------|------|
| `OnDragStarted` | 拖拽开始 | 无 |
| `OnCellSelected` | 拖拽中选中单元格变化 | `GridCell` |
| `OnCellDropped` | 拖拽放下 | `GridCell`（目标） |
| `OnDragStopped` | 拖拽结束 | 无 |

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

### GridDragHandler

| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `Snap Distance` | float | 0.5 | 吸附距离，拖拽到此距离内连线变绿 |
| `Drag Speed` | float | 10 | 拖拽速度（预留） |
| `Drag Indicator` | GameObject | null | 拖拽指示器（跟随鼠标） |
| `Connection Line` | LineRenderer | null | 起点到当前位置的连线 |
| `Valid Color` | Color | green | 距离内连线颜色 |
| `Invalid Color` | Color | red | 距离外连线颜色 |

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

> **注意：** `WorldToGridPosition` 返回的 `Vector2Int` 中 `x = col, y = row`，而 `GetCell` 接收 `(row, col)`。调用时需用 `GetCell(pos.y, pos.x)`。

---

## 使用示例

### 基础：创建一个 3×3 网格

**1. 制作单元格 Prefab：**
- 新建空 GameObject，命名 `CellPrefab`
- 添加 SpriteRenderer（作为 backgroundRenderer）
- 可选：添加子 GameObject + SpriteRenderer（作为 highlightRenderer）
- 挂载 `GridCell` 脚本
- 拖入对应引用，保存为 Prefab

**2. 挂载并初始化：**
```csharp
public class GridExample : MonoBehaviour
{
    public GridSystem gridSystem;
    public GameObject cellPrefab;
    public Transform parent;

    private void Start()
    {
        // 创建 3 行 × 3 列网格
        gridSystem.InitializeGrid(3, 3, cellPrefab, parent);
    }
}
```

### 查询单元格

```csharp
// 按行列号获取
GridCell cell = gridSystem.GetCell(1, 2);  // 第1行第2列

// 按世界坐标获取最近的单元格
GridCell nearest = gridSystem.GetNearestCell(transform.position);

// 获取所有单元格
List<GridCell> all = gridSystem.GetAllCells();
```

### 坐标转换

```csharp
// 网格坐标 → 世界坐标
Vector2 worldPos = gridSystem.GridToWorldPosition(1, 2);

// 世界坐标 → 网格坐标
Vector2Int gridPos = gridSystem.WorldToGridPosition(transform.position);
GridCell cell = gridSystem.GetCell(gridPos.y, gridPos.x); // 注意 y=row, x=col
```

### 交换两个单元格

```csharp
// 交换 (0,0) 和 (2,2) 的单元格
gridSystem.SwapCells(0, 0, 2, 2);
```

### 高亮所有单元格

```csharp
gridSystem.HighlightValidCells(true);   // 全部高亮
gridSystem.HighlightValidCells(false);  // 全部取消
```

### 拖拽交互

```csharp
public class DragExample : MonoBehaviour
{
    public GridDragHandler dragHandler;
    public GridSystem gridSystem;
    public Camera cam;

    private void Start()
    {
        // 订阅事件
        dragHandler.OnDragStarted += () => Debug.Log("拖拽开始");
        dragHandler.OnCellDropped += (cell) => Debug.Log($"放到 {cell.Row},{cell.Col}");
        dragHandler.OnDragStopped += () => Debug.Log("拖拽结束");
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Vector2 mousePos = cam.ScreenToWorldPoint(Input.mousePosition);
            GridCell cell = gridSystem.GetNearestCell(mousePos);
            if (cell != null)
                dragHandler.StartDrag(cell, mousePos);
        }

        if (Input.GetMouseButton(0) && dragHandler.IsDragging)
        {
            Vector2 mousePos = cam.ScreenToWorldPoint(Input.mousePosition);
            dragHandler.UpdateDrag(mousePos);

            // 更新最近单元格
            GridCell nearest = gridSystem.GetNearestCell(mousePos);
            if (nearest != null)
                dragHandler.SetNearestCell(nearest);
        }

        if (Input.GetMouseButtonUp(0) && dragHandler.IsDragging)
        {
            Vector2 mousePos = cam.ScreenToWorldPoint(Input.mousePosition);
            dragHandler.EndDrag(mousePos);
        }
    }
}
```

### 继承 GridCell 扩展功能

```csharp
using UnityEngine;

public class PuzzleTile : GridCell
{
    public int tileId;

    public override void Initialize(int row, int col)
    {
        base.Initialize(row, col);
        // 扩展初始化逻辑
    }

    public override void OnCellClicked()
    {
        Debug.Log($"Tile {tileId} at ({Row}, {Col}) clicked!");
        SetHighlighted(!IsHighlighted);
    }
}
```

---

## 常见问题

### Q: InitializeGrid 后单元格位置不对？

检查 `Grid Origin`（网格起始坐标）和 `Cell Size`（单元格尺寸）。世界坐标计算公式：`(gridOrigin.x + col × cellSize, gridOrigin.y + row × cellSize)`。

### Q: WorldToGridPosition 返回的坐标和 GetCell 参数对不上？

`WorldToGridPosition` 返回 `Vector2Int(x=col, y=row)`，而 `GetCell` 接收 `(row, col)`。调用时用 `GetCell(pos.y, pos.x)`。

### Q: 拖拽连线不显示？

检查 `GridDragHandler` 上是否拖入了 `Connection Line`（LineRenderer 组件引用）。未赋值则不显示连线，但拖拽逻辑仍正常工作。

### Q: 高亮没有视觉效果？

检查 `GridCell` 的 `Highlight Renderer` 是否赋值。高亮时会 `SetActive(true)` 并设置颜色；未赋值则无视觉变化。

### Q: 如何在运行时动态修改网格？

- 调用 `InitializeGrid` 会先销毁旧单元格再创建新的
- 使用 `SetCell(row, col, newCell)` 替换单个单元格
- 使用 `SwapCells(r1, c1, r2, c2)` 交换两个单元格位置
