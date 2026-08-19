param (
    [string]$Version = "",
    [string]$PackageName = "AbiruzzamanMolla.DailyPrayerTimer",
    [string]$Publisher = "CN=3F039B3D-068B-4926-BC95-27F183E3F277",
    [string]$PublisherDisplayName = "AbiruzzamanMolla",
    [string]$Architecture = "x64"
)

$ErrorActionPreference = "Stop"

# Get current script root
$PSScriptRoot = Get-Location

Write-Host "=== Daily Prayer Timer MSIX Packager ===" -ForegroundColor Cyan

# 1. Parse Version if not provided
if ([string]::IsNullOrEmpty($Version)) {
    Write-Host "Parsing version from DailyPrayerTime.Native.csproj..."
    [xml]$csproj = Get-Content "$PSScriptRoot/DailyPrayerTime.Native/DailyPrayerTime.Native.csproj"
    $Version = $csproj.Project.PropertyGroup.Version
    Write-Host "Detected Version: $Version" -ForegroundColor Green
}

# MSIX requires a 4-part version number (e.g. 1.0.0.0)
if ($Version -match "^\d+\.\d+\.\d+$") {
    $Version = "$Version.0"
}
if ($Version -notmatch "^\d+\.\d+\.\d+\.\d+$") {
    throw "Invalid version format '$Version'. MSIX requires a 4-part version (e.g., 2.10.0.0)."
}

# Define Paths
$StagingDir = "$PSScriptRoot/Output/MSIX/Staging"
$OutputDir = "$PSScriptRoot/Output/MSIX"
$SourceAssets = "$PSScriptRoot/DailyPrayerTime.Native/Assets/Store"

# Clean directories
if (Test-Path $StagingDir) {
    Remove-Item -Path $StagingDir -Recurse -Force
}
New-Item -ItemType Directory -Path $StagingDir -Force | Out-Null
if (!(Test-Path $OutputDir)) {
    New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null
}

# 2. Publish WPF App
Write-Host "Publishing DailyPrayerTime.Native (loose files)..." -ForegroundColor Yellow
$rid = "win-$Architecture"
dotnet publish "$PSScriptRoot/DailyPrayerTime.Native" `
    -c Release `
    -r $rid `
    --self-contained `
    -p:PublishReadyToRun=true `
    -o $StagingDir

# Remove any extra unneeded packaging files or single-file relics if they exist
if (Test-Path "$StagingDir/DailyPrayerTime.Native.pdb") {
    Remove-Item "$StagingDir/DailyPrayerTime.Native.pdb" -Force
}

# 3. Create Manifest from Template
Write-Host "Configuring AppxManifest.xml..." -ForegroundColor Yellow
$manifestTemplate = Get-Content "$PSScriptRoot/DailyPrayerTime.Native/AppxManifest.xml" -Raw
$manifestContent = $manifestTemplate.
    Replace("TEMPLATE_PACKAGE_NAME", $PackageName).
    Replace("TEMPLATE_PUBLISHER_DISPLAY_NAME", $PublisherDisplayName).
    Replace("TEMPLATE_PUBLISHER", $Publisher).
    Replace("TEMPLATE_VERSION", $Version).
    Replace("TEMPLATE_ARCHITECTURE", $Architecture)

$manifestContent | Out-File -FilePath "$StagingDir/AppxManifest.xml" -Encoding utf8

# 4. Copy Visual Assets
Write-Host "Copying visual assets..." -ForegroundColor Yellow
$StagingAssetsDir = "$StagingDir/Assets"
if (!(Test-Path $StagingAssetsDir)) {
    New-Item -ItemType Directory -Path $StagingAssetsDir -Force | Out-Null
}
Copy-Item -Path "$SourceAssets/*" -Destination $StagingAssetsDir -Force

# 5. Find makeappx.exe
Write-Host "Locating makeappx.exe in Windows SDK..." -ForegroundColor Yellow
$sdkPath = "C:\Program Files (x86)\Windows Kits\10\bin"
$makeappx = $null

if (Test-Path $sdkPath) {
    $makeappx = Get-ChildItem -Path $sdkPath -Filter "makeappx.exe" -Recurse | `
        Where-Object { $_.FullName -like "*\x64\*" } | `
        Sort-Object LastWriteTime -Descending | `
        Select-Object -First 1 -ExpandProperty FullName
}

if (!$makeappx) {
    throw "makeappx.exe could not be found. Please ensure the Windows 10/11 SDK is installed."
}

Write-Host "Found makeappx.exe at: $makeappx" -ForegroundColor Green

# 6. Package the MSIX file
$msixFileName = "DailyPrayerTimer_${Version}_${Architecture}.msix"
$msixPath = "$OutputDir/$msixFileName"

Write-Host "Packaging MSIX package..." -ForegroundColor Yellow
& $makeappx pack /d $StagingDir /p $msixPath /o

Write-Host "=============================================" -ForegroundColor Green
Write-Host "MSIX Package created successfully!" -ForegroundColor Green
Write-Host "File Location: $msixPath" -ForegroundColor Green
Write-Host "=============================================" -ForegroundColor Green
