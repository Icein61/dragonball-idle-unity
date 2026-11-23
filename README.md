# 龙珠爆裂激斗 — Unity 技术 Demo
开发源码 **v0.18.0 九宫格回合制**。战斗数据、飘字、追打和受击动画已更新。[本地 Windows 试玩包](Builds/Releases/v0.18.0/龙珠爆裂激斗-v0.18.0-Windows-x64.zip) · [v0.18.0 发布记录](Releases/v0.18.0.md)。
GitHub：https://github.com/Icein61/dragonball-idle-unity

开发指令与试玩反馈：[v0.18.0 开发单](docs/release-plans/v0.18.0.md) · [v0.17.4 开发单](docs/release-plans/v0.17.4.md) · [v0.17.3 开发单](docs/release-plans/v0.17.3.md) · [v0.17.2 开发单](docs/release-plans/v0.17.2.md) · [v0.17.1 开发单](docs/release-plans/v0.17.1.md) · [v0.17.0 开发单](docs/release-plans/v0.17.0.md) · [v0.16.2 开发单](docs/release-plans/v0.16.2.md) · [v0.16.1 开发单](docs/release-plans/v0.16.1.md) · [v0.16.0 开发单](docs/release-plans/v0.16.0.md) · [v0.15.4 开发单](docs/release-plans/v0.15.4.md) · [v0.15.3 开发单](docs/release-plans/v0.15.3.md) · [v0.15.2 开发单](docs/release-plans/v0.15.2.md) · [v0.15.1 开发单](docs/release-plans/v0.15.1.md) · [v0.15.0 开发单](docs/release-plans/v0.15.0.md) · [v0.14.5 开发单](docs/release-plans/v0.14.5.md) · [v0.14.4 开发单](docs/release-plans/v0.14.4.md) · [v0.14.3 开发单](docs/release-plans/v0.14.3.md) · [v0.14.2 开发单](docs/release-plans/v0.14.2.md) · [v0.14.1 开发单](docs/release-plans/v0.14.1.md) · [v0.14.0 开发单](docs/release-plans/v0.14.0.md) · [v0.13.10 开发单](docs/release-plans/v0.13.10.md) · [v0.13.9 开发单](docs/release-plans/v0.13.9.md) · [v0.13.8 开发单](docs/release-plans/v0.13.8.md) · [v0.13.7 开发单](docs/release-plans/v0.13.7.md) · [v0.13.6 开发单](docs/release-plans/v0.13.6.md) · [v0.13.5 开发单](docs/release-plans/v0.13.5.md) · [v0.13.4 开发单](docs/release-plans/v0.13.4.md) · [v0.13.3 开发单](docs/release-plans/v0.13.3.md) · [v0.13.2 开发单](docs/release-plans/v0.13.2.md) · [v0.13.1 开发单](docs/release-plans/v0.13.1.md) · [v0.13.0 开发单](docs/release-plans/v0.13.0.md) · [v0.12.8 试玩反馈](docs/release-plans/v0.12.8.md) · [全部版本](docs/release-plans/README.md)。

## 下载与试玩
[查看 Windows x64 Demo 发布附件](https://github.com/Icein61/dragonball-idle-unity/releases)。
下载对应版本的 Windows x64 ZIP，完整解压后运行龙珠爆裂激斗.exe。[v0.17.4 GitHub 版本](https://github.com/Icein61/dragonball-idle-unity/releases/tag/v0.17.4)。[参考视频恢复说明](docs/engineering/handoff.md#参考视频的归档与本地清理)。
需要Windows，不需要Unity、开发账号或联网。请保留Data目录及附带运行库，不要只复制EXE。

启动时加载稀有度徽章和八张上线立绘。主页提供角色、编队、战斗 Demo 以及声音与显示设置；存档可从设置中打开目录备份。

- **角色**：浏览悟空、贝吉塔、比克、孙悟饭【幼年】、克林、拉蒂兹、雅木茶和天津饭，按是否拥有分区。新启动默认按序号，游戏期间记住玩家最近选择的排序。
- **编队**：把右侧角色拖入左侧九宫格，上阵一至五人，点击「保存编队」后生效。点击列表卡片可查看属性和技能。
- **战斗 Demo**：己方采用已保存阵位，敌方随机抽五张上线卡并随机站位。可观察真实阵位中的索敌、2D 动作、特效和伤害。能暂停、单步和重开，每次重开切换战场和配乐。[场景概念图](Art/Concepts/Scenes/README.md)
- **回合规则**：开场按阵位编号分配固定手位，我方先，双方交替；退场不重排。角色正常出手为普攻，已触发的技能先处理，随后处理满怒必杀和追打。同时满怒按原手位。
- **旧档**：新九宫格用独立字段保存。旧挑战、养成、装备、召唤暂退，数据保留。

新版[角色设计表](docs/design/character-rework/README.md)收录已上线的八张角色卡。技能和试阵配置按功能存放于 [Config](Assets/DragonIdle/Resources/Config)。细节、默认数值和迁移见[回合制开发说明](docs/engineering/round-combat-v0130.md)。

## 开发与验证
1. 用Unity Hub打开此目录，使用 **Unity 2022.3.62f3** 与Windows构建模块。
2. 打开Assets/DragonIdle/Scenes/Demo.unity并Play。
3. Windows PowerShell运行 ./Tools/validate-core.ps1 验证纯C#逻辑。
4. 使用Unity菜单 龙珠爆裂激斗 > Build Windows Demo，或 ./Tools/build-windows.ps1。
5. 提交源码后运行 ./Tools/package-windows.ps1，后台重新构建、无图形Player验证并打包。不调用-VisibleSmoke。
6. 脚本按锁定版本查找常见Unity安装目录；路径不同可传 -UnityPath 或设置 DRAGON_IDLE_UNITY_PATH。打包输出按VERSION写入Builds/Releases；环境安装与检查见[本地Unity环境](docs/engineering/unity-setup.md)。

模拟核心独立于Unity帧率，GitHub Actions运行核心与资源检查；Unity构建在有编辑器许可证的本机进行。
测试窗口使用独立测试存档，不修改正式存档。

## 换环境接手
先读 [AGENTS.md](AGENTS.md)、[当前状态](docs/STATUS.md)、[确认设计](docs/design/confirmed.md)。
[默认方案](docs/design/defaults.md)可调整，不能冒充用户明确确认；[纠正记录](docs/design/decisions.md)防止恢复旧解释。
- [项目开发规范](docs/standards/README.md)
- [美术方向](docs/production/art-direction.md)
- [工程与发布约定](docs/engineering/handoff.md)
- [完整目标验收场景](docs/verification/acceptance.md)
- [当前验证与交付记录](docs/STATUS.md)

后续每个可玩版本仍需对应源码提交、标签、版本说明与可运行PC包。不要提交Library或Builds缓存。
