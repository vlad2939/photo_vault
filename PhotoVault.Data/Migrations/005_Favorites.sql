-- Flag „Favorite" pe poze (§12.3): steag simplu, distinct de tag-uri, pentru acces rapid
ALTER TABLE Photos ADD COLUMN IsFavorite INTEGER NOT NULL DEFAULT 0;
