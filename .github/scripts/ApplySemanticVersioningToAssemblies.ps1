Param
(
  [string]$applicationName = $env:BUILD_APPLICATION_NAME,

  [string]$pathToSearch = $env:BUILD_SOURCE_PATH,

  [string]$versionFile = $env:BUILD_VERSION_FILE,
	
  [regex]$pattern = "\d+\.\d+\.\d+\.\d+",
	
  [string]$patternSplitCharacters = ".",
	
  [int]$patternExpectedVersionNumbers = 4,
	
  [int]$versionNumbersInVersion = 4,
	
  [string]$searchFilter = "AssemblyInfo.*",

  [string]$debugMode = 'false'
)

# Declare functions
function Replace-Version($content, $version, $attribute) 
{
  $exitFunction = $false
  $content | %{
    if ($_ -match 'exclude from semantic versioning')
    {
        Write-Host "     * Skipping $attribute due to exclude"
        $exitFunction = $true
    }
    if ($_ -match 'include semantic versioning' -and $_ -notmatch "include semantic versioning - $applicationName")
    {
        Write-Host "     * Skipping $attribute due to include not matching"
        $exitFunction = $true
    }
  }

  if ($exitFunction)
  {
    return $content
  }

  $versionAttribute = "[assembly: $attribute(""$version"")]"
  $pattern = "\[assembly: $attribute\("".*""\)\]"
  $versionReplaced = $false
  
  $content = $content | %{
	if ($_ -match $pattern) 
    {
	  $versionReplaced = $true
	  $_ = $_ -replace [regex]::Escape($Matches[0]),$versionAttribute
	  Write-Host "     * Replaced $($Matches[0]) with $versionAttribute"
	}
	$_
  }
  
  if (-not $versionReplaced) 
  {
	$content += [Environment]::NewLine + $versionAttribute
	Write-Host "     * Added $versionAttribute to end of content"
  }

  return $content
}

function Get-VersionString($numberOfVersions, $extractedBuildNumbers) 
{
  return [string]::Join(".",($extractedBuildNumbers | select -First ($numberOfVersions)))
}

$ErrorActionPreference = "Stop"

if ($debugMode -eq "true") 
{
  Write-Host "##[section]Starting: DEBUG INFORMATION"

  Write-Host "##[debug]applicationName = $applicationName"
  Write-Host "##[debug]pathToSearch = $pathToSearch"
  Write-Host "##[debug]versionFile = $versionFile"
  Write-Host "##[debug]pattern = $pattern"
  Write-Host "##[debug]patternSplitCharacters = $patternSplitCharacters"
  Write-Host "##[debug]patternExpectedVersionNumbers = $patternExpectedVersionNumbers"
  Write-Host "##[debug]versionNumbersInVersion = $versionNumbersInVersion"
  Write-Host "##[debug]searchFilter = $searchFilter"

  Write-Host "##[section]Finishing: DEBUG INFORMATION"
  Write-Host ""
}

# read the build number from the version file
$buildNumber = Get-Content $versionFile

if ($buildNumber -match $pattern -ne $true) 
{
  Write-Host "Could not extract a version from [$buildNumber] using pattern [$pattern]"
  exit 2
}

# Set version variables
$extractedBuildNumbers = @($Matches[0].Split(([char[]]$patternSplitCharacters)))
if ($extractedBuildNumbers.Length -ne $patternExpectedVersionNumbers) 
{
  Write-Host "The extracted build number $($Matches[0]) does not contain the expected $patternExpectedVersionNumbers elements"
  exit 2
}

$version = Get-VersionString -numberOfVersions $versionNumbersInVersion -extractedBuildNumbers $extractedBuildNumbers
$fileVersion = Get-VersionString -numberOfVersions $versionNumbersInVersion -extractedBuildNumbers $extractedBuildNumbers
Write-Host "Using version $version and file version $fileVersion"

# iterate the search path (and sub directories) looking for files that match the search filter
Get-ChildItem -Path $pathToSearch -Filter $searchFilter -Recurse | %{
  Write-Host "  -> Checking $($_.FullName)"
		 
  # remove the read-only bit on the file
  #sp $_.FullName IsReadOnly $false
  Set-ItemProperty $_.FullName -name IsReadOnly -value $false
 
  # run the regex replace
  $content = Get-Content $_.FullName
  $content = Replace-Version -content $content -version $version -attribute 'AssemblyVersion'
  $content = Replace-Version -content $content -version $fileVersion -attribute 'AssemblyFileVersion'
  $content | Set-Content $_.FullName -Encoding UTF8
}

Write-Host "Done"
exit 0
