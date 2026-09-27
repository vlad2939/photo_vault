-- Indecși pentru colecții mari (profiling pe 50.000 de poze, Faza 7):
--  • AlbumPhotos / PhotoTags căutate după PhotoId (cuvinte-cheie de căutare, ștergere în cascadă a pozelor)
--  • PhotoTags după TagId (filtrare după tag, ștergere tag)
--  • Albums.CoverPhotoId (ON DELETE SET NULL la ștergerea pozelor)
CREATE INDEX IF NOT EXISTS idx_albumphotos_photo ON AlbumPhotos(PhotoId);
CREATE INDEX IF NOT EXISTS idx_phototags_tag ON PhotoTags(TagId);
CREATE INDEX IF NOT EXISTS idx_albums_cover ON Albums(CoverPhotoId);
