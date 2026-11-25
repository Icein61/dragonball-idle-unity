---
name: dragonidle-release-verify
description: 运行龙珠爆裂激斗工程改动或版本收尾的全链路后台回归。适用于固定种子对照、Windows x64 构建、隔离存档、图鉴编队战斗回归和开发单回填。
---

# 全链路后台验收

从开发单取得基线标签、验收范围和上线阵容。按[后台验收规则](../../../docs/verification/acceptance.md#工程改动全链路后台回归)区分模拟、构建、Player、存档与可见检查。为本次验证分配新的构建目录和证据目录。

| 工作 | 可重复入口 |
| --- | --- |
| 配表回读 | `Tools/sync-character-sheet.ps1 -Check`，必要时传入 `-PythonPath`。 |
| 固定种子 | `Tools/compare-round-traces.ps1 -BaselineTag <已验收标签> -OutputDirectory <本次轨迹目录>`。保持同一阵容用例和种子。 |
| 构建和基础检查 | `Tools/build-windows.ps1 -BuildFolder <Windows-本次标识>`，必要时传 `-CharacterSheetPython`。它会跑配表、核心、图形资产和界面检查。 |
| 实际 Player | `Tools/smoke-windows.ps1 -Executable <本次 exe> -OutputDirectory <本次 Player 证据>`。有已核对的归档清单时加 `-ResourceArchiveManifest`。 |
| 存档 | `Tools/smoke-profile.ps1 -Executable <同一 exe> -OutputDirectory <全新存档验证目录>`。 |

核对配置、模拟轨迹和 Player JSON 与基线的差异。将首次建档、重启、失败保护、可见检查的实际覆盖分别登记。回归使用脚本分配的隔离档，本机试玩档留在原位。发布动作及校验和回读按已授权的[版本交付流程](../../../docs/engineering/handoff.md#git内容与发布)执行。逐项回填开发单并附证据路径。
