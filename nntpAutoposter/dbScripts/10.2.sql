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
	UploadAttempts,
	RarPassword,
	PriorityNum,
	NzbContents,
	IsRepost)

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
	UploadAttempts,
	RarPassword,
	PriorityNum,
	NzbContents,
	IsRepost
FROM UploadEntries
