# C风格 v0.3 立绘

日期：2026-11-16。使用 imagegen 内置生成工具，C版悟空作为画风参考。用户已选手绘漫画风；本轮是代理制作原型，非逐卡正式定稿。

悟空复用 Art/Candidates/Goku-Raditz-20261116/C-hand-ink-v1.png，未改画。比克对应新的迎战拉蒂兹卡，不能复用为赛亚人袭来时守护悟饭的旧卡。拉蒂兹保留长发、侦测器、尾巴和早期战斗服。

已逐张查看静态图。生成结果保留柔和晕染背景，虽然请求了透明背景，不将其描述为干净抠图；目前只用作卡面与特写，真正战场精灵需另做去背景和动作验证。图片不是3D模型或逐帧动画。原始资源保留1024x1536比例，不强制拉伸为正方形。

原始输出：
- 比克 exec-0d815a61-ff49-4430-bd07-7b0841e87549.png
- 拉蒂兹 exec-5d27f8f3-53bb-4f86-92cb-5d604b2d7ff0.png

项目运行资源位于 Assets/DragonIdle/Resources/Art/，文件以 c-v030 命名，不依赖个人生成目录。未下载或导入dokkaninfo图片。

## 比克提示词

Create a single polished full-body character illustration for a personal Dragon Ball Z game. Use attached approved Goku image ONLY as an art-style reference: controlled variable black ink lines, two-tone cel shading, sparse hand hatching, subtle hand-painted pigment, confident early DBZ manga anatomy. Do not copy Goku's clothes, colors or face. Transparent background, no halo, no scenery, no text or border. All hair, fingers and feet entirely inside canvas with 8% clear margins, portrait 2:3. Clean readable silhouette, three-quarter stance facing viewer-right. Subject: Piccolo at the beginning of DBZ fighting alongside Goku against Raditz. Adult tall green Namekian, pointed ears, two antennae, pink ribbed arm patches, purple sleeveless gi, blue waist sash, brown pointed shoes. No turban, no white cape, no shoulder armor. Both arms intact. One hand with index and middle fingers together close to his forehead preparing Special Beam Cannon; other arm held defensively. Focused serious expression, subtle small violet ki sparks near the two fingertips only. Keep characteristic angular Piccolo face and green skin.

## 拉蒂兹提示词

Create a single polished full-body character illustration for a personal Dragon Ball Z game. Use attached approved Goku image ONLY as an art-style reference: controlled variable black ink lines, two-tone cel shading, sparse hand hatching, subtle hand-painted pigment, confident early DBZ manga anatomy. Do not copy Goku's clothes, colors or face. Transparent background, no halo, no scenery, no text or border. All hair, fingers and feet entirely inside canvas with 8% clear margins, portrait 2:3. Clean readable silhouette, three-quarter stance facing viewer-right. Subject: Raditz, the adult Saiyan arriving on Earth at the beginning of Dragon Ball Z. Tall muscular man with very long unruly black spiked hair reaching below the waist, distinctive widow's peak, confident stern expression. Early Saiyan brown segmented chest and shoulder armor over dark black fitted bodysuit, white armor edging, wrist guards and white boots with brown/gold toe accents. Brown furry tail wrapped around waist. Green scouter lens over his left eye. Both hands open, a compact yellow ki glow in each palm, poised dual-energy attack. No oversized explosion, no gold hair, no cape. Clear anatomically correct hands.
