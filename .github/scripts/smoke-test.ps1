# Smoke test UI pentru PhotoVault (Windows PowerShell 5.1):
# pornește aplicația, verifică că nu se închide/nu scrie erori în logs/,
# acționează butoanele prin UI Automation și salvează capturi de ecran.
param(
    [Parameter(Mandatory)] [string] $Exe,
    [Parameter(Mandatory)] [string] $OutDir
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

function Save-Screen([string] $name) {
    Start-Sleep -Milliseconds 700
    $b = [System.Windows.Forms.Screen]::PrimaryScreen.Bounds
    $bmp = New-Object System.Drawing.Bitmap $b.Width, $b.Height
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.CopyFromScreen($b.Location, [System.Drawing.Point]::Empty, $b.Size)
    $bmp.Save((Join-Path $OutDir "$name.png"), [System.Drawing.Imaging.ImageFormat]::Png)
    $g.Dispose(); $bmp.Dispose()
    Write-Host "Captură: $name.png"
}

function Find-ByName($root, [string] $name) {
    $cond = New-Object System.Windows.Automation.PropertyCondition ([System.Windows.Automation.AutomationElement]::NameProperty, $name)
    for ($i = 0; $i -lt 20; $i++) {
        $el = $root.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $cond)
        if ($el) { return $el }
        Start-Sleep -Milliseconds 250
    }
    throw "Elementul '$name' nu a fost găsit."
}

function Invoke-Element($el) {
    $el.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke()
}

function Assert-Alive($proc) {
    if ($proc.HasExited) { throw "Aplicația s-a închis neașteptat (exit code $($proc.ExitCode))." }
}

function Find-TopWindow([string] $name, [int] $timeoutSec = 15) {
    $cond = New-Object System.Windows.Automation.PropertyCondition ([System.Windows.Automation.AutomationElement]::NameProperty, $name)
    $deadline = (Get-Date).AddSeconds($timeoutSec)
    while ((Get-Date) -lt $deadline) {
        $w = [System.Windows.Automation.AutomationElement]::RootElement.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $cond)
        if ($w) { return $w }
        Start-Sleep -Milliseconds 300
    }
    throw "Fereastra '$name' nu a apărut."
}

function Wait-ForText($root, [string] $prefix, [int] $timeoutSec) {
    $deadline = (Get-Date).AddSeconds($timeoutSec)
    $textType = New-Object System.Windows.Automation.PropertyCondition ([System.Windows.Automation.AutomationElement]::ControlTypeProperty, [System.Windows.Automation.ControlType]::Text)
    while ((Get-Date) -lt $deadline) {
        foreach ($t in $root.FindAll([System.Windows.Automation.TreeScope]::Descendants, $textType)) {
            if ($t.Current.Name.StartsWith($prefix)) { return $t.Current.Name }
        }
        Start-Sleep -Milliseconds 400
    }
    throw "Textul '$prefix...' nu a apărut în $timeoutSec s."
}

function Get-GridItems($root) {
    $grid = Find-ByName $root 'PhotoGrid'
    $itemType = New-Object System.Windows.Automation.PropertyCondition ([System.Windows.Automation.AutomationElement]::ControlTypeProperty, [System.Windows.Automation.ControlType]::ListItem)
    return @{ Grid = $grid; Items = $grid.FindAll([System.Windows.Automation.TreeScope]::Children, $itemType) }
}

# Colecție de test: 1.500 JPEG-uri în subfoldere + câteva PNG (verifică indexarea recursivă și virtualizarea)
function New-TestPhotos([string] $root, [int] $count) {
    $colors = @('#E07A5F','#3D405B','#81B29A','#F2CC8F','#6D597A','#355070','#B56576','#EAAC8B','#2A9D8F','#E9C46A')
    for ($i = 1; $i -le $count; $i++) {
        $sub = Join-Path $root ("{0:D4}" -f (2000 + ($i % 7)))
        New-Item -ItemType Directory -Force -Path $sub | Out-Null
        $w = if ($i % 5 -eq 0) { 480 } else { 720 }; $h = if ($i % 5 -eq 0) { 720 } else { 480 }
        $bmp = New-Object System.Drawing.Bitmap $w, $h
        $g = [System.Drawing.Graphics]::FromImage($bmp)
        $g.Clear([System.Drawing.ColorTranslator]::FromHtml($colors[$i % $colors.Length]))
        $g.FillEllipse([System.Drawing.Brushes]::White, [int]($w * 0.3), [int]($h * 0.3), [int]($w * 0.4), [int]($h * 0.4))
        $g.DrawString("$i", (New-Object System.Drawing.Font 'Segoe UI', 48), [System.Drawing.Brushes]::Black, 20, 20)
        $ext = if ($i % 50 -eq 0) { 'png' } else { 'jpg' }
        $fmt = if ($ext -eq 'png') { [System.Drawing.Imaging.ImageFormat]::Png } else { [System.Drawing.Imaging.ImageFormat]::Jpeg }
        $bmp.Save((Join-Path $sub ("DSC_{0:D4}.{1}" -f $i, $ext)), $fmt)
        $g.Dispose(); $bmp.Dispose()
    }
}

