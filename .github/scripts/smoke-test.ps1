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
    [DllImport("user32.dll")] public static extern void mouse_event(uint flags, int dx, int dy, uint data, UIntPtr extra);
    [DllImport("user32.dll")] public static extern void keybd_event(byte vk, byte scan, uint flags, UIntPtr extra);
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

function Find-Control($root, [string] $name, $controlType) {
    $cond = New-Object System.Windows.Automation.AndCondition @(
        (New-Object System.Windows.Automation.PropertyCondition ([System.Windows.Automation.AutomationElement]::NameProperty, $name)),
        (New-Object System.Windows.Automation.PropertyCondition ([System.Windows.Automation.AutomationElement]::ControlTypeProperty, $controlType)))
    for ($i = 0; $i -lt 20; $i++) {
        $el = $root.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $cond)
        if ($el) { return $el }
        Start-Sleep -Milliseconds 250
    }
    throw "Controlul '$name' nu a fost găsit."
}

function Invoke-DoubleClick($el) {
    $pt = $el.GetClickablePoint()
    [System.Windows.Forms.Cursor]::Position = New-Object System.Drawing.Point([int]$pt.X, [int]$pt.Y)
    Start-Sleep -Milliseconds 200
    for ($i = 0; $i -lt 2; $i++) {
        [Native]::mouse_event(0x0002, 0, 0, 0, [UIntPtr]::Zero)   # LEFTDOWN
        [Native]::mouse_event(0x0004, 0, 0, 0, [UIntPtr]::Zero)   # LEFTUP
        Start-Sleep -Milliseconds 60
    }
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
# Enter #1 navighează în folderul tastat; dacă dialogul e încă deschis, Enter #2 (buton implicit „Select Folder") îl alege
[System.Windows.Forms.SendKeys]::SendWait('{ENTER}')
Start-Sleep -Seconds 2
$nameCond = New-Object System.Windows.Automation.PropertyCondition ([System.Windows.Automation.AutomationElement]::NameProperty, 'Alege un folder cu poze')
if ([System.Windows.Automation.AutomationElement]::RootElement.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $nameCond)) {
    Save-Screen '01c-folder-picker-navigated'
    [System.Windows.Forms.SendKeys]::SendWait('{ENTER}')
}

$sw.Restart()
$done = Wait-ForText $root 'Miniaturi generate' 240
Write-Host "Indexare + miniaturi: $([int]$sw.Elapsed.TotalSeconds) s — footer: '$done'"
Assert-Alive $proc
Save-Screen '02-grid-dark'

$grid = Get-GridItems $root
Write-Host "Elemente expuse de grid prin UI Automation: $($grid.Items.Count)"
if ($grid.Items.Count -lt 2) { throw "Grid-ul nu afișează pozele indexate." }
Wait-ForText $root '1.500' 5 | Out-Null

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

# ---- Faza 2: arbore foldere + filtrare, lightbox, Info, Opțiuni, logo ----
$treeItem = [System.Windows.Automation.ControlType]::TreeItem
$rootNode = Find-Control $root 'Poze' $treeItem
$rootNode.GetCurrentPattern([System.Windows.Automation.ExpandCollapsePattern]::Pattern).Expand()
Start-Sleep -Milliseconds 600
$subNode = Find-Control $root '2001' $treeItem
$subNode.GetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern).Select()
Start-Sleep -Seconds 1
Save-Screen '10-tree-filter-dark'
$filtered = (Get-GridItems $root).Items.Count
Write-Host "Grid filtrat pe subfolderul 2001: $filtered elemente expuse"
if ($filtered -ge 1500) { throw "Filtrarea după folder nu funcționează." }
Invoke-Element (Find-Control $root 'Afișează toate pozele din bibliotecă' ([System.Windows.Automation.ControlType]::Button))
Start-Sleep -Milliseconds 600

# Lightbox: dublu-click real cu mouse-ul pe prima poză, apoi →, zoom, Esc
$grid = Get-GridItems $root
Invoke-DoubleClick $grid.Items[0]
Start-Sleep -Seconds 2
$lightbox = Find-TopWindow 'Înapoi la galerie' 5
Save-Screen '11-lightbox'
[System.Windows.Forms.SendKeys]::SendWait('{RIGHT}')
Start-Sleep -Seconds 1
[System.Windows.Forms.SendKeys]::SendWait('{ADD}{ADD}')
Start-Sleep -Milliseconds 800
Save-Screen '12-lightbox-next-zoomed'
[System.Windows.Forms.SendKeys]::SendWait('{ESC}')
Start-Sleep -Milliseconds 800
Assert-Alive $proc
Save-Screen '13-details-after-lightbox'

