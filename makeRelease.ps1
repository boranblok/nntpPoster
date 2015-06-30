$version = "0.01"
$baseDir = $MyInvocation.MyCommand.Path
$outputFolder = $baseDir + "Build"
$msbuild = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe"
$options = "/noconsolelogger /p:Configuration=Package"
$releaseFolder = $baseDir + "Releases"

# if the output folder exists, delete it
if ([System.IO.Directory]::Exists($outputFolder))
{
 [System.IO.Directory]::Delete($outputFolder, 1)
}

# make sure our working directory is correct
cd $baseDir

# create the build command and invoke it 
# note that if you want to debug, remove the "/noconsolelogger" 
# from the $options string
$clean = $msbuild + " ""nntpPoster.sln"" " + $options + " /t:Clean"
$build = $msbuild + " ""nntpPoster.sln"" " + $options + " /t:Build"
Invoke-Expression $clean
Invoke-Expression $build

# move all the files that were built to the output folder
[System.IO.Directory]::Move($releaseFolder, $outputFolder)