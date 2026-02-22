## 最近更新（物品选中操作窗 + 旋转交互）

- 新增物品操作窗 `SelectItemWindow`：点击 `InventoryItemView` 可选中物品并在物品旁显示操作窗。
- 支持再次点击同一物品关闭操作窗；点击空白区域也会关闭操作窗。
- 操作窗位置已优化为首次点击即稳定定位到物品旁（避免首次打开偏移到屏幕中部）。
- `SelectItemWindow` 显示物品名称，并根据物品是否可旋转显示/隐藏旋转按钮。
- 物品是否可旋转改为基于尺寸判断：`ItemCatalogEntry.Size.x != Size.y` 可旋转，移除 `CanRotate` 字段。
- 点击旋转按钮后可原地旋转物品（占格尺寸交换，`ItemInstance.Rotated` 更新），支持背包容器与仓库容器。
- `InventoryItemView` 图标显示支持 90°旋转，并在创建时按“未旋转尺寸”设置图标基准大小。

## 最近更新（仓库持久化 + 收益统计口径）

- 新增局外仓库窗口 `WarehouseWindow`，支持与玩家局外装备区双向拖拽管理物资。
- 新增 `WarehouseContainerView`，仓库内物品支持拖拽、自动摆放与坐标记忆。
- 新增持久化模型 `PersistentInventoryModel`，仓库物资以扁平 `Items` 列表保存。
- 新增持久化脚本拆分：`InventorySystem` 负责局内运行时逻辑，`PersistentInventorySystem` 负责读档/存档。
- 新增局外装备区持久化（`PlayerLoadoutSaveData`）：局外装备与容器内容可保存并在下次启动恢复。
- 调整开局逻辑：开始对局时会把局外装备区作为本局带入物资，并在存档中清空局外同批物资，避免重复占有。
- 收益口径改为净收益：`净收益 = 带出总价值 - 带入总价值`，并用于 `GameOverWindow`、`PlayerProgress`、`PlayerInventoryView` 显示。
- 新增玩家进度字段 `TotalAsset`（总资产）：`Cash + 仓库物资总价值 + 局外装备区总价值`。
- 新增 `PlayerDataView` 用于展示玩家进度数据（等级、经验、现金、总对局、成功撤离、总净收益、上局净收益、总资产）。
- 存档结构升级（当前版本 `v6`）：包含 `PlayerInventory`、`PlayerLoadout`、`PlayerProgress`，并支持仓库物品位置保存。

## 最近更新（流程状态机 + 场景流转）

- 新增全局流程状态机（`GameFlowModel + GameFlowSystem`），统一管理 `StartMenu / LoadingToGame / InRaid / RaidEnded / LoadingToMenu`。
- `GameLaunch` 已接入流程状态切换：启动、进入开始菜单、进入游戏加载、对局内状态都由统一流程驱动。
- 新增 `StartScene` 启动流程：先打开 `HomeWindow`，点击 Start 后进入 `GameScene`。
- 场景切换时新增 `LoadingWindow`，进度条宽度按 `0~1000` 更新，场景加载完成后延迟 `1s` 关闭。
- 新增 `GameOverWindow`：玩家死亡或撤离成功时弹出；显示“撤离成功/撤离失败”和本局收益。
- `GameOverWindow` 与 `InventoryWindow` 一致：弹窗期间显示鼠标并禁用玩家输入。
- `GameOverWindow` 的 Home 按钮支持返回 `StartScene`，并走 Loading 过场（切场景后延迟 1 秒关闭）。
- `UIModule` 已支持 `UIRoot` 跨场景保留，并在 `Initialize()` 时自动把 `UICamera` 重新加入当前主相机 Stack（URP）。
- 修复 `MapSystem` 二次开局不生成玩家问题：流程离开对局后会统一重置 `spawnedPlayer` 等运行时状态，保证下一局可正常生成玩家。
## 最近更新（数据配置 Catalog 化）

- 物品定义已改为 `SOItemCatalog + ItemCatalogEntry`，运行时不再依赖 `SOItemDefinition`。
- 容器定义已改为 `SOContainerCatalog + ContainerCatalogEntry`，运行时不再依赖 `SOContainerConfig`。
- `ItemInstance.Definition` 已切换为 `ItemCatalogEntry`，背包/装备/武器/交互展示链路同步适配。
- 容器解析统一通过 `ContainerConfigId` 从 `SOContainerCatalog` 查找 `ContainerCatalogEntry`。
- 地图场景容器生成与容器模型创建已切换到 `ContainerCatalogEntry`。
### 已完成的功能

#### 架构与启动流程
- 已搭建基于 QFramework 的整体架构，集中注册 Utility / Model / System（`GameArchitecture`）。
- 已实现统一的系统更新调度器 `SystemUpdateScheduler`，由 `GameLaunch.Update()` 驱动所有 `IUpdateSystem`。
- 已接入资源加载入口（YooAsset 封装的 `IResLoader`），并在启动时完成初始化与系统注册。
- 游戏启动后会弹出主 HUD（`GameWindow`），并广播 `EventGameInit` 供各模块初始化。

