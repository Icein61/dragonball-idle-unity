# 龙珠爆裂激斗 — Unity 技术 Demo
当前源码 **v0.12.1 角色立绘更新**。个人离线养成游戏的战斗核心与角色构筑原型。发布内容见[本轮记录](Releases/v0.12.1.md)。
GitHub：https://github.com/Icein61/dragonball-idle-unity

## 下载与试玩
[查看 Windows x64 Demo 发布附件](https://github.com/Icein61/dragonball-idle-unity/releases)。
下载对应版本的龙珠爆裂激斗-v0.12.1-Windows-x64.zip，完整解压后运行龙珠爆裂激斗.exe。
需要Windows，不需要Unity、开发账号或联网。请保留Data目录及附带运行库，不要只复制EXE。

启动时用聚气环显示徽章与角色立绘的实际异步加载进度。主页左侧显示修炼进度与通关状态，右侧提供剧情、奖励和设置。底栏进入人物志、编队、养成、装备和召唤。原剧情和试炼也可从“挑战”进入。
在人物志“我的角色/全部图鉴”浏览卡面，直接筛选、排序与搜索，查看属性、招式、培养与获取途径。详情页可修炼、共鸣、选择阵位；进入装备工坊后可返回原浏览位置。声音设置提供音量、失焦静音与减少动效。
“角色”查看9张已配置卡；“编队”先选阵位再点候补，可设置队长与保存3组阵容；“召唤”获得新卡，重复卡转该CardId专属资源。
在主页领取首胜奖励和离线素材，在“声音与显示”里打开存档目录。战斗时按空格或 Esc 暂停，可切换 1/2/4 倍演出与自动重复挑战。
音乐和操作音为原创程序合成样曲，音量设置独立保存。
进度自动保存在用户Unity persistentDataPath；可通过界面打开存档目录，备份profile-v1.json。

## 当前可玩范围
- 每队最多六人、前三后三、速度行动条、严格同刻阵位顺序、独立怒气必杀。
- 独立状态贡献期限、护盾、治疗、控制、暴击闪避、队长独立乘区。
- 9张已配置卡、8张初始可用；正式uGUI角色页、立绘卡面、直接筛选、排序、长文本详情与实际开战属性预览。
- 3组阵容预设、候补换人、4关独立剧情和4个素材试炼，含首通奖励与重复掉落预览。
- 修炼、共鸣、三装备槽、两技能槽、珠子镶嵌、基础任务与离线素材。
- 背包类别/归属/收藏筛选、三槽装备差值比较、技能残页解锁。
- 本地存档/备份、按CardId迁移、811项核心检查、无图形Player测试与Windows打包脚本。
- 主页和战斗使用雾白、墨青和暖金的原创组件。启动加载显示实际请求进度。

全9卡已接入按阶段/招式区分姿势的C风格1024×1536独立立绘，以及每卡12姿态的2D小人（共108姿态），按事件播放攻击、蓄气、受击、闪避、格挡和倒地。19类招式特效、4种合成战斗音已接入。当前是关键姿态动画，没有3D模型，长期数值尚未定稿。
游戏名为“龙珠爆裂激斗”，Windows图标为庄严正面悟空与龟仙人小岛；按用户给出的角色图参考修订，原始站点图片不作为运行资源。改名保持旧存档和声音设置目录兼容。动作节奏区分蓄力、飞行、受击与回位，仍使用原12姿态。
队长标签必须属于本人且允许单标签；定位关键词及逐卡数值为可调整默认；dokkaninfo无法读取，没有导入其素材。C手绘漫画画风已确认，本版采用2D实现，不代表放弃3渲2偏好。
后台强制静音验证覆盖真实资源请求、uGUI 事件、中文文本高度、导航、聚气数据、召唤保底和声音设置。原创风格现用于启动页、主页、战斗和五个功能页签。可见效果与操作体验待验收。
已配置卡使用各自的招式组合，专属被动、重复卡分支、变身、多波副本与最终配乐仍待扩展。两卡池保底独立，已删除非赛亚人篇可玩卡；旧成长归档、装备退回背包、队伍自动补齐。
详见 [版本说明](Releases/v0.12.0.md)、[角色图鉴实现](docs/engineering/gallery-v040.md)、[专属立绘来源和提示词](docs/production/portraits-v051.md)与[游戏标识](docs/production/branding-v052.md)。

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
- [美术方向](docs/production/art-direction.md)
- [工程与发布约定](docs/engineering/handoff.md)
- [完整目标验收场景](docs/verification/acceptance.md)
- [当前验证与交付记录](docs/STATUS.md)

后续每个可玩版本仍需对应源码提交、标签、版本说明与可运行PC包。不要提交Library或Builds缓存。

新版简表中的九张上线卡均已接入实战。图鉴展示表内技能原文和实战数值，悟空与贝吉塔已换为确认后的手绘立绘。可见体验待验收。

角色页已升级暖白圆角主题、四列大卡与独立详情屏，当前有效属性可展开拆解。返回保留浏览状态及键盘焦点；筛选可单独清除，选卡不重建列表。第二阶段编队、养成、装备也已接入，角色上下文与装配比较见docs/engineering/workshop-v061.md。详见docs/engineering/gallery-v060.md。可见观感与试听仍未验收。

