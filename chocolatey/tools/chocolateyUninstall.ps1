$ErrorActionPreference = 'Stop';

$packageArgs = @{
  packageName   = 'dailyprayertimer'
  softwareName  = 'Daily Prayer Timer*'
  fileType      = 'exe'
  silentArgs    = '/VERYSILENT /SUPPRESSMSGBOXES /NORESTART'
  validExitCodes= @(0, 3010, 1641, 2359302)
}

Uninstall-ChocolateyPackage @packageArgs
