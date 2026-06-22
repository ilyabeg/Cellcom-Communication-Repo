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
            public string ID { get; set; } = "unknown.user"; // default value for ID
            private bool _callActive;

            // each customer has 3 cellcom methods...
            public void JoinCellcom(string command)
            {
                serialPort.WriteLine(String.Format("<{0}> : {1} : JOINING CELLCOM ENTERPRISE...", this.ID, command));
                for (int i = 1; i <= 10; i++)
                {
                    serialPort.Write(i + " ");
                    Thread.Sleep(500); // simulate some work being done...
                }
                serialPort.WriteLine(String.Format("\r\n<{0}> : DONE", this.ID));
            }

            public async Task InitiateCall(string command)
            {
                serialPort.WriteLine(String.Format("<{0}> : {1}", this.ID, command));
                _callActive = true; // activate call

                while (_callActive)
                {
                    serialPort.WriteLine(String.Format("<{0}> : CELLCOM", this.ID));
                    await Task.Delay(1000); // delay one second...
                }
            }

            public void CloseCall(string command)
            {
                serialPort.WriteLine(String.Format("<{0}> : {1}", this.ID, command));
                serialPort.WriteLine(String.Format("<{0}> : BYE.", this.ID));
                _callActive = false; // close call
            }
        }

        static void Main(string[] args)
        {
            serialPort = new SerialPort();
            Thread commandsThread = new Thread(ReadCommands);

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
            serialPort.NewLine = "\r\n"; // forces return upon a new line.

            // read/write timeouts in milliseconds:
            serialPort.ReadTimeout = 500;
            serialPort.WriteTimeout = 500;

            // creating new customer and setting ID
            Customer c1 = new Customer();
            c1.ID = SetCustomerID(c1.ID);

            // open serial port and start commands thread...
            serialPort.Open();
            _continue = true;
            commandsThread.Start();

            Console.WriteLine("Type EXIT to stop");
            string command;

            while (_continue)
            {
                command = Console.ReadLine();

                switch (command)
                {
                    case "EXIT" or "Exit" or "exit":
                        _continue = false;
                        break;
                    case "JOIN" or "Join" or "join":
                        c1.JoinCellcom(command);
                        break;
                    case "NEW" or "New" or "new":
                        c1.InitiateCall(command);
                        break;
                    case "STOP" or "Stop" or "stop":
                        c1.CloseCall(command);
                        break;
                    default:
                        serialPort.WriteLine(String.Format("<{0}> : {1} : Command not found.", c1.ID, command));
                        break;
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
                    //string command = serialPort.ReadLine();
                    //Console.WriteLine(command);
                }
                catch (TimeoutException e)
                {
                    Console.WriteLine("\n\tError. Application stopped due to timeout exception: " + e.Message + ".");
                }
            }
        }

        public static string SetCustomerID(string defaultID)
        {
            Console.WriteLine("Enter customer ID (or press Enter to use default ID: {0}):", defaultID);
            string inputID = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(inputID))
            {
                return defaultID;
            }
            else
            {
                Console.WriteLine("\tCustomer ID set to: {0}", inputID);
                return inputID;
            }
        }
    }
}