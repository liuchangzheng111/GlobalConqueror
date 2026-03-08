# Unity 2D项目适配说明

## 概述

本项目已针对Unity 2D项目进行了适配，主要改动是将坐标系统从3D的XZ平面改为2D的XY平面。

## 主要改动

### 1. 坐标系统转换

#### HexCoordinate.cs
- **FromWorldPosition**: 使用 `worldPos.y` 替代 `worldPos.z`
- **ToWorldPosition**: 返回 `new Vector3(x, y, 0)` 而不是 `new Vector3(x, 0, z)`

```csharp
// 修改前（3D）
float z = worldPos.z;
return new Vector3(x, 0, z);

// 修改后（2D）
float y = worldPos.y;
return new Vector3(x, y, 0);
```

### 2. 相机控制器（MapCameraController.cs）

#### 拖拽功能
- **移动方向**: 在XY平面上移动，而不是XZ平面
- **边界限制**: 使用 `targetPosition.y` 替代 `targetPosition.z`

```csharp
// 修改前（3D）
Vector3 moveDelta = new Vector3(-delta.x * dragSpeed, 0, -delta.y * dragSpeed);
targetPosition.z = Mathf.Clamp(targetPosition.z, dragBoundsMin.y, dragBoundsMax.y);

// 修改后（2D）
Vector3 moveDelta = new Vector3(-delta.x * dragSpeed, -delta.y * dragSpeed, 0);
targetPosition.y = Mathf.Clamp(targetPosition.y, dragBoundsMin.y, dragBoundsMax.y);
```

#### 地图边界初始化
- **地图高度**: 使用 `mapBounds.size.y` 替代 `mapBounds.size.z`
- **相机位置**: Z轴保持当前值（通常为-10），只调整XY
- **拖拽边界**: 使用XY平面的边界

```csharp
// 修改前（3D）
float mapHeight = mapBounds.size.z;
mapCenter.y = transform.position.y;
dragBoundsMin = new Vector2(mapBounds.min.x - margin, mapBounds.min.z - margin);

// 修改后（2D）
float mapHeight = mapBounds.size.y;
mapCenter.z = transform.position.z;
dragBoundsMin = new Vector2(mapBounds.min.x - margin, mapBounds.min.y - margin);
```

### 3. 地图管理器（MapManager.cs）

#### 地图边界计算
- **坐标轴**: 使用Y轴替代Z轴
- **边界中心**: 使用 `(minX + maxX) / 2f, (minY + maxY) / 2f, 0`

```csharp
// 修改前（3D）
float minZ = float.MaxValue;
float maxZ = float.MinValue;
Vector3 center = new Vector3((minX + maxX) / 2f, 0, (minZ + maxZ) / 2f);

// 修改后（2D）
float minY = float.MaxValue;
float maxY = float.MinValue;
Vector3 center = new Vector3((minX + maxX) / 2f, (minY + maxY) / 2f, 0);
```

### 4. 地块选择器（TileSelector.cs）

#### 屏幕坐标转换
- **方法**: 使用 `Camera.ScreenToWorldPoint` 替代射线检测
- **Z轴**: 保持为0

```csharp
// 修改前（3D）
Ray ray = mainCamera.ScreenPointToRay(screenPosition);
Plane plane = new Plane(Vector3.up, Vector3.zero);

// 修改后（2D）
Vector3 worldPos = mainCamera.ScreenToWorldPoint(screenPosition);
worldPos.z = 0;
```

#### Gizmos绘制
- **高亮高度**: 在Z轴上偏移，而不是Y轴

```csharp
// 修改前（3D）
worldPos.y = highlightHeight;

// 修改后（2D）
worldPos.z = highlightHeight;
```

### 5. 调试器（TileCoordinateDebugger.cs）

#### 坐标显示
- **坐标轴**: 显示 `worldPos.y` 替代 `worldPos.z`

```csharp
// 修改前（3D）
$"世界位置: ({worldPos.x:F2}, {worldPos.z:F2})"

// 修改后（2D）
$"世界位置: ({worldPos.x:F2}, {worldPos.y:F2})"
```

## Unity 2D项目设置建议

### 相机设置
1. **相机类型**: 使用正交相机（Orthographic Camera）
2. **Z轴位置**: 通常设置为 -10（保持固定）
3. **投影**: Projection = Orthographic

### Grid和Tilemap设置
1. **Grid布局**: Cell Layout = Hexagon
2. **坐标系统**: 使用XY平面
3. **Z轴**: 所有Tile的Z坐标应该为0

### 场景设置
```
场景结构：
├── Grid (Grid组件，Cell Layout = Hexagon)
│   └── Tilemap (Tilemap组件，Z = 0)
├── MapManager (MapManager组件)
├── Main Camera (MapCameraController组件，Z = -10)
└── TileSelector (TileSelector组件)
```

## 注意事项

1. **Z轴处理**:
   - 地图Tile的Z坐标应该为0
   - 相机的Z坐标通常为-10（保持固定）
   - 高亮效果在Z轴上偏移

2. **坐标转换**:
   - 所有世界坐标转换都使用XY平面
   - HexCoordinate转换也使用XY平面

3. **边界计算**:
   - 地图边界使用XY平面的尺寸
   - 相机拖拽边界也使用XY平面

4. **屏幕坐标**:
   - 使用 `Camera.ScreenToWorldPoint` 进行转换
   - 转换后Z轴设为0

## 测试检查清单

- [ ] 相机拖拽在XY平面上正常工作
- [ ] 地图边界计算正确（使用Y轴）
- [ ] 地块点击选中功能正常
- [ ] 坐标显示正确（显示XY而不是XZ）
- [ ] 相机自动初始化正确（定位到地图中心）
- [ ] 缩放功能正常
- [ ] Gizmos高亮显示正确

## 常见问题

### Q: 为什么相机Z轴要保持-10？
A: Unity 2D相机通常设置在Z=-10的位置，这样可以确保相机能看到Z=0平面的所有对象。

### Q: 如果我的地图不在Z=0平面怎么办？
A: 可以修改`HexCoordinate.ToWorldPosition()`方法，将Z坐标设置为你的地图Z坐标。

### Q: 拖拽方向不对？
A: 检查`MapCameraController.HandleDrag()`中的`moveDelta`计算，确保使用XY平面。

### Q: 坐标显示错误？
A: 检查`TileCoordinateDebugger`中的坐标显示，确保使用`worldPos.y`而不是`worldPos.z`。

## 总结

所有代码已针对Unity 2D项目进行了适配：
- ✅ 坐标系统：XZ → XY
- ✅ 相机控制：适配2D平面
- ✅ 地图边界：使用Y轴计算
- ✅ 屏幕转换：使用2D方法
- ✅ 调试显示：显示XY坐标

代码现在完全兼容Unity 2D项目！
