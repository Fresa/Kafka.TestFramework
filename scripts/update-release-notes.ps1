$ErrorActionPreference = "Stop"
git fetch --tags
$tags = git tag -l v*
if ($tags)
{
    $releaseNotes = git log "$(git describe --tags --match v* --abbrev=0)..$($env:APPVEYOR_REPO_COMMIT)" --pretty=format:"-%s" --no-merges
} 
else
{
    $releaseNotes = git log $($env:APPVEYOR_REPO_COMMIT) --pretty=format:"-%s" --no-merges
}

$releaseNotesAsString = if ($releaseNotes -eq $null) { "" } else { [string]::join("`n", $releaseNotes) }

Write-Host "Release notes: '$($releaseNotesAsString)'"

$path = "$env:APPVEYOR_PROJECT_NAME/$env:APPVEYOR_PROJECT_NAME.csproj"
[xml]$xml = Get-Content -Path $path
$xml.GetElementsByTagName("PackageReleaseNotes").set_InnerXML("$releaseNotesAsString")
Set-Content $path -Value $xml.InnerXml -Force