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

# Dropdown sortare (ComboBox restilizat)
$combo = $root.FindFirst([System.Windows.Automation.TreeScope]::Descendants,
    (New-Object System.Windows.Automation.PropertyCondition ([System.Windows.Automation.AutomationElement]::ControlTypeProperty, [System.Windows.Automation.ControlType]::ComboBox)))
if ($combo) {
    $combo.GetCurrentPattern([System.Windows.Automation.ExpandCollapsePattern]::Pattern).Expand()
    Save-Screen '02-combobox-dark'
    $combo.GetCurrentPattern([System.Windows.Automation.ExpandCollapsePattern]::Pattern).Collapse()
}

# Meniu contextual pe câmpul de căutare
$edit = $root.FindFirst([System.Windows.Automation.TreeScope]::Descendants,
    (New-Object System.Windows.Automation.PropertyCondition ([System.Windows.Automation.AutomationElement]::ControlTypeProperty, [System.Windows.Automation.ControlType]::Edit)))
if ($edit) {
    $edit.SetFocus()
    [System.Windows.Forms.SendKeys]::SendWait('+{F10}')
    Save-Screen '03-contextmenu-dark'
    [System.Windows.Forms.SendKeys]::SendWait('{ESC}')
}

# Dialog custom (butonul Info afișează mesajul „În curând" în Faza 0)
Invoke-Element (Find-ByName $root 'Informații')
Save-Screen '04-dialog-dark'
[System.Windows.Forms.SendKeys]::SendWait('{ESC}')
Start-Sleep -Milliseconds 500
Assert-Alive $proc

# Comutare pe tema luminoasă
Invoke-Element (Find-ByName $root 'Comută pe tema luminoasă')
Save-Screen '05-main-light'

Invoke-Element (Find-ByName $root 'Informații')
Save-Screen '06-dialog-light'
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
