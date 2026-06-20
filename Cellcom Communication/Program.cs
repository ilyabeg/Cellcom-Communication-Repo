using System;
using System.Threading.Tasks;
using System.IO.Ports;

namespace Cellcom_Communication
{
    internal class Program
    {
        static SerialPort serialPort = new SerialPort(); // global serial port

        public class Customer
        { 
            // each customer has 3 cellcom methods...
            public void JoinCellcom()
            {
            }

            public void InitiateCall()
            {
            }

            public void CloseCall() 
            {
            }
        }

        static void Main(string[] args)
        {
            // RS-232 protocol configuration:
            serialPort.BaudRate = 9600;
            serialPort.DataBits = 8;
            serialPort.Parity = Parity.None;
            serialPort.StopBits = StopBits.One;
            serialPort.Handshake = Handshake.None;

        }
    }
}