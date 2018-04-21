using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace Chat_Server
{   public class Maintenance
    {
        public enum State
        {
            Stopped,
            Started
        }

        public delegate void OnMessage(string message);
        public event OnMessage Log;
        private Server server;
        private State state;
        private Timer timer;      
        public void Init(Server server)
        {
            this.server = server;

            timer = new Timer();
            timer.Interval = 850;
            int remainingTime = 60;

            timer.Elapsed += delegate
            {
                if (remainingTime != 0)
                {
                    remainingTime--;

                    switch(remainingTime)
                    {
                        case 45:
                        case 30:
                        case 15:
                        case 10:
                        case 5:
                        case 4:
                        case 3:
                        case 2:
                        case 1:
                            Console.WriteLine(string.Format("Za {0} minut odbeda sie prace konserwacyjne !", remainingTime));
                            Log(string.Format("Za {0} minut odbeda sie prace konserwacyjne !", remainingTime));
                            break;
                        case 0:
                            server.Stop();
                            state = State.Stopped;
                            break;
                    }
                }else
                {
                    timer.Stop();
                }
            };

        }
        public void Start()
        {
            if(state == State.Started)
            {
                return;
            }

            state = State.Started;

            timer.Start();
        }
        public void Stop()
        {
            if(state == State.Stopped)
            {
                return;
            }

            state = State.Stopped;

            timer.Stop();
        }
    }
}
