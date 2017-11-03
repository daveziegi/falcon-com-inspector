﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;


namespace Falcon.Com
{
    public class UDPClientCom : UdpClient
    {
        List<Action<byte[]>> subsFuncs_ = new List<Action<byte[]>>();
        IPEndPoint endpoint_;
        bool isDead_ = true;

        public bool IsDead { get { return isDead_; } }


        public bool ConnectTo(string ip, int port)
        {
            isDead_ = false;
            try
            {
                Connect(ip, port);
            }
            catch (SocketException exp)
            {
                isDead_ = true;
            }
            var netAddr = new NetworkAdderss(ip, port);
            endpoint_ = new IPEndPoint(netAddr.IP, netAddr.Port);
            AsyncListen();
            return !isDead_;
        }

        private void AsyncListen()
        {
            if (!IsDead)
                BeginReceive(new AsyncCallback(OnIncomingBytes), null);
        }

        private void OnIncomingBytes(IAsyncResult res)
        {
            try
            {
                byte[] bytes = EndReceive(res, ref endpoint_);
                Publish(bytes);
                AsyncListen(); //keep listening
            }
            catch (SocketException exp)
            {
                //other side disconnected
            }
            catch (System.ObjectDisposedException exp)
            {
                //disconnect
            }
        }

        public void Publish(byte[] bytes)
        {
            foreach (var func in subsFuncs_)
                func(bytes);
        }

        public void Kill()
        {
            isDead_ = true;
            Close();
        }

        public void Subscribe(Action<byte[]> func)
        {
            subsFuncs_.Add(func);
        }

        public void Unsubscribe(Action<byte[]> func)
        {
            subsFuncs_.Remove(func);
        }

        public bool Send(byte[] bytes)
        {
            if (!IsDead)
            {
                Send(bytes, bytes.Length);
                return true;
            }
            return false;
        }
    }
}