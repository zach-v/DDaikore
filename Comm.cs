using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Net;
using System.Net.Sockets;

namespace DDaikore
{
    public class Comm
    {
        private readonly ushort GameVersion, CoreVersion;
        public long lastFrameCounterReceivedViaComm { get; private set; } = 0;
        public bool IsConnected { get; private set; } = false;
        private Func<long> GetFrameCounter;
        private Action<long> SetFrameCounter;

        public long lastCommReceived;
        private readonly int port;
        private Socket connection = null;
        private int headerLength = 12; //Message length is first in the header, followed by the frame counter when the message was generated (nothing else is currently necessary)
        private List<byte[]> incomingMessages = new List<byte[]>(); //Incoming network messages. Should be one at a time (per connection)
        public PseudoRandom safeRnd = new PseudoRandom();
        private bool actedAsListener = false;
        private Thread commThread, commHostThread;
        public Mutex acceptingConnectionMutex = new Mutex(); //When this is locked, the game loop should freeze entirely

        private byte[] buffer = new byte[1024 * 1024]; //A megabyte buffer? Why not!

        /// <summary>
        /// Network connection made successfully. Occurs when the first message is received from the other side.
        /// Parameters are connected peer's CoreVersion and connected peer's GameVersion
        /// </summary>
        public Action<ushort, ushort> Connected;
        /// <summary>
        /// Message received over the network. This should contain game state changes, complete with frame counter, since the last message.
        /// </summary>
        public Action<byte[]> ReceiveMessage;

        /// <summary>
        /// Estimated number of frames for a round-trip message-response with the connected player (running average)
        /// </summary>
        public long estimatedPing { get; private set; } = 0;

        public Comm(ushort coreVersion, ushort gameVersion, Func<long> getFrameCounter, Action<long> setFrameCounter,
            Action<ushort, ushort> onConnect, Action<byte[]> receiveMessage, int port = 53252)
        {
            GameVersion = gameVersion;
            CoreVersion = coreVersion;
            GetFrameCounter = getFrameCounter;
            SetFrameCounter = setFrameCounter;
            ReceiveMessage = receiveMessage;
            Connected = onConnect;
            this.port = port;

            lastCommReceived = 0;

            commThread = new Thread(RunComms);
            commThread.Start();
        }

        //Note about random number generators: 
        //You never have to sync the seed if you generate a random number exactly once per frame and you allow both clients to calculate every frame.

        /// <summary>
        /// Network multiplayer-safe random number. Changes once per frame to keep clients in sync in case random numbers may be generated due to player behavior.
        /// </summary>
        public double RandomDouble()
        {
            return (double)safeRnd.lastValue / ((double)uint.MaxValue + 1);
        }

        /// <summary>
        /// Network multiplayer-safe random number. Changes once per frame to keep clients in sync in case random numbers may be generated due to player behavior.
        /// </summary>
        public int RandomInt(int maxValue)
        {
            return (int)(safeRnd.lastValue % maxValue); //Modulus hurts the uniform distribution, especially with large maxValue, but I'm not worried about it
        }

        /// <summary>
        /// When expecting another player to connect to you, call this. Creates a new thread.
        /// </summary>
        public void ListenForIncomingConnection()
        {
            if (GameVersion == 0) throw new Exception("Game version must be set for networking");
            commHostThread = new Thread(CommListen);
            commHostThread.Start();
        }

        public void StopListening()
        {
            if (commHostThread != null && commHostThread.IsAlive)
            {
                commHostThread.Abort();
                connection.Close();
                commHostThread = null;
            }
        }

        /// <summary>
        /// This must be the first message sent after connecting to a peer. Lock acceptingConnectionMutex around this call.
        /// </summary>
        private void SendInitialMessage()
        {
            //Send the initial message (Core version, game version, lastRandom but modified to avoid a sign error, and frameCount)
            var buffer = new byte[32];
            var tBytes = BitConverter.GetBytes((ushort)IPAddress.HostToNetworkOrder(CoreVersion));
            tBytes.CopyTo(buffer, 0);
            tBytes = BitConverter.GetBytes((ushort)IPAddress.HostToNetworkOrder(GameVersion));
            tBytes.CopyTo(buffer, 2);
            safeRnd.lastValue &= 0x7FFFFF7F; //Mask to avoid sign issues
            tBytes = BitConverter.GetBytes((int)IPAddress.HostToNetworkOrder(safeRnd.lastValue));
            tBytes.CopyTo(buffer, 4);
            tBytes = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(GetFrameCounter()));
            tBytes.CopyTo(buffer, 8);

            SendCommMessage(buffer);
        }

