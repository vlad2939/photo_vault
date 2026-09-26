-- PhotoVault — schema inițială (vezi PhotoVault_Plan.md §4)

-- Foldere sursă indexate (adăugate manual de utilizator)
CREATE TABLE SourceFolders (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    FolderPath TEXT NOT NULL UNIQUE,
    DateAdded TEXT NOT NULL,            -- ISO 8601
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
    DateTakenExif TEXT,                 -- din EXIF, dacă disponibil
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
    Name TEXT NOT NULL,
    DateCreated TEXT NOT NULL,
    CoverPhotoId INTEGER,               -- poză copertă, opțional
    FOREIGN KEY (CoverPhotoId) REFERENCES Photos(Id) ON DELETE SET NULL
);

-- Relație many-to-many: poze în albume
CREATE TABLE AlbumPhotos (
    AlbumId INTEGER NOT NULL,
    PhotoId INTEGER NOT NULL,
    DateAdded TEXT NOT NULL,
    SortOrder INTEGER NOT NULL DEFAULT 0,
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

-- Setări aplicație (cheie-valoare)
CREATE TABLE AppSettings (
    Key TEXT PRIMARY KEY,
    Value TEXT
);
