# 相机自动初始化功能说明

## 功能概述

地图初始化完成后，相机会自动根据地图数据设置以下参数：
- **初始相机位置**：自动定位到地图中心
- **初始相机大小**：自动计算适合显示整个地图的缩放级别
- **拖拽边界**：自动设置相机可拖拽的范围
- **缩放限制**：自动计算最小和最大缩放值

## 工作原理

### 1. 地图边界计算

MapManager在初始化地图后会自动计算地图边界（Bounds）：
- 遍历所有地块的世界坐标
- 找到最小和最大的X、Z坐标
- 添加边距后生成Bounds

### 2. 相机参数自动设置

MapCameraController监听`OnMapInitialized`事件，当地图初始化完成后：

#### 初始相机位置
- 设置为地图边界的中心点
- Y坐标保持当前值不变

#### 初始相机大小（Orthographic Size）
计算公式：
```
初始大小 = (地图最大尺寸 / 2) / 初始缩放比例
```
- 默认初始缩放比例：0.8（显示80%的地图）
- 可根据需要调整`Initial Zoom Ratio`参数

#### 拖拽边界
- 基于地图边界计算
- 添加10%的边距，允许相机稍微超出地图边界
- 自动设置`dragBoundsMin`和`dragBoundsMax`

#### 缩放限制
- **最小缩放**：`(地图大小 / 2) / 最小缩放比例`
  - 默认最小缩放比例：0.5（能看到50%的地图）
- **最大缩放**：`(地图大小 / 2) / 最大缩放比例`
  - 默认最大缩放比例：0.1（能看到10%的地图，即放大10倍）

## 配置参数

在MapCameraController的Inspector中：

| 参数 | 说明 | 默认值 |
|------|------|--------|
| **Auto Initialize From Map** | 是否启用自动初始化 | true |
| **Initial Zoom Ratio** | 初始缩放比例（0-1） | 0.8 |
| **Min Zoom Ratio** | 最小缩放比例（0-1） | 0.5 |
| **Max Zoom Ratio** | 最大缩放比例（0-1） | 0.1 |

### 缩放比例说明

缩放比例表示相机能看到的地图比例：
- `1.0` = 显示100%的地图（完全显示）
- `0.8` = 显示80%的地图（稍微缩小）
- `0.5` = 显示50%的地图（缩小2倍）
- `0.1` = 显示10%的地图（缩小10倍，即放大10倍）

## 使用示例

### 示例1：默认设置
```csharp
// MapCameraController设置：
Auto Initialize From Map: ✓
Initial Zoom Ratio: 0.8
Min Zoom Ratio: 0.5
Max Zoom Ratio: 0.1

// 结果：
// - 初始显示80%的地图
// - 可以缩小到显示50%的地图
// - 可以放大到显示10%的地图（10倍放大）
```

### 示例2：完全显示地图
```csharp
// MapCameraController设置：
Auto Initialize From Map: ✓
Initial Zoom Ratio: 1.0  // 完全显示地图
Min Zoom Ratio: 0.8      // 最小缩小到80%
Max Zoom Ratio: 0.2      // 最大放大到20%（5倍放大）

// 结果：
// - 初始完全显示地图
// - 可以稍微缩小
// - 可以放大5倍查看细节
```

### 示例3：禁用自动初始化
```csharp
// MapCameraController设置：
Auto Initialize From Map: ✗

// 结果：
// - 使用Inspector中手动设置的参数
// - 初始位置和缩放不会自动调整
```

## 手动调用

如果需要手动初始化相机，可以调用：

```csharp
MapCameraController cameraController = FindObjectOfType<MapCameraController>();
MapManager mapManager = FindObjectOfType<MapManager>();

if (cameraController != null && mapManager != null)
{
    cameraController.InitializeFromMapBounds(mapManager.MapBounds);
}
```

## 重置相机

调用`ResetCamera()`方法可以重置相机到初始状态：

```csharp
cameraController.ResetCamera();
```

如果启用了自动初始化，重置会使用地图边界重新计算参数。

## 注意事项

1. **地图必须已初始化**：确保MapManager已经完成地图初始化
2. **相机类型**：建议使用正交相机（Orthographic Camera）
3. **坐标系**：假设地图在XZ平面上（Y=0）
4. **性能**：地图边界计算在地图初始化时执行一次，不会影响运行时性能

## 调试信息

相机初始化完成后，会在Console中输出详细信息：
```
相机初始化完成:
  地图中心: (0, 0, 0)
  地图大小: 20
  初始缩放: 12.5
  缩放范围: 20 ~ 100
  拖拽边界: (-11, -11) ~ (11, 11)
```

## 常见问题

### Q: 相机没有自动初始化？
A: 检查：
1. `Auto Initialize From Map`是否勾选
2. MapManager是否已初始化地图
3. MapCameraController是否正确找到MapManager

### Q: 初始缩放不合适？
A: 调整`Initial Zoom Ratio`参数：
- 增大值（接近1.0）= 显示更多地图
- 减小值（接近0）= 显示更少地图（放大）

### Q: 缩放范围不合适？
A: 调整`Min Zoom Ratio`和`Max Zoom Ratio`：
- `Min Zoom Ratio`：控制最小缩放（缩小限制）
- `Max Zoom Ratio`：控制最大缩放（放大限制）

### Q: 拖拽边界不合适？
A: 相机初始化会自动计算边界，如果需要调整：
1. 禁用自动初始化
2. 手动设置`dragBoundsMin`和`dragBoundsMax`
