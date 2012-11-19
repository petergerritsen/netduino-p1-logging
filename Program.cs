using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;
using SecretLabs.NETMF.Hardware;
using SecretLabs.NETMF.Hardware.NetduinoPlus;
using System.Text;
using System.IO;

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
            try {

                StringBuilder content = new StringBuilder(2048);
                content.AppendLine("{");
                content.AppendLine("\"ApiKey\": \"bWFpbEBwZXRlcmdlcnJpdHNlbi5ubA\",");
                content.AppendLine("\"Timestamp\": \"" + e.Data.LogMoment.ToString("yyyy-MM-ddTHH:mm:ss") + "\",");
                content.AppendLine("\"E1\": \"" + e.Data.E1.ToString() + "\",");
                content.AppendLine("\"E2\": \"" + e.Data.E2.ToString() + "\",");
                content.AppendLine("\"E1Retour\": \"" + e.Data.E1Retour.ToString() + "\",");
                content.AppendLine("\"E2Retour\": \"" + e.Data.E2Retour.ToString() + "\",");
                content.AppendLine("\"CurrentTariff\": \"" + e.Data.CurrentTariff.ToString() + "\",");
                content.AppendLine("\"CurrentUsage\": \"" + e.Data.CurrentUsage.ToString() + "\",");
                content.AppendLine("\"CurrentRetour\": \"" + e.Data.CurrentRetour.ToString() + "\",");
                content.AppendLine("\"GasMeasurementMoment\": \"" + e.Data.LastGasTransmit.ToString("yyyy-MM-ddTHH:mm:ss") + "\",");
                content.AppendLine("\"GasMeasurementValue\": \"" + e.Data.Gas.ToString() + "\"");
                content.AppendLine("}");
                var address = new IPEndPoint(IPAddress.Parse("10.0.0.8"), 50222);

                // produce request
                using (Socket connection = HttpClient.Connect(address, 1000)) {
                    if (connection != null)
                        HttpClient.SendRequest(connection, "10.0.0.8:50222", "POST /api/logentries HTTP/1.1", content.ToString());
                }
            } catch (Exception) { }

            Debug.Print("New message received " + System.DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss"));
            Debug.Print(e.RawMessage);
            Debug.Print("==================================================================");            
        }
    }
}
