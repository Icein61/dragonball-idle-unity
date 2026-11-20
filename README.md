# 龙珠爆裂激斗 — Unity 技术 Demo
当前源码 **v0.13.0 九宫格回合制**。个人离线养成游戏的战斗核心与角色构筑原型。发布内容见[本轮记录](Releases/v0.13.0.md)。
GitHub：https://github.com/Icein61/dragonball-idle-unity

开发指令与试玩反馈：[v0.13.1 开发单](docs/release-plans/v0.13.1.md) · [v0.13.0 开发单](docs/release-plans/v0.13.0.md) · [v0.12.8 试玩反馈](docs/release-plans/v0.12.8.md) · [全部版本](docs/release-plans/README.md)。

## 下载与试玩
[查看 Windows x64 Demo 发布附件](https://github.com/Icein61/dragonball-idle-unity/releases)。
下载对应版本的龙珠爆裂激斗-v0.13.0-Windows-x64.zip，完整解压后运行龙珠爆裂激斗.exe。
需要Windows，不需要Unity、开发账号或联网。请保留Data目录及附带运行库，不要只复制EXE。

启动时加载稀有度徽章和九张立绘。主页保留图鉴、编队以及声音与显示设置；存档可从设置中打开目录备份。

- **图鉴**：浏览九张卡、筛选、切换详情，查看本版回合制试阵用招式。八张默认拥有，已保存的收藏继续保留。
- **编队**：九宫格从右列开始、每列先上后下编号。先点阵位再选角色；最多五人，已上阵角色可以换位。
- **试阵**：在编队选择五、七或九人的敌阵。双方站位镜像；可暂停或逐动作推进。试阵不发奖、不消耗资源。
- **回合规则**：开场按阵位编号分配固定手位，我方先，双方交替；退场不重排。角色正常出手为普攻，满怒必杀插入；同时满怒按手位。
- **旧档**：新九宫格用独立字段保存。旧挑战、养成、装备、召唤暂退，数据保留；试阵不读取旧装备、培养和速度效果。

技能和试阵配置按功能存放于 [Config](Assets/DragonIdle/Resources/Config)。细节、默认数值和迁移见[回合制开发说明](docs/engineering/round-combat-v0130.md)。

## 开发与验证
1. 用Unity Hub打开此目录，使用 **Unity 2022.3.62f3** 与Windows构建模块。
2. 打开Assets/DragonIdle/Scenes/Demo.unity并Play。
3. Windows PowerShell运行 ./Tools/validate-core.ps1 验证纯C#逻辑。
4. 使用Unity菜单 龙珠爆裂激斗 > Build Windows Demo，或 ./Tools/build-windows.ps1。
5. 提交源码后运行 ./Tools/package-windows.ps1，后台重新构建、无图形Player验证并打包。不调用-VisibleSmoke。
6. 脚本按锁定版本查找常见Unity安装目录；路径不同可传 -UnityPath 或设置 DRAGON_IDLE_UNITY_PATH。打包输出按VERSION写入Builds/Releases；环境安装与检查见[本地Unity环境](docs/engineering/unity-setup.md)。

模拟核心独立于Unity帧率，GitHub Actions运行核心与资源检查；Unity构建在有编辑器许可证的本机进行。
测试窗口使用独立测试存档，不修改正式存档。

## 换环境接手
先读 [AGENTS.md](AGENTS.md)、[当前状态](docs/STATUS.md)、[确认设计](docs/design/confirmed.md)。
[默认方案](docs/design/defaults.md)可调整，不能冒充用户明确确认；[纠正记录](docs/design/decisions.md)防止恢复旧解释。
- [美术方向](docs/production/art-direction.md)
- [工程与发布约定](docs/engineering/handoff.md)
- [完整目标验收场景](docs/verification/acceptance.md)
- [当前验证与交付记录](docs/STATUS.md)

后续每个可玩版本仍需对应源码提交、标签、版本说明与可运行PC包。不要提交Library或Builds缓存。
