# PhotoVault — Plan Tehnic Detaliat

**Aplicație portabilă de management și vizualizare albume foto personale**
Platformă: Windows 11 · Framework: C# / WPF · .NET 10 (self-contained)

---

## 1. Viziune și principii de proiectare

PhotoVault este o aplicație desktop **complet portabilă**, care rulează dintr-un singur folder, fără instalare, fără dependențe de sistem (rulează pe orice Win11 chiar dacă .NET nu este instalat pe calculatorul gazdă). Aplicația **indexează** fotografii aflate în locații alese de utilizator (fișierele originale nu sunt niciodată mutate, copiate sau modificate fizic), oferind un strat logic de organizare (albume, tag-uri) deasupra sistemului de fișiere existent.

Principii cheie:

1. **Non-distructiv** — nicio operațiune din aplicație nu modifică fișierele originale (nici rotirea, nici organizarea în albume, nici eliminarea din albume).
2. **Index, nu copie** — modelul de lucru este similar cu Adobe Lightroom: biblioteca e o bază de date care *referă* poze, nu le deține fizic.
3. **Amprentă minimă** — puține foldere/fișiere vizibile în directorul aplicației; toată complexitatea internă e ascunsă în 2-3 fișiere/foldere (executabil, bază de date, cache thumbnails).
4. **Portabilitate totală** — copiezi folderul pe alt PC / stick USB, aplicația rulează identic, cu condiția ca folderele sursă indexate să fie accesibile la aceleași căi (sau realiniate manual).
5. **Simplitate peste funcționalitate excesivă** — fără editare avansată, fără cloud, fără sincronizare — un tool focalizat, rapid, fiabil.

---

## 2. Cerințe funcționale — sumar

| Categorie | Funcționalitate |
|---|---|
| **Import** | Adăugare manuală de foldere sursă; aplicația reține lista de foldere indexate; re-scanare manuală la cerere |
| **Formate suportate** | JPEG, PNG · RAW: CR2, NEF, DNG (preview din embedded JPEG) |
| **Vizualizare** | Arbore de foldere (stil Explorer) în panoul stâng; grid de thumbnail-uri central; panou de detalii poză |
| **Lightbox** | Fullscreen, navigare cu săgeți, zoom/pan manual |
| **Albume** | Albume manuale (poză poate fi în mai multe albume); adăugare în masă; eliminare individuală din album (fără ștergere fizică) |
| **Tag-uri** | Etichete custom, atribuibile uneia sau mai multor poze |
| **Sortare** | După nume fișier (ascendent/descendent) |
| **Căutare** | Simplă: după nume fișier, tag, sau album |
| **Redenumire batch** | Utilitar separat, pre-import, pe foldere de pe disc, cu pattern configurabil și preview obligatoriu |
| **Rotire** | Rotire logică 90°/180°/270° (stocată în DB, fișier original neatins) |
| **Slideshow** | Efect Ken Burns (zoom+pan aleator), tranziție fade, muzică MP3 în playlist/loop, parametri configurabili |
| **Teme** | Dark / Light, comutare manuală |
| **Gestiune index** | Eliminare automată din index a pozelor lipsă (fișier mutat/șters extern) |

---

## 3. Arhitectură tehnică generală

### 3.1 Stack tehnologic

| Componentă | Alegere | Motivație |
|---|---|---|
| UI Framework | WPF (.NET 10) | Nativ Windows, control fin asupra UI, performanță bună pentru grid-uri mari de imagini |
| Limbaj | C# 13 | Aliniat cu .NET 10 |
| Bază de date | SQLite (fișier local `.db`) | Suportă eficient 50.000+ înregistrări, tranzacții sigure, interogări indexate rapide |
| ORM / acces DB | `Microsoft.Data.Sqlite` + Dapper (micro-ORM) | Ușor, fără overhead-ul unui ORM complet (Entity Framework ar fi prea "greu" pentru cerința de amprentă minimă) |
| Pattern arhitectural | MVVM (Model-View-ViewModel) | Standard pentru WPF, testabilitate, separare curată UI/logică |
| Citire metadate EXIF/RAW | `MetadataExtractor` (NuGet, open-source, .NET pur) | Suportă JPEG + principalele formate RAW (CR2, NEF, DNG); extrage embedded preview JPEG fără decodare RAW completă |
| Procesare imagini | `System.Drawing.Common` sau `SixLabors.ImageSharp` pentru generare thumbnails | ImageSharp preferat: multiplatformă, fără dependențe GDI+ native, activ întreținut |
| Redare audio (MP3) | `NAudio` (NuGet) | Librărie matură pentru playback MP3 în WPF, suportă playlist/loop |
| Deployment | .NET Single-File Self-Contained Publish | Un singur `.exe` care include runtime-ul .NET, fără instalare necesară |
| Distribuție | Trimmed, ReadyToRun (opțional) | Reduce dimensiunea și timpul de pornire |

### 3.2 Structura fizică a folderului aplicației (portabil)

Scopul e "amprentă minimă" — cât mai puține elemente vizibile:

```
PhotoVault/
├── PhotoVault.exe              (executabil self-contained, singurul fișier "principal")
├── PhotoVault.exe.config       (dacă e generat automat de .NET publish)
├── data/
│   ├── photovault.db           (baza de date SQLite — index, albume, tag-uri, setări)
│   └── thumbnails/             (cache thumbnail-uri .jpg, generate automat)
│       ├── ab/                 (subfoldere după primele 2 caractere ale hash-ului, pt performanță filesystem)
│       │   └── ab34f8c1...jpg
│       └── ...
├── logs/                       (opțional — log erori, rotativ, max N fișiere)
└── README.txt                  (instrucțiuni scurte de utilizare/portabilitate)
```

Notă: subfolderele din `thumbnails/` (ex: `ab/`, `cd/`) evită problema clasică Windows de performanță scăzută când un singur folder conține zeci de mii de fișiere mici.

### 3.3 Structura proiectului C#/WPF (soluție Visual Studio)

```
PhotoVault.sln
│
├── PhotoVault.App/                    (proiect WPF principal — UI)
│   ├── App.xaml / App.xaml.cs
│   ├── Views/
│   │   ├── MainWindow.xaml
│   │   ├── LightboxWindow.xaml
│   │   ├── SlideshowWindow.xaml
│   │   ├── BatchRenameWindow.xaml
│   │   └── SettingsWindow.xaml
│   ├── ViewModels/
│   │   ├── MainViewModel.cs
│   │   ├── FolderTreeViewModel.cs
│   │   ├── PhotoGridViewModel.cs
│   │   ├── AlbumViewModel.cs
│   │   ├── LightboxViewModel.cs
│   │   ├── SlideshowViewModel.cs
│   │   └── BatchRenameViewModel.cs
│   ├── Controls/                      (UserControls custom: PhotoThumbnailControl, KenBurnsImageControl, etc.)
│   ├── Converters/                    (IValueConverter pentru binding-uri WPF)
│   ├── Themes/
│   │   ├── DarkTheme.xaml
│   │   └── LightTheme.xaml
│   └── Resources/                     (iconițe, stiluri XAML comune)
│
├── PhotoVault.Core/                   (logică de business, independentă de UI)
│   ├── Models/
│   │   ├── PhotoItem.cs
│   │   ├── Album.cs
│   │   ├── Tag.cs
│   │   ├── SourceFolder.cs
│   │   └── AppSettings.cs
│   ├── Services/
│   │   ├── IPhotoIndexService.cs / PhotoIndexService.cs
│   │   ├── IThumbnailService.cs / ThumbnailService.cs
│   │   ├── IAlbumService.cs / AlbumService.cs
│   │   ├── ITagService.cs / TagService.cs
│   │   ├── IMetadataService.cs / MetadataService.cs   (wrapper peste MetadataExtractor)
│   │   ├── IBatchRenameService.cs / BatchRenameService.cs
│   │   └── ISlideshowService.cs / SlideshowService.cs
│   └── Utils/
│       ├── RawPreviewExtractor.cs
│       └── FileHashHelper.cs
│
├── PhotoVault.Data/                   (acces la date, SQLite)
│   ├── DatabaseContext.cs             (conexiune, inițializare schemă)
│   ├── Repositories/
│   │   ├── PhotoRepository.cs
│   │   ├── AlbumRepository.cs
│   │   ├── TagRepository.cs
│   │   └── SourceFolderRepository.cs
│   └── Migrations/
│       └── 001_InitialSchema.sql
│
└── PhotoVault.Tests/                  (teste unitare — opțional, dar recomandat)
    ├── Services/
    └── Repositories/
```

---

## 4. Schema bazei de date (SQLite)

