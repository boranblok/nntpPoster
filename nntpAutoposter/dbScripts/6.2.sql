INSERT INTO
    UploadEntries_tmp(
    Name, 
    Size,
    CleanedName, 
    ObscuredName, 
    RemoveAfterVerify,
    CreatedAt,
    UploadedAt,
    NotifiedIndexerAt,
    SeenOnIndexerAt,
    Cancelled,
    WatchFolderShortName,
	UploadAttempts)

SELECT
	Name, 
    Size,
    CleanedName, 
    ObscuredName, 
    RemoveAfterVerify,
    CreatedAt,
    UploadedAt,
    NotifiedIndexerAt,
    SeenOnIndexerAt,
    Cancelled,
    WatchFolderShortName,
	UploadAttempts
FROM UploadEntries
