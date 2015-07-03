#requires -version 3
#this script can only run on Poweshell Version 3 or higher.

$version = "0.01"
$baseDir = $PSScriptRoot
$outputFolder = Join-Path $baseDir "Build"
$msbuild = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe"
$options = "/p:Configuration=Package"
$releaseFolder = $baseDir + "Releases"

function ZipFiles( $zipfilename, $sourcedir )
{

   Add-Type -Path "C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\System.IO.Compression.FileSystem.dll"
   $compressionLevel = [System.IO.Compression.CompressionLevel]::Optimal
   [System.IO.Compression.ZipFile]::CreateFromDirectory($sourcedir,
        $zipfilename, $compressionLevel, $false)
}

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

# zip all the files that were built into a release.
echo "Output folder: " + $outputFolder
#ZipFiles($version + ".zip", $outputFolder)