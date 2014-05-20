# Testing
# Kill open ports.
$port.Close()
$port.Dispose()
# [System.IO.Ports.SerialPort]::getportnames()
# for some bizarre reason, each of these has to be exectued on the command line, one at a time.
$port= new-Object System.IO.Ports.SerialPort COM8,9600,None,8,one # Bluetooth SSP
# $port= new-Object System.IO.Ports.SerialPort COM4,19200,None,8,one # EasyRadio
$start = [Byte[]] (,0x02) # used to indicate the start of the message
$end = [Byte[]] (,0x03) # end of message
$port.Open()

function realMessage
{
    $msg = (get-date -f mm:ss.f)
    $msg
    $port.Write($start,0,1)
    $port.Write([System.BitConverter]::GetBytes($msg.Length), 0, 1)
    $port.Write($msg)
    $port.Write($end,0,1) 
}

function errorMessages
{
    $msg = (get-date -f mm:ss.f)
    "no start"
    $port.Write([System.BitConverter]::GetBytes($msg.Length), 0, 1)
    $port.Write($msg)
    $port.Write($end,0,1) 
   "no end"
    $port.Write($start,0,1)
    $port.Write([System.BitConverter]::GetBytes($msg.Length), 0, 1)
    $port.Write($msg)
}

Start-Sleep 1

# Example tests
1..10 | foreach { start-sleep -m 200 ; realMessage ; errorMessages }

$port.ReadExisting() # read everything in the buffer.

$port.Close() 