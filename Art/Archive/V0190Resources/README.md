# v0.19.0 旧运行资源归档

本批逐项核对了 Unity Resources 内的 372 个美术、音频、界面及配置文件。归档 90 个资源键，包括 66 个美术文件及动作清单、14 个音频文件、10 个界面文件。210 个原始文件包含每个资源的 `.meta` 和 30 份目录 `.meta`，共 137,671,706 字节。原目录层级、原始字节和 Unity GUID 均保留。

- [逐项核对表](inventory.tsv)记录 372 个资源的原路径、GUID 和保留或归档依据。
- [恢复清单](manifest.json)记录 90 个资源键、210 个文件和涉及的目录。每个文件有原路径、归档相对路径、字节数及 SHA-256。
- [前批概念图归档](../LegacyConcepts/README.md)有独立的恢复清单。

## 运行目录与保留依据

| 目录或入口 | 核对依据 |
| --- | --- |
| `Resources/Art` | 上线立绘来自生成的 `RoundPortraits`。技能图来自 `RoundKits` 和角色表映射。动作与受击沿 `RoundMotionUI`、`RoundDemoUI` 解析新版优先和回退清单，再检查每个图集的相对或跨目录路径。保留仍在用的早期图集、早期制作脚本的输出组和已有脚本名分支指向的纹理。 |
| `Resources/Art/V050`、`V051` | 旧图集、帧矩形和立绘继续服务历史显示回退及 `validate-art.ps1` 的构建校验。离线角色的对应素材由已有的 Windows 构建规则排除。 |
| `Resources/Audio` | 逐一检查九个角色音库、连击音效、菜单音乐及场景音乐的资源键。克林散射、天津饭一指功和栽培人保留当前音库对应文件。 |
| `Resources/Interface` | 当前图鉴底图来自 `Gallery/Rarity/V0171`，定位图来自 `Role/V0174`。保留独立徽章根目录及仍被读取的 `Rarity/V0170/C`。 |
| `Resources/Config` | 角色表、客户端立绘、角色音库和战斗场景的现行配置继续使用原路径。工作簿无外部文件链接。 |
| `Resources/Videos` | 本地制作参考，沿用 Git 忽略与构建排除规则。恢复入口见[工程交接](../../../docs/engineering/handoff.md#参考视频的归档与本地清理)。 |

Unity 场景、项目配置、资源元数据及工程脚本按 GUID 和资源键复查。旧动作清单只引用同批历史资源时，保留相对路径并一并归档。对仍由现行动作或回退清单读取的跨目录资源，继续保留原位。编辑器导入器仍可按原路径恢复旧资源的导入设置。

## 恢复原路径

PowerShell 在仓库根目录执行下列脚本。脚本先核对归档哈希和目标冲突，再复制原文件及其 `.meta`，已存在且哈希相同的文件会跳过。跨目录的旧动作依赖随整批一同恢复。

```powershell
$ErrorActionPreference = 'Stop'
$repo = (Get-Item -LiteralPath '.').FullName
$archive = Join-Path $repo 'Art/Archive/V0190Resources'
$runtime = Join-Path $repo 'Assets/DragonIdle/Resources'
$archivePrefix = $archive + [IO.Path]::DirectorySeparatorChar
$runtimePrefix = $runtime + [IO.Path]::DirectorySeparatorChar
$manifest = Get-Content -LiteralPath (Join-Path $archive 'manifest.json') -Raw -Encoding UTF8 | ConvertFrom-Json
$copies = foreach ($file in $manifest.files) {
    $source = [IO.Path]::GetFullPath((Join-Path $archive $file.archive))
    $target = [IO.Path]::GetFullPath((Join-Path $repo $file.original))
    if (!$source.StartsWith($archivePrefix, [StringComparison]::OrdinalIgnoreCase) -or
        !$target.StartsWith($runtimePrefix, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Path outside archive or runtime directory: $($file.original)"
    }
    if ((Get-FileHash -LiteralPath $source -Algorithm SHA256).Hash -ne $file.sha256) {
        throw "Archive hash differs: $source"
    }
    if (Test-Path -LiteralPath $target) {
        if (!(Test-Path -LiteralPath $target -PathType Leaf) -or
            (Get-FileHash -LiteralPath $target -Algorithm SHA256).Hash -ne $file.sha256) {
            throw "Existing file conflicts: $target"
        }
        continue
    }
    [PSCustomObject]@{ Source = $source; Target = $target; Hash = $file.sha256 }
}
foreach ($copy in $copies) {
    New-Item -ItemType Directory -Path ([IO.Path]::GetDirectoryName($copy.Target)) -Force | Out-Null
    Copy-Item -LiteralPath $copy.Source -Destination $copy.Target
    if ((Get-FileHash -LiteralPath $copy.Target -Algorithm SHA256).Hash -ne $copy.Hash) {
        throw "Restored hash differs: $($copy.Target)"
    }
}
```

隔离还原检查已按原路径恢复 210 个文件，逐个通过 SHA-256 核对，验证了 30 个目录及其原始元数据。
