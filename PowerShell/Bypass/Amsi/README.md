# RFC_OffensivePowerShell.Bypass.Amsi

This repository contains offensive PowerShell scripts that leverage AMSI (Antimalware Scan Interface) bypass techniques. These scripts are intended for educational and research purposes only. Please use them responsibly and ethically.

## What is AMSI?
AMSI is a Windows feature that provides enhanced malware protection by allowing antivirus products to scan PowerShell scripts and other content for potential threats. However, certain techniques can bypass or evade AMSI, enabling the execution of malicious PowerShell code without detection.

## Scripts

| Script | Description |
| -- | -- |
| Invoke-AnsiFail.ps1 | Bypass AMSI by setting "amsiInitFailed" to true. (The error message "amsiInitFailed" indicates a failure in initializing the AMSI interface. This error can occur if there is a problem with the AMSI.dll file or if the associated antivirus/antimalware software is not properly installed or functioning correctly.)  |
| Invoke-AnsiPatch.ps1 | Bypass AMSI by patch "AmsiScanBuffer" function to always return `AMSI_RESULT_CLEAN ` |

## Requirements
* PowerShell 5.1 or later.
* Windows operating system.

## Disclaimer
These scripts are provided as-is, without any warranty or guarantee. They are meant for educational and research purposes only. The authors and contributors of this repository are not responsible for any misuse or damage caused by these scripts.

## Usage and Legal Considerations
It is important to note that using these scripts for any malicious or unauthorized activities is illegal. Ensure that you have appropriate permissions and adhere to all applicable laws and regulations before using these scripts. Misuse of these scripts may result in criminal charges or other legal consequences.