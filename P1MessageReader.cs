using System;
using Microsoft.SPOT;
using System.IO.Ports;
using SecretLabs.NETMF.Hardware.NetduinoPlus;
using System.Text;
using Microsoft.SPOT.Hardware;
using System.Threading;
using System.Collections;

namespace netduino_p1_logging
{
    public class P1MessageReader
    {

        private Thread serverThread = null;
        private SerialPort port = null;
        private StringBuilder sbMessage = null;

        #region Events

        public delegate void MessageReceivedDelegate(object sender, MessageReceivedEventArgs e);
        public event MessageReceivedDelegate MessageReceived;

        public class MessageReceivedEventArgs : EventArgs
        {
            public string RawMessage { get; set; }
            public P1Data Data { get; set; }
        }

        private void OnMessageReceived(MessageReceivedEventArgs e)
        {
            var handler = MessageReceived;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        #endregion

        #region Constructor

        public P1MessageReader()
        {
            serverThread = new Thread(Run);
            sbMessage = new StringBuilder();
        }

        #endregion

        #region Methods

        public void Start()
        {
            serverThread.Start();
        }

        private void Run()
        {
            port = new SerialPort(SerialPorts.COM2, 9600, Parity.Even, 7, StopBits.One);
            port.DataReceived += new SerialDataReceivedEventHandler(port_DataReceived);
            port.Open();

        }

        void port_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            int bytesToRead = port.BytesToRead;
            if (bytesToRead > 0)
            {
                byte[] buff = new byte[bytesToRead - 1];
                port.Read(buff, 0, bytesToRead - 1);
                for (int i = 0; i < bytesToRead - 1; i++)
                {
                    sbMessage.Append(Convert.ToChar(buff[i]));
                    if (buff[i] == 33)
                    {
                        try {
                            OnMessageReceived(new MessageReceivedEventArgs() { RawMessage = sbMessage.ToString(), Data = ParseData(sbMessage) });
                        } catch (Exception) { }
                        sbMessage.Clear();
                    }
                }
            }
        }

        P1Data ParseData(StringBuilder sb) {
            var data = new P1Data();
            data.LogMoment = DateTime.Now;
            
            var values = GetValuesFromMessage(sbMessage.Replace("(m3)\r\n", "").Replace("\r\n", "#").Replace("*kWh", "").Replace("*kW", "").Replace(")(", "@").ToString());            
            
            data.E1 = Double.Parse(((string[])values["1-0:1.8.1"])[0]);
            data.E2 = Double.Parse(((string[])values["1-0:1.8.2"])[0]);
            data.E1Retour = Double.Parse(((string[])values["1-0:2.8.1"])[0]);
            data.E2Retour = Double.Parse(((string[])values["1-0:2.8.2"])[0]);
            data.CurrentUsage = Double.Parse(((string[])values["1-0:1.7.0"])[0]);
            data.CurrentRetour = Double.Parse(((string[])values["1-0:2.7.0"])[0]);
            data.CurrentTariff = Double.Parse(((string[])values["0-0:96.14.0"])[0]);
            if (values.Contains("0-1:24.3.0")) {
                data.LastGasTransmit = ((string[])values["0-1:24.3.0"])[0];
                data.Gas = Double.Parse(((string[])values["0-1:24.3.0"])[5]);
            }
            return data;
        }

        Hashtable GetValuesFromMessage(string message) {
            Hashtable values = new Hashtable();
            
            var lines = message.Split('#');
            foreach (var line in lines) {
                if (line.IndexOf("(") > 0) {
                    var first = line.IndexOf("(");                   
                    string id = line.Substring(0, first);
                    string[] linevalues = line.Substring(first + 1, line.Length - first - 2).Split('@');                   
                    
                    if (!values.Contains(id))
                        values.Add(id, linevalues);
                }
            }

            return values;
        }


        #endregion


    }

    public class P1Data
    {
        public DateTime LogMoment { get; set; }
        public System.Double E1 { get; set; }
        public System.Double E2 { get; set; }
        public System.Double E1Retour { get; set; }
        public System.Double E2Retour { get; set; }
        public System.Double CurrentUsage { get; set; }
        public System.Double CurrentRetour { get; set; }
        public System.Double CurrentTariff { get; set; }
        public System.String LastGasTransmit { get; set; }
        public System.Double Gas { get; set; }
    }
}
