Param (
  [string]$repositoryUrl = $env:SYSTEM_COLLECTIONURI,
  [string]$repositoryName = $env:SYSTEM_TEAMPROJECT,
  [string]$definitionsFilter = $env:BUILD_DEFINITIONNAME,
  [string]$variableName = 'VersionRevision',
  [string]$authorization = $env:SYSTEM_ACCESSTOKEN,
  [string]$debugMode = $env:SYSTEM_DEBUG
)

$ErrorActionPreference = 'Stop'

if ($debugMode -eq 'true') {
  Write-Host '##[section]Starting: DEBUG INFORMATION'

  Write-Host "##[debug]repositoryUrl = $repositoryUrl"
  Write-Host "##[debug]repositoryName = $repositoryName"
  Write-Host "##[debug]definitionsFilter = $definitionsFilter"
  Write-Host "##[debug]variableName = $variableName"

  Write-Host '##[section]Finishing: DEBUG INFORMATION'
  Write-Host ''
}

$null = [Reflection.Assembly]::LoadFile("$pwd\Newtonsoft.Json.dll")

# Remove the branch name '[<branch>]' from the definitions filter. 
# This was we can check the build numbers in sync acrosss the branches
$defFilter = $definitionsFilter
$defFilterParts = $defFilter.Split("(", [StringSplitOptions]'RemoveEmptyEntries')
$defFilter = $defFilterParts[0].Trim() + "*"
if ($debugMode -eq 'true') {
  Write-Host "##[debug]defFilter = $defFilter"
}

# Get the value from the environment variable (if exists)
$variableValue = get-content env:$variableName -ErrorAction SilentlyContinue
if ($debugMode -eq 'true') {
  Write-Host "##[debug]variableValue = $variableValue"
}

if($repositoryUrl.EndsWith('/')) {
  $repositoryUrl = $repositoryUrl.TrimEnd('/')
}

# Get an overview of all build definitions in this team project
$definitionsOverviewUrl = "$repositoryUrl/$repositoryName/_apis/build/Definitions"
if ($debugMode -eq 'true') {
  Write-Host "##[debug]definitionsOverviewUrl = $definitionsOverviewUrl"
}

$headers = @{ 
  'Authorization' = $authorization; 
  'Accept' = 'application/json; api-version=5.1' 
}
$definitionsOverviewResponse = Invoke-WebRequest -Uri $definitionsOverviewUrl -Method Get -ContentType 'application/json' -Headers $headers
$definitionsOverview = (ConvertFrom-Json -InputObject $definitionsOverviewResponse.Content).value

# Process all builds that have <definitionsFilter> in their name
foreach($definitionEntry in ($definitionsOverview | Where-Object { $_.name -like $defFilter })) {
  $buildDefinitionUrl = $definitionEntry.url
  if ($debugMode -eq 'true') {
    Write-Host "##[debug]buildDefinitionUrl = $buildDefinitionUrl"
  }

  $headers = @{ 
    'Authorization' = $authorization; 
    'Accept' = 'application/json; api-version=5.1' 
  }
  $buildDefinitionResponse = Invoke-WebRequest -Uri $buildDefinitionUrl -Method Get -ContentType 'application/json' -Headers $headers
  $buildDefinition = [Newtonsoft.Json.JsonConvert]::DeserializeObject($buildDefinitionresponse.Content)

  # If the build has the variable, update it.
  if($buildDefinition.variables.$variableName) {
    [int]$value = 0
    # Check if the environment variable value was set
    if($variableValue -eq $null -or $variableValue.Trim() -eq '') {
      # use the value from the build definition
      $value = $buildDefinition.variables.$variableName.value
      Write-Host 'Variable value used from build definition.'
    } else {
      $value = $variableValue
      Write-Host 'Variable value used from environment variable.'
    }  

    $buildDefinition.variables.$variableName.value = $value + 1
    [int]$newValue = $buildDefinition.variables.$variableName.value

    Write-Output -InputObject "Updating ""$($definitionEntry.name)"" $variableName from $($value) to $($newValue)..."

    $serialized = [Newtonsoft.Json.JsonConvert]::SerializeObject($buildDefinition)
    $postData = [Text.Encoding]::UTF8.GetBytes($serialized)

    $headers = @{ 
      'Authorization' = $authorization; 
      'Accept' = 'application/json; api-version=5.1' 
    }
    $response = Invoke-WebRequest -UseDefaultCredentials -Uri $buildDefinitionUrl -Method Put -ContentType 'application/json' -Headers $headers -Body $postData
    Write-Host "Response Status = $($response.StatusDescription)"
  }
}

Write-Host 'Done'
exit 0