```sql
-- Foldere sursă indexate (adăugate manual de utilizator)
CREATE TABLE SourceFolders (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    FolderPath TEXT NOT NULL UNIQUE,
    DateAdded TEXT NOT NULL,           -- ISO 8601
    LastScanned TEXT                    -- ISO 8601, NULL dacă niciodată re-scanat
);

-- Fotografii indexate
CREATE TABLE Photos (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    SourceFolderId INTEGER NOT NULL,
    FullPath TEXT NOT NULL UNIQUE,
    FileName TEXT NOT NULL,
    Extension TEXT NOT NULL,            -- jpg, cr2, nef, dng
    FileSizeBytes INTEGER,
    DateAdded TEXT NOT NULL,            -- când a fost indexată
    DateTakenExif TEXT,                 -- din EXIF, dacă disponibil (fallback pentru viitor)
    RotationDegrees INTEGER NOT NULL DEFAULT 0,  -- 0, 90, 180, 270 — rotire LOGICĂ
    ThumbnailPath TEXT,                 -- cale relativă în data/thumbnails/
    IsMissing INTEGER NOT NULL DEFAULT 0, -- 1 dacă fișierul nu mai există la scanare
    FOREIGN KEY (SourceFolderId) REFERENCES SourceFolders(Id) ON DELETE CASCADE
);
CREATE INDEX idx_photos_filename ON Photos(FileName);
CREATE INDEX idx_photos_sourcefolder ON Photos(SourceFolderId);

-- Albume (manuale)
CREATE TABLE Albums (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Name TEXT NOT NULL,                 -- unic la nivel de aplicație (fără diferență de majuscule), vezi §6.5
    Subtitle TEXT,                      -- subtitlu liber scris de utilizator, ex. „10–15.08.2021" (adăugat prin migrarea 003)
    DateCreated TEXT NOT NULL,
    CoverPhotoId INTEGER,               -- poză copertă, opțional
    FOREIGN KEY (CoverPhotoId) REFERENCES Photos(Id) ON DELETE SET NULL
);

-- Relație many-to-many: poze în albume
CREATE TABLE AlbumPhotos (
    AlbumId INTEGER NOT NULL,
    PhotoId INTEGER NOT NULL,
    DateAdded TEXT NOT NULL,
    SortOrder INTEGER NOT NULL DEFAULT 0,   -- ordine custom în cadrul albumului, dacă necesar
    PRIMARY KEY (AlbumId, PhotoId),
    FOREIGN KEY (AlbumId) REFERENCES Albums(Id) ON DELETE CASCADE,
    FOREIGN KEY (PhotoId) REFERENCES Photos(Id) ON DELETE CASCADE
);

-- Tag-uri custom
CREATE TABLE Tags (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Name TEXT NOT NULL UNIQUE
);

-- Relație many-to-many: poze cu tag-uri
CREATE TABLE PhotoTags (
    PhotoId INTEGER NOT NULL,
    TagId INTEGER NOT NULL,
    PRIMARY KEY (PhotoId, TagId),
    FOREIGN KEY (PhotoId) REFERENCES Photos(Id) ON DELETE CASCADE,
    FOREIGN KEY (TagId) REFERENCES Tags(Id) ON DELETE CASCADE
);

-- Setări aplicație (cheie-valoare, simplu)
CREATE TABLE AppSettings (
    Key TEXT PRIMARY KEY,
    Value TEXT
);
-- Exemple de chei: "Theme" (dark/light), "SlideshowDurationSec",
-- "SlideshowFadeMs", "SlideshowPanIntensity", "SlideshowZoomIntensity",
-- "SlideshowPlaylistPaths" (JSON array), "SlideshowVolume" (0–100)
```

**Notă privind evoluția schemei**: schema e aplicată prin scripturi de migrare numerotate, incluse în assembly (`PhotoVault.Data/Migrations/NNN_*.sql`), iar versiunea curentă e ținută în `PRAGMA user_version`; o bază de date existentă se actualizează automat la pornire. Migrări aplicate până acum: `001_InitialSchema` (schema de mai sus), `002_RetryFailedThumbnails` (reîncercarea miniaturilor RAW eșuate), `003_AlbumSubtitle` (coloana `Albums.Subtitle`), `004_PerformanceIndexes` (indecși pentru biblioteci mari). Convenție: `Photos.ThumbnailPath` = `NULL` → miniatură negenerată încă; `''` (text gol) → fișier ilizibil (se afișează iconița de rezervă, nu se reîncearcă la fiecare pornire, dar se reîncearcă la re-scanarea folderului).

**Notă privind eliminarea automată din index**: la fiecare re-scanare manuală a unui `SourceFolder`, aplicația verifică existența fizică a fiecărui fișier din `Photos`; dacă lipsește, rândul este șters direct (cu efect de cascadă asupra `AlbumPhotos` și `PhotoTags`), fără prompt de confirmare.

---

## 5. Design UI/UX — considerente detaliate

Această secțiune completează layout-ul general descris în §6.1, cu cerințe explicite de design modern și coerență vizuală completă între temele Dark/Light — inclusiv elementele care în WPF folosesc implicit stilul nativ Windows (scrollbar, mesaje de sistem, title bar), care trebuie toate re-stilizate pentru a nu "rupe" aspectul aplicației.

### 5.1 Principiu general de temă

Nimic din interfață nu trebuie să rămână cu aspectul WPF/Windows implicit. Se stilizează explicit prin `ResourceDictionary` (Dark/Light):

- **ScrollBar** — template custom (bandă subțire, colțuri rotunjite, culoare din paleta temei, fără săgeți implicite gen Windows 95).
- **Mesaje de informare/avertizare/eroare** — fără `MessageBox.Show()` nativ (acesta e întotdeauna alb, indiferent de temă); se construiește un `CustomDialogWindow` reutilizabil, cu iconițe proprii (info/warning/error/succes), stilizat identic cu restul aplicației, afișat ca overlay/modal peste fereastra principală.
- **Tooltip-uri, ComboBox, CheckBox, Slider, meniuri contextuale (`ContextMenu`)** — toate restilizate cu `ControlTemplate`/`Style` custom, colțuri rotunjite, tranziții subtile de hover/click (`Storyboard` scurte, 100-150ms).
- **Title bar Windows nativ** — rămâne nativ (nu se suprascrie complet), dar culoarea/tema title bar-ului Windows se sincronizează cu tema aplicației via API-ul `DwmSetWindowAttribute` (`DWMWA_USE_IMMERSIVE_DARK_MODE`), disponibil pe Windows 11, astfel încât title bar-ul nativ să fie și el închis la culoare când aplicația e în Dark mode (nu doar conținutul ferestrei).

### 5.2 Design modern — carduri, sliders, meniuri

- **Carduri** (albume, poate și alte elemente listate ca și carduri) — colțuri rotunjite (`CornerRadius` 8-12px), umbră subtilă (`DropShadowEffect` discret), animație scurtă de scale/highlight la hover (ex. scale 1.0 → 1.02).
- **Slidere** (folosite în setări slideshow: durată, intensitate pan/zoom) — track subțire, thumb rotunjit, culoare de accent din temă, valoare curentă afișată ca tooltip/label lângă thumb.
- **Meniuri dropdown / meniuri contextuale** — fundal cu blur ușor sau culoare solidă din temă (fără alb implicit WPF), colțuri rotunjite, spațiere generoasă între itemi, iconițe aliniate consistent, animație scurtă de apariție (fade + scale ușor).
- **Fonturi și spațiere** — un font modern (ex. `Segoe UI Variable`, disponibil nativ pe Win11), ierarhie clară de mărimi (titluri/etichete/corp text), padding generos pentru a evita aglomerarea vizuală.

### 5.3 Bară secundară sub title bar (custom, integrată cu tema)

Sub title bar-ul nativ Windows (care rămâne standard, cu minimize/maximize/close native), aplicația afișează o **bară secundară proprie**, complet integrată vizual cu tema curentă:

- **Stânga**: logo aplicație (imagine furnizată) + text "PhotoVault".
- **Dreapta**: 4 butoane tip iconiță, fără text vizibil (cu tooltip la hover), în această ordine:
  - **Redenumire batch** — lansează fereastra utilitarului de redenumire batch (§6.8), independentă de indexul principal.
  - **Info** — deschide un modal cu: nume aplicație, versiune curentă, autor, scurte instrucțiuni de utilizare, și o listă de shortcut-uri de tastatură disponibile în aplicație (text static, format simplu, scrollabil dacă e nevoie).
  - **Opțiuni** — deschide fereastra de Setări existentă (parametri slideshow, temă, culoare accent, limbă, gestiune foldere sursă).
  - **Temă** — comută instant între Dark/Light (iconiță schimbată dinamic: soare pentru light, lună pentru dark).
- Bara respectă complet culorile temei active (fundal, culoare iconițe, hover state).

**Shortcut-uri de tastatură incluse în lista din modalul Info** (se completează pe parcursul dezvoltării, pe măsură ce sunt implementate):

| Tastă | Acțiune |
|---|---|
| `Esc` | Închide lightbox / slideshow / modal logo |
| `←` / `→` | Navigare poză anterioară/următoare (lightbox, slideshow) |
| `R` | Rotire poză 90° (grid sau lightbox) |
| `Space` | Play/Pause slideshow |
| `Ctrl+F` | Focus pe câmpul de căutare |

### 5.4 Click pe logo → lightbox fullscreen dedicat

