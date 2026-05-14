## 一、胜利与失败（与代码一致）

敌方战败的硬条件（你们描述与实现一致）：

- 某国 `ownedCities` 变为空 → `isDefeated = true`，并触发 `OnNationDefeated`（见 `UnitManager.CaptureCity` 里对 `oldOwner.ownedCities.Count == 0` 的处理）。
- 战败后 `UnitManager` 会移除该国所有单位（`OnNationDefeated` → `RemoveAllUnitsOfNation`）。

占领城市的硬条件（地面逻辑）：

- 己方单位 `TryMoveUnit` 走入敌方城市的 `cityLocation` 格 → `CaptureCity` → `CityManager.TransferCityOwnership`（更新 `NationData` 的城市列表与 `CityData.ownerNationId` 等）。

占领城市的另一条捷径（空军逻辑）：

- 空投落在某格：若该格是敌方城市且无守军，`AirManager.ExecuteParadrop` 会调用 `UnitManager.CaptureCity`，与陆军踩城等价（见 `AirManager` 中 paradrop 末尾分支）。

你们尚未在运行时统一调用的点（实现 AI 时需心里有数）：

- `NationManager.CheckNationDefeat` 目前是 private 且未被引用；实际战败入口主要靠 `CaptureCity` 后城市列表为空。AI 设计不必依赖 `CheckNationDefeat`，以「城市归属」为真源即可。

玩家侧胜利（代码里未见集中 `GameVictory`，属产品层）：

- 可定义为：所有非玩家国家 `isDefeated`，或「仅剩玩家拥有城市」等；与 AI 的「最大化让玩家失去所有城市」对称。

------

## 二、单回合时间轴（AI 决策顺序要贴合）

`NationManager.StartTurn` 顺序大致为：

1. 确定 `currentNation`（跳过 `isDefeated`）。

2. `ProcessResourceProduction`：按该国 `ownedCities`+ `PortManager` 该国港口 结算金币/工业/科技。

3. ``` 
   OnNationTurnStart → UnitManager.OnNationTurnStart
   ```

   - 该国单位 建造回合推进；
   - 城市补给治疗（仅对 `cityLocation` 上且为己方的单位生效，与「城市格站人」强相关）；
   - 重置该国单位本回合 `hasMoved` / `hasAttacked`（建造中单位被锁为已行动）。

因此 AI 的「战略层」应默认：先吃满本回合新增资源与单位状态，再规划花钱/造兵/空军，最后才是单位移动与攻击（与玩家体验一致，也避免用错资源快照）。

------

## 三、经济与城市：AI 的「长期棋」

收入结构（来自 `ProcessResourceProduction` + `CityData`）：

- 每城：金币 / 工业 / 科技 与 城市等级、工业等级、科技等级 挂钩；首都名与 `city.name` 匹配时有额外一笔固定加成。
- 港口：按国名在 `PortManager.NationOwnPorts` 中累加金币/工业（科技是否来自港口需看 `PortData`，当前 AI 战略上至少要把「港口线」当工业/金来源）。

支出结构（代码里已存在的主要出口）：

- 买兵：`TryPurchaseUnit(CityData|PortData, …)`，受 `CanSatisfyProduceCondition`（城市等级 / 工业 / 机场 / 港口等级与 `UnitTypeConfig.produceCondition`）与 三资源花费 约束。
- 造堡垒：`TryBuildFort` — 需在 本国 `ownerId` 的地块、且地形为 平原/森林/山地、空格、扣费后生成 建造中 Fort（多回合后才完整）。
- 造防空：`AntiAirManager.TryBuildAntiAir` — 本国地、部分地形、扣费、`MapTileData.antiAir`。
- 空军任务：`AirManager.TryExecuteMission` — 依赖 `currentCity`（出发城）、机场等级、航程、目标合法性、扣费。

对 AI 的战略含义：

- 城市不仅是胜利目标，更是现金流与兵种上限；抢一座高等级城往往比单纯歼灭一队兵更伤对手经济。
- 首都在数据上有 `capital` 字符串与产出加成，AI 宜单独建模：防守权重极高、进攻优先级可按「是否敌方首都 / 高产出城」排序。
- 港口影响产出与海军生产条件；海图或两栖战线里，港口应进入「战区资产」而不只是点缀。

------

## 四、地面战斗与机动（战术层的事实）

移动（`GetReachablePositions` / `FindPath` / `TryMoveUnit`）：

