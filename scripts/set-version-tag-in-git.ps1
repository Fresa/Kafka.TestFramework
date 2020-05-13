$ErrorActionPreference = "Stop"
git config --global credential.helper store
Add-Content -Path "$env:USERPROFILE\.git-credentials" -Value "https://$($env:git_access_token):x-oauth-basic@github.com`n" -NoNewline
git config --global user.email "fresa@fresa.se"
git config --global user.name "Fredrik Arvidsson"
git tag v$($env:APPVEYOR_BUILD_VERSION) $($env:APPVEYOR_REPO_COMMIT)
git push origin --tags --quiet