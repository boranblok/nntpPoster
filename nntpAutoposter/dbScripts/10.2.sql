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
	IsRepost,
	CurrentLocation)

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
	IsRepost,
	Cancelled + 3
FROM UploadEntries
