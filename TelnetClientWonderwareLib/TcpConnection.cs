using System;
using System.Net;
using System.Net.Sockets;

namespace TelnetClientWonderwareLib
{
    internal class TcpConnection
    {
        private IPAddress _ipServerAddress;
        private bool _ipAddressSet;
        private int _ipServerPort;
        private bool _ipPortSet;
        private bool IsIpPortAndAddressSet => _ipAddressSet && _ipPortSet;
        private TcpClient _client;

        public TcpConnection(string ipAddress, int ipPort)
        {
            SetServerIpAddress(ipAddress);
            SetServerIpPort(ipPort);
        }

        public int Available => _client.Available;

        public bool Connected => _client.Connected;

        public int SetServerIpAddress(string ipAddress)
        {
            if (IPAddress.TryParse(ipAddress, out IPAddress ip))
            {
                _ipServerAddress = ip;
                // Ip address Set
                _ipAddressSet = true;
                return 0;
            }
            else
            {
                throw new Exception("Ip address setting fault");
            }
        }

        public int SetServerIpPort(int ipPort)
        {
            if ((ipPort >= 0) && (ipPort <= 65535))
            {
                _ipServerPort = ipPort;
                // Ip Port set
                _ipPortSet = true;
                return 0;
            }
            else
            {
                throw new Exception("Ip Port setting fault");
            }
        }

        public void Connect()
        {
            if (IsIpPortAndAddressSet)
            {
                _client = new TcpClient();
                _client.Connect(_ipServerAddress, _ipServerPort);
            }
        }

        public int Disconnect()
        {
            try
            {
                if (_client != null)
                {
                    if (_client.Connected)
                    {
                        _client.Close();
                        return 0;
                    }
                    else
                    {
                        // Client was not connected
                        return 1;
                    }
                }
                else
                {
                    // Connection client not created
                    return 2;
                }
            }
            catch
            {
                // Disconnection failure
                return 3;
            }
        }

        public void WriteData(byte[] byteArray)
        {
            NetworkStream ns = _client.GetStream();
            ns.Write(byteArray, 0, byteArray.Length);
        }

        public void WriteByte(byte b)
        {
            NetworkStream ns = _client.GetStream();
            ns.WriteByte(b);
        }

        public byte[] ReadData()
        {
            NetworkStream ns = _client.GetStream();
            // Receive the TcpServer.response.
            // Buffer to store the response bytes.
            byte[] data = new byte[256];

            int bytes = ns.Read(data, 0, data.Length);
            byte[] dataReceived = new byte[bytes];
            Array.Copy(data, dataReceived, bytes);

            return dataReceived;
        }

        public int ReadByte()
        {
            NetworkStream ns = _client.GetStream();
            return ns.ReadByte();
        }
    }
}
