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
   准备新版本时，先从[版本开发单模板](../release-plans/TEMPLATE.md)建立本版工单并更新入口。按用户填写的「版本开发内容」与「版本修复BUG」执行，保留原文，逐条回填落地与验证情况。已发布版本的修订可转入新补丁版本，并在原单注明。
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
- `Assets/DragonIdle/Resources/Videos` 的参考 MP4 通过 Git LFS 归档；随版本保留路径和 SHA-256 清单。

不提交：
- Library/Temp/Obj/Logs/UserSettings、生成IDE文件。
- Builds、发布ZIP、临时截图/验证缓存、个人存档、凭据。
- 大型源素材采用适当的大文件策略；不能把PC整包持续塞入Git历史。

每个可玩版本：
1. 通过版本文件、更新说明明确版本号与内容。
2. 通过 Windows 打包脚本同步角色工作簿和运行配置，再执行适用的模拟测试、配置校验和 Unity 导入／编译检查。
3. Windows x64完整构建到干净输出目录。
4. 用无窗口 Player 检查 PC 包的战斗、存档和版本。自 2026-11-22 起，开发时可按需打开 Unity 编辑器并进入 Play 模式核对画面。编辑器观感与 PC 包启动结果分别记录。
5. 确保源码提交是构建来源且工作区无未记录游戏内容修改。
6. ZIP必须包含EXE、Data目录及运行所需文件，不能只上传EXE。
7. 生成SHA256及manifest：版本、源码提交、Unity版本、平台、构建时间、验证结果。
8. 推送源码和新增或修改的 Git LFS 对象，以同一提交创建版本标签和 GitHub Release，上传同版本 ZIP、校验和与说明。
9. 回读远端标签和附件，核对 Windows 包的 SHA-256。在独立位置使用空 LFS 缓存拉取视频，按清单验证新增或修改的文件可以恢复；首版归档验证整个目录。发布完成才报告可下载。
10. 保留旧版本供回退，不重用标签覆盖不同内容。

### 参考视频的归档与本地清理

每个小版本从远端发布基线准备提交。视频路径内只暂存新增和修改，保留用户为释放空间而在本地清理的已归档视频。需要正式移除参考时单独记录并显式暂存。首次归档 v0.17.3 的清单位于 [Releases](../../Releases/v0.17.3-reference-videos.sha256)。

在暂存其他项目文件时排除视频目录及目录的 Unity 元数据，再用以下命令暂存工作区中现有的视频与元数据。新版本的清单以暂存区的 LFS 指针及其 SHA-256 为准，包含从之前版本保留的对象。

```powershell
git add -A -- . ':(exclude)Assets/DragonIdle/Resources/Videos' ':(exclude)Assets/DragonIdle/Resources/Videos.meta'
git add --ignore-removal -- Assets/DragonIdle/Resources/Videos
if (Test-Path -LiteralPath 'Assets/DragonIdle/Resources/Videos.meta') {
    git add --ignore-removal -- Assets/DragonIdle/Resources/Videos.meta
}
```

取回视频时可在空闲盘创建只取视频的克隆，避免覆盖游戏开发目录中的修改。先安装 Git LFS，替换命令里的版本和保存目录。

```powershell
$env:GIT_LFS_SKIP_SMUDGE = '1'
try {
    git clone --branch v0.17.3 --depth 1 --no-checkout https://github.com/Icein61/dragonball-idle-unity.git video-restore
    git -C video-restore sparse-checkout init --cone
    git -C video-restore sparse-checkout set Assets/DragonIdle/Resources/Videos
    git -C video-restore checkout v0.17.3
} finally {
    Remove-Item Env:GIT_LFS_SKIP_SMUDGE -ErrorAction SilentlyContinue
}
git -C video-restore lfs pull --include='Assets/DragonIdle/Resources/Videos/**' --exclude=''
```

需要减少 D 盘占用时，发布暂存目录及 LFS 缓存使用其他盘。收到远端核验结果后，用户可以清理 D 盘工作目录中的视频原片；Windows 玩家包不使用这些原片。GitHub 默认生成的源码 ZIP 可能只有 LFS 指针，恢复素材使用 Git LFS。

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

