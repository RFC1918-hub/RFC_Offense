# RFC_OffensivePowerShell.Bypass.Applocker

This repository contains offensive PowerShell scripts that leverage techniques to bypass AppLocker, a Windows feature used for application whitelisting. These scripts are intended for educational and research purposes only. Please use them responsibly and ethically.

## What is AppLocker?
AppLocker is a Windows feature that allows administrators to define rules for allowing or blocking specific applications or scripts from running on a system. However, certain techniques can bypass or circumvent AppLocker restrictions, enabling the execution of unauthorized PowerShell code.

## Scripts

| Script | Description |
| -- | -- |
| Get-AppLockerRules.ps1 | The PowerShell script provided enumerates and retrieves AppLocker rules on a local system. |
| Invoke-InstallUtils.ps1 | This PowerShell script uses InstallUtil.exe to run a custom C# binary to bypass AppLocker and execute a PowerShell payload. |

## Requirements
* PowerShell 5.1 or later.
* Windows operating system.

## Disclaimer
These scripts are provided as-is, without any warranty or guarantee. They are meant for educational and research purposes only. The authors and contributors of this repository are not responsible for any misuse or damage caused by these scripts.

## Usage and Legal Considerations
It is important to note that using these scripts for any malicious or unauthorized activities is illegal. Ensure that you have appropriate permissions and adhere to all applicable laws and regulations before using these scripts. Misuse of these scripts may result in criminal charges or other legal consequences.