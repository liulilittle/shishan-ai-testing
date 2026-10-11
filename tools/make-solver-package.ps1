# make-solver-package.ps1
# 打包答题者工作区:仅导出单个题目领域库 topics/Shishan.Domain.<code>
# 用法:powershell -ExecutionPolicy Bypass -File tools/make-solver-package.ps1 -Topic D002 -Out <dir>
# 说明:输出目录仅含领域库源码 + csproj(UTF8 写入),不含 tests/solutions/src/.git/obj/bin。

param(
    [Parameter(Mandatory = $true)]
    [string]$Topic,
    [Parameter(Mandatory = $true)]
    [string]$Out
)

$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$root = Split-Path -Parent $root

$code = $Topic.Trim().ToUpperInvariant().Replace('-', '')
$srcDir = Join-Path $root ("topics\Shishan.Domain." + $code)
if (-not (Test-Path -LiteralPath $srcDir)) {
    throw "Topic directory not found: $srcDir (expected Shishan.Domain.$code under topics/)"
}

$packageDir = Join-Path $Out ("Shishan.Domain." + $code)
if (-not (Test-Path -LiteralPath $packageDir)) {
    New-Item -ItemType Directory -Path $packageDir -Force | Out-Null
}

$excluded = @('tests', 'solutions', 'src', '.git', 'obj', 'bin')
$utf8 = New-Object System.Text.UTF8Encoding($false)
$count = 0

Get-ChildItem -LiteralPath $srcDir -Recurse -File | ForEach-Object {
    $rel = $_.FullName.Substring($srcDir.Length).TrimStart('\')
    $parts = $rel -split '\\'
    foreach ($seg in $parts) {
        if ($excluded -contains $seg) { return }
    }
    $dest = Join-Path $packageDir $rel
    $destDir = Split-Path -Parent $dest
    if (-not (Test-Path -LiteralPath $destDir)) {
        New-Item -ItemType Directory -Path $destDir -Force | Out-Null
    }
    [System.IO.File]::WriteAllText($dest, [System.IO.File]::ReadAllText($_.FullName, [System.Text.Encoding]::UTF8), $utf8)
    $count++
}

Write-Output ("Packaged {0} file(s) -> {1}" -f $count, $packageDir)