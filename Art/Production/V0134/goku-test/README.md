# 0001Goku v0.13.4 音频来源与留白

0001Goku 对应运行卡 `goku-test`。已检查 `Assets/DragonIdle/Resources/Videos` 的五段参考录屏。每段各有一条双声道音轨，未找到可独立接入的角色语音或招式音效。本版按开发单在 [GokuAudio.json](../../../../Assets/DragonIdle/Resources/Config/Characters/GokuAudio.json) 中保留 17 个空资源路径。到达这些演出时点时跳过播放。

| 提示点 | 参考片段和结果 |
| --- | --- |
| `voice-entry`、`voice-opening`、`cloth`、`land` | 待机和防御片段用于观察姿势，进场、热身、衣料和落地音各留白。 |
| `voice-punch`、`punch`、`heavy`、`sweep` | `0001Guku普攻.mp4` 的接触参考约 0.165、0.442、0.905 秒，四项留白。 |
| `guard` | `0001Goku防御.mp4` 的架挡提示留白。 |
| `voice-kick`、`uppercut` | `0001Goku追打.mp4` 主要接触约 0.250 秒，起势和命中音留白。 |
| `voice-gather`、`charge`、`voice-fire`、`beam`、`beam-tail` | `0001Goku必杀.mp4` 用于参考聚气、出手及余波时点，五项留白。 |
| `dodge` | 参考片段内没有合适的独立闪避提示，留白。 |

v0.13.2 与 v0.13.3 的旧合成样音已移到各自的制作目录，供历史版本复查。本地参考录屏位于作者素材目录，Windows 包使用独立的 [运行时音频清单](../../../../Assets/DragonIdle/Resources/Config/Characters/GokuAudio.json)。[v0.13.2 音频档案](../../V0132/goku-test/audio) · [v0.13.3 音频档案](../../V0133/goku-test/audio)
