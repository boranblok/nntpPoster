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
    WatchFolderShortName)

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
    WatchFolderShortName
FROM UploadEntries
