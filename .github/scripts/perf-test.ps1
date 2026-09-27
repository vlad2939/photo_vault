# Test de performanță pe o bibliotecă de 50.000 de poze (§9 Faza 7), Windows PowerShell 5.1.
# Măsoară: indexare, generare miniaturi, pornire cu indexul plin, căutare, sortare, derulare, memorie.
# Rezultatele ajung în artifacts/perf-results.md și în rezumatul job-ului GitHub Actions.
param(
    [Parameter(Mandatory)] [string] $Exe,
    [Parameter(Mandatory)] [string] $OutDir,
    [int] $Count = 50000
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName UIAutomationClient, UIAutomationTypes, System.Windows.Forms, System.Drawing
Add-Type @"
using System;
using System.Runtime.InteropServices;
public static class Native {
    [DllImport("user32.dll")] public static extern bool SetWindowPos(IntPtr h, IntPtr after, int x, int y, int w, int ht, uint flags);
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
    [DllImport("user32.dll")] public static extern bool SetProcessDPIAware();
}
"@
[Native]::SetProcessDPIAware() | Out-Null

New-Item -ItemType Directory -Force -Path $OutDir | Out-Null
$exePath = (Resolve-Path $Exe).Path
$appDir = Split-Path $exePath
$results = New-Object System.Collections.Generic.List[string]
function Add-Result([string] $name, [string] $value) {
    $results.Add("| $name | $value |")
    Write-Host "$name : $value"
}

function Save-Screen([string] $name) {
    Start-Sleep -Milliseconds 500
    $b = [System.Windows.Forms.Screen]::PrimaryScreen.Bounds
    $bmp = New-Object System.Drawing.Bitmap $b.Width, $b.Height
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.CopyFromScreen($b.Location, [System.Drawing.Point]::Empty, $b.Size)
    $bmp.Save((Join-Path $OutDir "$name.png"), [System.Drawing.Imaging.ImageFormat]::Png)
    $g.Dispose(); $bmp.Dispose()
}

function Find-ByName($root, [string] $name) {
    $cond = New-Object System.Windows.Automation.PropertyCondition ([System.Windows.Automation.AutomationElement]::NameProperty, $name)
    for ($i = 0; $i -lt 40; $i++) {
        $el = $root.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $cond)
        if ($el) { return $el }
        Start-Sleep -Milliseconds 250
    }
    throw "Elementul '$name' nu a fost găsit."
}

function Assert-Alive($proc) {
    if ($proc.HasExited) { throw "Aplicația s-a închis neașteptat (exit code $($proc.ExitCode))." }
}

# Așteaptă un text care începe cu prefixul dat; întoarce textul găsit (verificare la 100 ms → timpi preciși)
function Wait-ForText($root, [string] $prefix, [int] $timeoutSec, $proc) {
    $deadline = (Get-Date).AddSeconds($timeoutSec)
    $textType = New-Object System.Windows.Automation.PropertyCondition ([System.Windows.Automation.AutomationElement]::ControlTypeProperty, [System.Windows.Automation.ControlType]::Text)
    while ((Get-Date) -lt $deadline) {
        if ($proc) { Assert-Alive $proc }
        foreach ($t in $root.FindAll([System.Windows.Automation.TreeScope]::Descendants, $textType)) {
            $name = $t.Current.Name
            if ($name -and $name.StartsWith($prefix)) { return $name }
        }
        Start-Sleep -Milliseconds 100
    }
    throw "Textul '$prefix...' nu a apărut în $timeoutSec s."
}

function Start-App {
    $p = Start-Process -FilePath $exePath -WorkingDirectory $appDir -PassThru
    for ($i = 0; $i -lt 120 -and $p.MainWindowHandle -eq 0; $i++) { Start-Sleep -Milliseconds 250; $p.Refresh() }
    Assert-Alive $p
    $screen = [System.Windows.Forms.Screen]::PrimaryScreen.Bounds
    [Native]::SetWindowPos($p.MainWindowHandle, [IntPtr]::Zero, 0, 0, [Math]::Min(1440, $screen.Width), [Math]::Min(900, $screen.Height - 40), 0x0040) | Out-Null
    [Native]::SetForegroundWindow($p.MainWindowHandle) | Out-Null
    return $p
}

function Get-MemoryMb($proc) {
    $proc.Refresh()
    return [int]($proc.WorkingSet64 / 1MB)
}

