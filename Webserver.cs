//using System;
//using Microsoft.SPOT;
//using System.Threading;
//using System.Net.Sockets;
//using System.Net;
//using System.Text;

//namespace netduino_p1_logging {
//    public class WebServer : IDisposable {
//        private Thread serverThread = null;
//        private int portNumber;
//        private Socket socket = null;   

//        public WebServer(int portNumber) {
//            this.portNumber = portNumber;
//            serverThread = new Thread(StartServer);
//            serverThread.Priority = ThreadPriority.BelowNormal;
//        }

//        public void Start() {
//            serverThread.Start();
//        }

//        private void StartServer() {
//            //Initialize Socket class
//            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
//            //Request and bind to an IP from DHCP server
//            socket.Bind(new IPEndPoint(IPAddress.Any, portNumber));
//#if DEBUG            
//            //Debug print our IP address
//            Debug.Print(Microsoft.SPOT.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces()[0].IPAddress);
//#endif
//            //Start listen for web requests
//            socket.Listen(1);
//            ListenForRequest();
//        }

//        public void ListenForRequest() {
//            while (true) {
//                using (Socket clientSocket = socket.Accept()) {
//                    //Get clients IP
//                    IPEndPoint clientIP = clientSocket.RemoteEndPoint as IPEndPoint;
//                    EndPoint clientEndPoint = clientSocket.RemoteEndPoint;
                    
//                    int bytesReceived = clientSocket.Available;
//                    if (bytesReceived > 0) {
//                        //Get request
//                        byte[] buffer = new byte[bytesReceived];
//                        int byteCount = clientSocket.Receive(buffer, bytesReceived, SocketFlags.None);
//                        string request = new string(Encoding.UTF8.GetChars(buffer));
//#if DEBUG
//                        Debug.Print(request);
//#endif

//                        //Compose a response
//                        string response = "Hello World";
//                        string header = "HTTP/1.0 200 OK\r\nContent-Type: text; charset=utf-8\r\nContent-Length: " + response.Length.ToString() + "\r\nConnection: close\r\n\r\n";
//                        clientSocket.Send(Encoding.UTF8.GetBytes(header), header.Length, SocketFlags.None);
//                        clientSocket.Send(Encoding.UTF8.GetBytes(response), response.Length, SocketFlags.None);                        
//                    }
//                }
//            }
//        }

//        #region Events

//        public delegate void ConfigValueReceivedDelegate(object sender, ConfigValueReceivedEventArgs e);
//        public event ConfigValueReceivedDelegate ConfigValueReceived;

//        public class ConfigValueReceivedEventArgs : EventArgs {
//            public string key { get; set; }
//            public string value { get; set; }
//        }

//        private void OnConfigValueReceived(ConfigValueReceivedEventArgs e) {
//            var handler = ConfigValueReceived;
//            if (handler != null) {
//                handler(this, e);
//            }
//        }

//        #endregion

//        #region IDisposable Members
//        ~WebServer()
//        {
//            Dispose();
//        }
//        public void Dispose()
//        {
//            if (socket != null)
//                socket.Close();
//        }
//        #endregion
//    }
//}
