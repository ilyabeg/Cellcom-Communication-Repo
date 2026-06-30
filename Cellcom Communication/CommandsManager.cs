using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO.Ports;
using System.Text;

namespace Cellcom_Communication
{
    internal class CommandsManager
    {
        private static ConcurrentDictionary<string, int> users = new ConcurrentDictionary<string, int>(); // current cellcom users
        private static ConcurrentDictionary<string, int> calls = new ConcurrentDictionary<string, int>(); // current active calls by users
        // שינוי הרשימות למבנה נתונים שתומך במערכת multi-thread...
        // int value in the dictionaries is junk

        // new Task Completion Source object for 'freezing' other commands if MSG command is running
        private TaskCompletionSource msgTask;
        private static bool _msgRunning = false;

        public void DetermineCommand(SerialPort serialPort, string userID, string command)
        {
            bool validMsg = false;
            // seperate if statement for message command
            if (command.StartsWith("MSG"))
            {
                // determine the msg of the user
                validMsg = Message(serialPort, userID, command);
            }
            else if (!validMsg)
            {
                switch (command)
                {
                    case "JOIN":
                        JoinCellcom(serialPort, userID);
                        break;

                    case "NEW":
                        InitiateCall(serialPort, userID);
                        break;

                    case "STOP":
                        CloseCall(serialPort, userID);
                        break;

                    case "EXIT":
                        // safe exit
                        ExitCellcom(serialPort, userID);
                        break;

                    default:
                        serialPort.WriteLine(String.Format("[SERVER]: <{0}> : '{1}' | Command not found.", userID, command));
                        break;
                }
            }
        }

        private async Task JoinCellcom(SerialPort serialPort, string userID)
        {
            // using await for 10 second timer

            //if (users.ContainsKey(userID))
            //{
            //    serialPort.WriteLine($"[SERVER]: *Error* | User already joined.");
            //} else

            if (users.Count >= 10)
            {
                serialPort.WriteLine("[SERVER]: *Error* | Maximum capacity of users reached.");
            }
            else
            {   // if user already joined, it's ok to run command again
                if (users.ContainsKey(userID) || users.TryAdd(userID, 1)) // TryAdd() - מנסה להוסיף בתנאי שאין עוד תהליך שעושה זאת בו זמנית
                {
                    serialPort.WriteLine(String.Format("[SERVER]: <{0}> : JOINING CELLCOM ENTERPRISE...", userID));
                    for (int i = 1; i <= 10; i++)
                    {
                        // if MSG command running -> freeze join timer, and wait for msg to finish.
                        if (_msgRunning)
                            await Task.WhenAll(msgTask.Task);

                        serialPort.WriteLine(i + "");
                        await Task.Delay(500); // simulate some work being done...
                    }
                    serialPort.WriteLine(String.Format("[SERVER]: <{0}> : DONE", userID));
                }
                else
                {
                    serialPort.WriteLine($"[SERVER]: *Error* | Unable to add user <{userID}>.");
                }
            }
        }

        private async Task InitiateCall(SerialPort serialPort, string userID)
        {
            // method has to be async to allow for the delay in the while loop without blocking the main thread + 
            // allow user to close the call...

            if (users.ContainsKey(userID))
            {
                if (!calls.ContainsKey(userID))
                {
                    if (calls.TryAdd(userID, 1)) // TryAdd() - מנסה להוסיף בתנאי שאין עוד תהליך שעושה זאת בו זמנית
                    {
                        while (calls.ContainsKey(userID)) // current user is in a call
                        {
                            // if MSG command running -> freeze join timer, and wait for msg to finish.
                            if (_msgRunning)
                                await Task.WhenAll(msgTask.Task);

                            serialPort.WriteLine(String.Format("[SERVER]: <{0}> : CELLCOM", userID));
                            await Task.Delay(1000); // delay one second...
                        }
                    }
                    else
                    {
                        serialPort.WriteLine($"[SERVER]: *Error* | Unable to start call with user <{userID}>.");
                    }
                }
                else
                {
                    serialPort.WriteLine($"[SERVER]: *Error* | User: <{userID}> is already in a call.");
                }
            }
            else
            {
                serialPort.WriteLine("[SERVER]: *Error* | User must JOIN before initiating a call.");
            }
        }

