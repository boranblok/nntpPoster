#
# Place setting files in this folder and they will be loaded in alphabetical order.
# A later file will overrule any previous settings it contains.
#
# copy this file and change extension to .ini to have a basic config file to start with.


[NewsHost]
# The address of your newshost you want to upload to
Address=Address of news host

# The port of your newshost you want to upload to usually 80 or 119 for non ssl and 443 or 563 for SSL
Port=443

# Credentials to use for uploading
Username=username
Password=password

# Use SSL or not. Take into account that forcing SLL on non SSL connections wont work, and vice versa
UseSSL=yes

# How many connections to open simultaneously. Your newshost. More is not always better. 
# Experiment with this to find the lowest value that saturates your connection for best performance.
MaxConnectionCount=10


[Indexer]
# enter your API key that is used for the indexer here. Then you can use ${ApiKey} in the urls below
ApiKey=APIKEY

# Method used to notify the index of the new upload.
# Supported values are 'NewznabHash' which uses the newznab Method (default)
# or 'NzbPost' which sends the nzb file to the notification url.
NotificationType=NewznabHash

# API url that is used to notify the indexer of an obfuscated release.
# The following two parameters will be replaced:
#   {0} by the obfuscated name of the file/folder
#   {1} by the original name of the file/folder
#   optionally ${ApiKey} will be replaced by the value entered above
ObfuscatedNotificationUrl=https://api.apiserver.com/api?hash={0}&name={1}&apikey=${ApiKey}

# Method used to verify that the upload was placed on the index.
# Supported values are 'NewznabSearch' which uses the newznab method (default)
# 'PostVerify' which uses a custom post verification method.
# or 'Dummy' which always considers an upload as successful and therefore cleans up the backup folder after VerifierIntervalMinutes
VerificationType=NewznabSearch

# API url that is used to search the indexer.
# The following two parameters will be replaced:
#   {0} by the search query
#   {1} by the age + 1 of the post in days
#   optionally ${ApiKey} will be replaced by the value entered above
SearchUrl=https://api.apiserver.com/api?t=search&q={0}&maxage={1}&apikey=${ApiKey}

[Folders]
# Folder used to prepare the files for posting. Any data in this folder is removed at startup!
Working=working

# What folder to use as queue for files that have not been uploaded yet.
Queue=queue

# Folder where to output NZB's leave empty to skip writing NZB's to the filesystem
NzbOutput=

# What folder to use as backup for the files that have not been verified yet.
Backup=backup

# The folder to put failed uploads in
UploadFailed=uploadfailed


[Posting]
# The maximum number of attempts a file will be reposted if it has an error. After this it goes to the postFailed folder
MaxRepostCount=3

# This list contains the rar and par2 settings depending on filesize add more entries for finer control
# Each entry contains the following 3 fields: "From size in megabytes,Rar size in megabyte,Par percentage" Separate each series by a |
# so "0,15,10" would mean: for files from 0 megabyes use a 15 megabyte par size and 10 percent par2.
RarNParSettings=0,15,10|1024,50,10|5120,100,5


[External Programs]
# How many minutes to wait on new output from an external process. 
# If you are on a slow system (Atom for instance) you might want to increase this value.
InactiveProcessTimeoutMinutes=5

# Where to find rar on the system, leave empty if rar is accesible trough the path
RarLocation=

# Extra parameters to add to the end of the rar command, careful with this as it can break stuff.
RarExtraParameters=

# Where to find par2 on the system, leave empty if par2 is accesible trough the path
ParLocation=

# Extra parameters to add to the end of the par command, careful with this as it can break stuff.
ParExtraParameters=
