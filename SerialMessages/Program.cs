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
        static StringBuilder message;
        static bool messageStarted = false;
        const int maxSerialBufferSize = 1024;  // 1K max buffer for the serial port
        static int maxMessageSize = 1024 * 10; // maximum message size is 10k: it's a small device
        // This serial program relies on these serial message delimiters.
        static byte[] startOfText = new byte[] { 0x02 }; // ASCII start of text
        static byte[] endOfText = new byte[] { 0x03 }; // ASCII end of text

        public static void Main()
        {
            SerialOpen();

            port.DataReceived += port_DataReceived;  // do something with received data

            while (true)
            {
                SerialSend("DateTime.Now: " + System.DateTime.Now + "\n"); // send a message
                System.Threading.Thread.Sleep(2000); // 2000 is 2 sec, release thread
            }
        }

        private static void port_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            byte currentByte;

            for (int i = 0; i < maxSerialBufferSize; i++)
            {
                currentByte = (byte)port.ReadByte();  // ensures that byte is always consumed
                if (currentByte == endOfText[0])  // this byte has already been consumed
                {
                    if (!messageStarted)
                    {
                        messageError();
                        break;
                    } // can't end it without starting it. 
                    doSomethingWithMessage(); // normal message completion.  Will process any remaining bytes as a new message.
                }

                if (currentByte == startOfText[0])
                {
                    messageStarted = true;
                    message = new StringBuilder(maxMessageSize);  // starts new message, even if in the middle of one.
                }

                if (messageStarted) // Byte already consumed.  Add the char to the message, check for errors.
                {
                    message.Append((char)currentByte); // one more char in.
                    if (message.Length >= maxMessageSize - 1) messageError(); // don't overrun the buffer
                }

            }
        }

        private static void messageError()
        {
            messageStarted = false;
            if (message != null) message.Clear();
        }

        private static void doSomethingWithMessage()
        {
            Debug.Print(message.ToString()); // preferably something more than this...
            messageStarted = false;
            message.Clear();
        }

        public static void SerialOpen(string _portname = "COM1", int _baud = 19200,
            Parity _parity = Parity.None, int _dataBits = 8, StopBits _stopBits = StopBits.One)
        // the defaults for EasyRadio advanced are set in this header
        {
            port = new SerialPort(_portname, _baud, _parity, _dataBits, _stopBits);
            port.Open();
        }

        private static void SerialSend(string _message)
        {


            UTF8Encoding encoder = new UTF8Encoding();
            byte[] bytesToSend = encoder.GetBytes(_message);

            if (bytesToSend.Length > maxMessageSize - 2)
            {
                new ArgumentException("Serial Message exceeded size limit of" + maxMessageSize.ToString() + "\n");
            }

            if (bytesToSend.Length > maxSerialBufferSize - 2)
            {
                new NotImplementedException("Implement methods to handle serial data sent when size is greater than" + maxSerialBufferSize.ToString());
                // need to do some work here
            }

            port.Write(startOfText, 0, startOfText.Length);
            port.Write(bytesToSend, 0, bytesToSend.Length);
            port.Write(endOfText, 0, endOfText.Length);

        }

    }
}
