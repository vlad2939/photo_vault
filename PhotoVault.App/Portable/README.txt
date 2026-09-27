PhotoVault - aplicatie portabila de organizare a fotografiilor
==============================================================

PORNIRE
  1. Dezarhiveaza folderul PhotoVault oriunde (Desktop, alt disc, stick USB).
  2. Porneste PhotoVault.exe (dublu-click). Nu necesita instalare si nici .NET.
  3. La prima pornire se creeaza automat folderul data\ (baza de date + miniaturi).

  Daca Windows afiseaza "Windows protected your PC" (SmartScreen):
  apasa "More info" -> "Run anyway" (executabilul nu este semnat digital).

CE CONTINE FOLDERUL
  PhotoVault.exe         aplicatia (singurul fisier necesar)
  data\photovault.db     biblioteca: foldere sursa, albume, tag-uri, rotiri, setari
  data\thumbnails\       miniaturile generate (se pot regenera, dar dureaza)
  logs\                  apare doar daca a aparut o eroare (detalii tehnice)
  Nu sterge si nu modifica manual folderul data\ - acolo e toata organizarea ta.
  Pozele tale NU sunt copiate si NU sunt modificate: raman unde erau.

MUTARE PE ALT CALCULATOR / STICK USB
  - Inchide aplicatia si copiaza TOT folderul PhotoVault (inclusiv data\).
  - Daca folderele cu poze au alta cale pe noul calculator (alta litera de disc,
    alt drive extern), in Biblioteca apare un semn de avertizare langa folder:
    click dreapta pe folder -> "Schimba locatia..." si alege noua cale.
    Albumele, tag-urile si rotirile se pastreaza.

Documentatia completa: README.md din proiectul sursa.

(c) concept si realizare vlad39
