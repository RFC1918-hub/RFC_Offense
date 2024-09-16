# RFC_OffensivePowerShell.Bypass.Hooking

This repository contains offensive PowerShell scripts that demonstrate hooking techniques to intercept and manipulate system or application behavior. These scripts are intended for educational and research purposes only. Please use them responsibly and ethically.

## What is Hooking?
Hooking is a technique used to intercept and modify the normal flow of execution in software applications or the operating system. It allows for the injection of custom code to monitor or modify the behavior of targeted processes or functions. In the context of PowerShell, hooking techniques can be used to manipulate PowerShell sessions, intercept API calls, or modify system behavior.

## Scripts

| Script | Description |
| -- | -- |
| Get-NtdllApiHooks.ps1 | Check for hooks in Ntdll functions |
| Invoke-NtdllApiUnhook.ps1 | Attempt to unhook all userland hooks on Ntdll.dll functions | 

## Requirements
* PowerShell 5.1 or later.
* Windows operating system.

## Disclaimer
These scripts are provided as-is, without any warranty or guarantee. They are meant for educational and research purposes only. The authors and contributors of this repository are not responsible for any misuse or damage caused by these scripts.

## Usage and Legal Considerations
It is important to note that using these scripts for any malicious or unauthorized activities is illegal. Ensure that you have appropriate permissions and adhere to all applicable laws and regulations before using these scripts. Misuse of these scripts may result in criminal charges or other legal consequences.