- 六角格、按地形消耗（`UnitTypeConfig` 的平原城/森林/山地/水域消耗；水域不可为 0 消耗等）。
- 敌军占格不可穿行（阻挡寻路扩展）；空格才可 `TryMoveUnit` 走入（攻城前常需 清格或空投空城）。
- 陆↔水：走入水/港口会触发 驳船换装；上岸再换回来（规则集中在 `TryMoveUnit` 里）。

攻击（`TryAttack` + `GetAttackablePositions`）：

- 射程为六角距离；对不同类型有 特攻表（`attackStrength_*` vs 目标 `UnitProperty`）。
- 随机浮动（约 0.8～1.2）→ AI 评估伤害只能做 期望值/区间，不宜当确定性棋。
- 反击规则：火炮/潜艇/航母等 可免反击；建造中 Fort、不可反击属性、射程不够 等会改变交换比 —— AI 若要「换血划算」必须读这些分支（都在 `TryAttack` 一段里）。

特殊克制：

- 潜艇 vs 陆军 互相不可打，例外：名为 「海岸炮」 的 Fort 可打潜艇（`GetAttackablePositions` 里硬编码逻辑）。

对 AI 的战略含义：

- 攻城 = 控制 `cityLocation` 那一格；守方会尽量用 单位卡位。
- 破城手段分化：
  - 歼灭/击退城门单位；
  - 空投到空城格（需机场与资源、且防空会削血甚至防空导弹能直接打死落地单位）；
  - 围城补给（间接）配合后续版本若你们有加围城的规则再扩展。

------

## 五、空军与防空（「第三维度」战线）

空袭（`ExecuteAttackTarget`）：

- 对格上 敌方单位 造成伤害；防空通过 `MapTileData.antiAir` → `GetAirStrikeMultiplier` 减伤。

空投（`ExecuteParadrop`）：

- 目标须 非水域/非港口、无单位；落地兵可能吃 `GetParadropDamage`；
- 若目标格是敌方城市且无守军 → 直接 `CaptureCity`。

对 AI 的战略含义：

- 软杀：空袭削血，为陆军补刀或逼退。
- 奇袭占城：对 `cityLocation` 空窗 的城市价值极高，应作为 高优先级子目标（与玩家对称的「防空降」也应成为防守 AI 的一条轴）。
- 防空投资是 针对玩家/敌方机场等级的反制；进攻方 AI 则需 算期望伤害 vs 任务费，避免亏资源。

实现注意（给后续写代码的 AI 工程师）：

- `AirManager.TryExecuteMission` 依赖 `currentCity`；UI 是在选任务前赋值的。程序化 AI 应在调用前 显式设置 `AirManager.instance.currentCity = 某城`，用毕可清空，避免状态污染。

------

## 六、堡垒与城市补给（「战线与节奏」）

- Fort：多回合建造、完成前几乎不参与机动输出；适合 卡口、迟滞、保护城市格前走廊。
- 补给治疗：只在 「城市所占格子的 `cityLocation`」且己方单位 上触发 —— AI 防守时应意识到：首都/关键城格上站一支可挨打单位 可能比「单位在城外一圈」更能吃补给（视你们是否希望利用该机制）。

------

## 七、建议的 AI 战略架构（分层，供后续加强用）

下面是一套与当前玩法 对齐、可拆任务 的逻辑框架。实现时可做成 多模块管线（每回合从上层往下层调用，下层可覆盖上层意图）。

### 层 0：态势抽象（World Model）

为所有上层提供只读视图（避免散落 `if`）：

- 领土：`MapTileData.ownerId`、城市归属、港口归属、防空格集合。
- 军事力量：按国的单位列表、关键属性（移动力、射程、血量比、是否建造中、是否潜艇/炮等）。
- 经济：三国资源 + 下回合预期收入（可用当前公式估算）。
- 威胁/机会：
  - 哪些 `cityLocation` 上无己方单位但仍是己方城（空降风险）；
  - 哪些敌方城格可一步/空投进入；
  - 主力接触线（六角距离意义下的前沿）。

### 层 1：战略目标（Strategic Goals）——对齐「夺城致败」

对「主要敌人」（初期可固定为玩家国，后期可做「最大威胁国」评分）生成 有序目标列表：

