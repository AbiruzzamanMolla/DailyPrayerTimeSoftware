# publish_portable.ps1
# This script generates a portable, single-file, self-contained executable for Daily Prayer Time (Native).

$Version = "2.5.1" # Should match csproj
$Runtime = "win-x64"
$ProjectDir = "./DailyPrayerTime.Native"
$OutputDir = "./Output/Portable"

Write-Host "🚀 Building Portable Version v$Version ($Runtime)..." -ForegroundColor Cyan

# Ensure output directory exists
if (-not (Test-Path $OutputDir)) {
    New-Item -ItemType Directory -Force $OutputDir | Out-Null
}
$TargetExe = Join-Path $OutputDir "DailyPrayerTimer_v$($Version)_Portable_x64.exe"
if (Test-Path $TargetExe) {
    Remove-Item -Force $TargetExe
}

# Publish the application
dotnet publish $ProjectDir `
    -c Release `
    -r $Runtime `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:PublishReadyToRun=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -o $OutputDir

# Rename the output to include version and architecture
$OldExe = Join-Path $OutputDir "DailyPrayerTime.Native.exe"

if (Test-Path $OldExe) {
    $NewName = "DailyPrayerTimer_v$($Version)_Portable_x64.exe"
    Rename-Item -Path $OldExe -NewName $NewName
    $FinalPath = Join-Path $OutputDir $NewName
    Write-Host "✅ Created: $FinalPath" -ForegroundColor Green
    
    # Create the .portable flag file
    $FlagFile = Join-Path $OutputDir ".portable"
    New-Item -ItemType File $FlagFile -Force | Out-Null
    Write-Host "✅ Created .portable flag file" -ForegroundColor Green
} else {
    Write-Host "❌ Error: Executable not found at $OldExe" -ForegroundColor Red
    exit 1
}

Write-Host "`n🎉 Portable Build Complete!" -ForegroundColor Green
Write-Host "Stored in: $OutputDir" -ForegroundColor Yellow
