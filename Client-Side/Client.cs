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

            Console.WriteLine("Select COM port >>");
            DisplayAvailablePorts();
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
                Console.WriteLine("Couldn't find/open port. Exiting program in 5 seconds...");
                for (int i = 5; i >= 1; i--)
                {
                    Console.Write(i + " ");
                    Thread.Sleep(1000);
                }
                return;
            }

            InputClientCommands(openPort);
        }

        private void InputClientCommands(SerialPort serialPort)
        {
            string command;

            Console.WriteLine("Type QUIT to close the program...\n");

            while (true)
            {
                command = Console.ReadLine();

                if (command != null && command.ToLower() == "quit")
                {
                    // safe exit...
                    serialPort.Close();
                    return;
                }

                serialPort.WriteLine(command);
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
            catch (TimeoutException ex)
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
                    catch (Exception e)
                    {
                        continue;
                    }
                    Console.WriteLine("\t- " + port.PortName);
                }
            }
        }
    }
}
