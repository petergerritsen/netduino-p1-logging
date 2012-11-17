using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;
using SecretLabs.NETMF.Hardware;
using SecretLabs.NETMF.Hardware.NetduinoPlus;

namespace netduino_p1_logging
{
    public class Program
    {
        public static void Main()
        {
            try
            {
                NTP.UpdateTimeFromNtpServer("pool.ntp.org", 1);                

                P1MessageReader messageReader = new P1MessageReader();

                messageReader.MessageReceived += new P1MessageReader.MessageReceivedDelegate(messageReader_MessageReceived);
                messageReader.Start();

                while (true) {
                    Thread.Sleep(500);
                }
            }
            catch (Exception ex)
            {
                Debug.Print(ex.ToString());
            }
        }

        static void messageReader_MessageReceived(object sender, P1MessageReader.MessageReceivedEventArgs e)
        {
            Debug.Print("New message received " + System.DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss"));
            Debug.Print(e.RawMessage);
            Debug.Print("==================================================================");            
        }
    }
}