# Lightbox și din tastatură (Enter pe poza selectată)
$grid = Get-GridItems $root
$grid.Items[2].GetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern).Select()
$grid.Items[2].SetFocus()
[System.Windows.Forms.SendKeys]::SendWait('{ENTER}')
Start-Sleep -Seconds 2
Find-TopWindow 'Înapoi la galerie' 5 | Out-Null
[System.Windows.Forms.SendKeys]::SendWait('{ESC}')
Start-Sleep -Milliseconds 800

# Modal Info
Invoke-Element (Find-Control $root 'Informații' ([System.Windows.Automation.ControlType]::Button))
Start-Sleep -Milliseconds 800
Save-Screen '14-info-dark'
[System.Windows.Forms.SendKeys]::SendWait('{ESC}')
Start-Sleep -Milliseconds 500

# Fereastra Opțiuni
Invoke-Element (Find-Control $root 'Opțiuni' ([System.Windows.Automation.ControlType]::Button))
Start-Sleep -Seconds 1
Save-Screen '15-settings-dark'
[System.Windows.Forms.SendKeys]::SendWait('{ESC}')
Start-Sleep -Milliseconds 500

# Click pe logo → logo mare peste fundal blurat
Invoke-Element (Find-Control $root 'PhotoVault' ([System.Windows.Automation.ControlType]::Button))
Start-Sleep -Seconds 1
Save-Screen '16-logo-dark'
[System.Windows.Forms.SendKeys]::SendWait('{ESC}')
Start-Sleep -Milliseconds 500
Assert-Alive $proc

# ---- Faza 3: albume și tag-uri ----
$menuItem = [System.Windows.Automation.ControlType]::MenuItem
$button = [System.Windows.Automation.ControlType]::Button

# Click dreapta real (mouse) pe prima poză — selecția multiplă existentă se păstrează
function Open-PhotoContextMenu($root) {
    $g = Get-GridItems $root
    $pt = $g.Items[0].GetClickablePoint()
    [System.Windows.Forms.Cursor]::Position = New-Object System.Drawing.Point([int]$pt.X, [int]$pt.Y)
    Start-Sleep -Milliseconds 200
    [Native]::mouse_event(0x0008, 0, 0, 0, [UIntPtr]::Zero)   # RIGHTDOWN
    [Native]::mouse_event(0x0010, 0, 0, 0, [UIntPtr]::Zero)   # RIGHTUP
    Start-Sleep -Milliseconds 700
}

function Click-At($el) {
    $pt = $el.GetClickablePoint()
    [System.Windows.Forms.Cursor]::Position = New-Object System.Drawing.Point([int]$pt.X, [int]$pt.Y)
    Start-Sleep -Milliseconds 150
    [Native]::mouse_event(0x0002, 0, 0, 0, [UIntPtr]::Zero)
    [Native]::mouse_event(0x0004, 0, 0, 0, [UIntPtr]::Zero)
    Start-Sleep -Milliseconds 400   # peste intervalul de dublu-click
}

# Selecție ca un utilizator: click pe prima poză, apoi Ctrl+click pe următoarele
function Select-Photos($root, [int] $count) {
    $g = Get-GridItems $root
    Click-At $g.Items[0]
    [Native]::keybd_event(0x11, 0, 0, [UIntPtr]::Zero)          # Ctrl apăsat
    for ($i = 1; $i -lt $count; $i++) { Click-At $g.Items[$i] }
    [Native]::keybd_event(0x11, 0, 0x0002, [UIntPtr]::Zero)     # Ctrl eliberat
    Start-Sleep -Milliseconds 300
    $selected = @($g.Items | Where-Object { $_.GetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern).Current.IsSelected }).Count
    Write-Host "Poze selectate: $selected"
}

# Album nou din panoul stâng (+) → prompt stilizat
Invoke-Element (Find-Control $root 'Album nou' $button)
Start-Sleep -Milliseconds 800
Save-Screen '20-album-prompt-dark'
[System.Windows.Forms.SendKeys]::SendWait('Vacanta 2024{TAB}10-15.08.2021{ENTER}')
Start-Sleep -Milliseconds 800
Find-Control $root 'Vacanta 2024' ([System.Windows.Automation.ControlType]::ListItem) | Out-Null

# Nume duplicat (altă scriere a majusculelor) → avertisment „Modifică numele / Renunță"; nu se creează al doilea album
Invoke-Element (Find-Control $root 'Album nou' $button)
Start-Sleep -Milliseconds 800
[System.Windows.Forms.SendKeys]::SendWait('vacanta 2024{ENTER}')
Start-Sleep -Milliseconds 900
Find-TopWindow 'Nume de album deja folosit' 5 | Out-Null
Save-Screen '20b-album-duplicate-dark'
Invoke-Element (Find-Control ([System.Windows.Automation.AutomationElement]::RootElement) 'Renunță' $button)
Start-Sleep -Milliseconds 800
$albumItems = (Find-ByName $root 'AlbumList').FindAll([System.Windows.Automation.TreeScope]::Children,
    (New-Object System.Windows.Automation.PropertyCondition ([System.Windows.Automation.AutomationElement]::ControlTypeProperty, [System.Windows.Automation.ControlType]::ListItem)))