#### 输入系统
- 已实现 `InputSys` 统一采集输入（移动、视角、开火、瞄准、换弹、切换射击模式、交互、背包等）。
- 支持通过 `SOGameConfig` 配置轴名与按键映射。
- 打开背包时会禁用输入并释放鼠标，关闭背包后恢复。

#### 玩家控制（第一人称）
- 已实现第一人称移动：行走 / 冲刺 / 跳跃 / 重力 / 地面检测（`FirstPersonController`）。
- 已实现第一人称视角控制：Yaw / Pitch、灵敏度、上下俯仰限制。
- 已实现武器影响移动速度（装备倍率、开镜移动倍率）。
- 已实现后坐力驱动的视角扰动与平滑过渡，并支持“玩家压枪”判定与停火回正。
- 已实现开镜时 FOV 平滑过渡与取消逻辑（基于 UniTask + CancellationToken）。

#### 武器系统（装备、切换、瞄准基准）
- 已实现武器槽位模型 `WeaponInventoryModel`（注册、激活、按索引切换、循环切换）。
- 已实现武器实例管理与切换（`WeaponSystem`）：切换时自动调用 `OnUnEquip / OnEquip` 并派发事件。
- 已实现从装备系统生成武器 Loadout（`WeaponSystem.InitializeLoadout` + `PlayerController` 自动刷新）。
- 已实现瞄准射线来源绑定（优先玩家相机，缺失时回退 `Camera.main`）。
- 已实现开火射线与方向计算，并包含“镜头前方阻挡”保护判断。

#### 枪械玩法（射击、后坐力、散布、换弹）
- 已实现三种射击模式：单发 / 三连发 / 全自动（`FirearmWeapon` + `SOFirearmConfig`）。
- 已实现射速限制（`fireRate` 控制下一次开火时间）。
- 已实现弹药系统：当前弹量、换弹流程、换弹完成事件通知（驱动 UI 刷新）。
- 已实现后坐力模式（`recoilPattern`）与后坐力控制系数、倍率区间、分武器抬升/回落速度。
- 已实现散布系统：静止/行走/奔跑/跳跃/开镜散布，连射累积散布与停火恢复。
- 已实现开镜事件链路：开镜时通知相机缩放，并调整移动速度倍率。

#### 命中判定与弹道
- 已实现近距离 hitscan（优势射程内直接 Raycast 命中）。
- 已实现远距离实体子弹模拟（超出优势射程后生成 `GOBullet`）。
- 已实现子弹管理器 `BulletManager`（对象池 + 统一更新 + 回收）。
- 已实现子弹重力下坠与分段 Raycast 防穿透。
- 已实现伤害衰减（距离倍率表 `damageFalloff`）与命中伤害计算。
- 已实现命中特效对象池（impact effect pooling）。

#### 生命与受击反馈
- 已实现 `HealthComponent`：伤害结算、死亡判定、碰撞体关闭、死亡特效与销毁。
- 已打通枪械命中到生命组件的伤害链路。

#### 交互系统（看向交互 + 提示）
- 已实现基于射线的交互系统 `InteractionSystem`（检测目标、变化事件、按键交互）。
- 已实现交互发起端 `InteractorView`（可配置射线起点、距离、层级、Trigger 策略）。
- 已实现交互提示事件 `EventInteractTargetChanged`，HUD 可实时显示提示文本。

#### 物品与背包系统（核心规则）
- 已实现容器模型 `InventoryContainerModel`，包含玩家装备与地图容器加载。
- 已实现网格背包 `InventoryGrid`：放置、移动、移除、旋转、自动寻位。
- 已实现堆叠逻辑：优先堆叠再放置，并支持自动堆叠已有物品。
- 已实现装备系统：穿戴/卸下/互换（武器、防具、头盔、胸挂、背包等槽位规则）。
- 已实现容器嵌套深度限制（防止容器无限套娃）。

#### 拾取、丢弃与场景容器
- 已实现世界物品交互 `WorldItemInteractable`：按键拾取。
- 已实现拾取指令 `CmdPickupItem`：优先自动装备空槽，其次自动放入玩家容器。
- 已实现丢弃指令 `CmdDropItem`：在玩家位置生成可交互掉落物。
- 已实现容器交互 `ContainerInteractable`：可打开场景容器并联动背包 UI。

#### UI（HUD + 背包）
- 已实现主 HUD（`GameWindow`）：显示当前武器名、弹药数、交互提示。
- 已实现背包窗口（`InventoryWindow`）。
- 支持玩家容器与场景容器的同时展示与切换。
- 支持拖拽放置、非法放置回滚、拖出网格丢弃。
- 打开背包时会解锁鼠标并禁用角色输入。

#### 调试与测试入口
- 已在 `GameLaunch` 中提供快捷测试：数字键 0-9 可向指定容器自动放入测试物品。





