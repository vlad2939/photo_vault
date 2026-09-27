-- Versiunile anterioare nu puteau citi previzualizarea din unele fișiere RAW (CR2/DNG)
-- și le marcau ca ilizibile (ThumbnailPath = ''). Se reîncearcă o dată, cu extractorul corectat.
UPDATE Photos SET ThumbnailPath = NULL WHERE ThumbnailPath = '';
