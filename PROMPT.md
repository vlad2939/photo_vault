# Prompt de inițiere proiect — Claude Code

> Instrucțiuni de folosire: copiază tot conținutul de mai jos ca prim mesaj către Claude Code, în folderul gol unde vrei să creezi soluția. Asigură-te că `PhotoVault_Plan.md` și cele 5 imagini (4 mockup-uri + logo) sunt deja copiate în acel folder înainte de a trimite promptul.

---

Vreau să construiești o aplicație desktop numită **PhotoVault** — Windows 11, C#/WPF, .NET 10, portabilă (self-contained, un singur folder, fără instalare).

## Documentul de referință

Am atașat `PhotoVault_Plan.md` — acesta este **documentul tehnic complet și obligatoriu de referință** pentru întregul proiect: arhitectură, schema bazei de date SQLite, structura de foldere, stack tehnologic, pachete NuGet, toate modulele funcționale (import, albume, tag-uri, redenumire batch, slideshow Ken Burns, teme Dark/Light), cerințele de UI/UX, roadmap-ul pe 9 faze, cerințele de livrare, și conținutul README.md.

**Citește acest document integral înainte de a scrie orice cod.** Este rezultatul mai multor runde de clarificare directă cu mine — toate deciziile din el sunt ferme, nu presupuneri. Dacă găsești o ambiguitate reală în document (nu doar un detaliu minor de implementare pe care poți să-l decizi rezonabil singur), întreabă-mă înainte să presupui.

## Imagini de referință vizuală (mockup-uri)

Am atașat 4 mockup-uri (`mockup_UI_main_dark.png`, `mockup_UI_main_light.png`, `mockup_slideshow_dark.png`, `mockup_viewer_dark.png`) și un logo (`PhotoVault_logo.png`).

**Important — statutul acestor imagini:**
- Mockup-urile sunt **orientative, nu specificații exacte**. Arată intenția generală de layout, densitate vizuală, paletă de culori și ton al design-ului (modern, colțuri rotunjite, accent portocaliu, carduri cu umbră discretă) — dar pot conține **inconsistențe minore de generare** (texte cu typo, numere care nu se potrivesc perfect între variante dark/light, elemente care nu corespund 100% cu `PhotoVault_Plan.md`). Când mockup-ul și documentul `.md` intră în conflict pe o decizie funcțională fermă (ex. număr de butoane în bara secundară, comportament exact), **documentul `.md` are întotdeauna prioritate**. Mockup-ul câștigă doar pe aspecte pur vizuale/estetice care nu sunt descrise explicit în document (ex. exact ce nuanță de gri pentru fundal, exact cât padding între carduri).
- **Logo-ul (`PhotoVault_logo.png`) este definitiv și real** — nu e placeholder. Trebuie folosit efectiv în aplicație: în bara secundară (§5.3), ca imagine afișată la click-pe-logo (§5.4), și convertit/exportat ca `icon.ico` (iconița executabilului Windows) și ca favicon, dacă la un moment dat aplicația expune vreo interfață web-based (probabil nu e cazul, dar păstrează logo-ul ca sursă unică pentru orice iconiță necesară în proiect).

Folosește mockup-urile ca ghid vizual atunci când construiești XAML-ul (culori, spațiere, aspect carduri, aspect bară slideshow), dar nu le trata ca pe un contract pixel-perfect — construiește UI-ul curat, coerent cu tema Dark/Light descrisă în §5 din document, inspirându-te din mockup-uri pentru "senzația" generală.

**Cerință suplimentară pentru fazele cu accent vizual puternic** (Faza 0 — sistem de teme, Faza 2 — lightbox, Faza 3 — carduri albume, Faza 6 — slideshow): după ce generezi XAML-ul, fă tu însuți o trecere explicită de auto-comparație cu mockup-ul corespunzător înainte să declari faza gata — verifică punct cu punct: paleta de culori se potrivește, spațierea/padding-ul e generos ca în mockup, colțurile sunt rotunjite consistent, elementele de accent (portocaliu) sunt folosite coerent. Nu te grăbi pe aceste faze de dragul vitezei — corectitudinea vizuală contează aici mai mult decât timpul de generare; dacă ai control asupra nivelului de efort/gândire pentru acest task, folosește un nivel mai ridicat pe fazele vizuale față de fazele pur de logică/backend (ex. servicii, repository, schema DB).

## Cum vreau să lucrezi

1. **Fazele din §9 ale documentului** sunt planul de lucru — respectă ordinea lor. Nu sări la Faza 3 înainte ca Faza 0-2 să fie complete și funcționale.
2. **La finalul fiecărei faze**, rulează `dotnet build` pe întreaga soluție și confirmă-mi explicit că build-ul trece fără erori (conform §10.1 din document) înainte să trecem la faza următoare. Dacă apar erori, repară-le tu însuți înainte să-mi raportezi finalizarea fazei.
3. Dacă o fază e mare, poți sparge munca în sub-pași și să-mi arăți progres intermediar, dar nu declara o fază "gata" până nu compilează curat. Ai capacitate bună de a lucra coerent pe baze de cod mari și multi-fișier — nu e nevoie să fragmentezi artificial o fază doar pentru că atinge mai multe straturi (App/Core/Data) simultan, atâta timp cât rămâi în interiorul unei singure faze din roadmap.
4. Respectă strict structura de foldere din §3.2 și §3.3 ale documentului (proiecte `PhotoVault.App`, `PhotoVault.Core`, `PhotoVault.Data`) — nu improviza o altă organizare.
5. Toate elementele de UI trebuie restilizate conform §5.1 din document — nicio componentă (ScrollBar, ComboBox, ContextMenu, mesaje de sistem) nu trebuie să rămână cu aspectul WPF/Windows implicit. Nu folosi `MessageBox.Show()` nativ.
6. Codul trebuie să fie curat, comentat rezonabil (nu excesiv), și organizat pe straturi (MVVM) conform §3.1.
7. La finalul Fazei 0, arată-mi o captură de ecran sau descriere a ferestrei goale + bara secundară + footer, ca să confirm direcția înainte de a continua pe fazele următoare.

## Începe cu Faza 0

Creează soluția .sln cu structura de proiecte descrisă în §3.3, schema SQLite din §4, sistemul de teme Dark/Light complet restilizat (§5.1, §6.11), sincronizarea title bar-ului cu tema (§5.1 — `DwmSetWindowAttribute`), bara secundară sub title bar cu logo-ul real + cele 4 butoane (Redenumire batch / Info / Opțiuni / Temă — §5.3), și footer-ul cu bara de progres (stânga, goală momentan) + textul de credit (dreapta, §5.5).

Confirmă-mi când Faza 0 e completă și compilează curat, apoi trecem la Faza 1.
