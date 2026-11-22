# 实现和发布流程

1. 确认正在使用当前工作树，阅读 [AGENTS.md](../../../AGENTS.md)、[项目状态](../../STATUS.md)、[确认设计](../../design/confirmed.md)、[代理默认](../../design/defaults.md)和[决策记录](../../design/decisions.md)。
2. 从[版本开发单](../../release-plans/README.md)取本版范围与优先级，保留用户原文。逐条回填实现、验证和待定项。
3. 战斗逻辑遵循纯 C# 模拟与 Unity 表现分层。配置和存档使用稳定标识，保持旧数据可迁移。修改行为或验收时同步项目状态和决策记录。
4. 核心逻辑、配置、素材、Unity 后台构建和实机启动分开验证。当前会话遵守项目的 Unity 窗口授权。
5. 可玩发布按[工程交接](../../engineering/handoff.md)锁定源码提交、构建与验证 Windows 包、生成校验和、同步 GitHub 并回读附件。素材预览记录对应版本和制作阶段。

[Unity 环境](../../engineering/unity-setup.md) · [验收场景](../../verification/acceptance.md)

## 可滚动控件

新增垂直滚动组件使用共用的 `UScroll`。内容由圆角底板承托并在视口裁切，使用 `BoundedScrollRect` 处理上下两端有限过拉和回弹。短描述可拖动回弹，鼠标滚轮按内容溢出与边界交给内层或外层。减少动效设置启用时使用限位模式。
