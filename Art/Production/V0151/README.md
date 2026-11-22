# v0.15.1 悟饭素材与贝吉塔图标

悟饭各姿态参考本机 0004 待机、移动和四项技能的多机位视频。立绘、动作关键姿态和图标以本项目 C 版立绘作为画风参考，使用 imagegen 生成。Unity 逐帧姿态按脚底和出手点对齐，技能特效由游戏绘制。

| 素材 | 简表参考 | 表现 | 交付 |
| --- | --- | --- | --- |
| 0004 孙悟饭【幼年】 | 角色设计!A49、E52 | 振奋的收拳站姿，紫色服装、白围巾与红腰带 | [PNG](../../../Assets/DragonIdle/Resources/Art/V0151/Gohan/portrait.png) |
| 0004 战斗姿态 | 角色设计!H50:H56 | 待机、入场、两帧跑步、振奋、重击、魔闪击、收招和回跳 | [PNG](../../../Assets/DragonIdle/Resources/Art/V0151/Gohan/combat-poses.png) |
| 0004 受击姿态 | 角色设计!E57、H57 | 交叉护臂、站立受击、小浮空、大浮空、倒地和退场 | [PNG](../../../Assets/DragonIdle/Resources/Art/V0151/Gohan/reaction-poses.png) |
| 0004 技能图标 | 角色设计!E54:E57 | 薄荷色振奋、重击出拳、暖金魔闪击、蓝色超级防御 | [PNG](../../../Assets/DragonIdle/Resources/Art/V0151/Gohan/skill-icons.png) |
| 0002 连续能源弹 | 角色设计!E27 | 交替出手和分层的淡黄色弹道 | [PNG](../../../Assets/DragonIdle/Resources/Art/V0151/Vegeta/continuous-energy-volley.png) |
| 0002 赛亚精英 | 角色设计!E29 | 以成人比例的自信神态、白金护甲和叉腰姿态呈现 | [PNG](../../../Assets/DragonIdle/Resources/Art/V0151/Vegeta/saiyan-elite.png) |

[动作时序与锚点](../../../Assets/DragonIdle/Resources/Art/V0151/Gohan/Motion.json) · [运行技能图标配置](../../../Assets/DragonIdle/Resources/Config/Characters/RoundKits.json)

## 语音和音效

所有新增音轨来自指定的本机赛亚人篇角色音频素材库。魔闪击采用同名素材的专用语音与效果。振奋选用短战斗喊声和通用蓄气片段，重击选出拳声音与命中音，超级防御选择格挡音。

| 事件 | 源文件 | 时长 | SHA-256 |
| --- | --- | --- | --- |
| gohan-entry | 孙悟饭（幼年）-攻击-内嵌0005.wav | 0.595 秒 | `1dea8d4b4d15e059606cc23b0246c3924c36ad14348cfeef318ae06b1cd805ca` |
| gohan-rally-voice | 孙悟饭（幼年）-战斗语音-内嵌0024.wav | 0.751 秒 | `b8e3c2773480fc16985e5dc968f3ff7b1a2a5dc42432a8afc7bade622a0f0b30` |
| gohan-rally | 通用-蓄气-内嵌0110.wav | 1.183 秒 | `61ee99f3ec581b86485ada229a0c34d26635600f0ebdc1ec5a66900f3fd05be7` |
| gohan-heavy-voice | 孙悟饭（幼年）-攻击-内嵌0013.wav | 0.641 秒 | `8596806dcd1b69eac0dcd06d8fece178ca8a36b7203e7252a66c06a4f8d9195e` |
| gohan-heavy | 通用-命中-内嵌0043.wav | 0.700 秒 | `7cb615570021c74365ae197ce4d385fb4cf6b69c4512b1dd7fcee0325befb3a0` |
| gohan-flash-voice | 孙悟饭（幼年）-魔闪击-内嵌0001.wav | 0.859 秒 | `d499e0f79d8ff919cc0985c85ba9b314de36c5c44e98cce1271a082e503fcce5` |
| gohan-flash | 魔闪击-音效-内嵌0001.wav | 1.448 秒 | `381312f52236c1e3db6268d9e9b4182b8371fce83c59bc00f0f90854f0210dbe` |
| gohan-guard | 通用-格挡-内嵌0134.wav | 1.167 秒 | `c9d85e7e98919adf14b518ebab64a31fbcf3c18231afb39ee198497fa66cae05` |
| gohan-hit-light | 孙悟饭（幼年）-轻受击-内嵌0027.wav | 0.597 秒 | `8da18be0514c7a2e0aa1296ab5d22b7a414076bb2a3edaf18b7f1087a3d70397` |
| gohan-hit-heavy | 孙悟饭（幼年）-重受击-内嵌0031.wav | 0.808 秒 | `a447081a9b193502f20bda3792dcf71f934b60b4f6b151cbfdfd234731939f43` |

入场落地复用已在项目使用的同库通用落地音。[音频事件配置](../../../Assets/DragonIdle/Resources/Config/Characters/GohanAudio.json)
