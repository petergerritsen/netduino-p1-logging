using System;
using Microsoft.SPOT;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading;

namespace netduino_p1_logging {
    public static class HttpClient {       
        
        public static bool TryConnect(this Socket s, EndPoint ep, int timeout) {
            bool connected = false;
            new Thread(delegate {
                    try {
                        // s.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.Linger, new byte[] { 0, 0, 0, 0 });                        
                        s.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.NoDelay, true);
                        s.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.KeepAlive, false);
                        s.SendTimeout = timeout;
                        s.ReceiveTimeout = timeout;
                        s.Connect(ep);

                        connected = true;
                    } catch { }
                }).Start();
            int checks = 10;
            while (checks-- > 0 && connected == false) Thread.Sleep(100);
            // if (connected == false) throw new Exception("Failed to connect");             
            return connected;
        }

        public static IPEndPoint GetIPEndPoint(string host, int port = 80) {
            // look up host’s domain name to find IP address(es)
            IPHostEntry hostEntry = Dns.GetHostEntry(host);
            
            // extract a returned address
            IPAddress hostAddress = hostEntry.AddressList[0];
            IPEndPoint remoteEndPoint = new IPEndPoint(hostAddress, port);

            return remoteEndPoint;
        }

        public static Socket Connect(IPEndPoint remoteEndPoint, int timeout) {          
            // connect!
            Debug.Print("connect...");

            var connection = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);            
            connection.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.NoDelay, true);
            connection.SendTimeout = timeout;
            connection.ReceiveTimeout = timeout;

            if (connection.TryConnect(remoteEndPoint, timeout))
                return connection;
            else
                return null;
        }

        public static void SendRequest(Socket s, string host, string request, string content) {
            byte[] contentBuffer = Encoding.UTF8.GetBytes(content);
            const string CRLF = "\r\n";

            var requestLine = request + CRLF;            
            var headers = requestLine + "Host: " + host + CRLF +
            "Content-Type: application/json" + CRLF +
            "Content-Length: " + contentBuffer.Length + CRLF + CRLF;            
            byte[] headersBuffer = Encoding.UTF8.GetBytes(headers);            
            s.Send(headersBuffer);
            s.Send(contentBuffer);
        }
    }
}
