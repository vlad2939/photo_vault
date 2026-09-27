# PhotoVault

Aplicație portabilă pentru organizarea și vizualizarea albumelor foto personale.

**Stadiu dezvoltare:** Faza 4 finalizată (sortare, căutare, rotire) · **Ultima actualizare:** septembrie 2026

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
- Apasă butonul **+** din dreptul secțiunii **Bibliotecă** (panoul din stânga) și alege un folder de pe disc. Lista folderelor sursă va fi disponibilă și în fereastra **Opțiuni**.
- Aplicația scanează recursiv acel folder (inclusiv subfoldere) și indexează toate pozele găsite (formate suportate: JPEG, PNG, CR2, NEF, DNG). Progresul apare în stânga jos, în footer; poți continua să folosești aplicația între timp.
- Miniaturile se generează în fundal și apar pe rând în grid. Pentru fișierele RAW se folosește previzualizarea JPEG încorporată de cameră (rapid, fără decodare RAW completă).
- Nu poți adăuga un folder care e deja inclus (sau un subfolder al unui folder deja adăugat) — aplicația te anunță.
- Folderele adăugate rămân în listă permanent — data viitoare când pornești aplicația, nu trebuie să le re-adaugi.
- Dacă adaugi poze noi într-un folder deja indexat, fă **click dreapta pe folder → Re-scanează** ca aplicația să le detecteze. Pozele care între timp au fost șterse sau mutate de pe disc sunt eliminate automat din index.
- Dacă folderul nu e accesibil la re-scanare (ex. disc extern deconectat), indexul **nu** este golit — reconectează discul și re-scanează.
- **Click dreapta pe folder → Elimină din bibliotecă** scoate folderul și pozele lui din index (fișierele de pe disc nu sunt atinse).
- Aplicația **nu verifică duplicate** — dacă aceeași poză există fizic în două foldere diferite adăugate, va apărea de două ori.

### Navigare și vizualizare

- Panoul din stânga arată structura de foldere (ca în Windows Explorer), cu subdirectoare, plus lista de Albume și lista de Tag-uri. Subfolderele se încarcă atunci când deschizi un nivel (săgeata din stânga folderului).
- Selectând un folder, grid-ul central afișează pozele din acel folder **și din toate subfolderele lui**. Click pe titlul **Bibliotecă** afișează din nou toate pozele.
- Click pe o poză afișează în panoul din dreapta detaliile ei: cale completă, dimensiune, extensie și — dacă există în fișier — dimensiunile în pixeli, data fotografierii, camera, obiectivul, ISO, timpul de expunere, diafragma și distanța focală.
- **Dublu-click** pe o poză (sau **Enter**) o deschide pe tot ecranul: ← / → pentru navigare, rotița mouse-ului sau + / − pentru zoom, tragere cu mouse-ul pentru deplasare când imaginea e mărită, 0 = potrivire în ecran, 1 = dimensiune reală, Esc = închidere.
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

- Buton dedicat în bara secundară (lângă Info/Opțiuni/Temă).
- Util **înainte** de a importa un folder în PhotoVault — de exemplu, ca să redenumești o serie de poze descărcate de pe cameră (nume gen `IMG_1234.jpg`) într-un format mai clar (`Vacanta_Grecia_001.jpg`, `Vacanta_Grecia_002.jpg`, ...).
- Alegi folderul, definești un pattern de nume (cu variabile: nume original, numărător secvențial, dată), și **vezi un preview** al numelor rezultate înainte să confirmi.
- Operațiunea redenumește efectiv fișierele pe disc — dacă folderul respectiv e deja indexat în PhotoVault, re-scanează-l după redenumire.

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

- Poți roti o poză din 90 în 90 de grade (tastă `R` sau click dreapta → **Rotește 90°**; funcționează și pentru mai multe poze selectate, și în vizualizarea pe tot ecranul).
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
