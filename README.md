# 龙珠长期养成游戏 · Unity 项目筹备
工作目录代号：dragonball-idle-unity。正式游戏名尚未决定。
规则基线日期：2026-11-13（Asia/Shanghai）。

GitHub仓库：https://github.com/Icein61/dragonball-idle-unity
默认分支：main。克隆后从本文件和AGENTS.md开始接手。

当前阶段：设计规范与工程准备。尚未创建本项目的 Unity 游戏工程、角色素材或可运行 PC 包。本文档不是已经实现的功能清单。

## 新环境从这里开始
1. 阅读 [AGENTS.md](AGENTS.md)，遵守已确认设计和协作授权。
2. 阅读 [确认设计](docs/design/confirmed.md)，不要根据缺失的聊天上下文重猜规则。
3. 阅读 [默认方案](docs/design/defaults.md)，其中参数可调，不能冒充用户明确选择。
4. 阅读 [状态与下一步](docs/STATUS.md) 和 [跨环境接手及交付](docs/engineering/handoff.md)。
5. 开发前检查工作区、分支、未提交修改及 Unity 环境；不要使用相邻其他游戏的文件代替本项目。

## 项目方向
Unity / Windows PC / 16:9 / 离线单机。龙珠角色收藏、队伍构筑、自动时间轴战斗、原著篇章与独立副本、装备和技能及珠子刷取、稀有抽卡、任务与挂机。
画面是核心质量要求：偏好精美手绘感 3 渲 2，也接受高质量 2D；最终路线需要同角色视觉样片和动作验证。

## 文档导航
- [已确认玩法](docs/design/confirmed.md)
- [代理自主采用的默认方案](docs/design/defaults.md)
- [关键决策及已废弃解释](docs/design/decisions.md)
- [美术方向与素材流程](docs/production/art-direction.md)
- [工程、存档与发布约定](docs/engineering/handoff.md)
- [规则验收场景](docs/verification/acceptance.md)
- [当前状态](docs/STATUS.md)

## 版本约定
后续每个可玩交付版本须对应源码提交、同版本标签、更新说明及可运行 Windows x64 ZIP。PC 包通过该项目 GitHub Releases 分发，不把 Unity 缓存或生成的 PC 包提交到源码历史。
本次为文档基线，不应伪造游戏版本、构建结果或 PC Release。
