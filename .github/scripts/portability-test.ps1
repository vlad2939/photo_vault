# Test de portabilitate (§9 Faza 8), Windows PowerShell 5.1, pe versiunea publicată (self-contained, single-file):
#  1. aplicația copiată pe un „stick" și pornită FĂRĂ .NET accesibil (PATH / DOTNET_ROOT golite);
#  2. biblioteca creată lângă exe (data\), nimic altceva în folderul aplicației;
#  3. „alt calculator": tot folderul copiat în altă locație + folderul cu poze mutat la altă cale →
#     biblioteca se păstrează, iar folderul sursă e realiniat cu „Schimbă locația...".
param(
    [Parameter(Mandatory)] [string] $PublishDir,
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
$results = New-Object System.Collections.Generic.List[string]
function Add-Result([string] $text) { $results.Add("- $text"); Write-Host $text }

function Save-Screen([string] $name) {
    Start-Sleep -Milliseconds 700
    $b = [System.Windows.Forms.Screen]::PrimaryScreen.Bounds
    $bmp = New-Object System.Drawing.Bitmap $b.Width, $b.Height
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.CopyFromScreen($b.Location, [System.Drawing.Point]::Empty, $b.Size)
    $bmp.Save((Join-Path $OutDir "$name.png"), [System.Drawing.Imaging.ImageFormat]::Png)
    $g.Dispose(); $bmp.Dispose()
}
function Find-Control($root, [string] $name, $controlType, [int] $timeoutSec = 10) {
    $cond = New-Object System.Windows.Automation.AndCondition @(
        (New-Object System.Windows.Automation.PropertyCondition ([System.Windows.Automation.AutomationElement]::NameProperty, $name)),
        (New-Object System.Windows.Automation.PropertyCondition ([System.Windows.Automation.AutomationElement]::ControlTypeProperty, $controlType)))
    $deadline = (Get-Date).AddSeconds($timeoutSec)
    while ((Get-Date) -lt $deadline) {
        $el = $root.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $cond)
        if ($el) { return $el }
        Start-Sleep -Milliseconds 250
    }
    throw "Controlul '$name' nu a fost găsit."
}
function Invoke-Element($el) { $el.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke() }
function Assert-Alive($proc) { if ($proc.HasExited) { throw "Aplicația s-a închis neașteptat (exit code $($proc.ExitCode))." } }
function Wait-ForText($root, [string] $prefix, [int] $timeoutSec) {
    $deadline = (Get-Date).AddSeconds($timeoutSec)
    $textType = New-Object System.Windows.Automation.PropertyCondition ([System.Windows.Automation.AutomationElement]::ControlTypeProperty, [System.Windows.Automation.ControlType]::Text)
    while ((Get-Date) -lt $deadline) {
        foreach ($t in $root.FindAll([System.Windows.Automation.TreeScope]::Descendants, $textType)) {
            $name = $t.Current.Name
            if ($name -and $name.StartsWith($prefix)) { return $name }
        }
        Start-Sleep -Milliseconds 300
    }
    throw "Textul '$prefix...' nu a apărut în $timeoutSec s."
}
function Exists-Named([string] $name) {
    $cond = New-Object System.Windows.Automation.PropertyCondition ([System.Windows.Automation.AutomationElement]::NameProperty, $name)
    return [bool]([System.Windows.Automation.AutomationElement]::RootElement.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $cond))
}
# Dialogul nativ de alegere folder: calea se tastează, Enter navighează, al doilea Enter (dacă e nevoie) o alege
function Pick-Folder([string] $dialogTitle, [string] $path) {
    for ($i = 0; $i -lt 40 -and -not (Exists-Named $dialogTitle); $i++) { Start-Sleep -Milliseconds 250 }
    Start-Sleep -Seconds 1
    [System.Windows.Forms.SendKeys]::SendWait([regex]::Replace($path, '[+^%~(){}\[\]]', '{$0}'))
    Start-Sleep -Milliseconds 500
    [System.Windows.Forms.SendKeys]::SendWait('{ENTER}')
    Start-Sleep -Seconds 2
    if (Exists-Named $dialogTitle) { [System.Windows.Forms.SendKeys]::SendWait('{ENTER}') }
    Start-Sleep -Seconds 1
}
function Start-App([string] $exe) {
    $p = Start-Process -FilePath $exe -WorkingDirectory (Split-Path $exe) -PassThru
    for ($i = 0; $i -lt 120 -and $p.MainWindowHandle -eq 0; $i++) { Start-Sleep -Milliseconds 250; $p.Refresh() }
    Assert-Alive $p
    # Fereastra principală pornește maximizată — nu mai e nevoie de redimensionare
    [Native]::SetForegroundWindow($p.MainWindowHandle) | Out-Null
    Start-Sleep -Seconds 2
    return $p
}
function Stop-App($p) {
    $p.CloseMainWindow() | Out-Null
    if (-not $p.WaitForExit(15000)) { $p.Kill(); throw "Aplicația nu s-a închis la cerere." }
}

