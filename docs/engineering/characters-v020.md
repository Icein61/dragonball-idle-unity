# v0.2.0角色系统实现

角色定义位于CharacterCatalog.cs；CardId是永久主键，PersonId/Stage/Form/Title/Rarity/Release独立记录。身份字段只读，Tags返回副本。不是人物+阶段联合唯一键。

Profile仍用数组供轻量界面访问，但schema2显式保存CardIds索引表。Normalize先按该表重排Training/Shards/Resonance/Policy/Owned/Skill/Skill2，再重排Team和Items.owner。schema1按固定LegacyCardIds解释。预设直接存字符串CardId。

旧存档文件名继续为profile-v1.json，避免另建文件导致看似丢档；内部schemaVersion为2。迁移前另存带时间戳的.pre-v020-*.bak，正常保存另有上一版.bak。未知角色ID、损坏数组、非法技能或重复装备槽拒绝读取并保留原文件，不静默删除数据。正式存档没有在自动测试中被访问。

角色被动在战斗快照初始化一次；开场怒气是明确例外。队长按精确身份标签给各成员注册独立LeaderAttack。阵容编辑和预设仅在非战斗时开放，离开挑战页取消自动重复，避免在构筑中自动开战。

图鉴支持名字/称号/阶段/形态/标签搜索、拥有情况、稀有度、职责和标签筛选。技能描述说明倍率、冷却、怒气和独立期限；原型机制还不是通用技能效果编辑器。

背包支持类别、归属、收藏筛选与三槽分别比较；没有分解操作。技能残页解锁是全局技能库，非每卡独立消耗副本；多余残页保留。

无图形Player验证入口为-logicSmoke，强制独立-savePath；Tools/smoke-windows.ps1传-batchmode -nographics。验证实际JsonUtility往返、旧JSON迁移、独立备份、战斗结算和资源装载。它不验证界面排版、可见交互、动画或截图。

尚未完成：逐卡专属被动与重复卡分支、正式完整角色美术、动画、音效、多波关卡、长期经济调校、全套装备预设。现有共鸣仍为小幅属性成长。
