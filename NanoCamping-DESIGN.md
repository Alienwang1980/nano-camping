# Nano Camping — MVP 设计文档

> 项目路径: `/Volumes/WD_BLACK/Claude/projects/Nano_camping/Nano Camping/`
> 引擎: Tuanjie 1.x (URP 14.2.0-t1, Unity 2022.3.62t9)
> 视觉: 低多边形 (Low-Poly),5 个 Asset Store 包风格一致

---

## 1. 玩法定位

**一句话**: 在低多边形海滩 / 森林场景里,捡资源、搭营地、生火做饭、睡帐篷过夜,自由探索。

**目标受众**: 单机休闲玩家 / 移动端触屏玩家(项目用 Tuanjie 模板,默认倾向 iOS / Android)。
**视角**: 第三人称(默认),WASD 移动 + 鼠标拖拽转视角 + 滚轮缩放。

**核心循环**:
```
探索 → 捡拾木材 / 石头 / 食物 → 打开制作面板 → 合成 / 放置 → 篝火做饭 → 回血 / 饱腹 → 帐篷睡觉 → 跳到第二天 → 重复
```

---

## 2. 场景 (MVP 1 张主场景)

**主场景**: `Assets/Scenes/Camping.unity`

**地形分层**:
- 中心: 海滩沙地 (TooMoose: beach 区,放营地)
- 边缘: 森林 (FreeDimensionForest: 树 / 蘑菇 / 桥)
- 远景: 天空盒 + 山 + 海(TooMoose 云 + 远处地形)
- 装饰: 散落的木材 / 石头(拾取点)

**预设物件**:
- `Player`(Capsule + 第三人相机 + PlayerController)
- `Directional Light`(URP 主光,绑 TimeOfDay)
- `Ground`(大地形 collider)
- 10–20 个 `ItemPickup`(木头 / 石头 / 食物,散落)
- 1 个 `Campfire`(未点燃状态)
- 1 个 `Tent`(可进入睡觉)

---

## 3. 核心系统 (C# 脚本)

| 脚本 | 文件 | 职责 |
|------|------|------|
| `PlayerController.cs` | `Assets/Scripts/Player/` | WASD 移动 + 跳跃 + 鼠标视角 |
| `PlayerCamera.cs` | `Assets/Scripts/Player/` | 第三人跟随相机 |
| `Interactor.cs` | `Assets/Scripts/Player/` | E 键交互检测(拾取 / 使用 / 进入) |
| `Inventory.cs` | `Assets/Scripts/Inventory/` | 物品数据 + 增删查 |
| `InventoryUI.cs` | `Assets/Scripts/Inventory/` | IMGUI / UGUI 简单背包界面 |
| `ItemPickup.cs` | `Assets/Scripts/World/` | 场景物品(可拾取,有 ItemData) |
| `PlaceableItem.cs` | `Assets/Scripts/World/` | 抽象基类(可放置物品) |
| `Campfire.cs` | `Assets/Scripts/World/` | 篝火:点燃 / 烹饪 / 熄灭 |
| `Tent.cs` | `Assets/Scripts/World/` | 帐篷:进入 → 跳过夜晚 |
| `TimeOfDay.cs` | `Assets/Scripts/World/` | 全局昼夜循环,绑光照颜色 + 强度 |
| `Crafting.cs` | `Assets/Scripts/Crafting/` | 配方表 + 制作逻辑 |
| `GameBootstrap.cs` | `Assets/Scripts/` | 启动时构造单例 / 注册物品 |

---

## 4. 物品清单 (MVP 12 个)

