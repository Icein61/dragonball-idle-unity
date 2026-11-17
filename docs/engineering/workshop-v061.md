# v0.6.1 第二阶段UI工程
WorkshopUI复用GalleryUI的Canvas、九宫格、按钮、文字、滚动和模态组件。IsModernPage覆盖角色/编队/养成/装备，旧OnGUI不再绘制这四页；其余页仍保留原实现。

养成/装备共享selectedCard，进入时未拥有卡回落第一张已拥有卡。browseCard保存人物志进入前选择，返回时恢复；原列表、详情页签和滚动不销毁。Workshop按页面/角色/装备页签保存滚动，刷新通过控件名恢复焦点。现阶段工坊操作后重建当前页，没有虚拟列表；大背包性能待后续实测。

所有变更经Change保存/失败回滚，WorkChange统一反馈并清除旧战斗快照。按钮根据资源、重复技能和存档可写状态禁用。装备比较只修改Json克隆，使用InspectCharacter计算前后实际值，确认后才提交；跨角色装备保持实例唯一。技能原文不在此页面改写。

WorkshopSmoke覆盖实际uGUI按钮事件、培养/策略/预设/装配存取、珠子守恒、技能解锁/重复限制、比较取消不改档、模态背景禁用、筛选零结果与上下文返回。workshop-smoke.json写入打包manifest；renderVerified固定false，不能把无图形事件验收当渲染验收。
