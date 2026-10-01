# PhotoVault

Aplicație portabilă pentru organizarea și vizualizarea albumelor foto personale.

**Versiune:** 1.1.0 — fazele de dezvoltare 0–8 + îmbunătățirile din capitolul 12 (copii de siguranță, verificarea folderelor, favorite, dimensiunea miniaturilor) · **Ultima actualizare:** septembrie 2026

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
- Spațiu liber pe disc:
  - aplicația: ~80 MB (un singur `PhotoVault.exe`);
  - biblioteca (`data/`): crește cu numărul de poze indexate — măsurat: ~3,5 MB la 1.000 de poze (miniaturi + bază de date), adică ~190 MB la 50.000 de poze;
  - la prima pornire, Windows mai despachetează ~20 MB de componente interne ale aplicației în folderul temporar al utilizatorului (vezi „Limitări cunoscute").
- Memorie: ~200–350 MB la o bibliotecă de 50.000 de poze.
- Nicio altă dependență necesară — aplicația include tot ce-i trebuie ca să ruleze (nu necesită .NET instalat separat)

---

## Instalare și prima rulare

PhotoVault nu se instalează în sensul clasic (nu există setup.exe care scrie în Program Files sau în Registry).

1. Copiază folderul `PhotoVault` (întreg, cu tot conținutul) oriunde vrei pe calculator — Desktop, o partiție separată, un stick USB.
2. Deschide folderul și rulează `PhotoVault.exe`.
3. La prima pornire, aplicația creează automat subfolderul `data/` (bază de date + cache thumbnail-uri) — nu este nevoie de nicio configurare inițială.
   - Dacă Windows afișează „Windows protected your PC" (SmartScreen), apasă **More info → Run anyway**: executabilul nu este semnat digital, dar nu se instalează nimic.
4. Adaugă primul folder cu poze (vezi secțiunea "Import de poze" mai jos) și poți începe să organizezi.

**Important:** dacă muți folderul `PhotoVault` pe alt calculator sau altă locație, folderele sursă indexate anterior (poza ta de pe alt disc/alt PC) pot deveni inaccesibile dacă nu sunt și ele mutate/disponibile la aceeași cale. Vezi secțiunea "Portabilitate" mai jos.

---

## Ghid de utilizare

### Import de poze

- PhotoVault **nu** scanează automat calculatorul. Tu adaugi manual folderele pe care vrei să le indexeze, unul câte unul.
- Apasă butonul **+** din dreptul secțiunii **Bibliotecă** (panoul din stânga) și alege un folder de pe disc. Lista folderelor sursă va fi disponibilă și în fereastra **Opțiuni**.
- Aplicația scanează recursiv acel folder (inclusiv subfoldere) și indexează toate pozele găsite (formate suportate: JPEG, PNG, CR2, NEF, DNG). Progresul apare în stânga jos, în footer; poți continua să folosești aplicația între timp.
- Miniaturile se generează în fundal și apar pe rând în grid. Pentru fișierele RAW se folosește previzualizarea JPEG încorporată de cameră (rapid, fără decodare RAW completă).
- Nu poți adăuga un folder care e deja inclus (sau un subfolder al unui folder deja adăugat) — aplicația te anunță.
- Folderele adăugate rămân în listă permanent — data viitoare când pornești aplicația, nu trebuie să le re-adaugi.
- Dacă adaugi poze noi într-un folder deja indexat, fă **click dreapta pe folder → Re-scanează** ca aplicația să le detecteze. Pozele care între timp au fost șterse sau mutate de pe disc sunt eliminate automat din index.
- Dacă folderul nu e accesibil la re-scanare (ex. disc extern deconectat), indexul **nu** este golit — reconectează discul și re-scanează.
- **Opțiuni → Foldere sursă → Re-scanează toate** verifică dintr-o dată toate folderele: adaugă pozele noi și elimină din index pozele șterse / mutate. Folderele indisponibile în acel moment sunt sărite (și anunțate), fără să li se atingă pozele.
- **Click dreapta pe folder → Elimină din bibliotecă** scoate folderul și pozele lui din index (fișierele de pe disc nu sunt atinse).
- Aplicația **nu verifică duplicate** — dacă aceeași poză există fizic în două foldere diferite adăugate, va apărea de două ori.

### Navigare și vizualizare

- Panoul din stânga arată structura de foldere (ca în Windows Explorer), cu subdirectoare, plus lista de Albume și lista de Tag-uri. Subfolderele se încarcă atunci când deschizi un nivel (săgeata din stânga folderului).
- Selectând un folder, grid-ul central afișează pozele din acel folder **și din toate subfolderele lui**. Click pe titlul **Bibliotecă** afișează din nou toate pozele.
- Click pe o poză afișează în panoul din dreapta detaliile ei: cale completă, dimensiune, extensie și — dacă există în fișier — dimensiunile în pixeli, data fotografierii, camera, obiectivul, ISO, timpul de expunere, diafragma și distanța focală.
- **Dublu-click** pe o poză (sau **Enter**) o deschide pe tot ecranul: ← / → pentru navigare, rotița mouse-ului sau + / − pentru zoom, tragere cu mouse-ul pentru deplasare când imaginea e mărită, 0 = potrivire în ecran, 1 = dimensiune reală, R = rotire, Esc = închidere.
- La pornire, fereastra aplicației se deschide maximizată.
- Vizualizarea pe tot ecranul arată ca slideshow-ul (și, ca el, urmează tema Dark / Light): sus numele folderului / albumului și al pozei, jos bara de control (navigare, zoom, rotire, contor, închidere); toate dispar după 3 secunde fără mișcare de mouse și reapar când miști mouse-ul.
- **Căutare** (câmpul din bara de deasupra grid-ului, sau **Ctrl+F**): caută simultan în numele fișierelor, în tag-uri și în numele albumelor, în ce e afișat în acel moment (toate pozele / folderul / albumul / tag-ul selectat). Nu ține cont de majuscule sau diacritice („vacanta” găsește „Vacanță”); mai multe cuvinte = toate trebuie să apară. **Esc** golește căutarea.
- **Sortare**: lista de lângă câmpul de căutare — după nume fișier, A → Z sau Z → A.

### Albume

- Creezi un album cu butonul **+** din dreptul secțiunii **Albume** (panoul stâng) sau direct din meniul pozelor. Îi dai un **nume** și, opțional, un **subtitlu** liber (ex. „Vacanță la mare” / „10–15.08.2021”) — subtitlul apare pe cardul albumului; data creării albumului apare doar în panoul de detalii.
- Numele albumelor sunt unice: dacă alegi un nume deja folosit (indiferent de majuscule), aplicația te anunță și poți modifica numele sau renunța.
- Selectezi poze în grid (click; **Ctrl + click** pentru mai multe; **Shift + click** pentru un interval; **Ctrl + A** pentru toate), apoi **click dreapta → Adaugă la album** → alegi un album existent sau **Album nou...**.
- Click pe titlul **Albume** afișează albumele ca grid de carduri (copertă, nume, dată creare, număr de poze). Un click pe card arată detaliile albumului în panoul din dreapta; **dublu-click** (sau click pe album în lista din stânga) deschide albumul.
- În interiorul unui album: **click dreapta → Elimină din album** sau **Setează ca copertă a albumului**. Coperta implicită este prima poză adăugată.
- **Click dreapta pe un album** în lista din stânga (sau pe card): editare (nume, subtitlu) / ștergere. Ștergerea unui album elimină doar gruparea — pozele rămân în bibliotecă și pe disc.
- **Flux recomandat:** dacă vrei un album din 95 de poze dintr-un folder cu 100, e mai rapid să adaugi tot folderul (100 poze) în album și apoi să elimini cele 5 nedorite, decât să selectezi manual 95.
- Eliminarea unei poze dintr-un album **nu șterge fișierul** de pe disc și nu o elimină din restul aplicației — poza rămâne indexată normal, doar nu mai apare în acel album specific.
- O poză poate face parte din mai multe albume simultan.

### Tag-uri

- Poți crea etichete custom (ex. "Familie", "Vacanță 2024") și le poți atribui uneia sau mai multor poze: selecție în grid → **click dreapta → Adaugă tag** → tag existent sau **Tag nou...**.
- Tag-urile pozei selectate apar în panoul din dreapta: **+** adaugă un tag, **×** îl elimină de pe poză.
- Pozele care au cel puțin un tag sunt marcate în grid cu un mic badge (iconița de tag) în colțul din stânga sus al miniaturii.
- Click pe un tag din panoul stâng afișează toate pozele care îl au, indiferent din ce folder provin.
- Numele tag-urilor sunt unice (fără diferență între majuscule și minuscule). **Click dreapta pe tag** → redenumire / ștergere (ștergerea îl elimină de pe toate pozele; pozele nu sunt afectate).

### Redenumire batch (utilitar separat)

- Buton dedicat în bara secundară (prima iconiță, lângă Info/Opțiuni/Temă) — deschide o fereastră separată.
- Util **înainte** de a importa un folder în PhotoVault — de exemplu, ca să redenumești o serie de poze descărcate de pe cameră (nume gen `IMG_1234.jpg`) într-un format mai clar (`Vacanta_Grecia_001.jpg`, `Vacanta_Grecia_002.jpg`, ...).
- Alegi folderul (se iau doar pozele JPG/PNG/CR2/NEF/DNG aflate direct în el, fără subfoldere), scrii un pattern și **vezi imediat, în listă, numele rezultate** pentru fiecare fișier. Nimic nu se schimbă pe disc până nu apeși „Aplică redenumirea” și confirmi.
- Variabile disponibile (se pot insera și cu butoanele de sub câmp):
  - `{name}` — numele original, fără extensie
  - `{counter}` sau `{counter:000}` — număr secvențial (cu zerouri în față după numărul de `0`); „Începe de la” alege primul număr
  - `{date}` sau `{date:yyyyMMdd}` — data fișierului de pe disc (implicit `yyyy-MM-dd`)
  - `{ext}` — extensia originală; extensia se păstrează oricum automat la final
- Numerotarea poate urma **numele fișierelor** sau **data fișierelor**.
- Exemplu: pattern `Vacanta_Grecia_{counter:000}` → `IMG_1234.JPG` devine `Vacanta_Grecia_001.JPG`.
- Conflictele sunt marcate cu roșu și blochează aplicarea: două fișiere cu același nume nou, un fișier existent (din afara lotului) cu acel nume, sau un nume invalid în Windows (caractere `< > : " / \ | ? *`, nume rezervate precum `CON`).
- Dacă folderul face parte din bibliotecă, fereastra te anunță; după redenumire pozele sunt **actualizate automat în index**, deci își păstrează albumele, tag-urile și rotirea (nu mai e nevoie de re-scanare).
- Dacă o redenumire eșuează la jumătate (ex. un fișier blocat de alt program), fișierele deja mutate revin la numele inițiale.

### Slideshow

- Pornește un slideshow din orice folder/album deschis: butonul **Slideshow** de deasupra listei de poze, tasta **F5** sau click dreapta pe o poză → „Pornește slideshow de aici”.
- Rulează cu pozele afișate în acel moment (inclusiv filtrul de căutare și sortarea), începând cu poza selectată; după ultima poză se încheie singur și revii în galerie, cu ultima poză vizionată selectată. Cu butonul **Buclă** (săgețile circulare) din bara de control sau tasta **L** comuți pe redare continuă: după ultima poză reia de la prima, până ieși tu; alegerea se păstrează pentru următoarele slideshow-uri.
- Fiecare poză are un efect lent de zoom (in sau out, aleator) și deplasare (pan, una din 4 diagonale, aleator), cu tranziție fade către poza următoare.
- Poți adăuga una sau mai multe piese MP3 din calculatorul tău — direct din slideshow, cu butonul **Muzică** (nota muzicală) din bara de control sau tasta **M**, ori din **Opțiuni → Slideshow → Muzică**. Piesele alese din slideshow încep să cânte imediat și rămân salvate în playlist; numele piesei curente apare discret deasupra barei de control. Piesele se redau în ordine, în buclă, până la finalul slideshow-ului. Piesele mutate sau șterse de pe disc sunt sărite; pe un calculator fără ieșire audio slideshow-ul rulează fără muzică.
- Bara de control (Pauză/Redă, Anterioara/Următoarea, Buclă, contor poze, Ieșire) și numele pozei/albumului dispar automat după 3 secunde de inactivitate a mouse-ului și reapar imediat la mișcarea mouse-ului — pentru o vizionare curată, fără elemente pe ecran. Pauza oprește și mișcarea pozei, și muzica.
- Parametrii (durată afișare 3–15 s, durată fade 0,5–3 s, intensitate pan 5–25%, intensitate zoom 1,05–1,30×, volum) se configurează din **Opțiuni** și se salvează imediat.

### Temă, culoare accent și limbă

- Butonul **Temă** din bara secundară comută instant între aspect Dark și Light.
- Din **Opțiuni** → secțiunea **General** alegi tema, culoarea de accent și limba interfeței.
- **Culoarea de accent** — 6 buline (portocaliu, albastru, verde, roșu, mov, turcoaz); se aplică imediat în toată aplicația (butoane, selecție, slidere, bara slideshow-ului), adaptată temei Dark / Light.
- **Limba** — Română sau English; se aplică după repornire. Aplicația te întreabă dacă vrei să repornească imediat („Repornește acum" / „Mai târziu").

### Favorite

- Pune mouse-ul pe o poză și apasă **inimioara** din colțul din dreapta jos al miniaturii (sau selectează una ori mai multe poze și apasă **F**, ori click dreapta → **Adaugă la favorite**). Pozele favorite au inimioara plină, mereu vizibilă.
- Butonul **Favorite** de deasupra grid-ului arată doar pozele favorite din contextul curent (folder, album, tag, rezultatul căutării); apasă-l din nou pentru toate pozele.
- Pe tot ecranul: tasta **F** sau inimioara din bara de jos.
- Favoritele sunt un simplu marcaj, separat de tag-uri — nu trebuie create sau configurate.

### Dimensiunea miniaturilor

- Glisorul de lângă lista de sortare mărește sau micșorează miniaturile din grid (de la foarte mici, pentru o privire de ansamblu, la mari, pentru detalii); la fel **Ctrl + rotița mouse-ului** peste grid.
- Dimensiunea aleasă se păstrează la următoarea pornire. Miniaturile de pe disc nu se regenerează.

### Copii de siguranță ale bibliotecii

- Înainte de orice operațiune care nu se poate anula — ștergerea unui album, a unui tag sau a unui folder sursă, eliminarea pozelor lipsă la re-scanare, redenumirea batch a unui folder din bibliotecă, schimbarea locației unui folder, actualizarea aplicației la o versiune nouă — PhotoVault salvează automat o copie a bazei de date în `data/`, cu numele `photovault.db.bak.<data-ora>`. Se păstrează ultimele **5** copii; cele mai vechi se șterg singure.
- **Opțiuni → Copii de siguranță** arată data ultimei copii și permite o copie manuală („Creează o copie acum") sau deschiderea folderului.
- **Restaurare** (dacă ceva n-a ieșit cum voiai): închide aplicația, în `data/` redenumește `photovault.db` (ex. în `photovault.db.vechi`), apoi redenumește copia dorită `photovault.db.bak.<data-ora>` în `photovault.db`; dacă există, șterge și `photovault.db-wal` / `photovault.db-shm`. La repornire, biblioteca e cea din copie.

### Rotire poze

- Poți roti o poză din 90 în 90 de grade (tastă `R` sau click dreapta → **Rotește 90°**; funcționează și pentru mai multe poze selectate, și în vizualizarea pe tot ecranul).
- Rotirea este **logică** — se reține doar în baza de date a aplicației; fișierul original de pe disc rămâne exact așa cum era. Dacă deschizi fișierul cu alt program, va apărea nerotit.

### Shortcut-uri de tastatură

| Tastă | Acțiune |
|---|---|
Aceeași listă apare și în aplicație, în modalul **Informații** (iconița „i" din bara de sus).

| Tastă | Acțiune |
|---|---|
| `Enter` / dublu-click | Deschide poza selectată pe tot ecranul |
| `Esc` | Închide vizualizarea pe tot ecranul / slideshow-ul / modalul logo / modalul Informații; în câmpul de căutare îl golește |
| `←` / `→` | Poza anterioară / următoare (pe tot ecranul, slideshow) |
| `Home` / `End` | Prima / ultima poză (pe tot ecranul) |
| `+` / `−` / rotița mouse-ului | Mărește / micșorează (pe tot ecranul) |
| `0` | Potrivire în ecran |
| `1` | Dimensiune reală (1:1) |
| `R` | Rotește poza cu 90° (în grid — și pentru mai multe poze selectate — sau pe tot ecranul) |
| `F` | Adaugă / elimină poza din favorite (în grid — și pentru mai multe poze selectate — sau pe tot ecranul) |
| `Ctrl` + rotița mouse-ului | Miniaturi mai mari / mai mici (grid) |
| `Ctrl+A` | Selectează toate pozele din grid |
| `Ctrl` / `Shift` + click | Selecție multiplă în grid |
| `Ctrl+F` | Focus pe câmpul de căutare |
| `F5` | Pornește slideshow-ul cu pozele afișate |
| `Space` | Pauză / redare (slideshow) |
| `M` | Adaugă muzică MP3 (slideshow) |
| `L` | Redare în buclă / o singură dată (slideshow) |

---

## Structura folderului aplicației

```
PhotoVault/
├── PhotoVault.exe        ← executabilul aplicației, rulează acest fișier
├── data/
│   ├── photovault.db     ← baza de date: albume, tag-uri, index poze, setări
│   └── thumbnails/       ← cache cu miniaturi generate automat
├── logs/                 ← apare doar dacă a apărut o eroare (detalii tehnice, utile la depanare)
└── README.txt            ← instrucțiuni scurte (pornire, mutare pe alt calculator)
```

- `data/thumbnails/` e împărțit în subfoldere (`ab/`, `cd/`...) ca Windows să rămână rapid și la zeci de mii de miniaturi. Dacă ștergi doar acest folder, miniaturile se regenerează automat la pornire (durează), fără pierderi de organizare.
- `data/photovault.db` conține **tot**: folderele sursă, albumele, tag-urile, favoritele, rotirile și setările. Pentru o copie de siguranță completă (inclusiv miniaturile), închide aplicația și copiază folderul `data/`.
- `data/photovault.db.bak.<data-ora>` — copiile de siguranță automate ale bazei de date (ultimele 5; vezi „Copii de siguranță ale bibliotecii").

**Nu șterge și nu edita manual** fișierele din `data/` — acolo e stocată toată organizarea ta (albume, tag-uri, rotații, setări). Ștergerea lor înseamnă pierderea acestei organizări (pozele originale de pe disc rămân neafectate, dar trebuie reindexate de la zero).

---

## Limitări cunoscute

- Formate suportate: **JPEG, PNG, CR2, NEF, DNG**. Nu este suportat HEIC și nici alte formate RAW în afara celor trei enumerate.
- Nu există funcții de editare a imaginii (culoare, expunere, crop) — singura modificare posibilă este rotirea logică din 90 în 90 de grade.
- Nu se verifică duplicate la import — aceeași poză, adăugată din două foldere diferite, va apărea de două ori.
- Sortarea și căutarea sunt intenționat simple (după nume fișier; căutare după nume/tag/album) — nu există filtre combinate avansate sau sortare după dată EXIF.
- Previzualizarea fișierelor RAW folosește imaginea JPEG încorporată de cameră (nu se decodează datele RAW): calitatea și dimensiunea ei depind de modelul camerei; un fișier RAW fără previzualizare JPEG încorporată apare cu iconița de rezervă.
- Schimbarea limbii interfeței se aplică după repornirea aplicației (aplicația oferă repornirea imediată).
- Re-scanarea folderelor este manuală (butonul Re-scanează / Re-scanează toate) — aplicația nu urmărește în timp real modificările de pe disc.
- Executabilul unic conține și câteva componente native (Windows / SQLite) pe care .NET le despachetează la prima pornire în folderul temporar al utilizatorului (`%TEMP%\.net\PhotoVault\`, ~20 MB). Datele tale rămân exclusiv în folderul aplicației; folderul temporar poate fi șters oricând (se recreează la pornire).
- Executabilul nu este semnat digital, deci SmartScreen poate cere confirmare la prima rulare pe un calculator nou.

---

## Portabilitate — mutarea aplicației pe alt calculator sau stick USB

1. Copiază folderul `PhotoVault` complet (inclusiv `data/`) la noua locație.
2. Rulează `PhotoVault.exe` de acolo — nu necesită nimic instalat pe noul calculator.
3. **Atenție la folderele sursă**: aplicația reține căile complete către folderele cu poze (ex. `D:\Poze\Vacanta2024`). Dacă un folder nu e disponibil la exact aceeași cale pe noul calculator (altă literă de disc, alt drive extern), în Bibliotecă apare o **iconiță de avertizare** în locul iconiței de folder, iar în Opțiuni un mesaj sub folderul respectiv. Pozele lui **rămân** în bibliotecă (cu albume, tag-uri, rotiri) — nimic nu se șterge automat.
   - Dacă doar ai conectat discul extern mai târziu, repornește aplicația sau re-scanează folderul.
   - Dacă folderul are acum altă cale: **click dreapta pe folder → Schimbă locația...** (sau butonul cu aceeași funcție din **Opțiuni → Foldere sursă**), alege noua locație, iar aplicația îți arată câte poze a regăsit acolo (ex. „30 din 30 de poze"). Confirmă cu **Actualizează locația** — căile sunt actualizate, iar albumele, tag-urile, rotirile și miniaturile se păstrează.
   - Dacă nicio poză nu se regăsește la locația aleasă, aplicația te avertizează înainte (probabil ai ales alt folder).
4. Dacă intenția e ca și pozele originale să fie portabile (nu doar aplicația), acestea trebuie copiate separat, păstrând ideal aceeași structură relativă de foldere.

---

## Pentru dezvoltator (context tehnic)

Acest README este destinat utilizatorului final al aplicației. Pentru detalii de arhitectură, schema bazei de date, stack tehnologic și decizii de design, consultă documentul tehnic complet: `PhotoVault_Plan.md` (secțiunea §15 conține deciziile luate pe parcursul implementării).

- **Stack:** C# / WPF, .NET 10, MVVM (CommunityToolkit.Mvvm), SQLite (Microsoft.Data.Sqlite + Dapper), MetadataExtractor, SixLabors.ImageSharp, NAudio.
- **Soluția:** `PhotoVault.App` (WPF), `PhotoVault.Core` (modele, servicii, logică), `PhotoVault.Data` (SQLite, migrări în `Migrations/NNN_*.sql`), `PhotoVault.Tests` (xUnit).
- **Build și teste:** `dotnet build PhotoVault.sln` · `dotnet test PhotoVault.Tests --filter "Category!=RealRaw"`.
- **Versiunea portabilă — cel mai simplu:** dublu-click pe **`build-app.bat`** (Windows, cu .NET 10 SDK instalat): compilează soluția, rulează testele unitare și creează `publish\PhotoVault\` (`PhotoVault.exe` + `README.txt`) plus arhiva `publish\PhotoVault-<versiune>-win-x64.zip`. `build-app.bat notest` sare peste teste. Folderul `publish\PhotoVault` e distribuția finală: se copiază ca atare pe alt calculator / stick USB.
- **Alternativ:** `.\publish.ps1` (PowerShell) → `publish\PhotoVault\` (`PhotoVault.exe` self-contained, single-file + `README.txt`) și arhiva `publish\PhotoVault-<versiune>-win-x64.zip`. Echivalent: `dotnet publish PhotoVault.App -p:PublishProfile=Portabil`. Trimming-ul nu e folosit (nu e suportat de WPF).
- **Verificare automată pe Windows** (`.github/workflows/windows-build.yml`, la fiecare push): build, teste unitare, test pe fișiere RAW reale descărcate din surse publice, smoke test UI cu capturi de ecran (toate fazele), publicare portabilă, test de portabilitate (pornire fără .NET de pe un „stick", mutare pe „alt calculator" + realiniere foldere sursă). La cerere (sau cu „[perf]" în mesajul commit-ului): test de performanță pe 50.000 de poze.

---

**Credit:** concept și realizare vlad39
