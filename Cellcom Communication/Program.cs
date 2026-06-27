using System;

namespace Cellcom_Communication
{
    internal class Program
    {
        static void Main(string[] args)
        { 
            // initialize and execute server
            Server server = new Server();
            server.InitServer();
            server.OpenServer();
        }
    }
}