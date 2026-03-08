# 从Tilemap初始化地图数据指南

## 功能说明

现在MapManager支持从Unity Tilemap系统初始化地图数据，而不是随机生成。系统会自动读取你在Tilemap中绘制的地图，并将其转换为游戏内部的地块数据。

## 使用步骤

### 1. 准备Tilemap

1. **创建Grid和Tilemap**：
   - 在场景中创建Grid GameObject（右键 Hierarchy -> 2D Object -> Tilemap -> Hexagonal）
   - 确保Grid的Cell Layout设置为"Hexagon"
   - 创建Tilemap子对象（会自动创建）

2. **绘制地图**：
   - 使用Tile Palette绘制你的地图
   - 使用不同的Tile表示不同的地形类型（平原、山地、森林、海洋、城市、港口）

### 2. 配置MapManager

1. **选择MapManager GameObject**

2. **在Inspector中设置**：
   - ✅ **Use Tilemap As Source**: 勾选此选项，启用从Tilemap初始化
   - ✅ **Auto Find Tilemap**: 勾选此选项，自动查找场景中的Tilemap
   - **Source Tilemap**: （可选）手动指定Tilemap，如果留空且Auto Find开启，会自动查找

3. **自定义Tile类型映射**（可选）：
   - 展开"Custom Tile Mappings"列表
   - 点击"+"添加映射项
   - 将你的Tile拖拽到"Tile"字段
   - 选择对应的"Tile Type"

### 3. Tile类型自动识别规则

如果不使用自定义映射，系统会根据Tile的Sprite名称自动识别类型：

| Sprite名称包含 | 识别为 |
|---------------|--------|
| water, sea, ocean | Water（海洋） |
| mountain, mount | Mountain（山地） |
| forest, tree | Forest（森林） |
| city, town | City（城市） |
| port, harbor | Port（港口） |
| plain, grass | Plain（平原） |
| 其他 | Plain（平原，默认） |

**命名建议**：
- 将你的Sprite命名为：`Plain.png`, `Water.png`, `Mountain.png` 等
- 或者在Sprite名称中包含关键词，如：`tile_water_01.png`, `grass_plain.png`

### 4. 运行测试

运行游戏后，MapManager会：
1. 自动查找场景中的Tilemap
2. 遍历所有有Tile的位置
3. 将Tilemap坐标转换为六边形坐标
4. 根据Tile类型创建地块数据
5. 在Console中显示读取的地块数量

## 示例场景设置

```
场景结构：
├── Grid (Grid组件，Cell Layout = Hexagon)
│   ├── Tilemap (Tilemap组件)
│   │   └── [你绘制的Tile]
│   └── Tilemap (Collider) [可选]
├── MapManager (MapManager组件)
│   ├── Use Tilemap As Source: ✓
│   ├── Auto Find Tilemap: ✓
│   └── Custom Tile Mappings: [可选配置]
├── Main Camera (MapCameraController组件)
└── TileSelector (TileSelector组件)
```

## 常见问题

### Q: 地图没有初始化？
A: 检查以下几点：
1. Tilemap是否存在且包含Tile
2. Grid的Cell Layout是否设置为Hexagon
3. MapManager的"Use Tilemap As Source"是否勾选
4. 查看Console是否有错误信息

### Q: Tile类型识别不正确？
A: 
1. 检查Sprite名称是否包含关键词（见上表）
2. 或者使用Custom Tile Mappings手动配置映射关系

### Q: 坐标转换不正确？
A:
1. 确保Grid的Cell Layout设置为Hexagon
2. 检查Grid的Cell Size是否合理
3. MapManager会自动根据Grid的Cell Size设置HexSize

### Q: 如何同时使用多个Tilemap？
A:
- 目前支持单个Tilemap，如果需要多个图层，可以：
  1. 使用多个Tilemap，但只指定一个作为主Tilemap
  2. 或者扩展代码支持多Tilemap合并

## 高级用法

### 自定义映射优先级

1. **自定义映射**（最高优先级）
2. **Sprite名称识别**（次优先级）
3. **默认类型**（Plain，最低优先级）

### 扩展Tile类型

如果需要添加新的Tile类型：
1. 在`TileType`枚举中添加新类型
2. 在`TileTypeMapper.cs`中添加识别规则
3. 或者在Custom Tile Mappings中配置

## 技术细节

- **坐标转换**：使用`TilemapToHexConverter`将Unity的Offset坐标转换为Axial坐标
- **Tile识别**：使用`TileTypeMapper`根据Sprite名称或自定义映射识别类型
- **数据存储**：所有地块数据存储在`MapManager`的`tileDataMap`字典中
