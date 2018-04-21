using System;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Threading;
using System.IO;
using System.Linq;

namespace Chat_Server
{
    public class Server
    {
        public enum State
        {
            Stopped,
            Started
        }

        private static Server instance;

        private string ip;
        private int port;
        private int backLog;

        private Socket socket;
        private IPEndPoint ipEndPoint;
        private State state;

        private List<string> log;
        private List<Session> cache;
        private int cacheCounter;

        private Thread listenerThread;
        private Thread updaterThread;
        private Thread receiverThread;

        public Room Room = new Room(1);
        public static Server GetInstance()
        {
            return instance;
        }
        public string Ip
        {
            get
            {
                return ip;
            }
            set
            {
                ip = value;
            }
        }
        public int Port
        {
            get
            {
                return port;
            }
            set
            {
                port = value;
            }
        }
        public int BackLog
        {
            get
            {
                return backLog;
            }
            set
            {
                backLog = value;
            }
        }
        public List<string> Log
        {
            get
            {
                return log;
            }
        }
        public List<Session> GetSessions()
        {
            return cache;
        }
        public bool CheckNameAvailable(string name)
        {
            foreach (Session session in cache)
            {
                if (session.User.Name.ToLower() == name.ToLower())
                {
                    return false;
                }
            }
            return true;
        }
        public Server()
        {
            instance = this;
            log = new List<string>();
        }
        public void Start()
        {
            try
            {
                if (state == State.Started)
                {
                    return;
                }

                socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                ipEndPoint = new IPEndPoint(string.IsNullOrEmpty(ip) ? IPAddress.Any : IPAddress.Parse(ip), port);
                socket.Bind(ipEndPoint);
                socket.Listen(backLog);

                cache = new List<Session>();

                listenerThread = new Thread(Listener);
                listenerThread.Start();

                updaterThread = new Thread(Updater);
                updaterThread.Start();

                receiverThread = new Thread(Receiver);
                receiverThread.Start();

                state = State.Started;

                Console.WriteLine("Server was started at : " + DateTime.Now.ToString("hh:mm:ss"));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        public void Stop()
        {
            try
            {
                if (state == State.Stopped)
                {
                    return;
                }

                listenerThread.Abort();

                Session[] temp = cache.ToArray();

                foreach (Session session in temp)
                {
                    if (session.IsConnected)
                    {
                        session.Close();
                    }
                }

                updaterThread.Abort();
                receiverThread.Abort();

                do
                {
                    Thread.Sleep(100);
                }
                while (listenerThread.IsAlive && updaterThread.IsAlive && receiverThread.IsAlive);

                socket.Close();

                state = State.Stopped;

                Console.WriteLine("Server was closed at : " + DateTime.Now.ToString("hh:mm:ss"));
                log.Add("Server was closed at : " + DateTime.Now.ToString("hh:mm:ss"));

                StreamWriter streamWriter = new StreamWriter("./" + DateTime.Now + "log.txt", false);
                log.ForEach(a => streamWriter.WriteLine(a));
                streamWriter.Close();
                streamWriter.Dispose();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        private void Listener()
        {
            while (true)
            {
                try
                {
                    if (socket.Poll(0, SelectMode.SelectRead))
                    {
                        Socket currentSocket = socket.Accept();
                        DateTime currentTime = DateTime.Now;

                        Session session = new Session();
                        session.Socket = currentSocket;
                        session.TimeCreated = currentTime;
                        session.User = new User(cacheCounter);

                        cache.Add(session);
                        cacheCounter++;

                        Console.WriteLine("New session at : " + DateTime.Now.ToString("hh:mm:ss"));
                    }
                    else
                    {
                        Thread.Sleep(1000);
                    }
                }
                catch (ThreadAbortException ex)
                {
                    Console.WriteLine("Listener was closed at : " + DateTime.Now.ToString("hh:mm:ss"));
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }
        private void Updater()
        {
            while (true)
            {
                try
                {
                    Session[] temp = cache.ToArray();

                    foreach (Session session in temp)
                    {

                        if (!session.IsConnected)
                        {
                            cache.Remove(session);

                            Console.WriteLine("Close session at :" + DateTime.Now.ToString("hh:mm:ss"));
                        }

                    }

                    Thread.Sleep(1);
                }
                catch (ThreadAbortException ex)
                {
                    Console.WriteLine("Updater was closed at : " + DateTime.Now.ToString("hh:mm:ss"));
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }
        private void Receiver()
        {
            while (true)
            {
                try
                {
                    Session[] temp = cache.ToArray();

                    foreach (Session session in temp)
                    {

                        if (session.IsConnected)
                        {
                            session.Receive();
                        }

                    }

                    Thread.Sleep(1);
                }
                catch (ThreadAbortException ex)
                {
                    Console.WriteLine("Receiver was closed at : " + DateTime.Now.ToString("hh:mm:ss"));
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }
    }
    public class ProtocolWriter : BinaryWriter
    {
        private MemoryStream memoryStream;

        public ProtocolWriter() : base(new MemoryStream())
        {
            this.memoryStream = (MemoryStream)BaseStream;
        }

        public byte[] GetBuffer()
        {
            return memoryStream.GetBuffer();
        }

        public int GetLenght()
        {
            return (int)memoryStream.Length;
        }
    }
    public class ProtocolReader : BinaryReader
    {
        public ProtocolReader(byte[] buffer) : this(new MemoryStream(buffer))
        {

        }
        public ProtocolReader(MemoryStream stream) : base(stream)
        {

        }
    }
    public enum RoleType
    {
        None,
        Moderator,
        Developer
    }
    public class User
    {
        private int id;
        private string name;
        private RoleType roleType;
        private LevelType levelType;
        public User(int id)
        {
            this.id = id;
        }
        public int Id
        {
            get
            {
                return id;
            }
        }
        public string Name
        {
            get
            {
                return name;
            }
            set
            {
                name = value;
            }
        }
        public RoleType RoleType
        {
            get
            {
                return roleType;
            }
            set
            {
                roleType = value;
            }
        }
        public LevelType CurrentLevel
        {
            get
            {
                return levelType;
            }
        }
        public void SetLevel(LevelType levelType)
        {
            this.levelType = levelType;
        }
    }
    public class Session
    {
        private enum Request
        {
            Debug,
            CheckNameAvailability,
            ChooseName,
            Join,
            Leave,
            SendText,
            SendWhisper,
        }
        private enum Reply
        {
            Init,
            Debug,
            NameAvailable,
            NameNotAvailable,
            Name,
            Joined,
            Left,
            Community,
            Message,
            Reset
        }
        private enum UpdateType
        {
            Joined,
            Left
        }
        public enum MessageType
        {
            Text,
            Whisper,
            Moderator,
            System,
            Error
        }
        private enum WhisperType
        {
            From,
            To
        }

        private Socket socket;
        private DateTime timeCreated;
        private DateTime lastActivity;

        private User user;
        public Session()
        {

        }
        public bool IsConnected
        {
            get
            {
                if (socket != null)
                {
                    return socket.Connected;
                }

                return false;
            }
        }
        public Socket Socket
        {
            get
            {
                return socket;
            }
            set
            {
                socket = value;
            }
        }
        public DateTime TimeCreated
        {
            get
            {
                return timeCreated;
            }
            set
            {
                timeCreated = value;
            }
        }
        public DateTime LastActivity
        {
            get
            {
                return lastActivity;
            }
        }
        public User User
        {
            get
            {
                return user;
            }
            set
            {
                user = value;
            }
        }
        public void Receive()
        {
            try
            {
                if (socket.Poll(0, SelectMode.SelectRead))
                {
                    lastActivity = DateTime.Now;

                    byte[] test = new byte[1];

                    if (socket.Receive(test, SocketFlags.Peek) == 0)
                    {
                        Server.GetInstance().Room.Leave(user);

                        Close();
                    }
                    else
                    {
                        if (socket.Available > 4)
                        {
                            byte[] buffor = new byte[4];
                            socket.Receive(buffor, 0, buffor.Length, SocketFlags.None);
                            int packetLenght = BitConverter.ToInt32(buffor, 0);
                            byte[] buffor2 = new byte[packetLenght];
                            socket.Receive(buffor2, 0, buffor2.Length, SocketFlags.None);

                            OnParseMessage(new ProtocolReader(buffor2));

                        }
                        else
                        {
                            Server.GetInstance().Room.Leave(user);

                            Close();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error :" + ex.Message);
            }
        }
        public void Reset()
        {
            Server.GetInstance().Room.Leave(user);

            user.Name = "";
            user.RoleType = RoleType.None;

            ProtocolWriter pw = new ProtocolWriter();
            pw.Write((int)Reply.Reset);
            SendMessage(pw);
        }
        private void OnParseMessage(ProtocolReader protocolReader)
        {
            ProtocolWriter pw = null;

            Request request = (Request)protocolReader.ReadInt32();

            Console.WriteLine(request);

            switch (request)
            {
                case Request.Debug:                  
                        break;
                case Request.CheckNameAvailability:

                    string temp = protocolReader.ReadString();

                    bool isAvailable = Server.GetInstance().CheckNameAvailable(temp);

                    if(user.CurrentLevel == LevelType.Starter)
                    {

                        if (isAvailable)
                        {
                            pw = new ProtocolWriter();
                            pw.Write((int)Reply.NameAvailable);
                            pw.Write(true);

                            SendMessage(pw);
                        }
                        else
                        {
                            pw = new ProtocolWriter();
                            pw.Write((int)Reply.NameNotAvailable);
                            pw.Write(false);

                            SendMessage(pw);

                            pw = new ProtocolWriter();
                            pw.Write((int)Reply.Message);
                            pw.Write((int)MessageType.System);
                            pw.Write("The username is not available.");

                            SendMessage(pw);
                        }
                    }
                   
                    break;
                case Request.ChooseName:

                    string temp2 = protocolReader.ReadString();

                    bool canChoose = Server.GetInstance().CheckNameAvailable(temp2);

                    if (user.CurrentLevel == LevelType.Starter)
                    {
                        if (canChoose)
                        {
                            user.SetLevel(LevelType.Test);
                            user.Name = temp2;

                            pw = new ProtocolWriter();
                            pw.Write((int)Reply.Name);
                            pw.Write(temp2);

                            SendMessage(pw);
                        }
                        else
                        {
                            pw = new ProtocolWriter();
                            pw.Write((int)Reply.NameNotAvailable);
                            pw.Write(false);

                            SendMessage(pw);

                            pw = new ProtocolWriter();
                            pw.Write((int)Reply.Message);
                            pw.Write((int)MessageType.System);
                            pw.Write("The username is not available.");

                            SendMessage(pw);
                        }
                    }

                    break;
                case Request.Join:
                   
                    if(user.CurrentLevel != LevelType.Starter)
                    {
                        Server.GetInstance().Room.Join(user);

                        if(user.CurrentLevel == LevelType.Room)
                        {
                            pw = new ProtocolWriter();
                            pw.Write((int)Reply.Joined);
                            pw.Write(true);

                            SendMessage(pw);
                        }
                    }
                 
                    break;
                case Request.Leave:

                   if(user.CurrentLevel != LevelType.Starter)
                    {
                        Server.GetInstance().Room.Leave(user);
                    }

                    break;
                case Request.SendText:

                    if (user.CurrentLevel == LevelType.Room)
                    {
                        string text = protocolReader.ReadString();
                        string from = user.Name;

                        Server.GetInstance().Room.SendText(from, text);
                    }
                    else
                    {
                        pw = new ProtocolWriter();
                        pw.Write(5);
                        pw.Write(3);
                        pw.Write("To write a message you must join the room.");

                        SendMessage(pw);
                    }

                    break;
                case Request.SendWhisper:

                    if(user.CurrentLevel == LevelType.Room)
                    {
                        string from = user.Name;
                        string to = protocolReader.ReadString();
                        string text = protocolReader.ReadString();

                        Server.GetInstance().Room.SendWhisper(from, to, text);
                    }
                    else
                    {
                        pw = new ProtocolWriter();
                        pw.Write(5);
                        pw.Write(3);
                        pw.Write("To write a message you must join the room.");

                        SendMessage(pw);
                    }

                    break;
            }
        }
        public void SendMessage(ProtocolWriter protocolWriter)
        {
            try
            {
                int packetLenght = protocolWriter.GetLenght();
                byte[] buffer = new byte[4 + packetLenght];
                byte[] temp = BitConverter.GetBytes(packetLenght);
                Array.Copy(temp, 0, buffer, 0, temp.Length);
                Array.Copy(protocolWriter.GetBuffer(), 0, buffer, 4, protocolWriter.GetLenght());

                socket.Send(buffer, 0, buffer.Length, SocketFlags.None);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message.ToString());
            }
        }
        public void Close()
        {
            if (socket != null)
            {
                socket.Shutdown(SocketShutdown.Both);
                socket.Close();
                socket = null;

            }
        }
    }
    public enum LevelType
    {
        Starter,
        Test,
        Room
    }
    public class Room
    {
        private ProtocolWriter pw;
        private Dictionary<string, Session> users;
        private int maxUsers;
        public Room(int maxUsers)
        {
            this.maxUsers = maxUsers;
            users = new Dictionary<string, Session>();
        }
        public void Join(User user)
        {
            string key = user.Name;

            Session temp = Server.GetInstance().GetSessions().Find(x => x.User.Id == user.Id);

            if (!users.ContainsKey(key))
            {
                if (users.Count >= maxUsers)
                {
                    pw = new ProtocolWriter();
                    pw.Write(8);
                    pw.Write(3);
                    pw.Write("There are too many users in this room.");

                    temp.SendMessage(pw);
                }
                else
                {
                    user.SetLevel(LevelType.Room);
                    users.Add(user.Name, temp);
                }
            }
            else
            {
                pw = new ProtocolWriter();
                pw.Write(8);
                pw.Write(3);
                pw.Write("You are already in this room.");

                temp.SendMessage(pw);
            }
        } 
        public void Leave(User user)
        {
            string key = user.Name;

            if(users.ContainsKey(key))
            {
                user.SetLevel(LevelType.Starter);
                users.Remove(key);
            }
        }
        public void SendText(string from, string text)
        {
            pw = new ProtocolWriter();
            pw.Write(8);
            pw.Write(0);
            pw.Write(from);
            pw.Write(text);

            foreach(Session session in users.Values)
            {
                session.SendMessage(pw);
            }
        }
        public void SendWhisper(string from, string to, string text)
        {
            Session sender = null;
            Session recipient = null;

            if (users.TryGetValue(from, out sender))
            {
                if (from != to)
                {
                    if (users.TryGetValue(to, out recipient))
                    {
                        pw = new ProtocolWriter();
                        pw.Write(8);
                        pw.Write(1);
                        pw.Write(1);
                        pw.Write(from);
                        pw.Write(to);
                        pw.Write(text);

                        sender.SendMessage(pw);

                        pw = new ProtocolWriter();
                        pw.Write(8);
                        pw.Write(1);
                        pw.Write(0);
                        pw.Write(from);
                        pw.Write(text);

                        recipient.SendMessage(pw);
                    }
                    else
                    {
                        pw = new ProtocolWriter();
                        pw.Write(8);
                        pw.Write(3);
                        pw.Write("User : " + to + " is offline.");

                        sender.SendMessage(pw);
                    }
                }
                else
                {
                    pw = new ProtocolWriter();
                    pw.Write(8);
                    pw.Write(3);
                    pw.Write("You can not whisper to yourself.");

                    sender.SendMessage(pw);
                }
            }
            else
            {
                pw = new ProtocolWriter();
                pw.Write(8);
                pw.Write(3);
                pw.Write("To write a message you must join the room.");

                sender.SendMessage(pw);
            }
        }
        public void ModeratorSays(string from, string text)
        {
            pw = new ProtocolWriter();
            pw.Write(8);
            pw.Write(2);
            pw.Write(from);
            pw.Write(text);

            foreach (Session session in users.Values)
            {
                session.SendMessage(pw);
            }
        }
        public void SystemSays(string text)
        {
            pw = new ProtocolWriter();
            pw.Write(8);
            pw.Write(3);
            pw.Write(text);

            foreach (Session session in users.Values)
            {
                session.SendMessage(pw);
            }
        }
    }
}