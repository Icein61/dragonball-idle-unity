---
name: dragonidle-character-sheet
description: 维护龙珠爆裂激斗的 character.xlsx、角色编号与客户端生成配置。适用于改角色表、接入正式角色资源或检查生成配置是否同步。
---

# 角色配表

按[配表规则](../../../docs/standards/design/character-data.md#客户端配置生成)定位工作簿、稳定编号、资源映射和生成文件。

1. 在「角色设计」按表内编号找到卡片，核对卡片身份、上线开关及用户原文。只修改任务指定内容。现有技能句式、数值、条件或资源不明确时，把要确认的原文和对照证据放进开发单。
2. 立绘或图标接入时将导入文件与 `Tools/round-character-assets.json` 对到同一张卡，检查路径、裁切和技能归属。客户端配置由生成工具写出。
3. 有已授权的来源变更时运行 `Tools/sync-character-sheet.ps1`，再用 `-Check` 回读。纯核查直接用 `-Check`。遇到锁表、缺素材或解析失败时先定位原单元格和输出差异。
4. 交接记录受影响的编号和 CardId、原字段、生成文件和校验结果。背景重构应核对工作簿与生成文件哈希。

工具可传 `-PythonPath` 或读取项目的 `DRAGON_SHEET_PYTHON`。入口与参数见[同步脚本](../../../Tools/sync-character-sheet.ps1)。
