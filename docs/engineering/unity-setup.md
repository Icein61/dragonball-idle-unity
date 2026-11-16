# 本地Unity环境

项目锁定Unity **2022.3.62f3**，changeset **96770f904ca7**，Windows x64 / Mono。继续开发前应先完成编辑器安装、许可证检查、项目后台编译与隔离Player验证。

2026-11-16本机结果：官方Windows安装包数字签名Valid，静默安装退出0；版本/changeset匹配，现有许可证通过，Windows Mono构建与隔离Player均通过。没有安装额外IL2CPP工具链或打开可见编辑器。详情见[本轮验证](../verification/v0.5.2.md)。

## 安装

从[官方版本页](https://unity.com/releases/editor/whats-new/2022.3.62f3)取得Windows Editor安装程序，下载完成检查文件大小及有效Unity Technologies SF数字签名。按[官方命令行安装文档](https://docs.unity.cn/2022.3/Documentation/Manual/InstallingUnity.html)可使用`/S /D=安装目录`静默安装，目录参数必须在最后。保留已有版本，不覆盖其他项目使用的编辑器。

本项目构建后端为Mono；先检查编辑器自带WindowsStandaloneSupport能否实际构建。IL2CPP、Android、WebGL不属于当前必要依赖，不因安装编辑器默认引入其他平台工具链。

## 查找与验证

`Tools/resolve-unity.ps1`按ProjectVersion读取准确版本，优先使用显式`-UnityPath`，其次`DRAGON_IDLE_UNITY_PATH`，最后查找常见版本目录。实际exe的ProductVersion必须匹配，不自动升级项目。

```powershell
./Tools/resolve-unity.ps1
./Tools/build-windows.ps1
./Tools/smoke-windows.ps1
```

自定义安装目录时传`-UnityPath`，或在当前终端设置`DRAGON_IDLE_UNITY_PATH`。无需将个人路径写入工程配置。

构建使用`-batchmode -nographics`和隐藏进程，日志保存在`.validation/build.log`；Player强制静音并使用隔离存档。当前不自动打开编辑器、游戏或可见测试窗口的约束保持。

许可证文件存在不代表可用，必须由编辑器实际验证。若必须登录或激活，通过Unity官方流程完成，不把账号、许可证内容或个人路径提交到仓库。安装失败、编译失败与体验待验收分别记录，不能沿用旧版本测试结果。
