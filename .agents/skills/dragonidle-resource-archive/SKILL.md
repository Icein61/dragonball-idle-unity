---
name: dragonidle-resource-archive
description: 核对龙珠爆裂激斗的 Unity 资源依赖，归档已替换的旧素材并验证恢复。适用于清理 Resources、核查回退引用或恢复历史资源。
---

# Unity 资源归档

按[引用核对与恢复规则](../../../docs/standards/engineering/resource-archive.md)处理。对单个资源同时记录 Resources 键、原路径和 Unity GUID。

1. 从现行场景、配置、脚本加载入口和构建校验查入站引用。展开拼接键、角色文件夹和旧版回退。沿动作与受击清单追到实际纹理、音频、图标及其他跨目录依赖。
2. 逐项记录「保留」或「归档」和依据。活动链条、回退或校验证据未排除时保留原路径。只存在于待归档组内部的历史清单引用可以随组一起保留。
3. 将确认项移到 `Art/Archive/` 下的本批目录。同步原文件、文件 `.meta`、腾空目录的 `.meta`、原路径、字节数和 SHA-256。元数据的行尾随原文件保存。先在隔离目录演练恢复。
4. 用 Windows Player 的 `-ResourceArchiveManifest` 跑归档键排除检查及现行链路回归。把保留依据、恢复方法和验证日志按批回填开发单。

已有[清单实例](../../../Art/Archive/V0190Resources/README.md)可用于核对结构和恢复命令。