1. 歼灭战目标：削弱其 守城兵力与机动预备队（否则夺城成本高）。
2. 夺城目标：按评分选城，例如加权：
   - 敌方首都 / 高产出城；
   - 通往其他城的咽喉地形（结合 `GetReachablePositions` 与地图狭窄处，可用「切断两国增援」的启发式）；
   - 空降可乘之城（`cityLocation` 无守军 + 我方有空投能力）。
3. 经济战目标：港口与工业城 —— 降低对手 买兵与空军任务 的能力（与第 1 点耦合）。

### 层 2：战役计划（Operational Plan）——每若干回合相对稳定的「剧本」

在「目标城」周围生成 子任务，不必每回合重算全部：

- 集结：在距离目标 ≤ N 六角 的己方领土上 优先买兵/修 Fort。
- 火力准备：若机场和资源足够，安排 空袭高价值单位或城门前单位。
- 主攻方向：选 1～2 条六角走廊（低移动消耗、少森林/山地）推进，避免兵力平均摊开（当前简单 AI 易犯的错）。
- 辅助方向：佯攻、夺港、空降牵制（即使不夺城也可拉走玩家守军）。

### 层 3：资源与建设（Economy / Build）——和「能打赢下一城」绑定

决策原则建议：

- 边际收益：下一单位 / 下一机场等级 / 下一防空，对 「预计 3～5 回合内攻下的那座城」 的胜率提升多少。

- 兵种组合

  （读

   

  ```
  UnitTypeConfig
  ```

   

  +

   

  ```
  CanSatisfyProduceCondition
  ```

  ）：

  - 廉价步兵填线、占格、占空城；
  - 装甲高机动突破；
  - 火炮在安全距离削血（利用免反击）；
  - 海军只在港口线有意义时投资；
  - Fort 在 玩家空军强或需要守首都格 时提高权重。

### 层 4：战术执行（Tactics）——你们已有 API 的「如何用」

在单回合内对单位排序时，建议 先远程/空军再近战（减少己方无谓反击损失），地面则：

- 移动：不止「一步贪心靠近」，可升级为：
  - 对目标城 `cityLocation` 做 受限 BFS/A*（成本上限 = 移动力）找 真正缩短回合数的路径；
  - 或 多步模拟（若性能允许）：「若本回合走到格 A，下回合能否攻击城门单位」。
- 攻击目标选择：在「期望伤害 − 期望反击损失」框架下选目标；对 火炮/潜艇 可更激进。
- 占城：任何能 走入/空投入 `cityLocation` 且满足 `TryMoveUnit` / paradrop 条件的动作，应对 「是否立刻结束战争」 赋予极高奖励（因为直接触发 `CaptureCity` 链）。

### 层 5：防守与反 AI（Mirror）——让玩家感到「会玩」

当 玩家兵力接近己方首都或高价值城 时切换 防守姿态：

- 城格站人、门前 Fort、防空针对玩家机场等级；
- 预备队放在 1 回合可增援 `cityLocation` 的格集合 上（六角距离 1 或移动力覆盖）。

------

## 八、与「简单 AI」的差距（后续加强的路线图）

当前 `SimpleNationSkirmishAi` 本质是 层 4 的极简贪心（靠近敌/敌城 + 打低血），缺少：

- 层 1～3（目标城、资源、买兵、空军、防空、Fort）；
- 多步 lookahead；
- 对空投占城、防空、首都补给格 的显式建模。

建议后续实现顺序（与代码依赖匹配）：

1. 程序化买兵（封装对 `TryPurchaseUnit` 的调用，复用 `CanSatisfyProduceCondition`）。
2. 目标城选择与主攻走廊（层 1～2）。
3. 移动/攻击 lookahead 或更好的路径目标（层 4）。
4. 空军 + 防空经济博弈（层 3～5，注意 `currentCity` 与扣费顺序）。
5. Fort 与港口线（地图依赖强，放中后期）。

------

## 九、小结

- 胜利链在代码里是：控制 `cityLocation` → `CaptureCity` → 敌方无城 → `isDefeated` → 清兵；空投是同一链条上的 捷径。
- 经济链是：城 + 港 → 回合初资源 → 买兵/空军/堡垒/防空 → 再转化为夺城能力。
- 一套可抗衡玩家的 AI，应把 「城」同时当作终点目标、收入源与战术格点（尤其 `cityLocation` 守军与防空），并在实现上分 态势 → 战略 → 战役 → 经济 → 战术 五层迭代。