## v0.6.1 第二阶段接手
见engineering/workshop-v061.md。共用Canvas现在覆盖四页，后台烟测必须含workshop-smoke.json。用户新增未提交素材不能混入发布：存在用户编辑时用干净源码副本构建，不移动/删除用户文件。

## v0.16.1 角色表累计与战斗接手

- `Tools/sync-character-sheet.py` 维护 Q 至 U 列，并重新校验玩家原文、位图锚点和新运行配置。表中的「截至本编号」包含本角色。
- `RoundCombat.BeginRound` 在回合起点恢复触发次数并更新阶段被动。状态追打与来源命中绑定。成功的普通闪避和技能闪避均不会生成新的追打状态。
- `GalleryStatsInfo.cs` 复用角色表潜力绘制列表卡片和详情三角图，用同一遮罩管理气泡与计算说明。
- 拉蒂兹和悟空动作文件位于 `Assets/DragonIdle/Resources/Art/V0161`。音频取自用户指定的本地音频库，映射和源文件记录在 `Art/Production/V0161/Raditz-audio-sources.json`。

## v0.16.2 图鉴与版本命名

- `GalleryAvatarSwipe` 保存连续拖动位置、当前索引和可见头像。刷新详情正文时保留拖动条本身。
- 稀有度底图统一从 `Resources/Interface/Gallery/Rarity` 读取。武术动作类图标继续由 `Tools/round-character-assets.json` 绑定。ImageGen 参考图、提示词和文件摘要见 `Art/Production/V0162/sources.json`。
- 版本号依次称大版本号、小版本号和中间版本号。v0.16 阶段总结与后续入口见 [v0.16 复盘](v0.16-retrospective.md)。

## v0.17.0 图鉴与编队重构前的入口

- 主页及侧栏只提供角色与独立战斗 Demo。旧阵容数据仍可迁移，九宫格预览继续有专用回归夹具。
- 新 C 角标由 `RarityResource` 同时供启动和图鉴读取。四档新底图由同一映射提供。来源和提示词见 [素材记录](../../Art/Production/V0170/sources.json)。
- 双重太阳日的光束按演出目标装配，实际命中仍由回合规则提供。克林图集裁切见 `Art/V0154/Krillin/Motion.json`。

## v0.17.1 编队与本地试玩包

- 编队和鼠标拖放位于 RoundFormationUI.cs。有效变更走 Profile 的现有九宫格写入接口，RoundScene.cs 读取已存阵位并抽取对手。
- 图鉴纹样见[来源清单](../../Art/Production/V0171/sources.json)。能力图和 info 由 GalleryStatsInfo.cs 与 GalleryDialogs.cs 绘制，使用 GalleryDetail.cs 的头像选择状态。
- DuelReaction 读取受击轨的 reactionHoldAt。气弹命中爆光按受击坐标，扬尘按战场阵位投影。
- [交付记录](../../Releases/v0.17.1.md)链接试玩包、后台截图、隔离 Player 结果和源码指纹。

## v0.17.2 雅木茶和编队草稿

- 雅木茶沿用 yamcha-saiyan-v020，正式配置由角色表生成。图集时间、锚点及保持帧见 Art/V0172/Yamcha/Motion，素材和本机音频见[制作清单](../../Art/Production/V0172/Yamcha/sources.json)。
- RoundFormationUI.cs 按格捕获指针，Profile 的草稿校验复用现有九宫格规则。显式存档成功后重新绘制，离开页面清理草稿。
- 复活排入触发技能队列。在上一个动作结束后结算，并带有具体技能来源的独立攻击贡献。演出在起身站稳时显示恢复值与增益。
- [交付记录](../../Releases/v0.17.2.md)收录本地包、画面和后台证据。

## v0.17.3 天津饭与素材

- 天津饭沿用 tien-test。角色表生成配置，Art/V0173/Tien/Motion 保存演出时间与定点。[图像和本机音效来源](../../Art/Production/V0173/Tien/sources.json)。
- RoundCombat 执行回合起手队列。气功炮将生命成本带入战斗事件，在演出到达时公布。
- GalleryRoleArt 绑定三个定位区域，GalleryDragonBallWatermarks 绘制小龙珠的折射和反光。
- Windows 通过 C 盘临时 Unity 工程构建，角色表、源文件、验收记录和交付 ZIP 在 D 盘项目。[交付记录](../../Releases/v0.17.3.md)。