$photosDir = Join-Path ([IO.Path]::GetTempPath()) 'PhotoVaultSmoke\Poze'
if (Test-Path $photosDir) { Remove-Item -Recurse -Force $photosDir }
$sw = [Diagnostics.Stopwatch]::StartNew()
New-TestPhotos $photosDir 1500
Write-Host "Poze de test generate în $([int]$sw.Elapsed.TotalSeconds) s: $photosDir"

$screen = [System.Windows.Forms.Screen]::PrimaryScreen.Bounds
Write-Host "Rezoluție ecran: $($screen.Width)x$($screen.Height)"

$proc = Start-Process -FilePath $exePath -WorkingDirectory $appDir -PassThru
for ($i = 0; $i -lt 60 -and $proc.MainWindowHandle -eq 0; $i++) { Start-Sleep -Milliseconds 500; $proc.Refresh() }
Assert-Alive $proc
if ($proc.MainWindowHandle -eq 0) { throw "Fereastra principală nu a apărut." }
$hwnd = $proc.MainWindowHandle

# Fereastra încape pe ecran (runner-ele au adesea rezoluție mică)
$w = [Math]::Min(1440, $screen.Width); $h = [Math]::Min(900, $screen.Height - 40)
[Native]::SetWindowPos($hwnd, [IntPtr]::Zero, 0, 0, $w, $h, 0x0040) | Out-Null
[Native]::SetForegroundWindow($hwnd) | Out-Null
Start-Sleep -Seconds 2
Assert-Alive $proc

$root = [System.Windows.Automation.AutomationElement]::FromHandle($hwnd)
Save-Screen '01-main-dark'

# ---- Faza 1: adăugare folder sursă prin dialogul nativ + indexare + miniaturi ----
Invoke-Element (Find-ByName $root 'Adaugă folder sursă')
$picker = Find-TopWindow 'Alege un folder cu poze'
Start-Sleep -Seconds 1
Save-Screen '01b-folder-picker'
# Câmpul „Folder:" are focus la deschidere → calea se tastează direct (caracterele speciale SendKeys sunt escapate)
$escaped = [regex]::Replace($photosDir, '[+^%~(){}\[\]]', '{$0}')
[System.Windows.Forms.SendKeys]::SendWait($escaped)
Start-Sleep -Milliseconds 500
$selectCond = New-Object System.Windows.Automation.AndCondition @(
    (New-Object System.Windows.Automation.PropertyCondition ([System.Windows.Automation.AutomationElement]::NameProperty, 'Select Folder')),
    (New-Object System.Windows.Automation.PropertyCondition ([System.Windows.Automation.AutomationElement]::ControlTypeProperty, [System.Windows.Automation.ControlType]::Button)))
Invoke-Element ($picker.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $selectCond))

$sw.Restart()
$done = Wait-ForText $root 'Miniaturi generate' 240
Write-Host "Indexare + miniaturi: $([int]$sw.Elapsed.TotalSeconds) s — footer: '$done'"
Assert-Alive $proc
Save-Screen '02-grid-dark'

$grid = Get-GridItems $root
Write-Host "Elemente expuse de grid prin UI Automation: $($grid.Items.Count)"
if ($grid.Items.Count -lt 2) { throw "Grid-ul nu afișează pozele indexate." }
Wait-ForText $root '1.500 poze' 5 | Out-Null