- La click pe logo (din bara secundară): se deschide un modal fullscreen dedicat, separat de lightbox-ul de poze (§6.4).
- Fundal: blur aplicat peste conținutul curent al ferestrei (efect `BlurEffect` peste un snapshot al ferestrei, sau overlay semi-transparent + blur), fără alte elemente/texte suprapuse — doar poza logo, centrată, la o dimensiune generoasă.
- Închidere: tastă `Esc` sau click oriunde pe imagine.
- Nicio bară de control, niciun text — experiență minimalistă, dedicată exclusiv vizualizării logo-ului la dimensiune mare.

### 5.5 Footer aplicație

- Bară fixă în partea de jos a ferestrei principale, discretă, integrată cu tema.
- **Stânga**: zonă rezervată pentru bara de progres la indexare și alte mesaje scurte de stare ale aplicației (vizibilă doar când există o operațiune activă sau un mesaj de afișat; altfel goală).
- **Dreapta**: text fix `© concept și realizare vlad39`.
- Fără alte elemente interactive în footer.

### 5.6 Panou lateral de detalii

Panou fix, poziționat în partea dreaptă a ferestrei principale (sau jos, sub grid — poziție exactă rezolvată în etapa de implementare a layout-ului), care afișează informații contextuale despre elementul selectat curent:

- **Când e selectată o poză** din grid: nume fișier, cale completă, dimensiune fișier, extensie, tag-uri asociate (cu opțiune de adăugare/eliminare tag direct din panou).
- **Când e selectat un card de album** (vezi §5.7): nume album, dată creare, număr total de poze din album (fără a deschide automat albumul).
- Panoul rămâne gol/cu mesaj neutru ("Selectează o poză sau un album") când nu există nicio selecție activă.
- Stilizat coerent cu tema activă (fundal, text, separatoare) — fără aspect WPF implicit.

### 5.7 Main Window — ajustări specifice