if ($albumItems.Count -ne 1) { throw "Albumul duplicat a fost creat ($($albumItems.Count) albume)." }

# 4 poze selectate → click dreapta → Adaugă la album → Vacanta 2024
Select-Photos $root 4
Open-PhotoContextMenu $root
$desktop = [System.Windows.Automation.AutomationElement]::RootElement
$addToAlbum = Find-Control $desktop 'Adaugă la album' $menuItem
$addToAlbum.GetCurrentPattern([System.Windows.Automation.ExpandCollapsePattern]::Pattern).Expand()
Start-Sleep -Milliseconds 600
Save-Screen '21-context-menu-album-dark'
Invoke-Element (Find-Control $desktop 'Vacanta 2024' $menuItem)
$added = Wait-ForText $root 'Adăugare în albumul' 5
if ($added -notlike '*: 4 poze') { throw "Mesaj neașteptat după adăugarea în album: $added" }

# Aceeași selecție → Adaugă tag → Tag nou... → „mare"
Open-PhotoContextMenu $root
$addTag = Find-Control $desktop 'Adaugă tag' $menuItem
$addTag.GetCurrentPattern([System.Windows.Automation.ExpandCollapsePattern]::Pattern).Expand()
Start-Sleep -Milliseconds 500
Invoke-Element (Find-Control $desktop 'Tag nou...' $menuItem)
Start-Sleep -Milliseconds 800
[System.Windows.Forms.SendKeys]::SendWait('mare{ENTER}')
Wait-ForText $root 'Tag-ul ' 5 | Out-Null
Save-Screen '22-photo-details-tags-dark'

# Grid-ul de carduri de albume + detalii album
Invoke-Element (Find-Control $root 'Afișează toate albumele' $button)
Start-Sleep -Seconds 1
$albumCard = Find-Control (Find-ByName $root 'AlbumGrid') 'Vacanta 2024' ([System.Windows.Automation.ControlType]::ListItem)
$albumCard.GetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern).Select()
Start-Sleep -Milliseconds 800
Save-Screen '23-albums-grid-dark'

# Dublu-click pe card → albumul deschis (4 poze); elimină o poză din album → 3 poze
Invoke-DoubleClick $albumCard
Start-Sleep -Seconds 1
Wait-ForText $root '4 poze' 5 | Out-Null
Select-Photos $root 1
Open-PhotoContextMenu $root
Invoke-Element (Find-Control $desktop 'Elimină din album' $menuItem)
Wait-ForText $root '3 poze' 5 | Out-Null
Save-Screen '24-album-open-dark'

# Filtrare după tag
$tagItem = Find-Control (Find-ByName $root 'TagList') 'mare' ([System.Windows.Automation.ControlType]::ListItem)
$tagItem.GetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern).Select()
Start-Sleep -Seconds 1
Wait-ForText $root '#mare' 5 | Out-Null
Select-Photos $root 1
Start-Sleep -Milliseconds 800
Save-Screen '25-tag-filter-dark'
Invoke-Element (Find-Control $root 'Afișează toate pozele din bibliotecă' $button)
Start-Sleep -Milliseconds 600
Assert-Alive $proc

# Dialog custom (Redenumire batch afișează „În curând" până în Faza 5)
Invoke-Element (Find-Control $root 'Redenumire batch' ([System.Windows.Automation.ControlType]::Button))
Save-Screen '07-dialog-dark'
[System.Windows.Forms.SendKeys]::SendWait('{ESC}')
Start-Sleep -Milliseconds 500
Assert-Alive $proc

# Comutare pe tema luminoasă
Invoke-Element (Find-ByName $root 'Comută pe tema luminoasă')
Save-Screen '08-grid-light'

Invoke-Element (Find-Control $root 'Informații' ([System.Windows.Automation.ControlType]::Button))
Start-Sleep -Milliseconds 800
Save-Screen '09-info-light'
[System.Windows.Forms.SendKeys]::SendWait('{ESC}')
Start-Sleep -Milliseconds 500
Invoke-Element (Find-Control $root 'Opțiuni' ([System.Windows.Automation.ControlType]::Button))
Start-Sleep -Seconds 1
Save-Screen '09b-settings-light'
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
Wait-ForText $root '1.500' 15 | Out-Null
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
