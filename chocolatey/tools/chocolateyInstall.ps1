$ErrorActionPreference = 'Stop';

$packageArgs = @{
  packageName   = 'dailyprayertimer'
  fileType      = 'exe'
  url           = 'https://github.com/AbiruzzamanMolla/DailyPrayerTimeSoftware/releases/download/v2.9.1/DailyPrayerTimer_Setup_v2.9.1_x86.exe'
  url64bit      = 'https://github.com/AbiruzzamanMolla/DailyPrayerTimeSoftware/releases/download/v2.9.1/DailyPrayerTimer_Setup_v2.9.1_x64.exe'
  softwareName  = 'Daily Prayer Timer*'
  checksum      = 'DD808BA249C5438190A475BEC66A1CE90529B2BB9DFE8CF2E719978CE4AB1D1D'
  checksumType  = 'sha256'
  checksum64    = 'F1BA4181008E3F51460D822807458A6CD78B13E8FEB2C82BEA061DF00F026E66'
  checksumType64= 'sha256'
  silentArgs    = '/VERYSILENT /SUPPRESSMSGBOXES /NORESTART'
  validExitCodes= @(0, 3010, 1641, 2359302)
}

Install-ChocolateyPackage @packageArgs
