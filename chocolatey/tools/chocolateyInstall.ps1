$ErrorActionPreference = 'Stop';

$packageArgs = @{
  packageName   = 'dailyprayertimer'
  fileType      = 'exe'
  url           = 'https://github.com/AbiruzzamanMolla/DailyPrayerTimeSoftware/releases/download/v2.4.4/DailyPrayerTimer_Setup_v2.4.4_x86.exe'
  url64bit      = 'https://github.com/AbiruzzamanMolla/DailyPrayerTimeSoftware/releases/download/v2.4.4/DailyPrayerTimer_Setup_v2.4.4_x64.exe'
  softwareName  = 'Daily Prayer Timer*'
  checksum      = '90ADC74DC6BA45938034E35E1D9471B418619DA2AD40A53DB250CD47EB8A37B7'
  checksumType  = 'sha256'
  checksum64    = 'FB293E39135D8B7A8DF65036FD3FCDDAF4C3EBA4303A6FC33F3BDB5B913CF24E'
  checksumType64= 'sha256'
  silentArgs    = '/VERYSILENT /SUPPRESSMSGBOXES /NORESTART'
  validExitCodes= @(0, 3010, 1641, 2359302)
}

Install-ChocolateyPackage @packageArgs