# ---- Poze de test (30 JPEG în 2 subfoldere) ----
$photos = 'C:\PVPortabil\Poze'
if (Test-Path 'C:\PVPortabil') { Remove-Item -Recurse -Force 'C:\PVPortabil' }
for ($i = 1; $i -le 30; $i++) {
    $sub = Join-Path $photos ($(if ($i % 2) { 'Vara' } else { 'Iarna' }))
    New-Item -ItemType Directory -Force -Path $sub | Out-Null
    $bmp = New-Object System.Drawing.Bitmap 800, 600
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.Clear([System.Drawing.Color]::FromArgb(40 + $i * 6, 90, 160 - $i * 3))
    $g.DrawString("$i", (New-Object System.Drawing.Font 'Segoe UI', 60), [System.Drawing.Brushes]::White, 40, 40)
    $bmp.Save((Join-Path $sub ("P_{0:D2}.jpg" -f $i)), [System.Drawing.Imaging.ImageFormat]::Jpeg)
    $g.Dispose(); $bmp.Dispose()
}

# ---- 1. „Stick USB": copia versiunii publicate, pornită fără niciun .NET accesibil ----
$stick = Join-Path $env:RUNNER_TEMP 'StickUSB\PhotoVault'
if (Test-Path (Split-Path $stick)) { Remove-Item -Recurse -Force (Split-Path $stick) }
New-Item -ItemType Directory -Force -Path $stick | Out-Null
Copy-Item -Path (Join-Path $PublishDir '*') -Destination $stick -Recurse
$published = Get-ChildItem $stick -Recurse -File
Add-Result ("Versiune publicată: " + (($published | ForEach-Object { "{0} ({1:N1} MB)" -f $_.Name, ($_.Length / 1MB) }) -join ', '))

$env:PATH = (($env:PATH -split ';') | Where-Object { $_ -notmatch 'dotnet' }) -join ';'
$env:DOTNET_ROOT = 'C:\nu-exista-dotnet'
$env:DOTNET_ROOT_X64 = 'C:\nu-exista-dotnet'
$env:DOTNET_MULTILEVEL_LOOKUP = '0'
Add-Result 'Mediu fără .NET: dotnet eliminat din PATH, DOTNET_ROOT către un folder inexistent'

$proc = Start-App (Join-Path $stick 'PhotoVault.exe')
$root = [System.Windows.Automation.AutomationElement]::FromHandle($proc.MainWindowHandle)
if (-not (Test-Path (Join-Path $stick 'data\photovault.db'))) { throw "data\photovault.db nu a fost creat lângă exe." }
Add-Result 'Pornire de pe „stick" fără .NET: OK; data\photovault.db creat lângă PhotoVault.exe'

Invoke-Element (Find-Control $root 'Adaugă folder sursă' ([System.Windows.Automation.ControlType]::Button))
Pick-Folder 'Alege un folder cu poze' $photos
Wait-ForText $root 'Miniaturi generate' 120 | Out-Null
Wait-ForText $root '30 de poze' 10 | Out-Null
Add-Result 'Indexare 30 de poze + miniaturi: OK'
Save-Screen 'port-01-stick'
Stop-App $proc

$topLevel = (Get-ChildItem $stick | ForEach-Object { $_.Name }) -join ', '
Add-Result "Conținutul folderului aplicației după utilizare: $topLevel"
$unexpected = Get-ChildItem $stick | Where-Object { $_.Name -notin @('PhotoVault.exe', 'README.txt', 'data') }
if ($unexpected) { throw "Fișiere neașteptate în folderul aplicației: $($unexpected.Name -join ', ')" }
$thumbCount = @(Get-ChildItem (Join-Path $stick 'data\thumbnails') -Recurse -File).Count