# Selecție (contur + bifă accent)
$grid.Items[1].GetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern).Select()
Save-Screen '03-grid-selected-dark'

# Derulare la jumătatea colecției
$grid.Grid.GetCurrentPattern([System.Windows.Automation.ScrollPattern]::Pattern).SetScrollPercent(-1, 50)
Start-Sleep -Seconds 1
Save-Screen '04-grid-scrolled-dark'
$grid.Grid.GetCurrentPattern([System.Windows.Automation.ScrollPattern]::Pattern).SetScrollPercent(-1, 0)

# Dropdown sortare (ComboBox restilizat)
$combo = $root.FindFirst([System.Windows.Automation.TreeScope]::Descendants,
    (New-Object System.Windows.Automation.PropertyCondition ([System.Windows.Automation.AutomationElement]::ControlTypeProperty, [System.Windows.Automation.ControlType]::ComboBox)))
if ($combo) {
    $combo.GetCurrentPattern([System.Windows.Automation.ExpandCollapsePattern]::Pattern).Expand()
    Save-Screen '05-combobox-dark'
    $combo.GetCurrentPattern([System.Windows.Automation.ExpandCollapsePattern]::Pattern).Collapse()
}

# Meniu contextual pe câmpul de căutare
$edit = $root.FindFirst([System.Windows.Automation.TreeScope]::Descendants,
    (New-Object System.Windows.Automation.PropertyCondition ([System.Windows.Automation.AutomationElement]::ControlTypeProperty, [System.Windows.Automation.ControlType]::Edit)))
if ($edit) {
    $edit.SetFocus()
    [System.Windows.Forms.SendKeys]::SendWait('+{F10}')
    Save-Screen '06-contextmenu-dark'
    [System.Windows.Forms.SendKeys]::SendWait('{ESC}')
}

# Dialog custom (butonul Info afișează mesajul „În curând" în Faza 0)
Invoke-Element (Find-ByName $root 'Informații')
Save-Screen '07-dialog-dark'
[System.Windows.Forms.SendKeys]::SendWait('{ESC}')
Start-Sleep -Milliseconds 500
Assert-Alive $proc

# Comutare pe tema luminoasă
Invoke-Element (Find-ByName $root 'Comută pe tema luminoasă')
Save-Screen '08-grid-light'

Invoke-Element (Find-ByName $root 'Informații')
Save-Screen '09-dialog-light'
[System.Windows.Forms.SendKeys]::SendWait('{ESC}')
Start-Sleep -Milliseconds 500
Assert-Alive $proc

# Tema trebuie să persiste între porniri (AppSettings.Theme)
$proc.CloseMainWindow() | Out-Null
if (-not $proc.WaitForExit(10000)) { $proc.Kill(); throw "Aplicația nu s-a închis la cerere." }

$proc = Start-Process -FilePath $exePath -WorkingDirectory $appDir -PassThru
for ($i = 0; $i -lt 60 -and $proc.MainWindowHandle -eq 0; $i++) { Start-Sleep -Milliseconds 500; $proc.Refresh() }
Assert-Alive $proc
$root = [System.Windows.Automation.AutomationElement]::FromHandle($proc.MainWindowHandle)
Find-ByName $root 'Comută pe tema întunecată' | Out-Null   # butonul de temă indică tema Light activă
Write-Host "Tema Light a fost reîncărcată corect după repornire."
Wait-ForText $root '1.500 poze' 15 | Out-Null
Write-Host "Indexul (1.500 poze) a fost reîncărcat corect după repornire."
Invoke-Element (Find-ByName $root 'Comută pe tema întunecată')   # revenire la Dark pentru rulările următoare
Start-Sleep -Milliseconds 500
$proc.CloseMainWindow() | Out-Null
$proc.WaitForExit(10000) | Out-Null

# Verificări finale: baza de date creată, fără erori logate
if (-not (Test-Path (Join-Path $appDir 'data/photovault.db'))) { throw "data/photovault.db nu a fost creat." }
$logs = Join-Path $appDir 'logs'
if ((Test-Path $logs) -and (Get-ChildItem $logs -Filter *.log)) {
    Get-ChildItem $logs -Filter *.log | ForEach-Object { Get-Content $_.FullName | Write-Host }
    throw "Aplicația a scris erori în logs/."
}
Write-Host "Smoke test OK."
