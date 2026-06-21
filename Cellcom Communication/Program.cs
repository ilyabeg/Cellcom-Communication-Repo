using System;
using System.Threading.Tasks;
using System.IO.Ports;

namespace Cellcom_Communication
{
    internal class Program
    {
        static SerialPort serialPort; // global serial port
        static bool _continue; // global flag

        public class Customer
        {
            public string ID;

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
            serialPort = new SerialPort();
            Thread commandsThread = new Thread(ReadCommands);
            string tmpID;

            // list available serial com ports on pc...
            Console.WriteLine("Available Ports:");
            foreach (string portName in SerialPort.GetPortNames())
            {
                Console.WriteLine("\t{0}", portName);
            }

            // RS-232 protocol configuration:
            serialPort.PortName = "COM3";
            serialPort.BaudRate = 9600;
            serialPort.DataBits = 8;
            serialPort.Parity = Parity.None;
            serialPort.StopBits = StopBits.One;
            serialPort.Handshake = Handshake.None;

            // read/write timeouts in milliseconds:
            serialPort.ReadTimeout = 500;
            serialPort.WriteTimeout = 500;

            Customer c1 = new Customer();
            Console.Write("Enter customer ID -> ");
            tmpID = Console.ReadLine();

            while (tmpID == null || tmpID == "")
            {
                Console.Write("\nInvalid input. Please re-enter ID -> ");
                tmpID = Console.ReadLine();
            }
            c1.ID = tmpID;

            serialPort.Open();
            _continue = true;
            commandsThread.Start();

            Console.WriteLine("Type EXIT to stop");
            string command;

            while (_continue)
            {
                command = Console.ReadLine();

                if (command.Equals("EXIT") || command.Equals("exit"))
                {
                    _continue = false;
                } else
                {
                    serialPort.WriteLine(String.Format("<{0}>: {1}", c1.ID, command));
                }
            }
            
            commandsThread.Join();
            serialPort.Close();
        }

        public static void ReadCommands()
        {
            while (_continue)
            {
                try
                {

                }
                catch (TimeoutException e) 
                { 
                    Console.WriteLine("\n\tError. Application stopped due to timeout exception: " + e.Message + ".");
                }
            }
        }
    }
}