# ---- 2. „Alt calculator": folderul aplicației copiat în altă parte, pozele mutate la altă cale ----
$otherPc = Join-Path $env:RUNNER_TEMP 'AltCalculator\PhotoVault'
if (Test-Path (Split-Path $otherPc)) { Remove-Item -Recurse -Force (Split-Path $otherPc) }
New-Item -ItemType Directory -Force -Path (Split-Path $otherPc) | Out-Null
Copy-Item -Path $stick -Destination $otherPc -Recurse
$movedPhotos = 'C:\PVPortabil\DiscNou\Poze'
New-Item -ItemType Directory -Force -Path (Split-Path $movedPhotos) | Out-Null
Move-Item $photos $movedPhotos

$proc = Start-App (Join-Path $otherPc 'PhotoVault.exe')
$root = [System.Windows.Automation.AutomationElement]::FromHandle($proc.MainWindowHandle)
Wait-ForText $root '30 de poze' 20 | Out-Null
Add-Result 'Biblioteca (30 de poze, miniaturi) reîncărcată din copia folderului aplicației: OK'
Save-Screen 'port-02-other-pc-unavailable'

# Realiniere: Opțiuni → Schimbă locația... → noua cale → confirmare
Invoke-Element (Find-Control $root 'Opțiuni' ([System.Windows.Automation.ControlType]::Button))
$settings = Find-Control ([System.Windows.Automation.AutomationElement]::RootElement) 'Opțiuni' ([System.Windows.Automation.ControlType]::Window)
Invoke-Element (Find-Control $settings 'Schimbă locația...' ([System.Windows.Automation.ControlType]::Button))
Pick-Folder 'Alege noua locație a folderului „Poze”' $movedPhotos
Wait-ForText ([System.Windows.Automation.AutomationElement]::RootElement) 'La noua locație' 10 | Out-Null
Save-Screen 'port-03-relocate-confirm'
Invoke-Element (Find-Control ([System.Windows.Automation.AutomationElement]::RootElement) 'Actualizează locația' ([System.Windows.Automation.ControlType]::Button))
Wait-ForText $root 'Locația folderului a fost actualizată' 15 | Out-Null
Add-Result "Folder sursă realiniat la ${movedPhotos}: OK"
Save-Screen 'port-04-relocated'
[System.Windows.Forms.SendKeys]::SendWait('{ESC}')
Start-Sleep -Seconds 1

# Re-scanarea după realiniere nu trebuie să piardă nimic (toate pozele se regăsesc)
Invoke-Element (Find-Control $root 'Opțiuni' ([System.Windows.Automation.ControlType]::Button))
$settings = Find-Control ([System.Windows.Automation.AutomationElement]::RootElement) 'Opțiuni' ([System.Windows.Automation.ControlType]::Window)
Invoke-Element (Find-Control $settings 'Re-scanează toate' ([System.Windows.Automation.ControlType]::Button))
Wait-ForText $root 'Re-scanare finalizată. Poze noi: 0, eliminate din index: 0' 30 | Out-Null
[System.Windows.Forms.SendKeys]::SendWait('{ESC}')
Start-Sleep -Seconds 1
Wait-ForText $root '30 de poze' 10 | Out-Null
Add-Result 'Re-scanare după realiniere: 0 poze noi, 0 eliminate (albumele / tag-urile / rotirile rămân legate de aceleași poze)'
$thumbAfter = @(Get-ChildItem (Join-Path $otherPc 'data\thumbnails') -Recurse -File).Count
if ($thumbAfter -ne $thumbCount) { throw "Miniaturile nu s-au păstrat ($thumbCount → $thumbAfter)." }
Add-Result "Miniaturi păstrate după mutare: $thumbAfter (nu au fost regenerate)"
Save-Screen 'port-05-after-rescan'
Stop-App $proc

foreach ($dir in @($stick, $otherPc)) {
    $logs = Join-Path $dir 'logs'
    if ((Test-Path $logs) -and (Get-ChildItem $logs -File)) {
        Get-ChildItem $logs -File | ForEach-Object { Get-Content $_.FullName | Select-Object -First 40 | Write-Host }
        throw "Aplicația a scris erori în $logs."
    }
}

$report = @('## PhotoVault — test de portabilitate', '') + $results
$report | Set-Content -Encoding UTF8 (Join-Path $OutDir 'portabilitate.md')
if ($env:GITHUB_STEP_SUMMARY) { $report | Add-Content -Encoding UTF8 $env:GITHUB_STEP_SUMMARY }
Write-Host 'Test de portabilitate OK.'
