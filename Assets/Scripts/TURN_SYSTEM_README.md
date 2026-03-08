# 回合制管理系统说明

## 系统概述

回合制管理系统实现了游戏的核心回合循环，包括：
- 回合切换管理
- 国家资源生产
- 科技研发系统
- 回合事件触发

## 核心组件

### 1. TurnManager（回合管理器）

**位置**: `Managers/TurnManager.cs`

**核心功能**:
- 管理回合循环
- 控制国家轮流行动
- 触发回合开始/结束事件
- 处理资源生产和科技研发

**主要方法**:
- `StartTurn()`: 开始新回合
- `EndTurn()`: 结束当前回合
- `AddNation(NationData)`: 添加国家
- `GetNation(int)`: 获取指定国家

**事件**:
- `OnTurnStart`: 回合开始事件
- `OnTurnEnd`: 回合结束事件
- `OnNationTurnStart`: 国家回合开始事件
- `OnNationTurnEnd`: 国家回合结束事件

### 2. NationData（国家数据模型）

**位置**: `Models/NationData.cs`

**数据结构**:
- `nationId`: 国家ID
- `nationName`: 国家名称
- `nationColor`: 国家颜色
- `gold/industry/science`: 资源数量
- `ownedTiles`: 拥有的地块列表
- `cities`: 城市列表
- `isPlayer`: 是否为玩家
- `isDefeated`: 是否已失败

### 3. ResourceData（资源数据模型）

**位置**: `Models/ResourceData.cs`

**资源类型**:
- `Gold`: 金币
- `Industry`: 工业
- `Science`: 科技

**主要方法**:
- `GetResource(ResourceType)`: 获取资源
- `AddResource(ResourceType, int)`: 增加资源
- `ConsumeResource(ResourceType, int)`: 消耗资源

### 4. TechnologyData（科技数据模型）

**位置**: `Models/TechnologyData.cs`

**科技类型**:
- `Infantry`: 步兵科技
- `Armor`: 装甲科技
- `Artillery`: 火炮科技
- `Navy`: 海军科技
- `AirForce`: 空军科技
- `Industry`: 工业科技
- `Economy`: 经济科技

## 使用流程

### 1. 初始化

```csharp
// TurnManager会自动在Start中初始化
// 默认创建玩家和AI国家
```

### 2. 回合循环

```
回合开始 → 资源生产 → 科技研发 → 玩家操作 → 回合结束 → 下一个国家
```

### 3. 资源生产

每个回合开始时，系统会：
1. 遍历国家拥有的所有地块
2. 根据地块类型计算资源产出
3. 城市地块额外产出资源

### 4. 科技研发

每个回合开始时，系统会：
1. 消耗国家的科技点
2. 投入到当前研究的科技中
3. 当进度达到要求时升级科技

## 配置参数

在TurnManager的Inspector中：
- `Max Turns`: 最大回合数（默认100）
- `Auto End Turn`: 是否自动结束回合（AI自动）
- `Auto End Turn Delay`: 自动结束延迟时间（秒）

## UI集成

使用`TurnUIController`显示回合信息：
- 回合数
- 当前国家
- 资源数量
- 结束回合按钮

## 扩展建议

1. **回合事件系统**: 添加随机事件触发
2. **胜利条件**: 实现胜利/失败判定
3. **AI系统**: 完善AI国家的自动行动
4. **存档系统**: 保存回合状态

## 注意事项

1. TurnManager使用单例模式，确保场景中只有一个实例
2. 国家数据通过事件系统更新UI
3. 资源生产基于地块数据，需要确保MapManager已初始化
4. 玩家回合需要手动调用`EndTurn()`，AI回合可自动结束
