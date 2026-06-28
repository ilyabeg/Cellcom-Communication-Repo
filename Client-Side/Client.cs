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
            string input = Console.ReadLine().Trim().ToUpper();

            if (!isValid(input))
            {
                do
                {
                    Console.WriteLine("Invalid input. Please enter a valid COM port (e.g. COM10, COM11, ... , COM19) >>");
                    input = Console.ReadLine().Trim().ToUpper();
                }
                while (!isValid(input));
            }

            SerialPort openPort = OpenPort(input);
            if (openPort == null)
            {
                Console.WriteLine("Couldn't find/open port. Exiting program...");
                return;
            }

            InputClientCommands(openPort);
        }

        private void InputClientCommands(SerialPort serialPort)
        {
            serialPort.WriteLine("#"); // random input to trigger event handler of the listening port to display commands for user
            string command;

            bool _continue = true;
            while (_continue)
            {
                command = Console.ReadLine();
                serialPort.Write(command);

                if (command == "EXIT") // if command is EXIT it is ok to send it first to close the listening port.
                {
                    try
                    {
                        serialPort.Close();
                        Console.WriteLine($"Port {serialPort.PortName} successfuly closed.");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error closing {serialPort.PortName}: {ex.Message}");
                    }
                    _continue = false;
                }

                try
                {
                    string response = serialPort.ReadLine();
                    Console.WriteLine(response);
                }
                catch (TimeoutException)
                {
                    Console.WriteLine("Timeout Error. Please try again.");
                    serialPort.DiscardInBuffer();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                    serialPort.DiscardInBuffer();
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

        private SerialPort OpenPort(string portName)
        {
            for (int i = 0; i <= 9; i++)
            {
                if (availablePorts[i].PortName == portName)
                {
                    try
                    {
                        availablePorts[i].Open();
                        Console.WriteLine($"Port {portName} opened successfully.");
                        return availablePorts[i];
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"*Critical Error* opening {portName} failed: {e.Message}");
                    }
                }
            }
            return null; // return null if the port was not found or could not be opened
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