        private void CloseCall(SerialPort serialPort, string userID)
        {
            if (users.ContainsKey(userID))
            {
                if (calls.ContainsKey(userID))
                {
                    // close current call
                    if (calls.TryRemove(userID, out _)) // Out _ = הפונקצייה מחייבת פלט כלשהו וזה דגל שאומר בעצם שהפלט סתמי ואינו חשוב
                    {
                        serialPort.WriteLine(String.Format("[SERVER]: <{0}> : BYE.", userID));
                    }
                    else
                    {
                        serialPort.WriteLine(String.Format("[SERVER]: *Error* | Unable to remove user <{0}> from a call.", userID));
                    }
                }
                else
                {
                    serialPort.WriteLine($"[SERVER]: *Error* | User: <{userID}> is currently not in a call.");
                }
            }
            else
            {
                serialPort.WriteLine("[SERVER]: *Error* | User must JOIN before stopping a call.");
            }
        }

        private void ExitCellcom(SerialPort serialPort, string userID)
        {
            if (users.TryRemove(userID, out _)) // Out _ = הפונקצייה מחייבת פלט כלשהו וזה דגל שאומר בעצם שהפלט סתמי ואינו חשוב
            {
                serialPort.WriteLine(String.Format("[SERVER]: User <{0}> has been removed.", userID));
            }
            else
            {
                serialPort.WriteLine(String.Format("[SERVER]: Error! unable to remove user <{0}>.", userID));
            }
            calls.TryRemove(userID, out _);
            //serialPort.Close();
        }

        private bool Message(SerialPort serialPort, string userID, string command)
        {
            if (users.ContainsKey(userID))
            {
                int start = 4; // message allways starts at index 4 (has to be space, otherwise the command can't be recognized)
                string msg;

                try
                {
                    msg = command.Substring(start);
                }
                catch (Exception)
                {
                    return false; // invalid!
                }

                if (msg == "")
                    return false; // invalid!

                try
                {   // remove last 3 characters (which are: 'space', 'com port number' that has to be 2 numbers)
                    msg = msg.Remove(msg.Length - 3);
                }
                catch (Exception)
                {
                    return false; // invalid!
                }

                if (msg == "" || msg.IsWhiteSpace())
                    return false; // invalid!

                // valid message >> print it out without interruptions
                Task.Run(async () => 
                {
                    msgTask = new TaskCompletionSource(); // create new Completion Source task

                    // הודעה חשובה: יוצרים משימה חדשה בכל פעם שמריצים את פקודת ההודעה כדי ששאר הפעולות יזהו
                    // שזו משימה שעוד לא התבצעה. בגלל שבכל פעם שזה מסתיים שאר הפעולות רואות שהמשימה מתה ולכן 
                    // הן יבצעו את הקוד בו זמנית - מה שלא טוב לנו, כי אנחנו רוצים להקפיא אותן כל עוד ההודעה לא סיימה

                    _msgRunning = true;

                    foreach (char letter in msg)
                    {
                        serialPort.WriteLine(letter + "");
                        await Task.Delay(500);
                    }

                    msgTask.SetResult();
                    _msgRunning = false;
                });

                return true; // valid! no need for switch/case
            }
            serialPort.WriteLine("[SERVER]: *Error* | User must JOIN before sending a message.");
            return false;
        }

        private void DisplayCommandsList(SerialPort serialPort)
        {
            // custom commands list
            serialPort.WriteLine(
                "\nList of all commands:" +
                "\r\n\t- <'user.id'> JOIN -> join our network" +
                "\r\n\t- <'user.id'> NEW  -> open a new call in our network (must join first)" +
                "\r\n\t- <'user.id'> STOP -> stop an ongoing call (call must be open)" +
                "\r\n\t- <'user.id'> EXIT -> exit the terminal" +
                "\r\n\r\nawaiting user input ...\r\n");
        }
    }
}
