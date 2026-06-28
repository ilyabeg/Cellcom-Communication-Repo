using Client_Side;
using System.Diagnostics;

namespace Client_Side
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var currentName = Process.GetCurrentProcess().ProcessName; // השם של התכנית הנוכחית
            Process[] currentRunningInstances = Process.GetProcessesByName(currentName); // מחזיר מערך של כמות התכניות שכרגע רצות - כלומר המשתמשים
            
            // limit 10 users
            if (currentRunningInstances.Length > 10)
            {
                Console.WriteLine("Max user limit reached.");
                return;
            }

            // run program if ok...
            Client client = new Client();
            client.Start();
        }
    }
}
