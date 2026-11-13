# 龙珠 · 试炼前线 — Unity 技术 Demo
当前版本 **v0.1.1**。个人离线养成游戏的首个战斗核心与基础流程版本。
GitHub：https://github.com/Icein61/dragonball-idle-unity

## 下载与试玩
[下载 Windows x64 Demo](https://github.com/Icein61/dragonball-idle-unity/releases/tag/v0.1.1)。
下载Release附件中的DragonIdle-v0.1.1-Windows-x64.zip，完整解压后运行DragonIdle.exe。
需要Windows，不需要Unity、开发账号或联网。请保留Data目录及附带运行库，不要只复制EXE。

点击“开始挑战”观看自动战斗；胜利后获得资源与装备，再到“养成”“装备”调整搭配。
“编队”中点击两个位置交换角色，可以设置队长；“召唤”抽取测试卡，重复卡转专属资源。
“说明”中领取首胜和离线素材。空格/ESC暂停，提供1/2/4倍演出与自动重复挑战。
进度自动保存在用户Unity persistentDataPath；可通过界面打开存档目录，备份profile-v1.json。

## 当前可玩范围
- 7v7、三前四后、速度行动条、严格同刻阵位顺序、独立怒气必杀。
- 独立状态贡献期限、护盾、治疗、控制、暴击闪避、队长独立乘区。
- 七张初始测试卡与一张可召唤卡、四个逐步开放的试炼。
- 修炼、共鸣、三装备槽、两技能槽、珠子镶嵌、基础任务与离线素材。
- 本地存档/备份、核心自动化测试、Windows打包脚本。

本版为技术切片：画面是临时角色卡和攻击轨迹，尚无正式角色立绘、动作、美术路线或长期数值定稿。
角色名不代表已确定原著阶段；dokkaninfo本次无法读取，没有导入其素材。
完整技能掉落池、分支成长、变身、多波副本、更多任务及音乐/音效将在后续版本扩展。
详见 [版本说明](Releases/v0.1.1.md) 与 [实现范围](docs/engineering/demo-implementation.md)。

## 开发与验证
1. 用Unity Hub打开此目录，使用 **Unity 2022.3.62f3** 与Windows构建模块。
2. 打开Assets/DragonIdle/Scenes/Demo.unity并Play。
3. Windows PowerShell运行 ./Tools/validate-core.ps1 验证纯C#逻辑。
4. 使用Unity菜单 Dragon Idle > Build Windows Demo，或 ./Tools/build-windows.ps1。
5. 提交源码后运行 ./Tools/package-windows.ps1 -VisibleSmoke 重新构建、短暂显示独立测试窗口并打包。
6. Unity路径不同可传 -UnityPath；打包输出在Builds/Releases/v0.1.1。

模拟核心独立于Unity帧率，GitHub Actions只运行核心检查；Unity构建在有编辑器许可证的本机进行。
测试窗口使用独立测试存档，不修改正式存档。

## 换环境接手
先读 [AGENTS.md](AGENTS.md)、[当前状态](docs/STATUS.md)、[确认设计](docs/design/confirmed.md)。
[默认方案](docs/design/defaults.md)可调整，不能冒充用户明确确认；[纠正记录](docs/design/decisions.md)防止恢复旧解释。
- [美术方向](docs/production/art-direction.md)
- [工程与发布约定](docs/engineering/handoff.md)
- [完整目标验收场景](docs/verification/acceptance.md)
- [本版本验证结果](docs/verification/v0.1.1.md)

后续每个可玩版本仍需对应源码提交、标签、版本说明与可运行PC包。不要提交Library或Builds缓存。