# ---- Colecția de test: 20 de imagini 640×480 distincte, copiate în 50 de subfoldere ----
$photosDir = Join-Path ([IO.Path]::GetTempPath()) 'PhotoVaultPerf\Poze'
if (Test-Path $photosDir) { Remove-Item -Recurse -Force $photosDir }
$sw = [Diagnostics.Stopwatch]::StartNew()
$templates = @()
$colors = @('#E07A5F','#3D405B','#81B29A','#F2CC8F','#6D597A','#355070','#B56576','#EAAC8B','#2A9D8F','#E9C46A')
$templateDir = Join-Path ([IO.Path]::GetTempPath()) 'PhotoVaultPerf\Templates'
New-Item -ItemType Directory -Force -Path $templateDir | Out-Null
for ($t = 0; $t -lt 20; $t++) {
    $bmp = New-Object System.Drawing.Bitmap 640, 480
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.Clear([System.Drawing.ColorTranslator]::FromHtml($colors[$t % $colors.Length]))
    $g.FillEllipse([System.Drawing.Brushes]::White, 180 + $t * 5, 120, 260, 240)
    $g.DrawString("T$t", (New-Object System.Drawing.Font 'Segoe UI', 40), [System.Drawing.Brushes]::Black, 20, 20)
    $path = Join-Path $templateDir "t$t.jpg"
    $bmp.Save($path, [System.Drawing.Imaging.ImageFormat]::Jpeg)
    $g.Dispose(); $bmp.Dispose()
    $templates += $path
}
for ($i = 1; $i -le $Count; $i++) {
    $sub = Join-Path $photosDir ("{0:D2}" -f ($i % 50))
    if ($i -le 50) { New-Item -ItemType Directory -Force -Path $sub | Out-Null }
    [IO.File]::Copy($templates[$i % 20], (Join-Path $sub ("IMG_{0:D6}.jpg" -f $i)))
}
$size = [int]((Get-ChildItem $photosDir -Recurse -File | Measure-Object Length -Sum).Sum / 1MB)
Add-Result 'Colecție de test' ("{0:N0} poze JPEG 640×480, {1} MB, generate în {2:N0} s" -f $Count, $size, $sw.Elapsed.TotalSeconds)

# ---- Pornire cu bibliotecă goală + adăugare folder (dialogul nativ) ----
$proc = Start-App
Start-Sleep -Seconds 2
$root = [System.Windows.Automation.AutomationElement]::FromHandle($proc.MainWindowHandle)
$root.FindFirst([System.Windows.Automation.TreeScope]::Descendants,
    (New-Object System.Windows.Automation.PropertyCondition ([System.Windows.Automation.AutomationElement]::NameProperty, 'Adaugă folder sursă'))
).GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke()
Start-Sleep -Seconds 2
[System.Windows.Forms.SendKeys]::SendWait([regex]::Replace($photosDir, '[+^%~(){}\[\]]', '{$0}'))
Start-Sleep -Milliseconds 500
[System.Windows.Forms.SendKeys]::SendWait('{ENTER}')
Start-Sleep -Seconds 2
$pickCond = New-Object System.Windows.Automation.PropertyCondition ([System.Windows.Automation.AutomationElement]::NameProperty, 'Alege un folder cu poze')
if ([System.Windows.Automation.AutomationElement]::RootElement.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $pickCond)) {
    [System.Windows.Forms.SendKeys]::SendWait('{ENTER}')
}
$sw.Restart()

# Indexarea se termină când grid-ul afișează toate pozele; miniaturile continuă pe fundal
$countText = "{0:N0} de poze" -f $Count
$countText = $countText.Replace(',', '.')
Wait-ForText $root $countText 900 $proc | Out-Null
Add-Result 'Indexare (scanare + inserare în DB)' ("{0:N1} s" -f $sw.Elapsed.TotalSeconds)
Save-Screen 'perf-01-indexed'

# Căutare în timp ce miniaturile se generează: UI-ul trebuie să rămână responsiv
$search = Find-ByName $root 'Caută (nume, tag, album)...'
$search.SetFocus()
$t0 = [Diagnostics.Stopwatch]::StartNew()
[System.Windows.Forms.SendKeys]::SendWait('IMG_0123')
Wait-ForText $root '100 de poze' 30 $proc | Out-Null
Add-Result 'Căutare în timpul generării miniaturilor (tastare + debounce 300 ms + filtrare)' ("{0:N0} ms" -f $t0.Elapsed.TotalMilliseconds)
[System.Windows.Forms.SendKeys]::SendWait('{ESC}')
Wait-ForText $root $countText 30 $proc | Out-Null

$thumbs = Wait-ForText $root 'Miniaturi generate' 2700 $proc
Add-Result 'Miniaturi generate (de la adăugarea folderului)' ("{0:N0} s ({1:N1} ms / poză) — {2}" -f $sw.Elapsed.TotalSeconds, ($sw.Elapsed.TotalMilliseconds / $Count), $thumbs)
Add-Result 'Memorie după indexare + miniaturi' ("{0} MB" -f (Get-MemoryMb $proc))
Save-Screen 'perf-02-thumbnails-done'

