# 跨环境接手与版本交付

## 事实与范围
本项目与相邻“灰烬边境/ember-hunt-unity”独立。不得把它的Unity代码、版本号、PC包或GitHub Release误称为本项目产物。
v0.1.0已建立Unity 2022.3.62f3技术Demo，使用内置渲染与临时IMGUI表现；最终美术路线仍待样片选择。
未来选定Unity版本后，在ProjectSettings/ProjectVersion.txt锁定，并提交Packages/manifest.json与packages-lock.json；不要只在口头提及版本。
默认Windows x64构建；离线游戏不需要用户登录开发账号。

## 接手流程
1. 克隆本项目仓库，进入含AGENTS.md的目录。
2. 阅读README、AGENTS、STATUS、confirmed、defaults与decisions。
3. git status确认未提交改动；读取远端变化，避免覆盖其他环境的新工作。
4. 查看当前阶段与阻塞，不重新假设已有游戏或已验收美术。
5. 有Unity工程时按ProjectVersion安装对应编辑器/Windows模块，不随意升级。
6. 根据当前模块执行验收；修改规则必须同步文档与测试。
7. 完成后更新STATUS，记录提交、测试、未解决限制和下一步，再同步远端。

## 代理工作方式
- 项目范围内合理默认已获授权，非阻塞细节无需逐项确认。
- 使用中文讨论，配置与代码标识可英文，文档使用仓库相对链接。
- 不把本机绝对路径、个人访问令牌、Unity许可证、用户存档提交到仓库。
- 先读已有文件和Git状态，保留用户修改；不强制推送、不重写远端历史。
- 本地提交署名使用本仓库配置，不能冒用另一个项目署名。
- 若需要新分支，默认codex/前缀；不要仅为文档基线制造无意义多层仓库。
- 工具不可用时说明实际失败原因，不虚报已写入、已同步或已构建。

## 默认技术组织（待实现）
- 纯C#模拟核心独立于Unity场景、动画、帧率和输入；表现层消费事件。
- 数据驱动定义CardId、PersonId、SkillId、ItemId、EffectTypeId；版本与显示名称不充当主键。
- 状态存ContributionId、SourceId、效果类别、数值、建立机会、到期机会/整场标志。
- 同名UI可汇总显示总值，但必须能查看每份来源/期限，不合并生命周期。
- 队长专属乘区与普通效果类别分离；不同类别需声明实际计算位置。
- 随机通过可复现的随机源控制；正常行动同刻顺序固定，必杀随机平局能重放。
- 用独立测试验证计时、排序、触发和存档；不把伤害绑定到动画事件。
- 所有经济参数集中配置并标明调参版本；调试常量不能伪装正式平衡。

## 存档与迁移默认
- 存档含schemaVersion、配置版本、CardId/物品实例、专属资源、技能与装备、队伍预设、进度、挂机领取状态。
- 用Unity持久化数据目录保存；用户可导出/导入便携存档。
- 写入使用临时文件和替换，保留上一次有效备份；迁移前备份。
- 不序列化场景对象引用或本机路径。
- 不以卡显示名映射旧存档；新增卡、改称号不改变既有ID。
- 离线领取与奖励发放必须防止同一事务重复执行。
- 云同步不在已确认范围内；跨电脑可先导出/导入，不依赖远程服务。

## Git内容与发布
源码仓库提交：
- 规则、设计、验证与交接文档。
- 后续Unity Assets及.meta、Packages锁文件、ProjectSettings。
- 可复现的构建/验证脚本、素材元数据和必要项目资源。

不提交：
- Library/Temp/Obj/Logs/UserSettings、生成IDE文件。
- Builds、发布ZIP、临时截图/验证缓存、个人存档、凭据。
- 大型源素材采用适当的大文件策略；不能把PC整包持续塞入Git历史。

每个可玩版本：
1. 通过版本文件、更新说明明确版本号与内容。
2. 运行适用模拟测试、配置校验和Unity导入/编译检查。
3. Windows x64完整构建到干净输出目录。
4. 实际启动PC包，检查战斗、存档和版本；可见窗口获授权时再检查菜单与画面。2026-11-16用户工作期间授权已撤回，默认使用-batchmode -nographics，预览包明确标记可见画面未验收。
5. 确保源码提交是构建来源且工作区无未记录游戏内容修改。
6. ZIP必须包含EXE、Data目录及运行所需文件，不能只上传EXE。
7. 生成SHA256及manifest：版本、源码提交、Unity版本、平台、构建时间、验证结果。
8. 推送源码，为该提交创建版本标签和GitHub Release，上传同版本ZIP、校验和与说明。
9. 回读远端标签/附件并检查对应关系，发布完成才报告可下载。
10. 保留旧版本供回退，不重用标签覆盖不同内容。

文档基线不要求伪造PC包；进入可玩阶段后不得仅推源码就称完成可玩版本交付。

## v0.5.0 角色演出接手
- 新图集和矩形：Assets/DragonIdle/Resources/Art/V050；提示词Art/Production/V050/prompts.json；制作说明docs/production/character-art-v050.md。
- 生成新图或改图只用imagegen内置工具。Tools/InspectAtlas.cs仅读取像素生成导入矩形，不修改PNG；Tools/validate-art.ps1用--check验证已提交矩形和透明通道。
- CharacterArtImport保持NPOT尺寸、alpha与无压缩，CharacterArt将真实轮廓矩形转换为UV，按原比例绘制，不以整张图集作为立绘。
- Core/ActionPresentation是只读显示快照；Presentation/BattleAnimation控制帧推进/收场，BattleEffects按事件绘制招式，CharacterArt用于图鉴和战场。核心不依赖Unity动画回调。
- 当前为关键姿态动画与IMGUI战场，非骨骼模型；图鉴仍uGUI。减少动效不影响逻辑；不得为了验证演出自行弹出窗口。

## v0.5.1 立绘与动作资源分离
- PortraitTexture读取Art/V051/{CardId}-portrait，FighterTexture读取Art/V050/{CardId}-atlas；缓存键包含版本/路径，不能互相替代。
- 独立立绘整图UV、原始2:3比例；ArtUV/Regions只用于V050动作图集。不要将旧立绘切片UV套到新整图。
- Tools/InspectPortraits.cs只读检查15张图尺寸、alpha与边缘留白，validate-art同时检查原图集和新立绘。图鉴与Player检查明确验证两类资源不同。
- 完整提示词/输出/哈希在Art/Production/V051/portraits.json；未来立绘必须结合阶段与技能设计不同姿势，不复用统一备战模板。

## v0.6.0 角色UI接手
GalleryTheme提供缓存圆角九宫格和焦点组件；CardLibrary/Detail切换保留列表节点及位置。RefreshGallery按结果ID/顺序决定是否重建，详情刷新按控件名恢复焦点。原文与代理数值仍分开，三卡概念图不能被旧V051全部立绘断言覆盖。gallery-refresh-smoke.json记录新增事件/比例/对比度与窗口数学边界，renderVerified仍为false。其他IMGUI页面尚未迁移。
