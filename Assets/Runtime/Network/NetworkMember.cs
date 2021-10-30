using System;
using System.Collections.Generic;

namespace Tofunaut.TofuECS.Network
{
    public abstract class NetworkMember
    {
        protected readonly int _maxMessageSize;
        protected readonly object _receiveLock;
        protected readonly object _sendLock;
        protected readonly Queue<byte[]> _sendQueue;
        protected readonly Queue<byte[]> _recieveQueue;

        protected NetworkMember(int maxMessageSize)
        {
            _maxMessageSize = maxMessageSize;
            _receiveLock = new object();
            _sendLock = new object();
            _sendQueue = new Queue<byte[]>();
            _recieveQueue = new Queue<byte[]>();
        }
        
        public byte[] GetNextMessage()
        {
            lock (_receiveLock)
            {
                return _recieveQueue.Count <= 0 ? Array.Empty<byte>() : _recieveQueue.Dequeue();
            }
        }

        public void SendNextMessage(byte[] message)
        {
            if (message.Length > _maxMessageSize)
                throw new MessageTooLongException(message.Length, _maxMessageSize);
            
            lock (_sendLock)
            {
                _sendQueue.Enqueue(message);
            }
        }

        public abstract void Start();

        public abstract void Stop();
    }

    public class MessageTooLongException : Exception
    {
        public readonly int MaxSize;
        public readonly int AttemptedSize;
        public override string Message =>
            $"the requested message length was {AttemptedSize}, the maximum size is {MaxSize}";
        public MessageTooLongException(int maxSize, int attemptedSize)
        {
            MaxSize = maxSize;
            AttemptedSize = attemptedSize;
        }
    }
}