# nntpPoster
A utility to bulk upload files and folders to usenet.

## short overview
This utility allows you to do mass uploads of files and/or folders.

Optionally the uploads can be obfuscated so only a select indexer has knowledge of the exact file that was uploaded.

## Prerequisites

In order to run this utility you need the following on your system:

* .Net 4.5+ or the latest version of mono
* The [rar commandline utility](http://www.rarlab.com/download.htm)
* The [par2 commandline utility](http://sourceforge.net/projects/parchive/files/par2cmdline/0.4/)

On non windows systems you will also need [sqlite3](https://www.sqlite.org/) installed.

Optionally if also stripping file metadata:
* Mkvpropedit (part of [Mkvtoolnix](https://www.bunkus.org/videotools/mkvtoolnix/))
* [ffmpeg](https://www.ffmpeg.org/)

## Installation

To install extract the Release zipfile to a folder.

The folder needs to be readable and writable by the user under which you execute the service.

Modify the `nntpAutoPosterWindowsService.exe.config` file to match your upload parameters.

Optionally modify the folder settings as well if you want the application to use a different location to watch/process etc.  
The folders are written in the native OS format so /mnt/folder etc on linux derivates and C:\folder on windows systems.  
By default the configured folder paths are relative to the application and are all subfolders of the application.

## Running the service

#### On Linux

If you want to run the service in a screen session you can use `start.sh` this script will also ensure the service runs in the correct environment (as it switched the current working folder)

Additionally an init.d shell script is included. (`init.d.sh`) you will need to modify the application path and executing user in this file before you can use it.  
This has been tested on Debian 7 If you have any issues with this on other systems or have a working autostart script for another linux derivate, please let me know I will include it in the package.

#### On Windows

On windows you can start the `nntpAutoposter.exe` as a console application. But it is more likely you will want to run it as a service.

To install the service run the `InstallService.cmd` file. This will install the service as a windows service.

To uninstall the service run `UninstallService.cmd`

If you ever want to move the application to another location please run uninstall first before moving, and run install afterwards. No settings are lost by this.

## Logging

For logging log4net is used. All logging is configured with the `log4netConfig.xml` file. This is monitored in real time so a service restart is not required if you modify logging settings.

The log files are the main source of information to monitor the application. When something does not work as expected, please check these first.

During the development phase I recomend letting the log levels as they are.
