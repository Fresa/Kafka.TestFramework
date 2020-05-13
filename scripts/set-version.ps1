$ErrorActionPreference = "Stop"
Write-Host "Versioning is built on the previous build version which is expected to be in semver format, ex: 1.1.6-alpha"
$token = $env:appveyor_api_token #should be defined as a secure variable
$branch = $env:APPVEYOR_REPO_BRANCH

$headers = @{ 
    "Authorization" = "Bearer $token"
    "Content-type" = "application/json"
}
$apiURL = "https://ci.appveyor.com/api/projects/$env:APPVEYOR_ACCOUNT_NAME/$env:APPVEYOR_PROJECT_SLUG"
$history = Invoke-RestMethod -Uri "$apiURL/history?recordsNumber=2" -Headers $headers  -Method Get

$version = (Get-Content .\version)
Write-Host "Current version specified: $version"        
[int]$major = $version.Substring(0, $version.IndexOf("."))
[int]$minor = $version.Substring($version.IndexOf(".") + 1)

# apply versioning strategy if this is not the first build
if ($history.builds.Count -eq 2)
{
    $previousVersion = $history.builds[1].version
    Write-Host "Previous version: $previousVersion"
    [int]$previousMajor = $previousVersion.Substring(0, $previousVersion.IndexOf("."))
    Write-Host "Previous major version: $previousMajor"
    [int]$previousMinor = $previousVersion.Substring($previousVersion.IndexOf(".") + 1, $previousVersion.LastIndexOf(".") - ($previousVersion.IndexOf(".") + 1))
    Write-Host "Previous minor version: $previousMinor"
    # handle suffix, eg. 1.2.3-alpha
    if ([int]$previousVersion.IndexOf("-") -eq -1){
        [int]$previousPatch = $previousVersion.Substring($previousVersion.LastIndexOf(".") + 1)
    } else {
        [int]$previousPatch = $previousVersion.Substring($previousVersion.LastIndexOf(".") + 1, $previousVersion.IndexOf("-")-($previousVersion.LastIndexOf(".") + 1))
    }
    Write-Host "Previous patch version: $previousPatch"
    
    $patch = $previousPatch + 1
    
    if ($previousMajor -ne $major)
    {
        if ($major -ne $previousMajor + 1)
        {
            throw "Major version identity $major can only be incremented by one in regards to previous major $previousMajor"
        }
        
        if ($minor -ne 0)
        {
            throw "Minor version has to be set to 0 when incrementing major version"
        }

        Write-Warning "Major version has been changed, setting patch to 0"
        $patch = 0    
    }
    if ($previousMinor -ne $minor)
    {
        if ($minor -ne $previousMinor + 1)
        {
            throw "Minor version identity $minor can only be incremented by one in regards to previous minor $previousMinor"
        }
        
        Write-Warning "Minor version has been changed, setting patch to 0"
        $patch = 0
    }
} else
{
    # first build
    Write-Warning "No previous builds found, setting patch to 0"
    $patch = 0
}

$versionSuffix = ""
if ($branch -ne "master")
{
    $versionSuffix="-alpha"
}

$currentBuildVersion = "$version.$patch$versionSuffix"
Write-Host "Setting build version to $currentBuildVersion"
Update-AppveyorBuild -Version "$currentBuildVersion"