- **Arbore foldere (File Explorer)**: trebuie să afișeze structura completă, inclusiv subdirectoare (expand/collapse standard `TreeView`, cu încărcare lazy a subdirectoarelor — nu se scanează tot subarborele la pornire, ci la expandare, pentru performanță).
- **Secțiunea Albume** (în main area, când e selectată din panoul stâng): afișare sub formă de **grid de carduri**, fiecare card conținând:
  - Fotografie copertă (prima poză adăugată sau una setată manual ca „copertă" — vezi `CoverPhotoId` deja prezent în schema DB).
  - Nume album.
  - **Subtitlu** scris de utilizator la crearea / editarea albumului (ex. „10–15.08.2021") — *nu* data creării, care e irelevantă pe card pentru poze vechi.
  - Număr total de poze din album.
- **Click pe card album** → panoul lateral de detalii (§5.6) afișează: copertă, nume album, subtitlu, dată creare, număr de poze, butoanele „Deschide albumul" / „Editează" — fără a deschide automat conținutul albumului (deschiderea propriu-zisă a albumului, cu toate pozele în grid, rămâne acțiune separată, ex. dublu-click pe card).
- **Bară de progres la indexare**: vizibilă în timpul scanării unui folder nou adăugat (sau re-scanare), afișată **în footer, aliniată în partea stângă** (vezi §5.5 actualizat), afișând progres (ex. "Indexare: 1.240 / 5.000 poze"), fără a bloca restul interfeței (indexarea rulează pe fundal, utilizatorul poate continua să navigheze). Alte mesaje scurte de stare ale aplicației (ex. confirmări discrete, status curent) folosesc aceeași zonă din footer.

### 5.8 Slideshow — bară de control auto-hide

- **Bară de control inferioară**: conține Play/Pause, Previous, Next, contor poze curent/total (ex. "12 / 87"), buton Exit (ieșire din slideshow).
- **Comportament auto-hide**: bara dispare automat (fade-out) după 3 secunde de inactivitate a mouse-ului; reapare (fade-in) la mișcare mouse sau hover în zona inferioară a ecranului.
- **Informații superioare** (nume album / nume poză curentă), afișate în colțurile stânga/dreapta sus: aceeași logică de auto-hide/reapariție, sincronizată cu bara de control (apar și dispar împreună).
- Scopul: vizionare "curată", fără elemente UI suprapuse peste poze în mod implicit, dar control complet disponibil instant la mișcarea mouse-ului.
- Implementare tehnică: `DispatcherTimer` care resetează un countdown de 3 secunde la fiecare `MouseMove`; la expirare, `Storyboard` de fade-out pe opacitate; la `MouseMove` ulterior, fade-in imediat.

---

## 6. Detalii pe module funcționale

### 6.1 Vizualizare principală (grid + arbore)

- **Panou stâng**: `TreeView` cu structura de foldere (ca Windows Explorer), plus secțiune separată "Albume" (listă simplă) și "Tag-uri" (listă simplă).
- **Panou central**: `ItemsControl`/`ListView` cu `VirtualizingStackPanel` (esențial pentru performanță la 50.000 poze — randare doar a elementelor vizibile), afișând thumbnail-uri.
- **Panou jos/dreapta**: detalii poză selectată — nume fișier, cale completă, dimensiune, extensie, tag-uri asociate, cu opțiune de editare tag-uri direct din panou.
- Selectarea unui folder în arbore filtrează grid-ul la pozele din acel folder **și din toate subfolderele lui**; click pe titlul „Bibliotecă" afișează din nou toate pozele. Selectarea unui album/tag similar.
- Deasupra grid-ului, o bară de context arată ce se afișează (Toate pozele / folder / album / #tag) și numărul de poze; dintr-un album deschis, săgeata ← revine la grid-ul de albume.
- Click dreapta pe poză (sau selecție multiplă): meniu contextual — "Adaugă la album", "Rotește 90°", "Elimină din album" (dacă contextul e un album deschis), "Adaugă tag".

### 6.2 Import și gestionare foldere sursă

- Utilizatorul adaugă un folder via dialogul nativ Windows de selecție folder (`OpenFolderDialog`, .NET 8+), din butonul **+** din antetul secțiunii Bibliotecă sau din fereastra Opțiuni (lista folderelor sursă, cu Re-scanează / Elimină).
- Nu se poate adăuga un folder deja adăugat, un subfolder al unui folder sursă existent sau un folder care conține un folder sursă existent (aplicația explică motivul).
- La adăugare: scanare recursivă (inclusiv subfoldere) după extensii cunoscute (`.jpg`, `.jpeg`, `.png`, `.cr2`, `.nef`, `.dng`), inserare în `Photos`, generare thumbnail asincronă (queue de background, nu blochează UI).
- Fără verificare de duplicate (per cerință) — dacă aceeași poză există fizic în două foldere sursă diferite, va apărea de două ori în index (comportament acceptat).
- Buton explicit "Re-scanează" per folder sursă (nu automat la pornire) — detectează poze noi adăugate manual pe disc și elimină cele lipsă.
- Lista de foldere sursă e vizibilă și editabilă (ștergere folder sursă din index = elimină toate pozele asociate din DB, fără a atinge fișierele fizice).

### 6.3 Generare thumbnails

- La import, pentru fiecare poză: se generează un thumbnail JPEG (ex. 300x300px, păstrând aspect ratio) folosind ImageSharp.
- Pentru JPEG/PNG: decodare directă + resize.
- Pentru RAW (CR2/NEF/DNG): extragere embedded preview via `MetadataExtractor`, apoi resize la thumbnail din acel preview (evită decodare RAW completă, mult mai rapid).
- Thumbnail-urile sunt salvate în `data/thumbnails/{primele 2 caractere hash}/{hash}.jpg`, hash calculat din calea completă a fișierului (pentru a evita coliziuni de nume).
- Generare procesată pe un thread pool în fundal, cu progress bar vizibil în UI la import-uri mari.

### 6.4 Lightbox (vizualizare fullscreen)

- Fereastră separată, fullscreen sau maximizată, fundal negru.
- Navigare cu săgeți stânga/dreapta (tastatură + click UI), Esc pentru închidere.
- Zoom manual (scroll wheel / butoane +/-) și pan (drag cu mouse) când zoom > 100%.
- Pentru RAW: afișare din embedded preview (aceeași sursă ca thumbnail, dar la rezoluție mai mare dacă disponibilă în fișier).
- Rotirea logică aplicată se reflectă automat în afișare (fișierul original neatins).
- **Aspect identic cu slideshow-ul (§5.8)** *(decizie de implementare, după Faza 6)*: poza ocupă tot ecranul; stânga sus „pastila" cu logo + PhotoVault + contextul (folder / album / tag), dreapta sus numele pozei; jos bara de control rotunjită — anterioara, potrivire în ecran (butonul central, cerc conturat cu accentul), următoarea | micșorează, procent zoom, mărește, 1:1, rotire | contor | închidere. Butoane-glifă în culoarea accentului; elementele dispar împreună după 3 s fără mișcare de mouse și reapar la mișcare.

### 6.5 Albume

*(Vezi §5.6 pentru afișarea sub formă de carduri în main area și comportamentul panoului lateral de detalii la click.)*

- Creare album nou: **nume** (obligatoriu) + **subtitlu** (opțional, text liber) + dată creare automată.
- **Numele albumelor sunt unice** (comparație fără diferență de majuscule, inclusiv la diacritice). La creare sau editare, un nume deja folosit afișează un avertisment cu opțiunile „Modifică numele" (formularul se redeschide cu textul introdus) sau „Renunță".
- Editare album (nume + subtitlu) din meniul contextual al albumului sau din panoul de detalii.
- Copertă: implicit prima poză adăugată; poate fi setată manual („Setează ca copertă a albumului", din interiorul albumului). Dacă poza-copertă e eliminată din album, coperta revine la cea implicită.
- Adăugare poze în album: selecție multiplă în grid → "Adaugă la album" → alegere album existent sau creare unul nou pe loc.
- Fluxul tipic: import folder întreg → selectare toate pozele → adăugare completă la album → deschidere album → eliminare individuală a pozelor nedorite (mult mai eficient decât selecție manuală poză cu poză).
- Eliminare din album: șterge doar rândul din `AlbumPhotos`; poza rămâne indexată și vizibilă în arborele de foldere / alte albume.
- Ștergere album complet: șterge doar structura logică (`Albums` + `AlbumPhotos`), fișierele fizice neafectate.

### 6.6 Tag-uri

- Creare tag-uri custom (nume liber).
- Atribuire tag(uri) unei poze sau unei selecții multiple simultan.
- Filtrare grid după tag selectat din panoul lateral.
- Numele tag-urilor sunt unice (fără diferență de majuscule, inclusiv diacritice); „tag nou" cu un nume existent refolosește tag-ul existent.
- Tag-urile pozei selectate apar în panoul de detalii ca etichete (× elimină, + adaugă).
- **Badge pe miniatură**: pozele care au cel puțin un tag sunt marcate în grid cu o iconiță de tag în colțul din stânga sus al miniaturii.
- Ștergere tag: elimină din `PhotoTags` pentru toate pozele, șterge din `Tags`.

### 6.7 Sortare și căutare

- **Sortare**: dropdown/buton în bara de instrumente — după nume fișier, ascendent/descendent.
- **Căutare**: câmp text în bara de sus; caută simultan în nume fișier, nume tag-uri asociate, și nume album; rezultatele filtrează grid-ul live (cu debounce ~300ms pentru performanță la tastare rapidă).

### 6.8 Modul redenumire batch (utilitar separat, pre-import)

Fereastră dedicată, independentă de indexul principal:

- Selectare folder de pe disc (nu neapărat indexat în PhotoVault).
- Afișare listă fișiere din folder cu **preview al numelor rezultate** înainte de aplicare (obligatoriu, per cerință — nicio redenumire fără confirmare vizuală prealabilă).
- Pattern configurabil, cu variabile suportate:
  - `{name}` — numele original (fără extensie)
  - `{counter}`, `{counter:000}` — numărător secvențial, cu padding configurabil
  - `{date}` — data fișierului de pe disc (format configurabil, ex. `yyyy-MM-dd`)
  - `{ext}` — extensia originală (păstrată automat)
- Exemplu pattern: `Vacanta_Grecia_{counter:000}` → `Vacanta_Grecia_001.jpg`, `Vacanta_Grecia_002.jpg`, ...
- Buton "Aplică" execută redenumirea fizică pe disc (folosind `File.Move` / `File.Rename`), cu verificare de coliziuni de nume înainte de execuție.
- Dacă folderul redenumit e deja indexat în PhotoVault, fereastra afișează un mesaj informativ, iar după redenumire pozele respective sunt **actualizate automat în index** (cale + nume, același Id) — albumele, tag-urile și rotirea se păstrează, fără re-scanare. *(Decizie de implementare, Faza 5 — înlocuiește re-scanarea manuală recomandată inițial, care ar fi pierdut organizarea pozelor redenumite.)*
- Se iau doar fișierele foto suportate aflate direct în folderul ales (fără subfoldere). Numerotarea poate urma numele sau data fișierelor, cu număr de start configurabil.
- Conflicte care blochează aplicarea: nume duplicat în lot, fișier existent (din afara lotului) cu același nume, nume invalid în Windows. Redenumirea se face în două etape (nume temporar → nume final), astfel încât schimburile de nume funcționează; la eroare, fișierele revin la numele inițiale.

### 6.9 Rotire logică

- Click dreapta pe poză (sau tastă rapidă, ex. `R`) → rotește 90° în sensul acelor de ceasornic.
- Valoare stocată în coloana `RotationDegrees` (0/90/180/270), aplicată ca `RenderTransform` (rotație) în WPF la afișarea thumbnail-ului și în lightbox.
- Fișierul original de pe disc rămâne complet neschimbat.

### 6.10 Slideshow (Ken Burns + muzică)

**Motor vizual:**
- Fiecare poză e afișată cu o animație de zoom lent combinată cu pan (translație X/Y), ambele cu variație aleatorie la fiecare poză, pentru maximă diversitate vizuală de-a lungul slideshow-ului:
  - **Direcție zoom**: aleasă aleatoriu între **zoom in** (ex. scale 1.0 → 1.15, imaginea "se apropie") și **zoom out** (ex. scale 1.15 → 1.0, imaginea "se depărtează" — valorile start/stop sunt pur și simplu inversate față de zoom in, folosind aceeași **Intensitate zoom** configurată).
  - **Direcție pan**: aleasă aleatoriu la fiecare poză din 4 variante posibile (ex. sus-stânga→jos-dreapta, sus-dreapta→jos-stânga, etc.), independent de direcția de zoom aleasă pentru acea poză (cele două randomizări sunt separate, deci orice combinație zoom in/out + orice direcție de pan e posibilă).
- Implementare via `Storyboard` WPF cu `DoubleAnimation` pe `ScaleTransform` și `TranslateTransform`, controlate din cod (durată, punct start/stop aleatorii în limitele configurate, inclusiv alegerea aleatorie zoom in vs. zoom out per poză).
- Tranziție fade între poze consecutive: `DoubleAnimation` pe proprietatea `Opacity`, două imagini suprapuse (ieșire fade-out / intrare fade-in simultan).

**Panou de configurare (în Setări):**
| Parametru | Descriere | Interval sugerat |
|---|---|---|
| Durată afișare poză | Cât timp stă o poză vizibilă (fără tranziție) | 3–15 secunde |
| Durată fade | Durata tranziției fade între poze | 0.5–3 secunde |
| Intensitate pan | Cât se deplasează imaginea (procent din dimensiune) | 5%–25% |
| Intensitate zoom | Factorul maxim de scalare (folosit atât pentru zoom in cât și pentru zoom out, doar cu sensul inversat) | 1.05x–1.30x |

**Muzică:**
- Utilizatorul adaugă 1 sau mai multe fișiere MP3 locale (dialog selecție fișiere).
- Playlist-ul se redă în buclă continuă (loop) până la finalul slideshow-ului, indiferent dacă muzica se termină înaintea pozelor sau invers.
- Implementare cu `NAudio` — `AudioFileReader` + `WaveOutEvent`, gestionare manuală a trecerii la piesa următoare/reluare playlist la capăt.
- Control volum simplu în panoul slideshow (slider), fără egalizor sau efecte audio.

**Controale în timpul slideshow-ului (bară inferioară, auto-hide):**
- Conținut bară: Play/Pause, Previous, Next, contor poze curent/total (ex. "12 / 87"), buton Exit.
- Comportament auto-hide: bara + informațiile din colțurile superioare (nume album / nume poză curentă) dispar automat (fade-out, ~300-400ms) după 3 secunde de inactivitate a mouse-ului; reapar (fade-in) instant la orice mișcare de mouse.
- Toate elementele UI ale slideshow-ului (bară jos + text sus stânga/dreapta) apar și dispar sincronizat, ca un singur grup, păstrând ecranul complet curat pentru vizionare în starea "ascunsă".
- Implementare: `DispatcherTimer` (interval 3s) resetat la fiecare eveniment `MouseMove` la nivel de fereastră; la expirare timer → `Storyboard` fade-out pe `Opacity` pentru containerul UI overlay; la `MouseMove` ulterior → fade-in imediat + resetare timer.
- Esc închide slideshow-ul indiferent de starea vizibilă/ascunsă a barei.

### 6.11 Teme Dark / Light

- Două `ResourceDictionary` separate (`DarkTheme.xaml`, `LightTheme.xaml`) cu aceleași chei de resurse (culori fundal, text, accent) dar valori diferite.
- Comutare manuală din meniu/buton dedicat în bara de titlu sau setări.
- Preferința salvată în `AppSettings` (cheie `"Theme"`) și reîncărcată la pornirea aplicației.
- Aplicare prin `Application.Current.Resources.MergedDictionaries` — swap dinamic fără repornire.

### 6.12 Setări generale — culoare accent și limbă

Adăugate în fereastra de Setări (Opțiuni), într-o secțiune separată "General":

- **Culoare de accent**: 6 culori predefinite, afișate ca buline colorate selectabile (ex. albastru, verde, portocaliu, roșu, mov, turcoaz — paletă exactă stabilită în etapa de design vizual). Culoarea selectată se aplică pe elementele de accent din întreaga aplicație (buton activ, slider thumb, highlight selecție, buton Play slideshow etc.), respectând totuși tema Dark/Light activă (accentul se adaptează la fundal, nu îl înlocuiește).
  - Stocare: cheie nouă în `AppSettings` (ex. `"AccentColor"`, valoare cod hex), citită la pornire și aplicată ca resursă dinamică suplimentară peste `DarkTheme.xaml`/`LightTheme.xaml`.
- **Limbă interfață**: selector cu 2 opțiuni — **Română** și **Engleză**.
  - Necesită externalizarea tuturor textelor din UI în resurse de localizare (`.resx` sau dicționare `ResourceDictionary` per limbă, cu `x:Static`/`DynamicResource` pentru texte).
  - Stocare preferință: cheie nouă în `AppSettings` (ex. `"Language"`, valoare `"ro"`/`"en"`).
  - Schimbarea limbii poate necesita repornirea aplicației (simplifică implementarea față de schimbare live) — decizie acceptabilă, comunicată clar în UI printr-un mesaj discret ("Repornește aplicația pentru a aplica limba selectată").

---

## 7. Pachete NuGet necesare

| Pachet | Scop | Notă |
|---|---|---|
| `Microsoft.Data.Sqlite` | Acces bază de date SQLite | Oficial Microsoft |
| `Dapper` | Micro-ORM pentru interogări SQL simple și rapide | Alternativă ușoară la Entity Framework |
| `MetadataExtractor` | Citire EXIF + extragere embedded preview din RAW | .NET pur, fără dependențe native |
| `SixLabors.ImageSharp` | Decodare/resize imagini pentru generare thumbnails | Multiplatformă, activ întreținut |
| `NAudio` | Playback audio MP3 pentru slideshow | Matur, ușor de integrat în WPF |
| `CommunityToolkit.Mvvm` | Helpers MVVM (RelayCommand, ObservableObject) | Reduce boilerplate în ViewModels |

*Notă*: toate aceste pachete sunt .NET-native sau au binare gestionate incluse — nu introduc dependențe de sistem externe, păstrând portabilitatea completă.

*Notă tehnică suplimentară*: sincronizarea title bar-ului Windows nativ cu tema Dark/Light (§5.1) nu necesită un pachet NuGet — se realizează printr-un apel P/Invoke direct către `dwmapi.dll` (`DwmSetWindowAttribute`), inclus ca fragment de cod utilitar în `PhotoVault.App/Utils/`, fără dependențe externe suplimentare.

---

## 8. Publicare / Deployment (portabil, self-contained)

Comandă de publicare recomandată (.NET 10):

```
dotnet publish -c Release -r win-x64 --self-contained true ^
  -p:PublishSingleFile=true ^
  -p:IncludeNativeLibrariesForSelfExtract=true ^
  -p:PublishTrimmed=true ^
  -p:EnableCompressionInSingleFile=true ^
  -o ./publish
```

- `--self-contained true` → include runtime .NET, aplicația rulează pe orice Win11 fără instalare prealabilă.
- `PublishSingleFile=true` → un singur `.exe`, minimizează numărul de fișiere vizibile (cerința de amprentă minimă).
- `PublishTrimmed=true` → elimină codul .NET nefolosit, reduce dimensiunea (necesită testare atentă — trimming poate elimina din greșeală cod folosit prin reflecție, comun la unele librării de imagine; se testează după fiecare build).
- `EnableCompressionInSingleFile=true` → reduce dimensiunea finală a executabilului.

Rezultat așteptat: un executabil de aproximativ 80-150MB (variază după trimming), plus folderul `data/` generat automat la prima rulare.

---

## 9. Roadmap de dezvoltare pe faze

Această secvențiere e gândită pentru dezvoltare incrementală cu asistență AI — fiecare fază produce o aplicație funcțională, testabilă, înainte de a trece mai departe.

Fiecare fază se încheie cu `dotnet build` reușit (0 erori) pe întreaga soluție, conform §10.1.

### Faza 0 — Fundație proiect + sistem de teme
- Creare soluție .sln cu cele 3-4 proiecte (App, Core, Data, opțional Tests).
- Configurare schemă SQLite inițială + `DatabaseContext` cu creare automată DB la prima pornire.
- Fereastră principală goală, cu layout de bază (arbore stânga / grid centru / panou detalii).
- Implementare sistem de teme Dark/Light de la bază (`ResourceDictionary`-uri, restilizare ScrollBar/ComboBox/CheckBox/Slider/ContextMenu, `CustomDialogWindow` pentru mesaje) — se construiește temeinic acum, ca toate ferestrele/controalele adăugate ulterior să moștenească automat stilul corect.
- Sincronizare title bar nativ Windows cu tema (`DwmSetWindowAttribute`).
- Bară secundară sub title bar (logo + titlu stânga, cele 4 butoane iconiță dreapta — funcționale doar vizual în această fază, fără acțiuni conectate încă).
- Footer cu textul de credit.

### Faza 1 — Import și indexare
- Dialog adăugare folder sursă, scanare recursivă, populare tabel `Photos`.
- Generare thumbnails asincronă (JPEG/PNG întâi, apoi RAW).
- Afișare grid cu virtualizare, populat din DB, stilizat cu design de carduri modern (colțuri rotunjite, hover, umbră discretă).
- Bară de progres la indexare (non-blocantă), poziționată în footer stânga.

### Faza 2 — Vizualizare și navigare
- Arbore de foldere funcțional, cu afișare completă a subdirectoarelor (lazy-load la expandare).
- Lightbox fullscreen cu navigare și zoom/pan.
- Panou lateral de detalii (poză selectată).
- Modal Info (logo → fullscreen blur) și modal Info aplicație (nume, versiune, autor, instrucțiuni, shortcut-uri) — conectare completă a celor 4 butoane din bara secundară (Redenumire batch / Info / Opțiuni → Setări / Temă).

### Faza 3 — Albume și tag-uri
- CRUD albume (creare, redenumire, ștergere).
- Afișare albume ca grid de carduri (cover + nume + dată) în main area.
- Click pe card album → populare panou lateral cu detalii (nume, dată, nr. poze).
- Adăugare/eliminare poze din albume.
- CRUD tag-uri + atribuire + filtrare după tag.

### Faza 4 — Sortare, căutare, rotire
- Sortare după nume fișier.
- Căutare simplă (debounced) peste nume/tag/album.
- Rotire logică 90° cu persistare în DB.

### Faza 5 — Modul redenumire batch
- Fereastră separată, lansată din butonul dedicat în bara secundară, selecție folder extern.
- Engine de pattern-uri + preview live.
- Execuție redenumire cu verificare coliziuni.

### Faza 6 — Slideshow Ken Burns + muzică
- Motor animație Ken Burns cu direcții aleatorii.
- Tranziții fade.
- Integrare NAudio pentru playlist MP3 în loop.
- Panou setări slideshow (durate, intensități) — integrat în fereastra de Setări (Opțiuni).
- Bară de control inferioară + informații superioare, cu comportament auto-hide (3 secunde inactivitate, fade in/out la mouse move).

### Faza 7 — Polish vizual, setări generale și gestiune index
- Trecere finală de restilizare pe toate meniurile dropdown și contextuale rămase (verificare vizuală completă Dark + Light, niciun element cu aspect WPF implicit).
- Secțiune "General" în Setări: selector culoare accent (6 buline predefinite) + selector limbă (Română/Engleză), cu externalizarea textelor UI în resurse de localizare.
- Gestiune poze lipsă (eliminare automată la re-scanare).
- Optimizări performanță (profiling pe colecție de test de 50.000 poze).

### Faza 8 — Packaging, documentație și testare finală
- Configurare publish self-contained single-file.
- Testare portabilitate (copiere pe alt PC / stick USB fără .NET instalat).
- Testare cu colecție reală de RAW (CR2/NEF/DNG) pentru validare extragere preview.
- Redactare `README.md` complet (conform §10.2).
- Verificare finală de compilare fără erori pe întreaga soluție.

---

## 10. Cerințe de livrare și calitate

Aceste reguli se aplică transversal, pe tot parcursul dezvoltării (fiecare fază din §9), nu doar la final:

### 10.1 Verificare compilare

- **Fiecare fișier de cod livrat trebuie verificat că se compilează fără erori** înainte de a fi considerat "gata" — nu se livrează cod netestat la compilare.
- La finalul fiecărei faze din roadmap (§9), se execută `dotnet build` pe întreaga soluție și se confirmă build reușit (0 erori) înainte de a trece la faza următoare.
- Avertismentele (warnings) se analizează și se rezolvă când e rezonabil (mai ales cele legate de nullable reference types, relevante în .NET 10), dar nu blochează neapărat livrarea — doar erorile de compilare sunt blocante.

### 10.2 README.md

Se livrează un fișier `README.md` (în rădăcina proiectului sursă, separat de `README.txt`-ul din folderul portabil publicat — vezi §3.2) care explică aplicația în detaliu, cu minim următoarele secțiuni:

- **Descriere generală** — ce este PhotoVault, pentru ce e util, principii de funcționare (index-only, non-distructiv, portabil).
- **Cerințe de sistem** — Windows 11, spațiu pe disc necesar, nicio dependență suplimentară de instalat.
- **Instalare / Prima rulare** — cum se despachetează/copiază folderul, cum se pornește aplicația prima dată.
- **Ghid de utilizare pe funcționalități**:
  - Cum se adaugă foldere sursă și cum funcționează indexarea.
  - Cum se creează și gestionează albume (inclusiv fluxul recomandat: adaugă tot → elimină ce nu vrei).
  - Cum se folosesc tag-urile.
  - Cum se folosește modulul de redenumire batch (cu exemplu de pattern).
  - Cum se pornește și configurează slideshow-ul (Ken Burns, muzică, parametri).
  - Cum se comută tema Dark/Light.
  - Cum se selectează culoarea de accent și limba interfeței (Română/Engleză).
- **Structura folderului aplicației** — explicație scurtă a ce reprezintă `data/photovault.db`, `data/thumbnails/`, de ce nu trebuie șterse manual.
- **Limitări cunoscute** — ex. formate suportate (JPEG, PNG, CR2, NEF, DNG — fără HEIC), fără editare avansată, fără verificare duplicate.
- **Portabilitate** — cum se mută aplicația pe alt calculator/stick USB, ce se întâmplă cu folderele sursă indexate dacă își schimbă calea (necesită realiniere manuală).
- **Credit** — `concept și realizare vlad39`.

Acest README este documentul de referință pentru utilizator (tu), separat de documentul tehnic de față (`PhotoVault_Plan.md`), care rămâne documentul de referință pentru dezvoltare.

---

## 11. Riscuri și puncte de atenție

| Risc | Mitigare |
|---|---|
| Performanță grid la 50.000 poze | Virtualizare UI obligatorie (`VirtualizingStackPanel`), thumbnails pre-generate, paginare/lazy-load la scroll dacă necesar |
| Formate RAW proprietare variate (CR2/NEF/DNG au sub-variante) | `MetadataExtractor` acoperă cazurile comune; testare cu fișiere reale din camera ta încă din Faza 1 |
| Trimming .NET elimină cod necesar (reflection-based) | Testare build-uri publish frecvent, nu doar la final; dezactivare trimming pentru librăriile problematice dacă apar erori |
| Căi de foldere sursă devin invalide (schimbare literă disc, USB extern) | Mesaj clar în UI + opțiune manuală de realiniere folder sursă (schimbare cale fără pierdere date din album/tag-uri) |
| Dimensiune executabil mare (self-contained) | Acceptat ca trade-off pentru portabilitate totală; compresie activată la publish |

---

## 12. Îmbunătățiri post-MVP

Aceste patru funcționalități **nu fac parte din scope-ul ferm** stabilit în restul documentului (§1-§11) și **nu sunt incluse** în roadmap-ul din §9. Sunt candidate cu efort mic și valoare practică mare, de evaluat și eventual implementat **după** ce aplicația de bază (Fazele 0-8) e completă și funcțională, ideal după câteva săptămâni de utilizare reală — abia atunci se vede clar ce chiar lipsește în practică.

### 12.1 Backup automat al bazei de date

- Înainte de operațiuni cu potențial distructiv asupra structurii logice (redenumire batch care afectează un folder indexat, ștergere album, ștergere în masă de poze din index), aplicația creează automat o copie a fișierului `data/photovault.db` sub forma `photovault.db.bak.{timestamp}`.
- Păstrare rotativă: ultimele 3-5 copii, cele mai vechi se șterg automat la crearea uneia noi.
- Scop: dacă baza se corupe sau o operațiune produce un rezultat nedorit, utilizatorul poate reveni manual la o copie anterioară (redenumire fișier `.bak` înapoi în `photovault.db`, cu aplicația închisă), fără a pierde luni de organizare (albume, tag-uri, rotații).
- Implementare: simplu `File.Copy` în serviciul care orchestrează operațiunea riscantă, fără nevoie de librării suplimentare.

### 12.2 Health-check foldere sursă la pornire

- La pornirea aplicației, se verifică rapid și silențios (fără a bloca UI, fără progress bar vizibil) dacă fiecare folder din `SourceFolders` este încă accesibil pe disc (`Directory.Exists`).
- Dacă un folder lipsește temporar (ex. disc extern/USB neconectat în acel moment), acesta **nu** este eliminat automat și pozele asociate **nu** sunt șterse din index (comportament diferit de eliminarea pozelor individuale lipsă din §6.2/§10 — aici scopul e să nu confunzi "disc deconectat momentan" cu "fișiere șterse definitiv").
- Se afișează un indicator discret (ex. iconiță de avertizare lângă folderul respectiv în arborele din stânga) — vizibil, dar neintruziv, fără pop-up care întrerupe fluxul de lucru.
- La reconectarea discului și o nouă pornire/re-scanare, indicatorul dispare automat dacă folderul redevine accesibil.

### 12.3 Flag "Favorite" pe poze

- Diferit de tag-urile custom (§6.6): un steag simplu boolean, gândit ca shortcut rapid de navigare, nu ca metadată de clasificare.
- Activare/dezactivare cu un singur click (iconiță tip inimă/stea pe thumbnail, la hover sau permanent vizibilă) sau tastă rapidă dedicată.
- Coloană nouă în schema DB: `Photos.IsFavorite INTEGER NOT NULL DEFAULT 0`.
- Filtru rapid disponibil în bara laterală sau bara de instrumente: "Arată doar favorite", aplicabil peste orice context curent (folder, album, rezultat căutare).
- Nu presupune fereastră de configurare sau opțiuni suplimentare — intenționat minimal.

### 12.4 Control de zoom pe grid (dimensiune thumbnail-uri)

- Slider sau grup de butoane (mic/mediu/mare, similar Windows Explorer) în bara de instrumente a grid-ului principal, care ajustează dimensiunea de afișare a thumbnail-urilor.
- Nu necesită regenerarea thumbnail-urilor stocate pe disc (acestea rămân la rezoluția fixă generată la import, ex. 300x300px) — doar dimensiunea de afișare (`Width`/`Height` pe elementul din `ItemsControl`) se ajustează, cu upscaling vizual dacă utilizatorul alege dimensiune mare.
- Preferința de zoom poate fi salvată în `AppSettings` (cheie ex. `"GridThumbnailSize"`) pentru a persista între sesiuni.
- Beneficiu: la 50.000 poze, uneori utilizatorul vrea să vadă mai multe simultan (thumbnail mic, scanare rapidă), alteori vrea detaliu vizual mai mare fără a intra în lightbox.

---

## 13. Rezumat decizii finale (checklist)

- [x] Bază de date: **SQLite**
- [x] Poze: **index-only**, rămân pe disc la locația originală
- [x] Thumbnails: **cache pe disc**, format .jpg, subfoldere pe hash
- [x] Albume: **manuale**, cu eliminare individuală de poze fără ștergere fizică
- [x] Metadate: **doar tag-uri custom**
- [x] Redenumire batch: **pre-import**, pe foldere externe, cu preview obligatoriu
- [x] Sortare: **nume fișier**
- [x] Căutare: **simplă** (nume/tag/album)
- [x] Slideshow: **Ken Burns** (direcții aleatorii) + **fade** + **playlist muzică în loop** + panou configurare complet
- [x] Import: **manual**, foldere reținute permanent, re-scanare la cerere
- [x] Duplicate: **neverificate**
- [x] Layout: **Explorer-style** (arbore stânga, grid centru, detalii jos/lateral)
- [x] Teme: **Dark/Light, comutare manuală**
- [x] Editare: **doar rotire logică** 90°, fișier original neatins
- [x] Formate: **JPEG, PNG, CR2, NEF, DNG** (fără HEIC)
- [x] Poze lipsă: **eliminare automată din index**, fără confirmare
- [x] Portabilitate: **self-contained**, rulează fără .NET preinstalat
- [x] Nume aplicație: **PhotoVault**
- [x] Title bar: **nativ Windows** (sincronizat cu tema via DWM), + **bară secundară custom** cu logo/titlu stânga și 4 butoane iconiță dreapta (Redenumire batch / Info / Opțiuni / Temă)
- [x] Click logo: **modal fullscreen** cu fundal blur, fără text/elemente, închidere Esc sau click pe imagine
- [x] Footer: **stânga** = bară progres indexare / mesaje de stare, **dreapta** = text fix „© concept și realizare vlad39"
- [x] Toate elementele UI (scrollbar, mesaje, dropdown-uri, meniuri contextuale, sliders) **restilizate complet**, fără aspect WPF/Windows implicit
- [x] Design: **carduri moderne** (colțuri rotunjite, hover, umbră discretă) pentru albume și alte liste
- [x] Arbore foldere: afișare **completă a subdirectoarelor**, lazy-load
- [x] Albume: afișate ca **grid de carduri** (cover, nume, dată) în main area; click → panou lateral cu detalii (nume, dată, nr. poze)
- [x] Bară de progres **la indexare**, non-blocantă, poziționată în footer stânga
- [x] Slideshow: bară de control + info superior cu **auto-hide** (3 secunde, fade in/out la mouse move)
- [x] Slideshow Ken Burns: **zoom in și zoom out aleator** (valori inversate), independent de direcția aleatorie de pan
- [x] Modal **Info**: nume aplicație, versiune, autor, instrucțiuni scurte de utilizare, **listă shortcut-uri tastatură**
- [x] Setări generale: **culoare accent** (6 buline predefinite selectabile) + **selector limbă** (Română/Engleză)
- [x] Cerință livrare: **toate fișierele verificate să compileze fără erori**
- [x] Cerință livrare: **README.md** detaliat, separat de acest document tehnic
- [x] Albume: **nume unice** (avertisment la duplicat) + **subtitlu liber** afișat pe card (data creării doar în panoul de detalii)
- [x] Tag-uri: **badge** pe miniaturile pozelor cu tag-uri
- [ ] *(post-MVP, opțional, §12)* Backup automat DB, health-check foldere, flag Favorite, zoom grid

---

## 15. Decizii stabilite în timpul implementării (Fazele 0–7)

Clarificări și ajustări convenite pe parcursul dezvoltării; au prioritate față de formulările inițiale din secțiunile anterioare acolo unde diferă (secțiunile relevante au fost deja actualizate).

| Subiect | Decizie |
|---|---|
| Buton „Adaugă folder sursă" | **+** în antetul secțiunii Bibliotecă + lista folderelor sursă în Opțiuni (§6.2) |
| Filtrare după folder | Folderul selectat **plus toate subfolderele** lui; click pe „Bibliotecă" = toate pozele (§6.1) |
| Lightbox | Fereastră separată, pe tot ecranul, fundal negru (conform §6.4, nu varianta „în fereastra principală" din mockup); zoom: rotiță / + − / 0 = potrivire / 1 = 1:1, pan prin tragere; deschidere cu dublu-click sau Enter; **același layout și design ca slideshow-ul** (pastile sus, bară de control jos, auto-hide 3 s — stiluri comune în `Resources/Styles/Viewer.xaml`) |
| Panou de detalii | Pe lângă câmpurile din §5.6: date EXIF citite din fișier (dimensiuni, data fotografierii, cameră, obiectiv, ISO, expunere, diafragmă, distanță focală) — doar afișare |
| Previzualizare RAW | Se folosește cea mai mare previzualizare JPEG **baseline/progressive** încorporată; datele RAW stocate ca JPEG lossless (CR2/DNG) sunt ignorate, fiind nedecodabile |
| Albume | Nume unice + subtitlu liber pe card (§4, §5.7, §6.5) |
| Tag-uri | Badge pe miniaturile pozelor cu tag-uri (§6.6) |
| Redenumire batch — index | Pozele deja indexate dintr-un folder redenumit sunt **actualizate direct în index** (cale + nume, același Id), deci își păstrează albumele, tag-urile și rotirea; nu mai e necesară re-scanarea (§6.8) |
| Redenumire batch — execuție | Doar fișierele foto suportate aflate direct în folder (fără subfoldere); redenumire în două etape (nume temporar → nume final), cu revenire la numele inițiale dacă o mutare eșuează; numerotare după nume sau după data fișierului, cu număr de start configurabil (§6.8) |
| Slideshow — pornire și final | Buton „Slideshow" deasupra grid-ului, F5 sau meniul contextual al pozei; rulează cu pozele afișate (context + căutare + sortare), de la poza selectată; redarea automată se încheie după ultima poză (revenire în galerie cu ultima poză selectată); navigarea manuală ← / → trece circular de la ultima la prima (§6.10) |
| Slideshow — Ken Burns | Scara de bază = 1 + intensitatea pan (ex. 1,10 la 10%), astfel încât deplasarea nu scoate niciodată marginea imaginii în cadru; zoom in = bază → bază × intensitate zoom, zoom out = invers; pan pe una din cele 4 diagonale; mișcarea durează fade-in + afișare + fade-out; pauza îngheață mișcarea (§6.10) |
| Slideshow — muzică | NAudio 3.x (`WaveOut`, fostul `WaveOutEvent`); volumul (0–100%, cheia `SlideshowVolume`) se setează în Opțiuni; piesele lipsă / ilizibile sunt sărite; fără dispozitiv audio → slideshow fără muzică, fără eroare (§6.10) |
| Opțiuni → General | Temă + culoare de accent (6 buline, aplicare imediată) + limbă; la schimbarea limbii, dialog „Repornește acum / Mai târziu" (repornire automată a executabilului) și mesaj discret cât timp repornirea e în așteptare (§6.12) |
| Gestiune index | Pe lângă eliminarea automată a pozelor lipsă la re-scanare: buton „Re-scanează toate" în Opțiuni → Foldere sursă; folderele indisponibile sunt sărite și anunțate, fără a le atinge pozele (§6.2) |
| Performanță (50.000 de poze) | Profiling: cuvintele-cheie de căutare citite într-o singură interogare agregată (27 s → ~40 ms); migrarea 004 adaugă indecși pe `AlbumPhotos(PhotoId)`, `PhotoTags(TagId)`, `Albums(CoverPhotoId)` (ștergere 20.000 de poze: 4,2 s → 0,4 s); căutarea compară texte normalizate o singură dată (fără diacritice, litere mici); progresul miniaturilor raportat din 20 în 20. Test automat de regresie (`LargeLibraryTests`) + job CI `perf-50k` care rulează aplicația reală pe 50.000 de poze (la cerere sau cu „[perf]" în mesajul commit-ului) |
| ImageSharp | Versiunea 3.1.x (4.x cere cheie de licență la build) |
| Structura proiectului | Pe lângă §3.3: `PhotoVault.Core/Abstractions/` (interfețele repository-urilor, ca serviciile din Core să nu depindă de Data), `PhotoVault.App/Utils/` (teme, DWM, dialoguri, localizare), `PhotoVault.App/Resources/Localization/` (texte RO/EN) |
| Verificare pe Windows | Workflow GitHub Actions (`.github/workflows/windows-build.yml`): build, teste unitare, smoke test UI cu capturi de ecran și versiune portabilă descărcabilă, la fiecare push |

---

*Document generat ca bază de lucru pentru dezvoltare incrementală asistată de AI. Fiecare fază din secțiunea 9 poate fi discutată și implementată separat, cu acest document ca referință completă a deciziilor arhitecturale stabilite. Elementele din secțiunea 12 sunt candidate opționale, în afara scope-ului ferm, de evaluat după finalizarea Fazelor 0-8.*

---

## 14. README.md

Conținutul de mai jos este textul propriu-zis al fișierului `README.md` al aplicației (conform specificației din §10.2), scris la nivelul de detaliu curent al planului. Este gândit ca **punct de plecare funcțional**, nu ca text final — pe măsură ce fiecare fază din §9 se implementează, secțiunile marcate `[de completat]` se actualizează cu detalii reale (capturi de ecran, exemple concrete, pași verificați). Se livrează în rădăcina proiectului sursă și se copiază/adaptează și în folderul aplicației publicate (ca variantă `README.txt`, conform §3.2).

Textul de mai jos poate fi copiat direct într-un fișier `README.md`, între cele două linii de delimitare.

***

```markdown
# PhotoVault

Aplicație portabilă pentru organizarea și vizualizarea albumelor foto personale.

**Versiune document:** corespunde stadiului de plan (pre-implementare) · **Ultima actualizare:** [de completat la fiecare fază finalizată]

---

## Ce este PhotoVault

PhotoVault este o aplicație desktop pentru Windows 11 care te ajută să organizezi, vizualizezi și prezinți colecția ta de fotografii personale — fără să muți, copiezi sau modifici fișierele originale de pe disc.

Principii de funcționare:

- **Non-distructiv** — nicio acțiune din aplicație nu modifică fișierele foto originale. Rotirea, organizarea în albume, tag-urile — toate sunt informații stocate separat, într-o bază de date proprie a aplicației.
- **Index, nu copie** — PhotoVault "ține minte" unde sunt pozele tale (în folderele pe care le adaugi manual), dar nu le duplică și nu le mută niciunde. Funcționează similar cu Adobe Lightroom.
- **Complet portabil** — toată aplicația stă într-un singur folder. Poți muta acest folder pe alt calculator sau pe un stick USB, iar aplicația rulează la fel, fără nicio instalare, fără să necesite programe suplimentare instalate pe calculatorul pe care rulează.

---

## Cerințe de sistem

- Windows 11
- Spațiu liber pe disc: [de completat — dimensiune reală după publish, estimat 100-200 MB pentru aplicație + spațiu suplimentar pentru cache-ul de thumbnail-uri, proporțional cu numărul de poze indexate]
- Nicio altă dependență necesară — aplicația include tot ce-i trebuie ca să ruleze (nu necesită .NET instalat separat)

---

## Instalare și prima rulare

PhotoVault nu se instalează în sensul clasic (nu există setup.exe care scrie în Program Files sau în Registry).

1. Copiază folderul `PhotoVault` (întreg, cu tot conținutul) oriunde vrei pe calculator — Desktop, o partiție separată, un stick USB.
2. Deschide folderul și rulează `PhotoVault.exe`.
3. La prima pornire, aplicația creează automat subfolderul `data/` (bază de date + cache thumbnail-uri) — nu este nevoie de nicio configurare inițială.
4. Adaugă primul folder cu poze (vezi secțiunea "Import de poze" mai jos) și poți începe să organizezi.

**Important:** dacă muți folderul `PhotoVault` pe alt calculator sau altă locație, folderele sursă indexate anterior (poza ta de pe alt disc/alt PC) pot deveni inaccesibile dacă nu sunt și ele mutate/disponibile la aceeași cale. Vezi secțiunea "Portabilitate" mai jos.

---

## Ghid de utilizare

### Import de poze

- PhotoVault **nu** scanează automat calculatorul. Tu adaugi manual folderele pe care vrei să le indexeze, unul câte unul.
- Din bara secundară → **Opțiuni**, sau din meniul de gestiune foldere sursă [de completat — locația exactă a butonului "Adaugă folder" în interfața finală], alegi un folder de pe disc.
- Aplicația scanează recursiv acel folder (inclusiv subfoldere) și indexează toate pozele găsite (formate suportate: JPEG, PNG, CR2, NEF, DNG).
- Folderele adăugate rămân în listă permanent — data viitoare când pornești aplicația, nu trebuie să le re-adaugi.
- Dacă adaugi poze noi într-un folder deja indexat, folosește butonul **"Re-scanează"** din dreptul acelui folder ca aplicația să le detecteze.
- Aplicația **nu verifică duplicate** — dacă aceeași poză există fizic în două foldere diferite adăugate, va apărea de două ori.

### Navigare și vizualizare

- Panoul din stânga arată structura de foldere (ca în Windows Explorer), cu subdirectoare, plus lista de Albume și lista de Tag-uri.
- Selectând un folder, un album sau un tag, grid-ul central afișează pozele corespunzătoare.
- Dublu-click pe o poză deschide vizualizarea fullscreen (lightbox) — navigare cu săgețile stânga/dreapta, zoom cu scroll, pan cu drag.
- Panoul din dreapta/jos arată detalii despre poza sau albumul selectat curent.

### Albume

- Creezi un album nou, îi dai un nume, apoi adaugi poze în el (selecție multiplă din grid → "Adaugă la album").
- **Flux recomandat:** dacă vrei un album din 95 de poze dintr-un folder cu 100, e mai rapid să adaugi tot folderul (100 poze) în album și apoi să elimini cele 5 nedorite, decât să selectezi manual 95.
- Eliminarea unei poze dintr-un album **nu șterge fișierul** de pe disc și nu o elimină din restul aplicației — poza rămâne indexată normal, doar nu mai apare în acel album specific.
- O poză poate face parte din mai multe albume simultan.

### Tag-uri

- Poți crea etichete custom (ex. "Familie", "Vacanță 2024") și le poți atribui uneia sau mai multor poze.
- Filtrarea după tag afișează toate pozele care au acel tag, indiferent din ce folder provin.

### Redenumire batch (utilitar separat)

- Buton dedicat în bara secundară (lângă Info/Opțiuni/Temă).
- Util **înainte** de a importa un folder în PhotoVault — de exemplu, ca să redenumești o serie de poze descărcate de pe cameră (nume gen `IMG_1234.jpg`) într-un format mai clar (`Vacanta_Grecia_001.jpg`, `Vacanta_Grecia_002.jpg`, ...).
- Alegi folderul, definești un pattern de nume (cu variabile: nume original, numărător secvențial, dată), și **vezi un preview** al numelor rezultate înainte să confirmi.
- Operațiunea redenumește efectiv fișierele pe disc — dacă folderul respectiv e deja indexat în PhotoVault, pozele sunt actualizate automat în index (își păstrează albumele, tag-urile și rotirea).

### Slideshow

- Pornește un slideshow din orice folder/album deschis.
- Fiecare poză are un efect lent de zoom (in sau out, aleator) și deplasare (pan, direcție aleatorie), cu tranziție fade către poza următoare.
- Poți adăuga una sau mai multe piese MP3 din calculatorul tău — se redau în buclă până la finalul slideshow-ului.
- Bara de control (Play/Pause, Next/Previous, contor poze, Exit) și numele pozei/albumului dispar automat după 3 secunde de inactivitate a mouse-ului și reapar imediat la mișcarea mouse-ului — pentru o vizionare curată, fără elemente pe ecran.
- Parametrii (durată afișare, durată fade, intensitate zoom/pan) se configurează din **Opțiuni**.

### Temă, culoare accent și limbă

- Butonul **Temă** din bara secundară comută instant între aspect Dark și Light.
- Din **Opțiuni** → secțiunea General, poți alege o culoare de accent (6 variante predefinite) și limba interfeței (Română sau Engleză — schimbarea limbii poate necesita repornirea aplicației).

### Rotire poze

- Poți roti o poză din 90 în 90 de grade (tastă `R` sau meniu contextual).
- Rotirea este **logică** — se reține doar în baza de date a aplicației; fișierul original de pe disc rămâne exact așa cum era. Dacă deschizi fișierul cu alt program, va apărea nerotit.

### Shortcut-uri de tastatură

| Tastă | Acțiune |
|---|---|
| `Esc` | Închide lightbox / slideshow / modal logo |
| `←` / `→` | Navigare poză anterioară/următoare |
| `R` | Rotire poză 90° |
| `Space` | Play/Pause slideshow |
| `Ctrl+F` | Focus pe câmpul de căutare |

[de completat — lista finală de shortcut-uri, pe măsură ce sunt implementate; identică cu lista din modalul "Info" al aplicației]

---

## Structura folderului aplicației

```
PhotoVault/
├── PhotoVault.exe        ← executabilul aplicației, rulează acest fișier
├── data/
│   ├── photovault.db     ← baza de date: albume, tag-uri, index poze, setări
│   └── thumbnails/       ← cache cu miniaturi generate automat
├── logs/                 ← jurnal de erori (dacă apar probleme)
└── README.txt
```

**Nu șterge și nu edita manual** fișierele din `data/` — acolo e stocată toată organizarea ta (albume, tag-uri, rotații, setări). Ștergerea lor înseamnă pierderea acestei organizări (pozele originale de pe disc rămân neafectate, dar trebuie reindexate de la zero).

---

## Limitări cunoscute

- Formate suportate: **JPEG, PNG, CR2, NEF, DNG**. Nu este suportat HEIC și nici alte formate RAW în afara celor trei enumerate.
- Nu există funcții de editare a imaginii (culoare, expunere, crop) — singura modificare posibilă este rotirea logică din 90 în 90 de grade.
- Nu se verifică duplicate la import — aceeași poză, adăugată din două foldere diferite, va apărea de două ori.
- Sortarea și căutarea sunt intenționat simple (după nume fișier; căutare după nume/tag/album) — nu există filtre combinate avansate sau sortare după dată EXIF.

---

## Portabilitate — mutarea aplicației pe alt calculator sau stick USB

1. Copiază folderul `PhotoVault` complet (inclusiv `data/`) la noua locație.
2. Rulează `PhotoVault.exe` de acolo — nu necesită nimic instalat pe noul calculator.
3. **Atenție la folderele sursă**: aplicația reține căile complete către folderele cu poze (ex. `D:\Poze\Vacanta2024`). Dacă acele foldere nu sunt disponibile la exact aceeași cale pe noul calculator (alta literă de disc, alt drive extern), pozele din ele vor apărea ca lipsă. Poți realinia manual calea către noua locație a folderului sursă din interfața aplicației [de completat — pașii exacți, odată implementată funcția de realiniere].
4. Dacă intenția e ca și pozele originale să fie portabile (nu doar aplicația), acestea trebuie copiate separat, păstrând ideal aceeași structură relativă de foldere.

---

## Pentru dezvoltator (context tehnic)

Acest README este destinat utilizatorului final al aplicației. Pentru detalii de arhitectură, schema bazei de date, stack tehnologic și decizii de design, consultă documentul tehnic complet: `PhotoVault_Plan.md`.

---

**Credit:** concept și realizare vlad39
```

***

### Note pentru menținerea acestui capitol

- Fiecare secțiune marcată `[de completat]` mai sus trebuie actualizată la finalul fazei din §9 în care funcționalitatea respectivă capătă formă finală (ex. pașii exacți de realiniere folder sursă se scriu abia când acea funcție există efectiv în UI).
- Secțiunea "Shortcut-uri de tastatură" trebuie sincronizată cu conținutul modalului **Info** din aplicație (§5.3) — cele două liste nu trebuie să diverge.
- La finalul Fazei 8 (§9), acest capitol devine sursa finală pentru fișierul `README.md` livrat efectiv în proiect (§10.2) — la acel moment, toate mențiunile `[de completat]` trebuie să fi dispărut, înlocuite cu conținut real și verificat.
