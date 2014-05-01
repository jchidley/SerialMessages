using System;
using System.Threading;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;
using SecretLabs.NETMF.Hardware;
using SecretLabs.NETMF.Hardware.Netduino;
using System.Text;
using System.IO.Ports;

namespace SerialMessages
{
    // Tested with:
    // * Netduino 2
    public class Program
    {
        static SerialPort port;
        private static byte[] _messageIn;
        static int currentPositionInMessage = 0;
        static bool messageStarted = false;
        const int maxMessageSize = 1024 * 10; // maximum message size is 10k: it's a small device

        // This serial program relies on these serial message delimiters.
        const byte startOfText = 2; // ASCII start of text
        const byte endOfText = 3; // ASCII end of text
        static UTF8Encoding encoder = new UTF8Encoding();

        public static void Main()
        {
            // See readme.md in root directory
            SerialOpen();  // Open the port with default settings

            port.DataReceived += port_DataReceived;  // do something with received data

            // Tests
            SerialSend("\n=== first started at: " + System.DateTime.Now + " ===\n\n");
            // send data for testing purposes.
            while (true)
            {
                WaitUntilNextPeriod(5000); // Send each new message on defined boundary, in milli seconds.
                string messageOut = "DateTime.Now: " + System.DateTime.Now;
                byte[] buff = encoder.GetBytes(messageOut);;
                messageOut = messageOut + "\tCRC-32: " + Utility.ComputeCRC(buff,0,buff.Length,0) + "\n";  // could be too much effort...
                SerialSend(messageOut); // send a message
            }
        }

        private static void port_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            byte currentByte;

            for (int i = 0; i < port.BytesToRead ; i++)
            {
                currentByte = (byte)port.ReadByte();  // ensures that byte is always consumed

                switch (currentByte)
                {
                    case startOfText: // starts new message, even if in the middle of one.
                        newMessage(); 
                        break; 
                    case endOfText: // it's an error if no messageStarted or it's normal if we have a start of message.
                        if (messageStarted)  // do something with what we've got.
                        {
                            var receivedMessage = new string(encoder.GetChars(_messageIn,0,_messageIn.Length));  // converts filled char[] elements to string (i.e. shortens it)
                            useMessage(receivedMessage); // normal message completion.
                        }  // fall through next steps
                        messageReset(); // Reset.  This will deal with incorrectly ended messages too.
                        break;
                    default: // add characters.
                        if (!messageStarted)
                        {
                            break; // ignore it.  current byte is already consumed.
                        }
                        // normal case
                        _messageIn[currentPositionInMessage] = currentByte;
                        currentPositionInMessage++;
                        break;
                }

                if (currentPositionInMessage >= maxMessageSize) // error.  Reset.
                {
                    messageReset(); // error.  Reset.
                }
            }
        }

        private static void messageReset()
        {
            messageStarted = false;
            currentPositionInMessage = 0;
            _messageIn = new byte[maxMessageSize];
        }

        private static void newMessage()
        {
            messageReset();  // start from a clean slate.
            messageStarted = true;
        }

        private static void useMessage(string receivedMessage)
        {
            Debug.Print(receivedMessage); // preferably something more than this...
        }

        public static void SerialOpen(string _portname = "COM1", int _baud = 19200,
            Parity _parity = Parity.None, int _dataBits = 8, StopBits _stopBits = StopBits.One)
        // the defaults for EasyRadio advanced are set in this header
        {
            port = new SerialPort(_portname, _baud, _parity, _dataBits, _stopBits);
            port.Open();
        }

        private static void SerialSend(string messageOut)
        {
            
           if (messageOut.Length > maxMessageSize - 1)
            {
                new ArgumentException("Serial Message exceeded size limit of" + maxMessageSize.ToString() + "\n");
            }
            
            port.Write((new byte[] {startOfText}), 0, 1);
            byte[] bytesToSend = encoder.GetBytes(messageOut);
            port.Write(bytesToSend, 0, bytesToSend.Length);
            port.Write((new byte[] { endOfText }), 0, 1);
        }

        public static void WaitUntilNextPeriod(int period)
        {
            long now = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
            var offset = (int)(now % period);
            int delay = period - offset;
            // Debug.Print("sleep for " + delay + " ms\r\n");
            Thread.Sleep(delay);
        }
    }
}
