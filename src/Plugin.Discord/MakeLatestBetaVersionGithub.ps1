Param
(
    [Parameter()]
    [string]$rootPath = "D:\GitHub\ServerManagers\src\Plugin.Discord",

    [Parameter()]
    [string]$binDir = "bin\Release\net462",

    [Parameter()]
    [string]$publishDir = "Publish",

    [Parameter()]
    [string]$srcFilename = "ServerManager.Plugin.Discord.dll",

    [Parameter()]
    [string]$destLatestFilename = "latestbeta.txt",

    [Parameter()]
    [string]$filenamePrefix = "ServerManager.Plugin.Discord_",

    [Parameter()]
    [string]$feedFilename = "VersionFeedBeta.xml",

    [Parameter()]
    [string]$signTool = "C:\Program Files (x86)\Microsoft SDKs\ClickOnce\SignTool\SignTool.exe",

    [Parameter()]
    [string]$signNFlag = "${env:SIGN_NFLAG}",

    [Parameter()]
    [string]$signTFlag = "http://timestamp.digicert.com",

    [Parameter()]
    [string]$githubRoot = "D:\GitHub\ServerManagers\Plugins\Discord\beta"
)

[string] $AppVersion = ""
[string] $AppVersionShort = ""

function Get-LatestVersion( $srcFile )
{   
	$assembly = [Reflection.Assembly]::Loadfile($srcFile)
	$assemblyName = $assembly.GetName()
	return $assemblyName.version;
}

function Sign-Application ( $sourcedir , $signFile )
{
	if(Test-Path $signTool)
	{
		if(($signFile -ne "") -and ($signNFlag -ne "") -and ($signTFlag -ne ""))
		{
			Write-Host "Signing $($signFile)"
			& $signTool sign /n "$($signNFlag)" /t $signTFlag "$($sourcedir)\$($signFile)"
		}
	}
}

function Create-Zip( $sourcePath , $zipFile )
{
    if(Test-Path $zipFile)
    {
        Remove-Item -LiteralPath:$zipFile -Force
    }
	Add-Type -Assembly System.IO.Compression.FileSystem
	Write-Host "Zipping $($sourcePath) into $($zipFile)"
	$compressionLevel = [System.IO.Compression.CompressionLevel]::Optimal
	[System.IO.Compression.ZipFile]::CreateFromDirectory($sourcePath, $zipFile, $compressionLevel, $false)
}

$srcFile = "$($rootPath)\$($binDir)\$($srcFilename)"
$feedFile = "$($rootPath)\$($feedFilename)"
$languageFile = "$($rootPath)\Globalization\en-US\en-US.xaml"

$AppVersion = Get-LatestVersion $srcFile
$AppVersionShort = $AppVersion
Write-Host "LatestVersion $($AppVersionShort) ($($AppVersion))"

# test if the publish directory exists
$versionWithUnderscores = $AppVersion.Replace('.', '_')
$publishPath = "$($rootPath)\$($publishDir)\$($filenamePrefix)$($versionWithUnderscores)"
if(!(Test-Path -Path ($publishPath)))
{
  Write-Host "Creating folder $($publishPath)"

  # create the destination directory
  New-Item -ItemType directory -Path "$($publishPath)"
}

# copy the source file
Copy-Item -Path $($srcFile) -Destination $($publishPath) -Force
Write-Host "Copied $($srcFile) to $($publishPath)"

# write latest version file
$txtDestFileName = "$($destLatestFilename)"
$txtDestFile = "$($rootPath)\$($publishDir)\$($txtDestFileName)"
$AppVersionShort | Set-Content "$($txtDestFile)"

Sign-Application $publishPath "*.dll"

# create the zip file
$zipDestFileName = "$($filenamePrefix)$($AppVersionShort).zip"
$zipDestFile = "$($rootPath)\$($publishDir)\$($zipDestFileName)"
Create-Zip $publishPath $zipDestFile

# copy the files to the GITHUB folder
Write-Host "Copying files to the github folder"
Copy-Item -Path "$feedFile" -Destination "$githubRoot\VersionFeed.xml"
Copy-Item -Path "$languageFile" -Destination $githubRoot
Copy-Item -Path "$txtDestFile" -Destination "$githubRoot\latest.txt"
Copy-Item -Path "$zipDestFile" -Destination "$githubRoot\latest.zip"
