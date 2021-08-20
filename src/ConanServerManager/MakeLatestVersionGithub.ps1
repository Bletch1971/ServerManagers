Param
(
    [Parameter()]
    [string]$rootPath = "D:\GitHub\ServerManagers\src\ConanServerManager",

    [Parameter()]
    [string]$publishDir = "publish",

    [Parameter()]
    [string]$srcXmlFilename = "ConanServerManager.application",

    [Parameter()]
    [string]$destLatestFilename = "latest.txt",

    [Parameter()]
    [string]$filenamePrefix = "ConanServerManager_",

    [Parameter()]
    [string]$feedFilename = "VersionFeed.xml",

    [Parameter()]
    [string]$signTool = "C:\Program Files (x86)\Microsoft SDKs\ClickOnce\SignTool\SignTool.exe",

    [Parameter()]
    [string]$signNFlag = "${env:SIGN_NFLAG}",

    [Parameter()]
    [string]$signTFlag = "http://timestamp.digicert.com",

    [Parameter()]
    [string]$installerTool = "C:\Program Files (x86)\Inno Setup 5\ISCC.exe",

    [Parameter()]
    [string]$githubRoot = "D:\GitHub\ServerManagers\CSM"
)

[string] $AppVersion = ""
[string] $AppVersionShort = ""

function Get-LatestVersion()
{   
    $xmlFile = "$($rootPath)\$($publishDir)\$($srcXmlFilename)"
    $xml = [xml](Get-Content $xmlFile)
    $version = $xml.assembly.assemblyIdentity | Select version
    return $version.version;
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

function Create-Installer()
{   
	if(Test-Path $installerTool)
	{
		if(($installScriptFile -ne "") -and ($installTextFile -ne ""))
		{
			$installTextFileContent = @"
#define AppVer "$($AppVersionShort)"
#define AppVerFull "$($AppVersion)"
#define AppVerPath "$($versionWithUnderscores)"
"@
			$installTextFileContent | Out-File -LiteralPath:$installTextFile -Force -Encoding ascii

			Write-Host "Creating installer $($installerFile)"
			& "$installerTool" "$installScriptFile"
		}
	}
}

$publishPath = "$($rootPath)\$($publishDir)"
$txtDestFile = "$($publishPath)\$($destLatestFilename)"
$feedFile = "$($rootPath)\$($feedFilename)"
$languageFile = "$($rootPath)\Globalization\en-US\en-US.xaml"
$installTextFile = "$($rootPath)\Installer.txt"
$installScriptFile = "$($rootPath)\Installer.iss"
$filenamePrefixStripped = $filenamePrefix.Replace(' ', '')

$AppVersion = Get-LatestVersion
$AppVersionShort = $AppVersion.Substring(0, $AppVersion.LastIndexOf('.'))
$AppVersionShort | Set-Content "$($txtDestFile)"
Write-Host "LatestVersion $($AppVersionShort) ($($AppVersion))"

$versionWithUnderscores = $AppVersion.Replace('.', '_')
$publishSrcDir = "$($publishPath)\Application Files\$($filenamePrefix)$($versionWithUnderscores)"
Remove-Item -Path "$($publishSrcDir)\$($srcXmlFilename)" -ErrorAction Ignore

#copy the server manager updater (exe) and prefix with 'New'
Write-Host "Copying the server manager updater (exe) and prefix with 'New'"
Copy-Item -Path "$($publishSrcDir)\ServerManagerUpdater.exe" -Destination "$($publishSrcDir)\NewServerManagerUpdater.exe"

#sign the executable files
Sign-Application $publishSrcDir "*.exe"

$zipDestFileName = "$($filenamePrefixStripped)$($AppVersionShort).zip"
$zipDestFile = "$($publishPath)\$($zipDestFileName)"
Create-Zip $publishSrcDir $zipDestFile

#delete the copied server manager updater File - do not want to include in the installer
Remove-Item -Path "$($publishSrcDir)\NewServerManagerUpdater.exe" -ErrorAction Ignore

$installerFileName = "$($filenamePrefixStripped)$($AppVersionShort).exe"
$installerFile = "$($publishPath)\$($installerFileName)"
Create-Installer

#sign the installer file
Sign-Application $publishPath $installerFileName

# copy the files to the GITHUB folder
Write-Host "Copying files to the github folder"
Copy-Item -Path "$feedFile" -Destination "$githubRoot\VersionFeed.xml"
Copy-Item -Path "$languageFile" -Destination $githubRoot
Copy-Item -Path "$txtDestFile" -Destination "$githubRoot\latest.txt"
Copy-Item -Path "$zipDestFile" -Destination $githubRoot
Copy-Item -Path "$installerFile" -Destination $githubRoot
Copy-Item -Path "$zipDestFile" -Destination "$githubRoot\latest.zip"
Copy-Item -Path "$installerFile" -Destination "$githubRoot\latest.exe"
