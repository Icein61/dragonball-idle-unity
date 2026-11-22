# v0.15.2 角色页素材

位图使用内置 imagegen，以项目原运行素材作参考。原图版本保留。运行配置见 [回合技能表](../../../Assets/DragonIdle/Resources/Config/Characters/RoundKits.json) 和 [悟饭动作轨道](../../../Assets/DragonIdle/Resources/Art/V0152/Gohan/Motion.json)。

| 素材 | 调整 |
| --- | --- |
| [比克立绘](../../../Assets/DragonIdle/Resources/Art/V0152/Piccolo/portrait.png) | 去除光束、螺旋、闪光与外缘光效，保留手势和站姿。 |
| [贝吉塔连续能源弹](../../../Assets/DragonIdle/Resources/Art/V0152/Vegeta/continuous-energy-volley.png) | 上、下手掌的拇指分别朝上和下，保留交替发弹辨识点。 |
| [悟饭重击蓄力](../../../Assets/DragonIdle/Resources/Art/V0152/Gohan/heavy-windup.png) | 两张收拳姿态共用脚底基线，接入原有跑步、出拳和回位。 |

## 角色页音乐

使用用户指定试听目录的《角色与大厅选择-循环版.wav》。源文件为 48 kHz 双声道，长 89.984 秒。按现有页面音乐组件循环播放，以 Streaming 导入。列表和详情共用同一播放状态。

[音频运行资源](../../../Assets/DragonIdle/Resources/Audio/V0152/BGM/character-selection-loop.wav) · SHA-256 `1c177e2f33db16eca4706f907a8aa6ae7fe73edb8de00379911ad2440e164a0e`

## 绘图提示词

### 比克立绘

Use case: precise-object-edit. Edit the attached original portrait from the user's Unity project. Remove the complete magic beam to the right of the fingers, coil, yellow spark, orange reflections caused by the beam, and any soft ambient fog or colored aura outside the body. Finish the outline of the two fingertips behind the old spark so nothing clips. Preserve the character's actual pose and existing framing closely: green muscular fighter with two antennae, both feet planted wide, shouting, extended arm level to the right, two parallel extended fingers, other hand gripping that forearm for support, purple gi, blue sash and wrist wraps, golden boots. Preserve silhouette, face, anatomical details, cel shadows, colors, original ink contours, height and proportions. Full body, centered horizontally and vertically with modest safe margins, graphic occupies about 89 percent of final vertical canvas so he sits at same scale as other portraits. Real RGBA transparency everywhere outside character. Isolated figure only, no laser, trail, particles, energy, weapon, background or ground shadow. Portrait 2:3 ratio, no text.

### 贝吉塔连续能源弹首轮

Use case: precise-object-edit. Redraw the user's attached Unity game icon with a crucial correction: anatomically opposite left and right human hands, each connected to its own forearm and sleeve. The two gloved hands are the focal point of two sequential energy shots flying left. Use clearly different rotation deliberately. The upper, farther hand is a five-digit hand seen mostly from the knuckle/back side in a three-quarter side view, thumb on the upper inward edge, four fingers pointing forward in profile. The lower, nearer hand is the other five-digit hand, with palm pad and wrist creases toward the viewer, thumb clearly below the other fingers and angled toward the lower-left. Upper and lower thumbs must be on opposite sides and upper knuckle plane clearly different from lower palm; no duplicated anatomy, no duplicated gloves, no extra hands. Fingers naturally bent in active releases. Link both wrists through separate navy sleeves to the hints of the original white-and-gold armored chest at right. Keep the project's hand-drawn cel comic style, dark teal backdrop and original small-radius gold frame. Distinct warm pale-yellow spherical projectiles spreading toward the LEFT in a stepped, staggered alternating stream, wings of light trailing back toward the hands. Square 1:1 illustration, no text, true transparent outer corners only.

### 贝吉塔左手修订

Use case: precise-object-edit. Correct only the UPPER hand in the attached user-owned square Unity icon. The LOWER white-gloved hand stays exactly as is, with its PALM and palm creases visible and the lower-left pointing thumb below its four other digits. The UPPER hand must be the opposite anatomical hand. Redraw it with its PALM FACING THE VIEWER too, never its back or knuckles. Both wrists are on the right and fingers are on the left. On the upper glove, the small broad fleshy base of the THUMB and its single extension are clearly ABOVE the other four digits, aimed up-left. The four other fingers emerge as a row sloping down-left: index nearest thumb, then middle, ring, little as lowest. Draw real curved lifeline and palm creases on the upper glove proving its palm side. This must look like a left palm above a right palm, each viewed from the palm side. The upper thumb is unmistakably short, with one bend. Give the upper hand a somewhat receded oblique angle and turn its fingers leftwards to separate the two silhouettes. Keep the frame, exact palette, armor, both sleeves, lower hand and all six pale gold leftward shots. Keep square crop and transparent external corners. No text.

### 悟饭重击蓄力

Use case: stylized-concept for a user-owned Unity project. Create one transparent two-frame horizontal animation strip for the small martial artist in the reference sprite sheet. Match the same kid identity, head-to-body proportions, messy black hair shape, purple gi, folded white neck scarf, red sash and red wristbands, mustard boots, hand-drawn black contours and two-tone cel shadow. These are two consecutive coiled anticipation poses for a right-facing grounded straight power punch, not another character, no layout frames. Left cell: readable three-quarter side view, its front boot right and back boot left, hips drop slightly and weight loads over the back foot. Right punching fist pulls back beside the right hip on the viewer's left, low lead hand aims forward, eyes fixed to screen right. Right cell: same angle, same visible face and exact ground contact, slightly deeper coil, shoulder rolls back further with tight fist at rear hip and front arm raised nearer cheek; right fist still pulled back, not punching yet. Both whole bodies including boots, two clearly separated equal-width columns, each centered on same horizontal ground line with at least 8% transparent gutter and margins. Pose at 3/4 side close to the right-facing punch in the reference, natural knee and arm anatomy. Clean transparent RGBA, no rays, aura, glow, lettering or shadows spilling outside body, no additional sprites. Landscape 3:2.
