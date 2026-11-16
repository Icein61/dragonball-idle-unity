# 龙珠 · 试炼前线 — Unity 技术 Demo
当前版本 **v0.4.0 角色与图鉴体验预览版**。个人离线养成游戏的战斗核心与角色构筑原型。
GitHub：https://github.com/Icein61/dragonball-idle-unity

## 下载与试玩
[查看 Windows x64 Demo 发布附件](https://github.com/Icein61/dragonball-idle-unity/releases)。
下载对应版本的DragonIdle-v0.4.0-Windows-x64.zip，完整解压后运行DragonIdle.exe。发布完成以Release实际附件为准。
需要Windows，不需要Unity、开发账号或联网。请保留Data目录及附带运行库，不要只复制EXE。

默认进入“人物志”。在“我的角色/全部图鉴”浏览卡面，直接筛选、排序与搜索，查看属性、招式、培养与获取途径。详情页可修炼、共鸣、选择阵位；进入装备工坊后可返回原浏览位置。左下“声音与显示”提供音量、失焦静音与减少动效。原剧情和试炼从“挑战”进入。
“角色”查看25张原型卡；“编队”先选阵位再点候补，可设置队长与保存3组阵容；“召唤”获得新卡，重复卡转该CardId专属资源。
“说明”中领取首胜和离线素材。空格/ESC暂停，提供1/2/4倍演出与自动重复挑战。
音乐和操作音为原创程序合成样曲，音量设置独立保存。
进度自动保存在用户Unity persistentDataPath；可通过界面打开存档目录，备份profile-v1.json。

## 当前可玩范围
- 7v7、三前四后、速度行动条、严格同刻阵位顺序、独立怒气必杀。
- 独立状态贡献期限、护盾、治疗、控制、暴击闪避、队长独立乘区。
- 25张原型卡、10张初始可用；正式uGUI角色页、立绘卡面、直接筛选、排序、长文本详情与实际开战属性预览。
- 3组阵容预设、候补换人、4关独立剧情和4个素材试炼，含首通奖励与重复掉落预览。
- 修炼、共鸣、三装备槽、两技能槽、珠子镶嵌、基础任务与离线素材。
- 背包类别/归属/收藏筛选、三槽装备差值比较、技能残页解锁。
- 本地存档/备份、按CardId迁移、454项核心检查、无图形Player测试与Windows打包脚本。

本版为玩法原型：已接入C风格悟空、比克、拉蒂兹三张AI立绘和短暂特写，其余角色仍为占位；没有正式模型/动作动画或长期数值定稿。
阶段与卡片设计为可调整默认；dokkaninfo无法读取，没有导入其素材。C手绘漫画画风已确认，最终2D/3渲2动作技术路线仍待验证。
本轮只做无窗口、强制静音验证；包含实际uGUI事件、中文文本高度、导航及声音设置存取检查。可见界面、真实操作与样曲试听待验收。角色页以外仍是旧UI。
赛亚人篇14卡已有不同招式组合，专属被动、重复卡分支、变身、多波副本及音效仍待扩展。两卡池保底独立，旧卡与资源保留。
详见 [版本说明](Releases/v0.4.0.md)、[角色图鉴实现](docs/engineering/gallery-v040.md) 与 [立绘来源和提示词](docs/production/saiyan-art-v030.md)。

## 开发与验证
1. 用Unity Hub打开此目录，使用 **Unity 2022.3.62f3** 与Windows构建模块。
2. 打开Assets/DragonIdle/Scenes/Demo.unity并Play。
3. Windows PowerShell运行 ./Tools/validate-core.ps1 验证纯C#逻辑。
4. 使用Unity菜单 Dragon Idle > Build Windows Demo，或 ./Tools/build-windows.ps1。
5. 提交源码后运行 ./Tools/package-windows.ps1，后台重新构建、无图形Player验证并打包。不调用-VisibleSmoke。
6. Unity路径不同可传 -UnityPath；打包输出在Builds/Releases/v0.4.0。

模拟核心独立于Unity帧率，GitHub Actions只运行核心检查；Unity构建在有编辑器许可证的本机进行。
测试窗口使用独立测试存档，不修改正式存档。

## 换环境接手
先读 [AGENTS.md](AGENTS.md)、[当前状态](docs/STATUS.md)、[确认设计](docs/design/confirmed.md)。
[默认方案](docs/design/defaults.md)可调整，不能冒充用户明确确认；[纠正记录](docs/design/decisions.md)防止恢复旧解释。
- [美术方向](docs/production/art-direction.md)
- [工程与发布约定](docs/engineering/handoff.md)
- [完整目标验收场景](docs/verification/acceptance.md)
- [本版本验证结果](docs/verification/v0.4.0.md)

后续每个可玩版本仍需对应源码提交、标签、版本说明与可运行PC包。不要提交Library或Builds缓存。
