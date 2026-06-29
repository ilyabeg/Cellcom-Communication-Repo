using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO.Ports;
using System.Text;

namespace Cellcom_Communication
{
    internal class Server // Server class for handling all logic
    {
        private static SerialPort[] ports = new SerialPort[10]; // 10 global serial ports
        private static CommandsManager commandsManager = new CommandsManager(); // command manager object

        public void InitServer()
        {
            // initializing all listening ports with RS-232 protocol configuration:
            for (int i = 0; i <= 9; i++)
            {
                ports[i] = new SerialPort("COM2" + i);
                ports[i].BaudRate = 9600;
                ports[i].DataBits = 8;
                ports[i].Parity = Parity.None;
                ports[i].StopBits = StopBits.One;
                ports[i].Handshake = Handshake.None;
                ports[i].NewLine = "\r\n"; // forces return upon a new line.

                // read/write timeouts in milliseconds:
                ports[i].ReadTimeout = 500;
                ports[i].WriteTimeout = 500;

                // attach event handler for data received on each port...
                ports[i].DataReceived += ReadCommands;

                Console.WriteLine($"Com port {ports[i].PortName} has been successfuly initialized.");
            }
            Console.WriteLine();
        }

        public void OpenServer()
        {
            for (int i = 0; i <= 9; i++)
            {
                try
                {   // open each port for listening/writing...
                    ports[i].Open();
                    Console.WriteLine($"Com port {ports[i].PortName} has been successfuly opened.");
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Error openning {ports[i].PortName}: {e.Message}");
                }
            }
            Console.WriteLine("\nCellcom Communication System is running. Press any key to exit...\n");
            Console.ReadLine();
        }

        private void ReadCommands(object sender, SerialDataReceivedEventArgs e)
        {
            SerialPort serialPort = (SerialPort)sender; // current active port

            try
            {
                // important note: we read untill 'enter' is pressed, and not waiting for '\r' becuase of putty...
                string msg = serialPort.ReadTo("\r").Trim(); // get users message and trim whitespaces

                // get user id...
                int idStart = msg.IndexOf('<') + 1;
                int idEnd = msg.IndexOf('>');

                if (idStart != -1 && idEnd > idStart) // valid input
                {
                    string userID = msg.Substring(idStart, idEnd - idStart);
                    string command = msg.Substring(idEnd + 1).Trim().ToUpper();
                    Console.WriteLine($"Recieved command: '{command}' from <{userID}> on port {serialPort.PortName}.");

                    // determine which command has been read
                    commandsManager.DetermineCommand(serialPort, userID, command);
                }
                else
                {
                    serialPort.WriteLine("[SERVER]: Invalid message format. Expected: '<userID> command'");
                    //DisplayCommandsList(serialPort);
                }
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
    }
}