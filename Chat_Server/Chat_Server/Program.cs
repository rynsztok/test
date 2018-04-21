using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chat_Server
{
    class Program
    {
        static void Main(string[] args)
        {
            Server server = new Server();

            //   server.Ip = Console.ReadLine();
            server.Port = 27050;
            server.BackLog = 10;
            server.Start();

            while (true)
            {
                string commandLine = Console.ReadLine();

                if (commandLine != "")
                {
                    if (commandLine.StartsWith("/"))
                    {
                        commandLine = commandLine.Substring(1, commandLine.Length - 1);
                        string[] array = commandLine.Split(' ');

                        switch (array[0])
                        {
                            case "reset_session":
                                Server.GetInstance().GetSessions().Find(x => x.User.Id == int.Parse(array[1])).Reset();
                                break;
                            case "send_text":
                                Server.GetInstance().Room.ModeratorSays("(Dev) Memento_Mori", string.Join(" ", array, 1, array.Length - 1));
                                break;
                            default:
                                Console.WriteLine("unknow command");
                                break;
                        }
                    }

                    commandLine = "";
                }
                else
                {
                    commandLine = "";
                }
            }
        }
    }
}
