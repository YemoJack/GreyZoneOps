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

