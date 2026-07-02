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

        // new change!!!: Concurrent dictionary for user messages
        private static ConcurrentDictionary<string, TaskCompletionSource> activeMessages = new ConcurrentDictionary<string, TaskCompletionSource>();

        // הודעה חשובה: יוצרים מילון שמקשר בין המשתמש להודעה שלו כיוון שכל המשתמשים ניגשים לשרת בכדי לשלוח הודעה
        // אך כשזה קורה, אם משתמש א' ישלח משהו בזמן שמשתמש ב' נמצא בשיחה למשל, השיחה של משתמש ב' תיעצר עד 
        // שהההודעה של משתמש א' לא תסיים להישלח. כדי למנוע את זה צריך "להקפיא" רק את הצד של מי ששלח ולכן השימוש במילון

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
                        // if MSG command running for current user -> freeze join timer, and wait for msg to finish.
                        if (activeMessages.TryGetValue(userID, out TaskCompletionSource msgTask))
                            await msgTask.Task;

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
                            // if MSG command running for current user -> freeze join timer, and wait for msg to finish.
                            if (activeMessages.TryGetValue(userID, out TaskCompletionSource msgTask))
                                await msgTask.Task;

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
                // user already sent message...
                if (activeMessages.ContainsKey(userID))
                {
                    serialPort.WriteLine($"[SERVER]: *Error* | User: <{userID}> already has an active message.");
                    return true;
                }

                // fixed!: start index is now at 3
                int start = 3; // message allways starts at index 3
                string msg;

                try
                {
                    msg = command.Substring(start).Trim(); // trim spaces if any exist
                }
                catch (Exception)
                {
                    return false; // invalid!
                }

                if (msg == "")
                    return false; // invalid!

                if (msg == "" || msg.IsWhiteSpace())
                    return false; // invalid!

                // valid message >> print it out without interruptions
                TaskCompletionSource tempTask = new TaskCompletionSource();
                activeMessages.TryAdd(userID, tempTask);

                Task.Run(async () => 
                {
                    foreach (char letter in msg)
                    {
                        serialPort.WriteLine(letter + "");
                        await Task.Delay(500);
                    }

                    tempTask.TrySetResult();
                    activeMessages.TryRemove(userID, out _);
                });

                return true; // valid! no need for switch/case
            }
            serialPort.WriteLine("[SERVER]: *Error* | User must JOIN before sending a message.");
            return false;
        }
    }
}