| ID | 名称 | 类型 | 用途 | 来源 |
|----|------|------|------|------|
| `wood` | 木头 | 资源 | 燃料 / 制作 | FreeDimensionForest (ForestTreeTrunk) |
| `stone` | 石头 | 资源 | 制作 | TooMoose (Rock) |
| `food_raw` | 生肉 | 食物 | 烤熟后回血 | Supercyan (MobileSardineCanClosed) |
| `food_cooked` | 烤肉 | 食物 | 直接回血 | 篝火烹饪产物 |
| `campfire_kit` | 篝火套件 | 工具 | 放置篝火 | Harpia / 合成 (3 wood + 1 stone) |
| `tent_kit` | 帐篷套件 | 工具 | 放置帐篷 | Harpia Tent prefab / 合成 (5 wood) |
| `axe` | 斧头 | 工具 | 砍树效率↑ | Harpia Tool 风格 |
| `water_bottle` | 水壶 | 消耗 | 解渴 | Supercyan (MobileThermos) |
| `mushroom` | 蘑菇 | 食物 | 可生吃可烤 | FreeDimensionForest (ForestMushroom) |
| `guitar` | 吉他 | 装饰 | 弹奏动画 | Harpia (Guitar acustic) |
| `backpack` | 背包 | 装饰 | 扩容 | Harpia (Backpack blue) |
| `sleeping_bag` | 睡袋 | 工具 | 野外睡觉 | Harpia (Sleeping Bag) |

---

## 5. 制作配方 (MVP 5 个)

| 产物 | 材料 | 工具 |
|------|------|------|
| 篝火套件 | 3 wood + 1 stone | 无 |
| 帐篷套件 | 5 wood | 无 |
| 烤肉 | 1 food_raw | 篝火 (3s) |
| 斧头 | 2 wood + 2 stone | 无 |
| 烤蘑菇 | 1 mushroom | 篝火 (2s) |

---

## 6. UI (MVP 最小集)

- **HUD 左上**: 生命值 / 饱腹度 / 时间(`TimeOfDay` 当前小时)
- **HUD 中下**: 交互提示(`[E] 拾取 木头`)
- **按 I 打开背包**: 4×4 格子,鼠标拖拽
- **按 C 打开制作**: 列表 + 材料提示
- **按 Esc 关闭 UI**

UI 用 UGUI(Canvas + Text/TMP),不引第三方 UI 框架,降低体积。

---

## 7. 输入映射

| 操作 | 键位 (PC) | 触屏 (后续) |
|------|----------|------------|
| 移动 | WASD | 摇杆 |
| 视角 | 鼠标拖拽 | 滑动 |
| 拾取 / 交互 | E | 按钮 |
| 打开背包 | I | 按钮 |
| 打开制作 | C | 按钮 |
| 跳跃 | Space | 按钮 |
| 切换放置预览 | Q | 按钮 |

---

## 8. 昼夜循环

- 1 游戏日 = 现实 5 分钟(可调)
- 06:00–18:00: 白天 (光照强度 1.0,色温偏黄)
- 18:00–20:00: 黄昏 (强度 0.6,色温偏橙)
- 20:00–06:00: 夜晚 (强度 0.2,色温偏蓝,篝火照明可见)
- `TimeOfDay` 每帧更新 `RenderSettings.ambientLight` + `Directional Light` 颜色

---

## 9. 不在 MVP 范围

- 多人联机 / 保存 / 加载
- 敌人 / 战斗 / 死亡
- 任务 / 剧情
- 声音系统(后期单独加)
- 完整移动端触屏适配(后期单独做)
- 复杂模型替换 / 动画过渡(用现有 prefab 即可)

---

## 10. 验证标准 (MVP 完成)

- [ ] 场景能进 Play 模式不报错
- [ ] WASD 移动 + 鼠标视角正常
- [ ] 走到木头前按 E 拾取 → 背包 +1
- [ ] 按 I 打开背包看得到物品
- [ ] 打开制作 → 篝火套件 → 消耗材料 → 获得 kit
- [ ] 选篝火 kit → 预览放置 → 左键放置 → 篝火出现
- [ ] 走近篝火 → 按 E 点燃 → 火光变化
- [ ] 把生肉丢进篝火 → 3 秒后变烤肉
- [ ] 帐篷前按 E → 时间跳到次日 06:00
- [ ] 时间从白天跑到夜晚 → 光照变暗
- [ ] `Window > MCP Dashboard` 显示无报错