# ---- Căutare, sortare, derulare cu biblioteca completă ----
$search.SetFocus()
$t0.Restart()
[System.Windows.Forms.SendKeys]::SendWait('IMG_04')
Wait-ForText $root '10.000 de poze' 30 $proc | Out-Null
Add-Result 'Căutare „IMG_04" → 10.000 de rezultate' ("{0:N0} ms (include debounce 300 ms)" -f $t0.Elapsed.TotalMilliseconds)
$t0.Restart()
[System.Windows.Forms.SendKeys]::SendWait('{ESC}')
Wait-ForText $root $countText 30 $proc | Out-Null
Add-Result 'Golire căutare → toate cele 50.000' ("{0:N0} ms" -f $t0.Elapsed.TotalMilliseconds)

$sort = $root.FindFirst([System.Windows.Automation.TreeScope]::Descendants,
    (New-Object System.Windows.Automation.PropertyCondition ([System.Windows.Automation.AutomationElement]::ControlTypeProperty, [System.Windows.Automation.ControlType]::ComboBox)))
$t0.Restart()
$sort.GetCurrentPattern([System.Windows.Automation.ExpandCollapsePattern]::Pattern).Expand()
$items = $sort.FindAll([System.Windows.Automation.TreeScope]::Descendants,
    (New-Object System.Windows.Automation.PropertyCondition ([System.Windows.Automation.AutomationElement]::ControlTypeProperty, [System.Windows.Automation.ControlType]::ListItem)))
$items[1].GetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern).Select()
$sort.GetCurrentPattern([System.Windows.Automation.ExpandCollapsePattern]::Pattern).Collapse()
$grid = Find-ByName $root 'PhotoGrid'
$itemType = New-Object System.Windows.Automation.PropertyCondition ([System.Windows.Automation.AutomationElement]::ControlTypeProperty, [System.Windows.Automation.ControlType]::ListItem)
while ($t0.Elapsed.TotalSeconds -lt 30) {
    $first = $grid.FindFirst([System.Windows.Automation.TreeScope]::Children, $itemType)
    if ($first -and $first.Current.Name -like 'IMG_050000*') { break }
    Start-Sleep -Milliseconds 50
}
Add-Result 'Sortare Z → A pe 50.000' ("{0:N0} ms" -f $t0.Elapsed.TotalMilliseconds)

# Derulare: 40 × PageDown prin grid (containere reciclate, miniaturi din cache / disc)
$grid.SetFocus()
$t0.Restart()
for ($i = 0; $i -lt 40; $i++) { [System.Windows.Forms.SendKeys]::SendWait('{PGDN}') }
Start-Sleep -Milliseconds 300
Add-Result 'Derulare 40 × PageDown' ("{0:N0} ms" -f ($t0.Elapsed.TotalMilliseconds - 300))
[System.Windows.Forms.SendKeys]::SendWait('^{END}')
Start-Sleep -Seconds 1
Save-Screen 'perf-03-scrolled-end'
Add-Result 'Memorie după căutări / derulare' ("{0} MB" -f (Get-MemoryMb $proc))
Assert-Alive $proc

# ---- Repornire: încărcarea indexului plin ----
$proc.CloseMainWindow() | Out-Null
if (-not $proc.WaitForExit(20000)) { $proc.Kill(); throw "Aplicația nu s-a închis la cerere." }
$t0.Restart()
$proc = Start-App
$root = [System.Windows.Automation.AutomationElement]::FromHandle($proc.MainWindowHandle)
Wait-ForText $root $countText 120 $proc | Out-Null
Add-Result 'Pornire cu 50.000 de poze în index (până la grid complet)' ("{0:N1} s" -f $t0.Elapsed.TotalSeconds)
Start-Sleep -Seconds 2
Add-Result 'Memorie după pornire' ("{0} MB" -f (Get-MemoryMb $proc))
Save-Screen 'perf-04-restart'
$db = [int]((Get-Item (Join-Path $appDir 'data/photovault.db')).Length / 1MB)
$th = [int]((Get-ChildItem (Join-Path $appDir 'data/thumbnails') -Recurse -File | Measure-Object Length -Sum).Sum / 1MB)
Add-Result 'Dimensiune data/ (bază de date / miniaturi)' ("{0} MB / {1} MB" -f $db, $th)
$proc.CloseMainWindow() | Out-Null
$proc.WaitForExit(20000) | Out-Null

$logs = Join-Path $appDir 'logs'
if ((Test-Path $logs) -and (Get-ChildItem $logs -File)) {
    Get-ChildItem $logs -File | ForEach-Object { Get-Content $_.FullName | Select-Object -First 40 | Write-Host }
    throw "Aplicația a scris erori în logs/."
}

$report = @("## PhotoVault — performanță pe {0:N0} de poze" -f $Count, '', '| Măsurătoare | Rezultat |', '|---|---|') + $results
$report | Set-Content -Encoding UTF8 (Join-Path $OutDir 'perf-results.md')
if ($env:GITHUB_STEP_SUMMARY) { $report | Add-Content -Encoding UTF8 $env:GITHUB_STEP_SUMMARY }
Write-Host "Test de performanță OK."
