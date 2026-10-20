param([string]$Destination = '')
$ErrorActionPreference = 'Stop'
$sourceDir = $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($Destination)) { $Destination = Join-Path $sourceDir 'dist' }
New-Item -ItemType Directory -Path $destination -Force | Out-Null
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path -LiteralPath $compiler)) { $compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe' }
$sources = @(Get-ChildItem -LiteralPath $sourceDir -Filter '*.cs' | ForEach-Object { $_.FullName })
$iconOptions = @()
if (Test-Path -LiteralPath (Join-Path $sourceDir 'SnapLite.ico')) { $iconOptions = @("/win32icon:$sourceDir\SnapLite.ico") }
& $compiler /nologo /target:winexe /platform:anycpu /optimize+ /utf8output /codepage:65001 /warn:4 "/out:$Destination\SnapLite.exe" "/win32manifest:$sourceDir\app.manifest" /reference:System.dll /reference:System.Core.dll /reference:System.Drawing.dll /reference:System.Windows.Forms.dll /reference:System.Xml.dll @iconOptions $sources
if ($LASTEXITCODE -ne 0) { throw 'Compilation failed' }
Write-Output (Join-Path $Destination 'SnapLite.exe')
