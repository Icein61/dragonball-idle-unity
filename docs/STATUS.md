# 当前状态
更新：2026-11-16（Asia/Shanghai）。项目：https://github.com/Icein61/dragonball-idle-unity

## 当前工作
v0.5.1标志性动作立绘版已发布。按用户纠正，全15卡立绘结合当前阶段、代表性招式和定位形成不同动作轮廓，保持C风格。三悟空分龟派气功蓄力/界王拳放波/元气弹举手，两比克分守护/魔贯光杀炮，克林托举气圆斩。

## 资源与接入
新立绘为15张1024×1536透明独立图，位于Assets/DragonIdle/Resources/Art/V051。图鉴、详情、养成与必杀特写接入；战斗小人的V050图集与180姿态保持。完整方案见docs/production/portraits-v051.md，提示词与输出信息在Art/Production/V051/portraits.json。
仍为15卡/8初始、7v7三前四后、19类战斗效果、4种合成战斗音、4关剧情与4关试炼、schema3；本轮不改变卡片身份、队长、关键词、数值或存档。

## 验证与交付
1030项核心检查、15张新立绘分辨率/透明/边缘/来源哈希检查、Unity后台构建与隔离Windows Player均通过。全15张独立立绘绑定、旧图集180姿态、图鉴导航、战斗/存档/迁移均通过；详情见docs/verification/v0.5.1.md。

[v0.5.1 Release](https://github.com/Icein61/dragonball-idle-unity/releases/tag/v0.5.1)已发布预览版：源码/标签3084ed6365f0c35a6b88b8655d4ecb6202b619d7，完整Windows ZIP 106,592,524 bytes；三个附件的远端SHA256均与本地一致。main/tag CI均success。本地启动入口为Builds/Windows/DragonIdle.exe，完整包在Builds/Releases/v0.5.1。

## 真实边界与下一步
静态图检查与无图形运行不等于可见验收。实际卡面缩放、动作观感与试听仍待用户方便时确认；现有小人仍为2D关键姿态动画，未新增3D模型。

## 环境约束
不显示/激活Unity或Player，不调用-VisibleSmoke，不关闭用户窗口；测试静音、隔离存档。Unity2022.3.62f3/Windows x64；独立main/public仓库，不触碰相邻项目。凭据只在系统管理器和进程内存使用。
