$ErrorActionPreference = 'Stop'
$source = Join-Path $PSScriptRoot 'src'
$assets = Join-Path $PSScriptRoot 'assets'
$destination = Join-Path $PSScriptRoot 'dist'
New-Item -ItemType Directory -Force -Path $destination | Out-Null
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path -LiteralPath $compiler)) {
    $compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe'
}
if (-not (Test-Path -LiteralPath $compiler)) { throw '.NET Framework compiler was not found.' }
$compileArgs = @(
    '/nologo', '/target:winexe', '/optimize+',
    "/out:$destination\映拾花映集.exe",
    "/win32icon:$source\app.ico", "/resource:$source\app.ico,Yingshi.ico",
    "/resource:$assets\reimu-banner.png,Reimu.banner",
    "/resource:$assets\marisa-sticker.png,Marisa.sticker",
    "/resource:$assets\cirno-banner.png,Cirno.banner",
    "/resource:$assets\patchouli-banner.png,Patchouli.banner",
    "/resource:$assets\remilia-banner.png,Remilia.banner",
    "/resource:$assets\flandre-banner.png,Flandre.banner",
    '/reference:System.Windows.Forms.dll', '/reference:System.Drawing.dll'
)
$compileArgs += Get-ChildItem -LiteralPath $source -Filter '*.cs' | ForEach-Object FullName
& $compiler @compileArgs
if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }
Write-Output "Built: $destination\映拾花映集.exe"
