$ErrorActionPreference = 'Stop';

$packageArgs = @{
  packageName   = 'dailyprayertimer'
  fileType      = 'exe'
  url           = 'https://github.com/AbiruzzamanMolla/DailyPrayerTimeSoftware/releases/download/v2.3.3/DailyPrayerTimer_Setup_v2.3.3_x86.exe'
  url64bit      = 'https://github.com/AbiruzzamanMolla/DailyPrayerTimeSoftware/releases/download/v2.3.3/DailyPrayerTimer_Setup_v2.3.3_x64.exe'
  softwareName  = 'Daily Prayer Timer*'
  checksum      = '5D1228F826E8143F294421ED1EFCB221C6F2200B9946B029610A26A1F1EFC943'
  checksumType  = 'sha256'
  checksum64    = '8DF2715E9C7AEC0A424692BE267F77A774C9D8FC366399FD15B4DFC1A46C9C29'
  checksumType64= 'sha256'
  silentArgs    = '/VERYSILENT /SUPPRESSMSGBOXES /NORESTART'
  validExitCodes= @(0, 3010, 1641, 2359302)
}

Install-ChocolateyPackage @packageArgs