        private void CommListen()
        {
            var localEndPoint = new IPEndPoint(IPAddress.Any, port);
            connection = new Socket(IPAddress.Any.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            connection.Bind(localEndPoint);
            connection.Listen(1);
            actedAsListener = true;

            while (true)
            {
                Socket client;
                try
                {
                    lock (acceptingConnectionMutex)
                    {
                        client = connection.Accept();
                        connection.Close();
                        connection = client;
                        SendInitialMessage();
                    }
                    return;
                }
                catch { }
                Thread.Sleep(100);
            }
        }

        public void Connect(IPAddress ip)
        {
            if (connection != null) connection.Close();
            actedAsListener = false;
            IPEndPoint remoteEndPoint = new IPEndPoint(ip, port);
            connection = new Socket(ip.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            connection.ReceiveTimeout = 10000; //Timeout is 10 seconds
            //Connect on a new thread so we don't block the client (though we could just do it in RunComms)
            new Thread(() => {
                connection.Connect(remoteEndPoint);
            }).Start();
        }

        public void Disconnect()
        {
            //TODO: References to connection need to be critical sections.
            if (connection != null) connection.Close();
            IsConnected = false;
        }

        /// <summary>
        /// This method blocks until a complete message is received or an exception is thrown
        /// </summary>
        private void ReadComm()
        {
            var receivedBytes = 0;
            //Get the message header
            while (receivedBytes < headerLength)
            {
                receivedBytes += connection.Receive(buffer, receivedBytes, headerLength - receivedBytes, SocketFlags.None);
            }
            //Now we know what to expect; wait for the rest of it
            receivedBytes = 0;
            //Get the first 32 bits from the buffer as an int the endianness-safe way
            var bodyLength = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(buffer, 0));
            //Get the next 64 bits from the buffer as a long the endianness-safe way. We'll store it in lastReceivedCommFrameCounter when we lock on incomingMessages.
            var incomingFrameCounter = IPAddress.NetworkToHostOrder(BitConverter.ToInt64(buffer, 4));
            while (receivedBytes < bodyLength)
            {
                receivedBytes += connection.Receive(buffer, receivedBytes, bodyLength - receivedBytes, SocketFlags.None);
            }

            if (IsConnected) //Continuing connection -> expect a message controlled by the game, not by Core
            {
                var msgBuffer = new byte[bodyLength];
                Array.Copy(buffer, msgBuffer, bodyLength);
                lock (incomingMessages)
                {
                    incomingMessages.Add(msgBuffer);
                    lastCommReceived = GetFrameCounter();
                    estimatedPing = (estimatedPing * 4 + incomingFrameCounter - lastFrameCounterReceivedViaComm) / 5;
                    lastFrameCounterReceivedViaComm = incomingFrameCounter;
                }
            }
            else //The first message received is different than the others. It has to check the program version and get the random seed.
            {
                lock (acceptingConnectionMutex)
                {
                    //First two bytes are Core version number; next are the game version number.
                    var clientCoreVersion = (ushort)IPAddress.NetworkToHostOrder(BitConverter.ToUInt16(buffer, 0));
                    var clientGameVersion = (ushort)IPAddress.NetworkToHostOrder(BitConverter.ToUInt16(buffer, 2));
                    if (actedAsListener) //Only the peer that accepted the other one's connection request takes the other's remaining data
                    {
                        lock (incomingMessages)
                        {
                            //Next four bytes are lastRandom (has to have been masked to avoid an exception due to signed numbers)
                            safeRnd.lastValue = (uint)IPAddress.NetworkToHostOrder(BitConverter.ToInt32(buffer, 4));
                            //Next four bytes are the frame counter we're going to start at
                            SetFrameCounter(IPAddress.NetworkToHostOrder(BitConverter.ToInt64(buffer, 8)));
                            //Throw a version-number-only message on the queue to cause the user's game to start responding
                            incomingMessages.Add(buffer.Take(4).ToArray());
                            lastCommReceived = GetFrameCounter();
                            lastFrameCounterReceivedViaComm = lastCommReceived;
                        }
                    }
                    //Ready to play multiplayer!
                    estimatedPing = 100; //Start off with a guess
                    Connected(clientCoreVersion, clientGameVersion); //TODO: Shouldn't we do this on the game thread? Well, it locks the same mutex, so it's probably fine.
                }
            }
        }

        private void RunComms()
        {
            while (true)
            {
                //Have to lock this around references to connection
                lock (this) //TODO: Rethink. Is there a clean way to do it?
                {
                    if (connection != null && connection.Connected)
                    {
                        try
                        {
                            ReadComm();
                            IsConnected = true;
                        }
                        catch
                        {
                            //No additional code needed; we probably got disconnected. If so, isConnected will be set to false on the next loop iteration.
                        }
                        Thread.Sleep(1);
                    } else IsConnected = false;
                }
                if (!IsConnected) Thread.Sleep(10);
            }
        }

        //TODO: Maintain a queue of events that can cause the clients to desync (should just be player actions, such ash JustPressed or JustReleased, *and the results thereof*)
        //Example: player pressed Up; velocity is now this (not from queue, but current); position is now this (ditto); player health now 3; enemy health now 0
        //When you receive a message, differences between its recorded events and yours should be rectified and the solution sent with the next message.

        //The best possible way to work would be tracking player JustPressed and JustReleased messages and recalculating what the current state of the game should be if those actions had been taken.
        //But that's very hard to do with the way that games usually get developed and with the sheer amount of data that can change frame to frame.

        public void SendCommMessage(byte[] message)
        {
            //Prepend the message length and send it
            var buffer = new byte[message.Length + 4];
            var msgLen = BitConverter.GetBytes(IPAddress.HostToNetworkOrder((int)message.Length));
            msgLen.CopyTo(buffer, 0);
            var frameCounterBytes = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(GetFrameCounter()));
            frameCounterBytes.CopyTo(buffer, 4);
            message.CopyTo(buffer, 12); //TODO: This is just wasting time. We can just send the 12 header bytes first and send 'message' in a second call.
            connection.Send(buffer);
        }

        public void ReceiveMessages()
        {
            lock (incomingMessages)
            {
                //Note: because of the mutexes, even if you're using loopback, you can't receive responses caused by responding to these messages until the next call.
                while (incomingMessages.Count != 0)
                {
                    ReceiveMessage(incomingMessages.First());
                    incomingMessages.RemoveAt(0);
                }
            }
        }

        public void Abort()
        {
            commThread.Abort();
            if (commHostThread != null && commHostThread.IsAlive)
            {
                commHostThread.Abort();
                commHostThread.Join();
            }
            commThread.Join();
            IsConnected = false;
        }
    }
}
