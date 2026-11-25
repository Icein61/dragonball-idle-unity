# 悟空首轮视觉样片

日期：2026-11-16。状态：AI 生成原型，未确定最终 2D / 3渲2 路线。

- 工具：内置 image_gen（非 CLI / API）。
- v0.19.0 归档位置见[悟空旧试制图](../../Art/Archive/V0190Resources/Resources/Art/goku-saiyan-sample-v1.png)，原路径为 `Assets/DragonIdle/Resources/Art/goku-saiyan-sample-v1.png`。
- 尺寸：1024 × 1536；PNG 带 alpha，抽样透明度 0–254，原文件未经后处理。
- 用途：goku-test 的图鉴详情与养成页；其他 19 张原型卡仍使用占位表现。
- 已目视检查：黑发常态悟空、橙蓝武道服、全身战斗姿势。只验静态样片，不代表已完成模型、绑定、动画或游戏内画面验收。
- 构图中有轻微背景光晕、脚部边距偏小；正式卡面应继续精修，不将此图当成生产用动作拆件。
- dokkaninfo 在本机仍返回 403；本轮没有下载其卡图。
- 可访问的文字与阶段参考：[龙珠官方悟饭人物介绍](https://en.dragon-ball-official.com/news/01_503.html)、[龙珠官方悟空人物介绍](https://en.dragon-ball-official.com/news/01_478.html)。这些页面用于人物与阶段资料核对；未将上游漫画图片直接打包，也没有冒充悟空赛亚人篇逐帧形象参考。

## 完整生成提示词

```text
Use case: stylized-concept. Asset type: transparent character illustration for a personal Dragon Ball Unity game, first visual sample, not a finished 3D model. Primary request: Son Goku, adult base form in the Saiyan saga, recognizable spiky black hair, orange martial arts gi with dark blue undershirt, blue wristbands and belt, dark blue boots. Full body entirely inside frame with clear margins, three-quarter view facing right, grounded confident combat stance, hands ready for martial arts, five fingers, natural anatomy. Style/medium: premium hand-drawn Japanese battle-card illustration inspired by Dragon Ball Z Dokkan Battle: expressive sharp ink contours, crisp two-tone cel shading, meticulously drawn face and hands, strong silhouette, restrained highlights, no photorealism or plastic 3D look. Isolated character only, transparent background with real alpha, no scenery, no card frame, no UI, no text, no watermark, no extra characters, no large aura hiding body. Portrait composition.
```
