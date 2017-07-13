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

See the wiki for instructions for [Windows](https://github.com/boranblok/nntpPoster/wiki/Installation-on-Windows) and [Linux](https://github.com/boranblok/nntpPoster/wiki/Installation-on-Linux)

## Logging

For logging log4net is used. All logging is configured with the `log4netConfig.xml` file. This is monitored in real time so a service restart is not required if you modify logging settings.

The log files are the main source of information to monitor the application. When something does not work as expected, please check these first.

During the development phase I recomend letting the log levels as they are.
