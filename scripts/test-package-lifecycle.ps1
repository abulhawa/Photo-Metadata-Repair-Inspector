# Run in a disposable Windows test profile, not the development profile.
# Install needs an elevated PowerShell for temporary certificate trust.
[CmdletBinding()]
param(
    [Parameter(Mandatory)][ValidateSet('Install','Update','Uninstall')][string]$Stage,
    [Parameter(Mandatory)][string]$BundleDirectory
)
$ErrorActionPreference = 'Stop'
$name = 'QortxAI.PhotoMetadataRepairInspector'
$bundle = [IO.Path]::GetFullPath($BundleDirectory)
$existing = Get-AppxPackage -Name $name
$certificatePath = Join-Path $bundle 'test-publisher.cer'
$certificate = [Security.Cryptography.X509Certificates.X509Certificate2]::new($certificatePath)
$trustPath = "Cert:/LocalMachine/TrustedPeople/$($certificate.Thumbprint)"
$reportPath = Join-Path $bundle "lifecycle-$Stage.json"
switch ($Stage) {
    'Install' {
        if ($existing) { throw 'A package already exists. Use a clean disposable profile; this script will not remove it.' }
        $admin = [Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()
        if (!$admin.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
            throw 'Run Install in elevated PowerShell in the disposable test environment.'
        }
        if (Test-Path -LiteralPath $trustPath) { throw 'Test certificate is already trusted; use a clean environment.' }
        Import-Certificate -FilePath $certificatePath -CertStoreLocation Cert:/LocalMachine/TrustedPeople | Out-Null
        try {
            Add-AppxPackage -Path (Join-Path $bundle 'PhotoRepair-1.0.0.0-test.msix')
            $installed = Get-AppxPackage -Name $name
            if ($installed.Version -ne '1.0.0.0') { throw 'Installed version mismatch.' }
        } catch {
            Remove-Item -LiteralPath $trustPath
            throw
        }
    }
    'Update' {
        if (!$existing -or $existing.Version -ne '1.0.0.0') { throw 'Update requires test version 1.0.0.0.' }
        Add-AppxPackage -Path (Join-Path $bundle 'PhotoRepair-1.0.1.0-test.msix')
        $installed = Get-AppxPackage -Name $name
        if ($installed.Version -ne '1.0.1.0') { throw 'Updated version mismatch.' }
    }
    'Uninstall' {
        if (!$existing -or $existing.Version -ne '1.0.1.0') { throw 'Uninstall requires test version 1.0.1.0.' }
        Remove-AppxPackage -Package $existing.PackageFullName
        if (Get-AppxPackage -Name $name) { throw 'Package remains registered after uninstall.' }
        # Remove only this bundle's short-lived test certificate, not other trust.
        if (Test-Path -LiteralPath $trustPath) { Remove-Item -LiteralPath $trustPath }
        $installed = $null
    }
}
[pscustomobject]@{
    Stage = $Stage
    TimeUtc = [DateTime]::UtcNow.ToString('o')
    WindowsVersion = [Environment]::OSVersion.Version.ToString()
    PackageFullName = $installed.PackageFullName
    Version = [string]$installed.Version
    Scope = 'Package registration only; launch, screenshots and user-file retention require manual verification.'
} | ConvertTo-Json | Set-Content -LiteralPath $reportPath -Encoding utf8
Get-Content -LiteralPath $reportPath
