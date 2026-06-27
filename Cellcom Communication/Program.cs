using System;
using System.Threading.Tasks;
using System.IO.Ports;

namespace Cellcom_Communication
{
    internal class Program
    {
        static void Main(string[] args)
        { 
            //// initializing all listening ports with RS-232 protocol configuration:
            //for (int i = 0; i <= 9; i++)
            //{
            //    ports[i] = new SerialPort("COM2" + i);
            //    ports[i].BaudRate = 9600;
            //    ports[i].DataBits = 8;
            //    ports[i].Parity = Parity.None;
            //    ports[i].StopBits = StopBits.One;
            //    ports[i].Handshake = Handshake.None;
            //    ports[i].NewLine = "\r\n"; // forces return upon a new line.

            //    // read/write timeouts in milliseconds:
            //    ports[i].ReadTimeout = 500;
            //    ports[i].WriteTimeout = 500;

            //    // attach event handler for data received on each port...
            //    ports[i].DataReceived += ReadCommands;

            //    try
            //    {   // open each port for listening/writing...
            //        ports[i].Open();
            //        //DisplayIntroMessage(ports[i]);
            //    }
            //    catch (Exception e)
            //    {
            //        Console.WriteLine($"Error initializing {ports[i].PortName}: {e.Message}");
            //    }
            //}

            //Console.WriteLine("Cellcom Communication System is running. Press any key to exit...");
            //Console.ReadLine();
        }
    }
}