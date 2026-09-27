[CmdletBinding()]
param(
    [string]$OutputDirectory = '.artifacts/store-release'
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = if ($PSScriptRoot) { Split-Path $PSScriptRoot -Parent } else { (Get-Location).Path }
$outputPath = if ([IO.Path]::IsPathRooted($OutputDirectory)) {
    [IO.Path]::GetFullPath($OutputDirectory)
} else {
    [IO.Path]::GetFullPath((Join-Path $repositoryRoot $OutputDirectory))
}
New-Item -ItemType Directory -Path $outputPath -Force | Out-Null
$project = Join-Path $repositoryRoot 'windows-native/src/PhotoRepair.App/PhotoRepair.App.csproj'
$manifestPath = Join-Path $repositoryRoot 'windows-native/src/PhotoRepair.App/Package.appxmanifest'
[xml]$sourceManifest = Get-Content -LiteralPath $manifestPath -Raw
$version = $sourceManifest.Package.Identity.Version
$packageDirectory = Join-Path $outputPath "$version/"

# Build a direct MSIX, a supported Partner Center upload format.
# Signing is performed by the Store; this script does not install a certificate.
& dotnet build $project -c Release -p:Platform=x64 `
    -p:GenerateAppxPackageOnBuild=true -p:AppxPackageSigningEnabled=false `
    -p:UapAppxPackageBuildMode=SideloadOnly -p:AppxBundle=Never `
    "-p:AppxPackageDir=$packageDirectory"
if ($LASTEXITCODE -ne 0) { throw 'Store package build failed.' }

Add-Type -AssemblyName System.IO.Compression.FileSystem
$packages = @(Get-ChildItem -LiteralPath $packageDirectory -Filter '*.msix' -Recurse)
if ($packages.Count -eq 0) { throw 'The build produced no MSIX package.' }
$reports = foreach ($package in $packages) {
    $archive = [IO.Compression.ZipFile]::OpenRead($package.FullName)
    try {
        $entry = $archive.GetEntry('AppxManifest.xml')
        if ($null -eq $entry) { throw 'MSIX is missing AppxManifest.xml.' }
        $reader = [IO.StreamReader]::new($entry.Open())
        try { [xml]$manifest = $reader.ReadToEnd() } finally { $reader.Dispose() }
        $identity = $manifest.Package.Identity
        if ($identity.Name -ne 'QortxAI.PhotoMetadataRepairInspector' -or
            $identity.Publisher -ne 'CN=7F9981DB-6481-4EE5-8747-A3A63C186D7D' -or
            $manifest.Package.Properties.PublisherDisplayName -ne 'QortxAI') {
            throw 'Built package does not match the reserved Store identity.'
        }
        if ($identity.ProcessorArchitecture -ne 'x64' -or $identity.Version -ne $version) {
            throw 'Built architecture or version differs from the release configuration.'
        }
        if ($manifest.Package.Dependencies.TargetDeviceFamily.Name -ne 'Windows.Desktop') {
            throw 'Release package must target Windows.Desktop.'
        }
        $capabilities = @($manifest.Package.Capabilities.ChildNodes | ForEach-Object { $_.Name })
        if ('runFullTrust' -notin $capabilities) { throw 'Desktop capability is missing.' }
        [pscustomobject]@{
            Package = $package.FullName
            Version = [string]$identity.Version
            Architecture = [string]$identity.ProcessorArchitecture
            Identity = [string]$identity.Name
            MinimumWindowsVersion = [string]$manifest.Package.Dependencies.TargetDeviceFamily.MinVersion
            Capabilities = $capabilities
            Sha256 = (Get-FileHash -LiteralPath $package.FullName -Algorithm SHA256).Hash
        }
    } finally { $archive.Dispose() }
}
$report = [pscustomobject]@{
    BuiltAtUtc = [DateTime]::UtcNow.ToString('o')
    Packages = @($reports)
    ValidationScope = 'Build and manifest checks only; not installation or Store certification.'
}
$report | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $outputPath 'package-report.json') -Encoding utf8
$report | ConvertTo-Json -Depth 6
