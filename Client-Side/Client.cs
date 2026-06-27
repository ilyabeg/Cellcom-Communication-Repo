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

            Console.WriteLine("Select COM port (e.g. COM10, COM11, ... , COM19) >>");
            string input = Console.ReadLine().Trim();

            if (!isValid(input))
            {
                do
                {
                    Console.WriteLine("Invalid input. Please enter a valid COM port (e.g. COM10, COM11, ... , COM19) >>");
                    input = Console.ReadLine().Trim();
                }
                while (!isValid(input));
            }

            OpenPort(input);
        }

        private void InputClientCommands(Object sender, SerialDataReceivedEventArgs e)
        {
            SerialPort serialPort = (SerialPort)sender; // current active port
            serialPort.Write("#"); // random input to trigger event handler of the listening port to display commands for user
            string command;

            Console.WriteLine("Type EXIT to stop...\n");

            bool _continue = true;
            while (_continue)
            {
                command = Console.ReadLine();
                serialPort.Write(command);

                if (command == "EXIT") // if command is exit it is ok to send it first to close the listening port.
                {
                    _continue = false;
                    try
                    {
                        serialPort.Close();
                        Console.WriteLine($"Port {serialPort.PortName} successfuly closed.");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error closing {serialPort.PortName}: {ex.Message}");
                    }
                }
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
            }
        }

        private async Task OpenPort(string portName)
        {
            for (int i = 0; i <= 9; i++)
            {
                if (availablePorts[i].PortName == portName)
                {
                    try
                    {
                        availablePorts[i].Open();
                        await Task.Delay(100); // wait for the port to open
                        Console.WriteLine($"Port {portName} opened successfully.");
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"Error opening {portName}: {e.Message}");
                    }
                }
            }
        }

        private bool isValid(string input)
        {
            if (input == null || input.IsWhiteSpace()) return false;

            if (input.StartsWith("COM") || input.StartsWith("com"))
            {
                int startIndex = 3;
                int numOfPort;

                try 
                {
                    numOfPort = int.Parse(input.Substring(startIndex).Trim());
                }
                catch (Exception e)
                {
                    return false;
                }

                if (numOfPort >= 10 && numOfPort <= 19)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
