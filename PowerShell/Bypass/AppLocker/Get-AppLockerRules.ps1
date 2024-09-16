<#
.SYNOPSIS
  This script will enumerate all AppLocker rules on the local system.
.DESCRIPTION
  This script will enumerate all AppLocker rules on the local system.
.PARAMETER Post
  This parameter will allow you to specify a URL to POST the results to.
.PARAMETER OutFile
  This parameter will allow you to specify a file to write the results to.
.EXAMPLE
  Get-AppLockerRules.ps1 -Post http://127.0.0.1:8080
.EXAMPLE
  Get-AppLockerRules.ps1 -OutFile C:\Temp\Get-AppLockerRules.txt
#>

#--------[Declarations]--------#
[CmdletBinding()]
Param(
  [Parameter(Mandatory=$False)]
  [string]$Post,

  [Parameter(Mandatory=$False)]
  [string]$OutFile
)

#--------[Functions]--------#
function Invoke-WebRequest {
  param (
    [Parameter(Mandatory=$True)]
    [string]$Uri,
    [Parameter(Mandatory=$False)]
    [string]$Method = 'POST',
    [Parameter(Mandatory=$False)]
    [string]$Body,
    [Parameter(Mandatory=$False)]
    [string]$ContentType = 'application/x-www-form-urlencoded',
    [Parameter(Mandatory=$False)]
    [string]$UserAgent = 'Mozilla/5.0 (Windows NT 6.1; WOW64; Trident/7.0; rv:11.0) like Gecko'
  )

  $request = New-Object System.Net.WebClient
  $request.Headers.Add('User-Agent', $UserAgent)
  $request.Headers.Add('Content-Type', $ContentType)
  $request.UploadString($Uri, $Method, $Body)
  
  return $request
}

function Get-AppLockerPolicy {
  $AppLockerPolicy = Get-AppLockerPolicy -Effective | Select-Object -ExpandProperty RuleCollections
  $AppLockerPolicyJSON = $AppLockerPolicy | ConvertTo-Json -Depth 10
  
  return $AppLockerPolicyJSON
}

function IsAppLockerEnabled {
  $AppLockerPolicy = Get-AppLockerPolicy -Effective | Select-Object -ExpandProperty RuleCollections
  $AppLockerPolicyJSON = $AppLockerPolicy | ConvertTo-Json -Depth 10
  
  if ($AppLockerPolicyJSON -eq $null) {
    return $False
  } else {
    return $True
  }    
}

#--------[Execution]--------#
if (IsAppLockerEnabled) {
  $AppLockerPolicy = Get-AppLockerPolicy -Effective
  if ($Post) {
    Invoke-WebRequest -Uri $Post -Body $AppLockerPolicy
  } elseif ($OutFile) {
    $AppLockerPolicy | Out-File $OutFile
  } else {
    Write-Host $AppLockerPolicy
  }
} else {
  Write-Host "AppLocker is not enabled on this system."
}

