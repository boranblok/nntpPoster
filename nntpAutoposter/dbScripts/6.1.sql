CREATE TABLE
    UploadEntries_tmp(
    Name TEXT NOT NULL, 
    Size INTEGER NOT NULL,
    CleanedName TEXT, 
    ObscuredName TEXT, 
    RemoveAfterVerify INTEGER,
    CreatedAt TEXT NOT NULL,
    UploadedAt TEXT,
    NotifiedIndexerAt TEXT,
    SeenOnIndexerAt TEXT,
    Cancelled INTEGER,
    WatchFolderShortName TEXT NOT NULL,
	UploadAttempts INTEGER DEFAULT 0,
	RarPassword TEXT)