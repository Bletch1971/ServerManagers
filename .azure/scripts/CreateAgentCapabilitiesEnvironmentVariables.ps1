Param (
  [string]$CollectionUri,
  [int]$AgentId,
  [string]$AgentName,
  [string]$AccessToken,
  [string]$DebugMode = 'false'
)

Function Get-AzureDevopsAgent() {
    param(
        [Parameter(Mandatory = $true)] [string]$baseUri,
        [Parameter(Mandatory = $true)] [string]$accessToken,
        [Parameter(Mandatory = $true)] [int]$agentId,
        [Parameter(Mandatory = $true)] [string]$agentName
    )

    try {
        [Net.ServicePointManager]::SecurityProtocol = "Tls12, Tls13"

        $headers = @{
            Authorization = "Basic $accessToken"
        }

        $uri = "$($baseUri)/distributedtask/pools?api-version=6.0"
        $responsePools = Invoke-RestMethod -Method Get -Uri $uri -Headers $headers -UseBasicParsing

        foreach ($pool in $responsePools.Value) {

            $uri = "$($baseUri)/distributedtask/pools/$($pool.Id)/agents?api-version=6.0&includeCapabilities=true"
            $responseAgents = Invoke-RestMethod -Method Get -Uri $uri -Headers $headers -UseBasicParsing

            $agents = $responseAgents.Value.Where({$_.id -eq $agentId -and $_.name -eq $agentName})
            if (!($agents) -or $agents.Count -eq 0) {
                continue
            }

            if ($agents.Count -gt 1) {
                throw "Multiple agents ($($agents.Count)) found with id: $agentId and name: $agentName"
            }

            return $agents.Item(0)
        }

        Write-Host -ForeGroundColor Yellow 'Agent NOT found'
        return $null
    }
    catch {

        Write-Host -ForeGroundColor Red 'Unhandled exception occurred during agent fetch!'
        Write-Host -ForegroundColor Red $_.Exception.Message
        throw 
    }
}

Function Output-AgentCapabilities() {
    param(
        [Parameter(Mandatory = $true)] [PSCustomObject]$capabilities,
        [Parameter(Mandatory = $true)] [string]$capabilityType
    )

    [int]$count = 0
    foreach ($capability in $capabilities.PSObject.Properties) {
        $envName = "AgentCapabilities.$($capabilityType).$($capability.Name)".Replace('_', '.')
        [System.Environment]::SetEnvironmentVariable($envName, $($capability.Value))

        $count = $count + 1
    }

    Write-Host -ForeGroundColor Cyan "Created $count AgentCapabilities.$capabilityType environment variables"
}

$ErrorActionPreference = 'Stop'

if ($debugMode -eq "true") 
{
  Write-Host "##[section]Starting: DEBUG INFORMATION"

  Write-Host "##[debug]CollectionUri = $CollectionUri"
  Write-Host "##[debug]AgentId = $AgentId"
  Write-Host "##[debug]AgentName = $AgentName"
  Write-Host "##[debug]AccessToken = $AccessToken"

  Write-Host "##[section]Finishing: DEBUG INFORMATION"
  Write-Host ""
}

$AgentData = Get-AzureDevopsAgent -baseUri "$($CollectionUri)_apis" -accessToken $AccessToken -agentId $AgentId -agentName $AgentName
if ($AgentData) {
    Output-AgentCapabilities -capabilities $AgentData.systemCapabilities -capabilityType 'System'
    Output-AgentCapabilities -capabilities $AgentData.userCapabilities -capabilityType 'User'
}

Write-Host 'Done'
exit 0
