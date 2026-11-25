# 项目开发规范

| 领域 | 从这里开始 |
| --- | --- |
| 角色制作与验收 | [从简表到可玩角色](character-production.md) |
| 角色与战斗数据 | [角色配置与回合制](design/character-data.md) |
| 战斗开发与验收 | [运行基线、实现与验证](design/battle.md) |
| 人物与界面美术 | [角色和界面视觉](art/visual-style.md) |
| 技能素材 | [技能图标、动作和特效](art/skill-assets.md) |
| 工程、验证与交付 | [实现和发布流程](engineering/workflow.md) |

[任务开发单](../release-plans/README.md)记录每版优先级、修复和落地状态。[确认设计](../design/confirmed.md)和[决策记录](../design/decisions.md)保留用户原始约定和纠正，按当前生效版本使用。各版本技术说明与制作记录保留在原目录，便于追溯。

## 项目技能

项目技能位于仓库 `.agents/skills`。按当前任务读对应入口。

| 任务 | 技能 | 规则 |
| --- | --- | --- |
| 表格供稿与配置生成 | [dragonidle-character-sheet](../../.agents/skills/dragonidle-character-sheet/SKILL.md) | [角色配表](design/character-data.md#客户端配置生成) |
| 旧资源查引用和恢复 | [dragonidle-resource-archive](../../.agents/skills/dragonidle-resource-archive/SKILL.md) | [Unity 资源归档](engineering/resource-archive.md) |
| 单卡整套素材质检 | [dragonidle-character-art-qa](../../.agents/skills/dragonidle-character-art-qa/SKILL.md) | [素材质检与证据](character-production.md#单卡质检与验收证据) |
| 开发单分工与接手 | [dragonidle-work-handoff](../../.agents/skills/dragonidle-work-handoff/SKILL.md) | [批次分工与交接](engineering/workflow.md#批次分工与交接) |
| 固定种子、隔离档和后台回归 | [dragonidle-release-verify](../../.agents/skills/dragonidle-release-verify/SKILL.md) | [全链路验收](../verification/acceptance.md#工程改动全链路后台回归) |
