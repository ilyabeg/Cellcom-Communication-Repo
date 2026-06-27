using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Text;

namespace Cellcom_Communication
{
    internal class Server // Server class for handling all logic
    {
        static SerialPort[] ports = new SerialPort[10]; // 10 global serial ports
        static List<string> users = new List<string>(); // current cellcom users
        static List<string> calls = new List<string>(); // current active calls by users

        public void InitServer()
        {
            // list available serial com ports on pc...
            //Console.WriteLine("Available Ports:");
            //foreach (string portName in SerialPort.GetPortNames())
            //{
            //    Console.WriteLine("\t{0}", portName);
            //}

            //================================================

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
            Console.WriteLine("\nCellcom Communication System is running. Press any key to exit...");
            Console.ReadLine();
        }

        private void ReadCommands(object sender, SerialDataReceivedEventArgs e)
        {
            SerialPort serialPort = (SerialPort)sender; // current active port

            DisplayIntroMessage(serialPort); // display commands for an active user

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

                    // determine which command has been read
                    DetermineCommand(serialPort, userID, command);
                }
                else
                {
                    serialPort.WriteLine("Invalid message format. Expected: '<userID> command'");
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

        private void DetermineCommand(SerialPort serialPort, string userID, string command)
        {
            switch (command)
            {
                case "JOIN":
                    JoinCellcom(serialPort, userID, command);
                    break;

                case "NEW":
                    InitiateCall(serialPort, userID, command);
                    break;

                case "STOP":
                    CloseCall(serialPort, userID, command);
                    break;

                case "EXIT":
                    // safe exit
                    users.Remove(userID);
                    calls.Remove(userID);
                    serialPort.Close();
                    break;

                default:
                    serialPort.WriteLine(String.Format("<{0}> : '{1}' | Command not found.", userID, command));
                    break;
            }
        }

        private async Task JoinCellcom(SerialPort serialPort, string userID, string command)
        {
            // using await for 10 second timer
            if (users.Contains(userID))
            {
                serialPort.WriteLine($"<{userID}> already joined.");
            }
            else if (users.Count >= 10)
            {
                serialPort.WriteLine("*Error* | Maximum capacity of users reached.");
            }
            else
            {
                users.Add(userID);
                serialPort.WriteLine(String.Format("<{0}> : {1} : JOINING CELLCOM ENTERPRISE...", userID, command));
                for (int i = 1; i <= 10; i++)
                {
                    serialPort.Write(i + " ");
                    await Task.Delay(500); // simulate some work being done...
                }
                serialPort.WriteLine(String.Format("\r\n<{0}> : DONE", userID));
            }
        }

        private async Task InitiateCall(SerialPort serialPort, string userID, string command)
        {
            // method has to be async to allow for the delay in the while loop without blocking the main thread + 
            // allow user to close the call...

            if (users.Contains(userID))
            {
                if (!calls.Contains(userID))
                {
                    calls.Add(userID); // current user is in a call
                    while (calls.Contains(userID))
                    {
                        serialPort.WriteLine(String.Format("<{0}> : CELLCOM", userID));
                        await Task.Delay(1000); // delay one second...
                    }
                }
                else
                {
                    serialPort.WriteLine($"*Error* | user: <{userID}> is already in a call.");
                }
            }
            else
            {
                serialPort.WriteLine("*Error* | User must JOIN before initiating a call.");
            }
        }

        private void CloseCall(SerialPort serialPort, string userID, string command)
        {
            if (users.Contains(userID))
            {
                if (calls.Contains(userID))
                {
                    serialPort.WriteLine(String.Format("<{0}> : BYE.", userID));
                    calls.Remove(userID); // close current call
                }
                else
                {
                    serialPort.WriteLine($"*Error* | user: <{userID}> is currently not in a call.");
                }
            }
            else
            {
                serialPort.WriteLine("*Error* | User must JOIN before stopping a call.");
            }
        }

        private void DisplayIntroMessage(SerialPort serialPort)
        {
            // custom commands list
            serialPort.WriteLine("=======================" +
                "\r\nWelcome to the Terminal" +
                "\r\n=======================" +
                "\r\n\r\nList of all commands:" +
                "\r\n\t* <'user.id'> JOIN -> join our network" +
                "\r\n\t* <'user.id'> NEW  -> open a new call in our network (must join first)" +
                "\r\n\t* <'user.id'> STOP -> stop an ongoing call (call must be open)" +
                "\r\n\t* <'user.id'> EXIT -> exit the terminal" +
                "\r\n\r\nawaiting user input ...\r\n\r\n");
        }
    }
}
