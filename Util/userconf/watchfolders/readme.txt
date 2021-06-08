#
# Place watchfolder files in this folder. Each file will define a new watch folder.
# Settings from the default watchfolder will be taken as base.
# If a file named default is placed in this folder it will overrule the default watch folder.

# copy this file and change extension to .ini to have a basic config file to start with.


[Watchfolder]
# The short identifying name for this watch folder. 
# This will be used as a subfolder in backup and working folders,
# so keep it short and legal for use as a folder name
ShortName=default

# What folder to watch for new files. Any files in this folder at startup will be added to the queue
Path=watch

# Wether or not to obfuscate the filename.
# If set to true the uploaded filename will be a GUID instead of the original filename.
# When set the ObfuscatedNotificationUrl parameter has to be filled in.
UseObfuscation=no

# wether to clean the name when uploading, this ensures a better name for usenet. 
# But might interfere with anime names.
CleanName=yes

# Uncomment and modify to change what characters to strip from the uploaded name, default: "()=@#$%^,?<>{}|"
# This is only relevant if CleanName=yes
# CharsToRemove="()=@#$%^,?<>{}|"

# A prefix to add to every release name, leave blank to omit
PreTag=

# A suffix to add to every release name, leave blank to omit
PostTag=

# What newsgroups to upload to.
# When entering more than one add a pipe character between
# Like a.b.multimedia|a.b.test|a.b.videos
TargetNewsgroups=alt.binaries.multimedia

# The newsgroup address that is added in the from header.
# This has no functional impact, but might be handy for debugging purposes.
# the fixed value RANDOM causes a random from address to be used for each file.
FromAddress=RANDOM

# Wether to apply a random password to the uploaded archive
ApplyRandomPassword=no

# Apply this fixed rar password to the uploaded archive
# If ApplyRandomPassword is set to true this takes precedence and this setting is ignored.
RarPassword=

# Apply this priority to uploads in this folder. Higher numbers have priority over lower numbers,
# even if they are added to the queue at a later time.
# This can be negative as well (a valid Int32 value)
Priority=0

# Use a random usenet message subject instead of the filename
# this extra obfuscation method makes it harder to reconstruct a message from usenet posts
# but might introduce issues with less advanced downloaders that check the message headers instead of the yenc headers.
UseRandomMessageSubjects=no