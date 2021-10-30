using System.Linq;
using System.Text;
using System.Threading;
using LiteNetLib;

namespace Tofunaut.TofuECS.Network
{
    public class Client : NetworkMember
    {
        private readonly ILogService _log;
        private readonly int _port;
        private readonly EventBasedNetListener _listener;
        private readonly NetManager _netManager;
        private Thread _clientThread;
        
        public Client(int port) : base(1024)
        {
            _port = port;
            _listener = new EventBasedNetListener();
            _netManager = new NetManager(_listener);
        }

        public override void Start()
        {
            _clientThread = new Thread(ClientThread);
            _clientThread.Start();
        }

        public override void Stop()
        {
            _clientThread?.Abort();
            _netManager.Stop();
        }

        private void ClientThread()
        {
            _netManager.Start();
            _netManager.Connect("localhost", _port, "SECRET-KEY");
            _listener.NetworkReceiveEvent += OnNetworkReceiveEvent;
            
            lock (_receiveLock)
            {
                _recieveQueue.Enqueue(Encoding.UTF8.GetBytes("client started"));
            }
            
            while (true)
            {
                _netManager.PollEvents();
                Thread.Sleep(15);

                lock (_sendLock)
                {
                    while (_sendQueue.Count > 0)
                    {
                        var toSend = _sendQueue.Dequeue();
                        _netManager.SendToAll(toSend, DeliveryMethod.ReliableOrdered);
                    }
                }
            }
        }

        private void OnNetworkReceiveEvent(NetPeer peer, NetPacketReader reader, byte channel, DeliveryMethod deliverymethod)
        {
            lock (_receiveLock)
            {
                _recieveQueue.Enqueue( Encoding.UTF8.GetBytes(reader.GetString(100)));
            }
            reader.Recycle();
        }
    }
}