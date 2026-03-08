# 代码架构说明

## 文件夹结构

```
Scripts/
├── Models/          # 数据模型层
│   ├── HexCoordinate.cs    # 六边形坐标系统
│   └── TileData.cs         # 地块数据模型
├── Views/           # 视图层（UI相关，待扩展）
├── Controllers/     # 控制器层
│   ├── MapCameraController.cs  # 地图相机控制（拖拽、缩放）
│   └── TileSelector.cs         # 地块选择器（点击、高亮）
├── Managers/        # 管理器层
│   └── MapManager.cs           # 地图管理器
├── Utils/           # 工具类
│   └── HexGridUtils.cs         # 六边形网格工具
├── Debug/           # 调试工具
│   └── TileCoordinateDebugger.cs  # 坐标调试器（命名空间：GlobalConqueror.DebugTools）
└── Editor/          # 编辑器扩展
    └── MapManagerEditor.cs        # MapManager编辑器扩展
```

## 核心功能说明

### 1. 六边形坐标系统 (HexCoordinate)

- 使用轴向坐标系统（Axial Coordinate System）
- 支持世界坐标与六边形坐标的相互转换
- 支持距离计算、邻居查找等功能

### 2. 地图管理器 (MapManager)

- 管理所有地块数据
- 提供坐标查询、地块数据获取等功能
- 处理地块选中事件

### 3. 地图相机控制器 (MapCameraController)

- **拖拽功能**：鼠标左键拖拽移动地图
- **缩放功能**：鼠标滚轮缩放地图
- 支持平滑移动和边界限制

### 4. 地块选择器 (TileSelector)

- **点击选中**：点击地块进行选中
- **高亮显示**：选中的地块会高亮显示
- 支持射线检测和屏幕坐标转换

### 5. 坐标调试器 (TileCoordinateDebugger)

- 显示选中地块的坐标信息
- 支持屏幕显示和控制台输出

## 使用步骤

### 1. 场景设置

1. **创建空场景**或使用现有场景
2. **添加MapManager**：

   - 创建空GameObject，命名为"MapManager"
   - 添加 `MapManager`组件
   - 设置地图参数（HexSize、MapWidth、MapHeight）
3. **设置相机**：

   - 选择Main Camera
   - 添加 `MapCameraController`组件
   - 设置拖拽和缩放参数
4. **添加地块选择器**：

   - 创建空GameObject，命名为"TileSelector"
   - 添加 `TileSelector`组件
5. **添加调试器**（可选）：

   - 创建空GameObject，命名为"TileCoordinateDebugger"
   - 添加 `TileCoordinateDebugger`组件

### 2. 地图Tilemap设置

如果使用Unity Tilemap系统：

1. 创建Grid GameObject（使用Hexagonal布局）
2. 创建Tilemap子对象
3. 使用六边形Rule Tile绘制地图

### 3. 测试功能

- **拖拽地图**：按住鼠标左键拖拽
- **缩放地图**：滚动鼠标滚轮
- **选中地块**：点击地图上的地块
- **查看坐标**：选中地块后，在控制台或调试UI中查看坐标信息

## 注意事项

1. **相机设置**：建议使用正交相机（Orthographic Camera）
2. **碰撞检测**：如果地块没有碰撞体，TileSelector会使用屏幕坐标转换
3. **六边形大小**：确保MapManager中的HexSize与实际Tilemap的六边形大小一致
4. **DOTween依赖**：MapCameraController使用了DOTween，确保已导入DOTween插件

## 后续扩展

- [ ] 添加地块可视化（使用Tilemap或Mesh）
- [ ] 实现地块类型切换功能
- [ ] 添加地块数据持久化
- [ ] 优化高亮显示效果
- [ ] 添加多选功能
