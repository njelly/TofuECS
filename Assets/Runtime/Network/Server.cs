using System.Linq;
using System.Text;
using System.Threading;
using LiteNetLib;
using LiteNetLib.Utils;

namespace Tofunaut.TofuECS.Network
{
    public class Server : NetworkMember
    {
        private readonly ILogService _log;
        private readonly int _port;
        private readonly int _expectedClients;
        private readonly EventBasedNetListener _listener;
        private readonly NetManager _netManager;
        private Thread _serverThread;
        
        public Server(int port, int expectedClients) : base(1024)
        {
            _port = port;
            _expectedClients = expectedClients;
            _listener = new EventBasedNetListener();
            _netManager = new NetManager(_listener);
        }

        public override void Start()
        {
            _serverThread = new Thread(ServerThread);
            _serverThread.Start();
        }

        public override void Stop()
        {
            _serverThread?.Abort();
            _netManager.Stop();
        }

        private void ServerThread()
        {
            
            _netManager.Start(_port);
            
            _listener.ConnectionRequestEvent += OnConnectionRequestEvent;
            _listener.PeerConnectedEvent += OnPeerConnected;
            _listener.NetworkReceiveEvent += OnNetworkReceiveEvent;
            
            lock (_receiveLock)
            {
                _recieveQueue.Enqueue(Encoding.UTF8.GetBytes("server started"));
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

        private void OnPeerConnected(NetPeer peer)
        {
            lock (_receiveLock)
            {
                _recieveQueue.Enqueue(Encoding.UTF8.GetBytes("client connected"));
            }

            var nw = new NetDataWriter();
            nw.Put("hello from the server");
            peer.Send(nw, DeliveryMethod.ReliableOrdered);
        }

        private void OnConnectionRequestEvent(ConnectionRequest request)
        {
            lock (_receiveLock)
            {
                _recieveQueue.Enqueue(Encoding.UTF8.GetBytes("client attempted connection"));
            }
            
            if (_netManager.ConnectedPeersCount < _expectedClients)
                request.AcceptIfKey("SECRET-KEY");
            else
                request.Reject();
        }

        private void OnNetworkReceiveEvent(NetPeer peer, NetPacketReader reader, byte channel, DeliveryMethod deliverymethod)
        {
            lock (_receiveLock)
            {
                _recieveQueue.Enqueue(reader.GetBytesWithLength());
            }
            reader.Recycle();
        }
    }
}