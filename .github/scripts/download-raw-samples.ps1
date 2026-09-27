# Descarcă câteva fișiere RAW reale (CR2 / NEF / DNG) din surse publice, pentru validarea extragerii
# previzualizării (§9 Faza 8). Totul e „cel mai bun efort": o sursă indisponibilă nu oprește build-ul.
#   Surse: raw.pixls.us (arhivă publică de mostre RAW, CC0) și câteva repo-uri GitHub cu fișiere de test.
param(
    [Parameter(Mandatory)] [string] $OutDir,
    [int] $PerFormat = 2,
    [long] $MaxBytes = 45MB
)

$ErrorActionPreference = 'Continue'
$ProgressPreference = 'SilentlyContinue'
New-Item -ItemType Directory -Force -Path $OutDir | Out-Null
$headers = @{ 'User-Agent' = 'PhotoVault-CI' }
if ($env:GITHUB_TOKEN) { $headers['Authorization'] = "Bearer $env:GITHUB_TOKEN" }

$wanted = @{ 'cr2' = 0; 'nef' = 0; 'dng' = 0 }

function Save-Sample([string] $url, [string] $ext) {
    if ($wanted[$ext] -ge $PerFormat) { return }
    try {
        $head = Invoke-WebRequest -Uri $url -Method Head -Headers $headers -UseBasicParsing -TimeoutSec 30
        $len = [long]($head.Headers['Content-Length'] | Select-Object -First 1)
        if ($len -gt $MaxBytes) { return }
        $name = [Uri]::UnescapeDataString(($url -split '/')[-1])
        $target = Join-Path $OutDir ("{0}_{1}" -f $ext, $name)
        Invoke-WebRequest -Uri $url -OutFile $target -Headers $headers -UseBasicParsing -TimeoutSec 180
        $wanted[$ext]++
        Write-Host ("Descărcat: {0} ({1:N1} MB) ← {2}" -f $name, ((Get-Item $target).Length / 1MB), $url)
    } catch {
        Write-Host "Nu s-a putut descărca $url : $($_.Exception.Message)"
    }
}

# ---- raw.pixls.us: listări de directoare (marcă → model → fișiere) ----
function Get-Links([string] $url) {
    try {
        $html = (Invoke-WebRequest -Uri $url -Headers $headers -UseBasicParsing -TimeoutSec 30).Content
        return [regex]::Matches($html, 'href="([^"?#]+)"') | ForEach-Object { $_.Groups[1].Value } |
            Where-Object { $_ -notmatch '^(\.\./|/|https?:)' } | Select-Object -Unique
    } catch {
        Write-Host "Listare indisponibilă: $url"
        return @()
    }
}

$pixls = 'https://raw.pixls.us/data/'
foreach ($source in @(@('Canon', 'cr2'), @('Nikon', 'nef'), @('Pentax', 'dng'), @('Leica', 'dng'), @('Ricoh', 'dng'))) {
    $make, $ext = $source
    if ($wanted[$ext] -ge $PerFormat) { continue }
    $models = Get-Links "$pixls$make/" | Where-Object { $_.EndsWith('/') } | Select-Object -First 12
    foreach ($model in $models) {
        if ($wanted[$ext] -ge $PerFormat) { break }
        $files = Get-Links "$pixls$make/$model" | Where-Object { $_ -match "\.$ext$" } | Select-Object -First 1
        foreach ($file in $files) { Save-Sample "$pixls$make/$model$file" $ext }
    }
}

# ---- GitHub: directoare publice cu fișiere RAW de test (listare prin API) ----
$githubDirs = @(
    'drewnoakes/metadata-extractor-images/cr2',
    'drewnoakes/metadata-extractor-images/nef',
    'drewnoakes/metadata-extractor-images/dng',
    'letmaik/rawpy/test'
)
foreach ($dir in $githubDirs) {
    if (($wanted.Values | Measure-Object -Minimum).Minimum -ge $PerFormat) { break }
    $owner, $repo, $path = $dir -split '/', 3
    try {
        $items = Invoke-RestMethod -Uri "https://api.github.com/repos/$owner/$repo/contents/$path" -Headers $headers -TimeoutSec 30
    } catch {
        Write-Host "Listare GitHub indisponibilă: $dir"
        continue
    }
    foreach ($item in $items | Where-Object { $_.type -eq 'file' -and $_.size -le $MaxBytes }) {
        $ext = [IO.Path]::GetExtension($item.name).TrimStart('.').ToLowerInvariant()
        if ($wanted.ContainsKey($ext)) { Save-Sample $item.download_url $ext }
    }
}

$summary = "Mostre RAW descărcate: CR2 = $($wanted['cr2']), NEF = $($wanted['nef']), DNG = $($wanted['dng'])"
Write-Host $summary
if ($env:GITHUB_STEP_SUMMARY) { "### $summary" | Add-Content -Encoding UTF8 $env:GITHUB_STEP_SUMMARY }
