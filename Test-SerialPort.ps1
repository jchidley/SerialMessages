# Testing
# Kill open ports.
$port.Close()
$port.Dispose()
# [System.IO.Ports.SerialPort]::getportnames()
# for some bizarre reason, each of these has to be exectued on the command line, one at a time.
$port= new-Object System.IO.Ports.SerialPort COM4,19200,None,8,one
$start = [Byte[]] (,0x02) # used to indicate the start of the message
$end = [Byte[]] (,0x03) # end of message
$port.Open()

function realMessage
{
    $port.Write($start,0,1)
    $sent = (get-date -f mm:ss.f)
    $sent
    $port.WriteLine( $sent ) # real message should be received
    $port.Write($end,0,1) 
}

function errorMessages
{
    $port.WriteLine("xxx") 
    $port.Write($end,0,1)  
    "no start"
    $port.Write($start,0,1)
    $port.WriteLine("xxx") 
    "no end"
}

Start-Sleep 1

# Example tests
1..10 | foreach { start-sleep -m 200 ; realMessage ; errorMessages }

while ($port.BytesToRead) { $port.ReadLine(); }

$port.Close() 