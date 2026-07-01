using System;
using System.Collections.Generic;
using System.Text;
using System.IO.Ports;

namespace Client_Side
{
    internal class Client
    {
        private static SerialPort[] availablePorts = new SerialPort[10]; // 10 global serial ports

        public void Start()
        {
            InitPorts();

            Console.WriteLine("Please enter a command (Example: <'userID'> 'command' 'port number') >>");
            DisplayAvailablePorts();

            InputClientCommands();
        }

        private void InputClientCommands()
        {
            Console.WriteLine("Type QUIT to close the program...\n");

            while (true)
            {
                string input = Console.ReadLine().Trim();

                if (input != null && input.ToLower() == "quit")
                {
                    // safe exit...
                    CloseAllOpenPorts();
                    return;
                }

                // get port number from input (last 2 characters)
                int portNumber = 0;
                try
                {
                    portNumber = int.Parse(input.Substring(input.Length - 2)); // pull port num from input
                    input = input.Remove(input.Length - 2); // remove port number from the input
                }
                catch (Exception) 
                {
                    Console.WriteLine("Invalid input, expected: <'userID'> 'command + port number'...");
                    continue;
                }

                // open chosen port
                SerialPort openPort = OpenPort("COM" + portNumber);
                if (openPort == null)
                {
                    Console.WriteLine("Couldn't find/open port. Exiting program in 5 seconds...");
                    for (int i = 5; i >= 1; i--)
                    {
                        Console.Write(i + " ");
                        Thread.Sleep(1000);
                    }
                    return;
                }

                openPort.WriteLine(input);
            }
        }

        private void ReadResponse(Object sender, SerialDataReceivedEventArgs e)
        {
            SerialPort serialPort = (SerialPort)sender;

            try
            {
                // read and display server response
                string respone = serialPort.ReadLine();
                Console.WriteLine(respone);
            }
            catch (TimeoutException)
            {
                serialPort.DiscardInBuffer(); // clear the buffer to avoid reading the same message again if timed out...
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Read Error: {ex.Message}");
            }
        }

        private void InitPorts()
        {
            // initialize ports with rs-232 protocol:
            for (int i = 0; i <= 9; i++)
            {
                availablePorts[i] = new SerialPort("COM1" + i);
                availablePorts[i].BaudRate = 9600;
                availablePorts[i].DataBits = 8;
                availablePorts[i].Parity = Parity.None;
                availablePorts[i].StopBits = StopBits.One;
                availablePorts[i].Handshake = Handshake.None;
                availablePorts[i].NewLine = "\r\n"; // forces return upon a new line.

                // read/write timeouts in milliseconds:
                availablePorts[i].ReadTimeout = 500;
                availablePorts[i].WriteTimeout = 500;

                // attach event handler to read server responses
                availablePorts[i].DataReceived += ReadResponse;
            }
        }

        private SerialPort OpenPort(string portName)
        {
            for (int i = 0; i <= 9; i++)
            {
                if (availablePorts[i].PortName == portName)
                {
                    // port is already open...
                    if (availablePorts[i].IsOpen)
                        return availablePorts[i];

                    try
                    {
                        availablePorts[i].Open();
                        Console.WriteLine($"Port {portName} opened successfully.");
                        return availablePorts[i];
                    }
                    catch (Exception)
                    { }
                }
            }
            return null; // return null if the port was not found or could not be opened
        }

        private void CloseAllOpenPorts()
        {
            for (int i = 0; i <= 9; i++)
            {
                try {
                    availablePorts[i].Close();
                }
                catch (Exception) {
                    continue;
                }
            }
        }

        private void DisplayAvailablePorts()
        {
            Console.WriteLine("Available COM ports: ");
            foreach (SerialPort port in availablePorts)
            {
                if (!port.IsOpen)
                {
                    try
                    {
                        port.Open();
                        port.Close();
                    }
                    catch (Exception)
                    {
                        continue;
                    }
                    Console.WriteLine("\t- " + port.PortName);
                }
            }
        }
    }
}
