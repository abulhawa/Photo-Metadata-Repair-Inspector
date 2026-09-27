# Prepare a disposable Sandbox session; does not install or launch on the host.
[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
$repo = if ($PSScriptRoot) { Split-Path $PSScriptRoot -Parent } else { (Get-Location).Path }
if (!(Test-Path -LiteralPath (Join-Path $repo 'docs/store-capture-guide.md'))) {
    throw 'Run from the repository root when executing this script inline.'
}
$report = Get-Content (Join-Path $repo '.artifacts/store-release/package-report.json') -Raw | ConvertFrom-Json
$package = @($report.Packages)[0]
if ((Get-FileHash -LiteralPath $package.Package -Algorithm SHA256).Hash -ne $package.Sha256) {
    throw 'Candidate hash differs from the build report.'
}
$bundle = Join-Path $repo ('.artifacts/store-capture-' + [DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss'))
$app = Join-Path $bundle 'App'
$photos = Join-Path $bundle 'Photos'
$output = Join-Path $repo '.artifacts/store-screenshots'
New-Item -ItemType Directory -Path $app,$photos,$output -Force | Out-Null
Add-Type -AssemblyName System.IO.Compression.FileSystem
[IO.Compression.ZipFile]::ExtractToDirectory($package.Package, $app)
$fixtures = Join-Path $repo 'windows-native/tests/PhotoRepair.Windows.Tests/Fixtures'
$names = @('IMG_20240321_174532.jpg','IMG_20240418_102015.jpg','IMG_20240506_083000.jpg',
    'IMG_20240615_143022.jpg','IMG_20240720_091510.jpg','IMG_20240811_181205.jpg')
for ($index = 0; $index -lt $names.Count; $index++) {
    $source = if ($index % 2 -eq 0) { 'empty.jpg' } else { 'nested.jpg' }
    $target = Join-Path $photos $names[$index]
    Copy-Item -LiteralPath (Join-Path $fixtures $source) -Destination $target
    [IO.File]::SetCreationTime($target, [DateTime]::new(2025,1,15,12,0,0))
    [IO.File]::SetLastWriteTime($target, [DateTime]::new(2025,1,16,12,0,0))
}
# A valid PNG gives the Library a second inspectable format.
Copy-Item -LiteralPath (Join-Path $repo 'windows-native/src/PhotoRepair.App/Assets/StoreLogo.png') -Destination (Join-Path $photos 'IMG_20240901_120000.png')
$launch = @'
$ErrorActionPreference = 'Stop'
try {
    # Never apply development configuration if this launcher is run on the host.
    if ($env:USERNAME -ne 'WDAGUtilityAccount' -or !(Test-Path C:\CaptureSource\App\AppxManifest.xml)) {
        throw 'This launcher must run inside Windows Sandbox as WDAGUtilityAccount.'
    }
    Copy-Item C:\CaptureSource\App C:\PhotoRepair -Recurse -Force
    Copy-Item C:\CaptureSource\Photos C:\TestPhotos -Recurse -Force
    # Loose unsigned registration requires Developer Mode in this disposable guest.
    # https://learn.microsoft.com/windows/advanced-settings/developer-mode
    $developerKey = 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\AppModelUnlock'
    New-Item -Path $developerKey -Force | Out-Null
    New-ItemProperty -Path $developerKey -Name AllowDevelopmentWithoutDevLicense -PropertyType DWord -Value 1 -Force | Out-Null
    Add-AppxPackage -Register C:\PhotoRepair\AppxManifest.xml
    $package = Get-AppxPackage QortxAI.PhotoMetadataRepairInspector
    if (!$package) { throw 'Package registration was not found.' }
    $package | Select-Object Name,Version,PackageFamilyName,InstallLocation |
        ConvertTo-Json | Set-Content C:\CaptureOutput\sandbox-registration.json
    [xml]$manifest = Get-Content C:\PhotoRepair\AppxManifest.xml
    $applicationId = @($manifest.Package.Applications.Application)[0].Id
    $activation = 'shell:AppsFolder\' + $package.PackageFamilyName + '!' + $applicationId
    $shell = New-Object -ComObject WScript.Shell
    $shortcut = $shell.CreateShortcut((Join-Path ([Environment]::GetFolderPath('Desktop')) 'Photo Metadata Repair Inspector.lnk'))
    $shortcut.TargetPath = Join-Path $env:WINDIR 'explorer.exe'
    $shortcut.Arguments = $activation
    $shortcut.IconLocation = 'C:\PhotoRepair\PhotoRepair.App.exe,0'
    $shortcut.Save()
    Start-Process explorer.exe -ArgumentList $activation
    Write-Host 'Verify the app actually opens. Registration alone is not launch evidence.'
} catch {
    $_ | Out-String | Set-Content C:\CaptureOutput\sandbox-launch-error.txt
    Write-Host $_ -ForegroundColor Red
    Write-Host 'Packaged capture is blocked. Do not label direct-executable images as packaged evidence.'
}
Write-Host 'Scan C:\TestPhotos. Read C:\CaptureSource\store-capture-guide.md.'
Read-Host 'Press Enter to close this console'
'@
$launch | Set-Content (Join-Path $bundle 'Launch.ps1') -Encoding utf8
Copy-Item -LiteralPath (Join-Path $repo 'docs/store-capture-guide.md') -Destination $bundle
$hostSource = [Security.SecurityElement]::Escape($bundle)
$hostOutput = [Security.SecurityElement]::Escape($output)
@"
<Configuration>
 <MemoryInMB>4096</MemoryInMB>
 <Networking>Disable</Networking>
 <AudioInput>Disable</AudioInput>
 <MappedFolders>
  <MappedFolder><HostFolder>$hostSource</HostFolder><SandboxFolder>C:\CaptureSource</SandboxFolder><ReadOnly>true</ReadOnly></MappedFolder>
  <MappedFolder><HostFolder>$hostOutput</HostFolder><SandboxFolder>C:\CaptureOutput</SandboxFolder><ReadOnly>false</ReadOnly></MappedFolder>
 </MappedFolders>
 <LogonCommand><Command>powershell.exe -NoProfile -NoExit -Command &amp; ([scriptblock]::Create((Get-Content C:\CaptureSource\Launch.ps1 -Raw)))</Command></LogonCommand>
</Configuration>
"@ | Set-Content (Join-Path $bundle 'StoreCapture.wsb') -Encoding utf8
[pscustomobject]@{
    SourceCommit = (& git -C $repo rev-parse HEAD)
    PreparedAtUtc = [DateTime]::UtcNow.ToString('o')
    CandidateSha256 = $package.Sha256
    Version = $package.Version
    SandboxConfiguration = Join-Path $bundle 'StoreCapture.wsb'
    ScreenshotOutput = $output
    Scope = 'Preparation only. Loose package registration is not signed-MSIX lifecycle validation.'
} | ConvertTo-Json | Set-Content (Join-Path $bundle 'capture-bundle.json') -Encoding utf8
Write-Output $bundle
