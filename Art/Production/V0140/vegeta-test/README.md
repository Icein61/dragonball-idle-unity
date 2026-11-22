# 0002 贝吉塔制作记录

## 设计与素材对应

| 招式 | 角色设计 | 本版演出 |
| --- | --- | --- |
| 格斗攻击 | C23、E23、H23 | 趋近后两次拳击动作。击退触发时追加末踢，结算对应最后的接触帧。 |
| 连续能源弹 | C24、E24 | 起手聚能，六发分次从掌心射出黄白弹体。每发使用实际敌阵目标与独立结算。 |
| 大锤 | C25、E25 | 接大浮空，双手上举合拳并下砸，触发倒地。 |
| 赛亚精英 | C26、E26、H26 | 叉腰冷哼，入场金光提示后恢复战斗姿势。 |

设计来源为 [character.xlsx](../../../../outputs/character/character.xlsx) 的「角色设计」。表内 0002贝吉塔 对应稳定运行 CardId `vegeta-test`。

核对本机参考视频 `0002贝吉塔待机.mp4`、`0002贝吉塔格斗攻击.mp4`、`0002贝吉塔大锤.mp4`、`0002贝吉塔连续能源波侧面视角.mp4` 和 `0002贝吉塔连续能源波正面视角.mp4`。用于确认警戒、拳脚接触、合拳预备及速射的动画节奏。视频画面留在本地参考目录。

## 运行资源

- [立绘](../../../../Assets/DragonIdle/Resources/Art/V0140/Vegeta/portrait.png)沿用原卡已审造型、伸掌构图和红色观察镜，加入暖黄色蓄能球。
- [技能图标](../../../../Assets/DragonIdle/Resources/Art/V0140/Vegeta/skill-icons.png)为格斗、六发能源弹、大锤、赛亚精英的四格图集。裁切由 [运行技能表](../../../../Assets/DragonIdle/Resources/Config/Characters/RoundKits.json)绑定。
- [叉腰与大锤姿势](../../../../Assets/DragonIdle/Resources/Art/V0140/Vegeta/opening-hammer.png)与项目已有战斗图集组合。动作节拍、脚底基准、掌心和命中锚点见 [Motion.json](../../../../Assets/DragonIdle/Resources/Art/V0140/Vegeta/Motion.json)。
- [音频配置](../../../../Assets/DragonIdle/Resources/Config/Characters/VegetaAudio.json)接入指定《Dragon Ball Xenoverse 2》本机音频目录的 9 条现成 WAV。逐条来源与哈希见 `.validation/v0140-vegeta-audio.json`。首条发射位于 350 毫秒，后续间隔 145 毫秒，每发的飞行时间为 260 毫秒。

## 内置图像生成提示

以下使用内置图像生成工具。立绘和补充姿势以本项目已有绘图为底稿。技能图标采用原创物件构图。

### 技能源图

> Use case: stylized-concept. Create a SINGLE square sprite atlas for an original martial-arts strategy game, with four distinct original illustrated skill icon tiles in a precise 2x2 grid. All four tiles are exactly equal square and contained in their own quadrant, with a transparent moat about 4% wide at the edges and between each tile. Top left: white padded glove throwing a punch with a diagonal kick motion slash behind, primarily indigo and warm yellow. Top right: six clearly separate golden energy orbs streaking right through deep ink-teal, leading orb slightly larger, energetic speed lines, no beam. Bottom left: two gauntleted fists clasped together in a downward hammer strike, jagged cream impact and a tight downward burst. Bottom right: abstract angular royal armor crest using an antique gold breastplate and paired triangular pauldrons on ink-teal with restrained warm halo, no face. All icons are original objects and compositions with no character portraits, no references to any other game or existing intellectual property. Hand-inked manga brush outlines, controlled cel-shaded highlights, coherent dark ink-teal field inside each tile, fine antique-gold ridged corners and subtle rounded square shape. Highly readable and simple at 32px. No text, letters, numbers, logos, watermark, backgrounds outside tiles, or particles crossing the gutters. Square overall canvas with genuine alpha and 2x2 exactly.

### 能源弹图标修订

> Edit the just-created skill icon atlas, preserving its original size, transparency, 2x2 placement, gutters, frames, all other artwork and colors. Change ONLY the top right tile. Remove the smallest bottom-left energy orb within that tile and its little tail; fill that local gap seamlessly with the dark ink-teal speed-line background. Verify top-right contains exactly six orbs total: one large lead orb, three small above, and two small below. Keep each of the other three entire tiles unchanged. Background outside tiles remains genuinely transparent.

### 叉腰和大锤姿势

> Use case: identity-preserve adaptation of the supplied project sprite sheet. The reference is existing project-owned illustration. Make a NEW transparent three-panel animation atlas of three distinct full-body key poses derived from this illustration for its current Unity project. Preserve the same hand-inked line weight, existing project's costume, proportions, scale and right-facing three-quarter view; no other game footage or artwork is a reference. Horizontal canvas with three equally wide columns separated by wide transparent gutters. Left pose: upright and proud, both white-gloved hands resting clearly on the hips with elbows out, chin slightly up, severe confident closed expression, planted feet, a calm pre-fight moment. Middle pose: double-fist hammer preparation, legs slightly crouched, both arms visibly raised overhead, hands clasped together forming one double-fist hammer; wrists aligned anatomically. Right pose: same double-fist attack in the downstroke contact position, both hands visibly clasped down and forward, torso leaning forward, knees bent, head to the viewer's right. Each is a complete silhouette from hair to boots, no overlaps between columns. No other character, no floor, no visual effects, no captions, no panel boxes, no scene or shadows, transparent all around figures. Disciplined human anatomy and correctly joined hands. These are original newly drawn pose adaptations for the supplied project's own atlas.

### 立绘精修

> Use case: precise-object-edit of the supplied existing project illustration for a vertical 2:3 hero card. Preserve the supplied project's own bold manga inking, hand anatomy, confident face and side visor, hair, costume, feet, white glove and the current one-hand-forward fighting silhouette. Add one compact but forceful warm ivory-yellow energy sphere just beyond the extended palm, with a hard bright core, thin directional gold filaments, and tapered speed slashes curling around the fingers. Do not cover the face or hide the anatomy. Refine the faint perimeter glow to warm cream with a restrained ink-teal edge accent. Keep the rest of the figure and composition unchanged and entirely inside the canvas with comfortable margins including both boots and the energy sphere. Genuine transparent background, no terrain or panel, no opaque backdrop, no text or markings. This is an original refinement of the supplied project's own portrait, with no external game imagery.

