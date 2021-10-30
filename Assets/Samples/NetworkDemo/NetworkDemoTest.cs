using System;
using System.Collections;
using System.Text;
using Tofunaut.TofuECS.Network;
using UnityEngine;

namespace Tofunaut.TofuECS.Samples.NetworkDemo
{
    public class NetworkDemoTest : MonoBehaviour
    {
        private const int Port = 9050;
        private NetworkMember _networkMember;


        private IEnumerator Start()
        {
#if UNITY_SERVER
            _networkMember = new Server(Port, 1);
#else
            _networkMember = new Client(Port);
#endif
            _networkMember.Start();

            while (true)
            {
                yield return new WaitForSeconds(1);
                _networkMember.SendNextMessage(Encoding.UTF8.GetBytes("hello world"));

                var nextMessage = _networkMember.GetNextMessage();
                if (nextMessage.Length > 0)
                {
                    Debug.Log($"received message: {Encoding.UTF8.GetString(nextMessage)}");
                }
            }
        }

        private void OnDestroy()
        {
            _networkMember.Stop();
        }
    }
}