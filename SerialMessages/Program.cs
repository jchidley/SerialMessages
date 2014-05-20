using System;
using System.Threading;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;
using SecretLabs.NETMF.Hardware;
using SecretLabs.NETMF.Hardware.Netduino;
using System.Text;
using System.IO.Ports;
using JCC;

namespace SerialMessages
{// See readme.md in root directory
    public class Tester
    {
        // created new testing class
        public static void Main()
        {
            // Bluetooth module is 9600, EasyRadio 19200 
            SerialMessages messengerSerial = new SerialMessages("COM1", 9600, Parity.None, 8, StopBits.One);
            UTF8Encoding encoder = new UTF8Encoding();

            // Tests
            messengerSerial.Send("\n=== first started at: " + System.DateTime.Now + " ===\n\n");
            // send data for testing purposes.
            while (true)
            {
                JCC.Helper.WaitUntilNextPeriod(5000); // Send each new message on defined boundary, in milli seconds.
                string messageOut = "DateTime.Now: " + System.DateTime.Now + "\r\n";
                messengerSerial.Send(messageOut); // send a message
            }
        }
        
    }
    // Tested with:
    // * Netduino 2

    enum MessageStatus
    {
        InProgress,
        Error,
        End
    }

    public interface ISerialMessage
    {
        void Received(object sender, SerialDataReceivedEventArgs e);
        bool Connect(byte SourceAddress, byte DestinationAddress);
        bool Send(string message);
        bool Send(byte SourceAddress, byte DestinationAddress, string message);
        int MaxMessageSize { get; }
        byte SourceAddress { get; set; }
        byte DestinationAddress { get; set; }
    }

    public class SerialMessages : ISerialMessage
    {
        public byte SourceAddress { get; set; }
        public byte DestinationAddress { get; set; }

        public bool Connect(byte SourceAddress, byte DestinationAddress)
        {
            return true;
        }
        // moved tests to the static Tester class.
        SerialPort port;
        byte[] messageIn;
        int currentPositionInMessage = 0;
        bool messageStarted = false;
        byte messageLength = 0;
        // This serial program relies on these serial message delimiters.
        const byte START_OF_TEXT = 2; // ASCII start of text
        const byte END_OF_TEXT = 3; // ASCII end of text        
        const int HEADER_SIZE = sizeof(byte) + sizeof(byte);  //START_OF_TEXT and messageLength
        const int TAIL_SIZE = sizeof(byte); //  + sizeof(uint);  //END_OF_TEXT and CRC sizes
        const int MAX_MESSAGE_SIZE = 250 - HEADER_SIZE - TAIL_SIZE;  // 250 bytes is EasyRadio max size
        UTF8Encoding encoder = new UTF8Encoding();

        public int MaxMessageSize {
            get {
                return MAX_MESSAGE_SIZE;
            }
        }

        public SerialMessages(string portname = "COM1", int baud = 9600, Parity parity = Parity.None, int dataBits = 8, StopBits stopBits = StopBits.One)
        {
        // the defaults for EasyRadio advanced are set in this header
            port = new SerialPort(portname, baud, parity, dataBits, stopBits);
            port.Open();
            port.DataReceived += Received;  // do something with received data
        }

        public void Received(object sender, SerialDataReceivedEventArgs e)
        {
            byte currentByte;

            while (port.BytesToRead > 0)
            {
                currentByte = (byte)port.ReadByte();  // ensures that byte is always consumed

                switch (currentByte)
                {
                    case START_OF_TEXT: // starts new message, even if in the middle of one.
                        newMessage();
                        messageLength = (byte)port.ReadByte();
                        break; 
                    case END_OF_TEXT: // do something with what we've got.
                        if (messageStarted)  // normal
                        {
                            // will need to chop off last few bytes for CRC, if implemented.
                            var receivedMessage = new string(encoder.GetChars(messageIn,0,messageIn.Length));  // converts filled char[] elements to string (i.e. shortens it)
                            useMessage(receivedMessage); // normal message completion.
                        }
                        messageReset(); // Deal with incorrectly ended messages
                        break;
                    default: // add characters.
                        if (!messageStarted) // ignore it 
                        {
                            break; // current byte is already consumed.
                        }
                        messageIn[currentPositionInMessage] = currentByte;
                        currentPositionInMessage++;
                        break;
                }

                if (currentPositionInMessage >= MAX_MESSAGE_SIZE || messageLength > MAX_MESSAGE_SIZE) // error.  Reset.
                {
                    messageReset(); // error.  Reset.
                }
            }
        }

        private void messageReset()
        {
            messageStarted = false;
            currentPositionInMessage = 0;
            messageLength = 0;
            messageIn = new byte[MAX_MESSAGE_SIZE];
        }

        private void newMessage()
        {
            messageReset();  // start from a clean slate.
            messageStarted = true;
        }

        private void useMessage(string receivedMessage)
        {
            JCC.Helper.DebugPrint(receivedMessage); // preferably something more than this...
        }

        public bool Send(byte SourceAddress, byte DestinationAddress, string message)
        {
            return false;
        }
        public bool Send(string messageOut)
        {
            
           if (messageOut.Length > MAX_MESSAGE_SIZE)
            {
                new ArgumentException("Serial Message exceeded size limit of" + MAX_MESSAGE_SIZE.ToString() + "\n");
            }

            // head
            port.Write((new byte[] {START_OF_TEXT}), 0, 1);
            port.Write((new byte[] { (byte)messageOut.Length }), 0, 1);

            // message
            byte[] bytesToSend = encoder.GetBytes(messageOut);
            port.Write(bytesToSend, 0, bytesToSend.Length);

            //tail
            //byte[] crc = BitConverter.GetBytes (Utility.ComputeCRC(bytesToSend, 0, bytesToSend.Length, 0));
            //port.Write(crc, 0, crc.Length); 
            port.Write((new byte[] { END_OF_TEXT }), 0, 1);

            return true;
        }

    }
}
