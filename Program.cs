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
using System.Collections;

namespace netduino_p1_logging {
    public class Program {
        private static IPEndPoint loggingEndpoint;
        private static InterruptPort s0Port;
        private const string configFilename = @"\sd\NetduinoP1.config";
        private static string loggingHostName = "netduinop1logging.apphb.com";
        private static int loggingPortNumber = 80;
        private static string apiKey = "bWFpbEBwZXRlcmdlcnJpdHNlbi5ubA";
        private static int webserverPortnumber = 9080;

        private static int s0Counter = 0;

        public static void Main() {
            try {
                var timeSet = false;
                NTP.UpdateTimeFromNtpServer("pool.ntp.org", 1);

                ReadConfiguration();

                loggingEndpoint = HttpClient.GetIPEndPoint(loggingHostName, loggingPortNumber);

                s0Port = new InterruptPort(Pins.GPIO_PIN_D12, false, Port.ResistorMode.Disabled, Port.InterruptMode.InterruptEdgeLow);
                s0Port.OnInterrupt += new NativeEventHandler(s0PulseReceived);
                s0Port.EnableInterrupt();

                P1MessageReader messageReader = new P1MessageReader();
                messageReader.MessageReceived += new P1MessageReader.MessageReceivedDelegate(messageReader_MessageReceived);
                messageReader.Start();

                while (true) {
                    Thread.Sleep(10000);

                    // Resync time and s0Counter at 3 o'clock at night                  
                    if (!timeSet && System.DateTime.Now.Hour == 3) {
                        timeSet = NTP.UpdateTimeFromNtpServer("pool.ntp.org", 1);
                        s0Counter = 0;
                    } else if (timeSet && System.DateTime.Now.Hour > 3) {
                        timeSet = false;
                    }
                }
            } catch (Exception ex) {
                Debug.Print(ex.ToString());
            }
        }

        private static void ReadConfiguration() {
            try {
                Hashtable values = new Hashtable();
                using (StreamReader sr = new StreamReader(configFilename)) {
                    while (true) {
                        var line = sr.ReadLine();
                        if (line == null) 
                            break;
                        
                        var index = line.IndexOf("=");
                        var key = line.Substring(0, index);
                        var value = line.Substring(index + 1);

                        values.Add(key, value);
                    }
                }

                if (values.Contains("loggingHostname"))
                    loggingHostName = values["loggingHostname"] as string;
                if (values.Contains("apiKey"))
                    apiKey = values["apiKey"] as string;
                if (values.Contains("webServerPort"))
                    webserverPortnumber = int.Parse(values["webServerPort"] as string);
            } catch (Exception ex) {
                Debug.Print("ERROR in ReadConfiguration: " + ex.StackTrace);
            }
        }

        static void messageReader_MessageReceived(object sender, P1MessageReader.MessageReceivedEventArgs e) {
            new Thread(delegate {
                try {
                    StringBuilder content = new StringBuilder(512);
                    content.AppendLine("{");
                    content.AppendLine("\"ApiKey\": \"" + apiKey + "\",");
                    content.AppendLine("\"Timestamp\": \"" + e.Data.LogMoment.ToString("yyyy-MM-ddTHH:mm:ss") + "\",");
                    content.AppendLine("\"E1\": \"" + e.Data.E1.ToString() + "\",");
                    content.AppendLine("\"E2\": \"" + e.Data.E2.ToString() + "\",");
                    content.AppendLine("\"E1Retour\": \"" + e.Data.E1Retour.ToString() + "\",");
                    content.AppendLine("\"E2Retour\": \"" + e.Data.E2Retour.ToString() + "\",");
                    content.AppendLine("\"CurrentTariff\": \"" + e.Data.CurrentTariff.ToString() + "\",");
                    content.AppendLine("\"CurrentUsage\": \"" + e.Data.CurrentUsage.ToString() + "\",");
                    content.AppendLine("\"CurrentRetour\": \"" + e.Data.CurrentRetour.ToString() + "\",");
                    var gasValue = "";
                    if (e.Data.LastGasTransmit != null)
                        gasValue = e.Data.LastGasTransmit;
                    content.AppendLine("\"GasMeasurementMoment\": \"" + gasValue + "\",");
                    content.AppendLine("\"GasMeasurementValue\": \"" + e.Data.Gas.ToString() + "\",");
                    content.AppendLine("\"PvProductionCounter\": \"" + s0Counter.ToString() + "\"");
                    content.AppendLine("}");

                    // produce request
                    using (Socket connection = HttpClient.Connect(loggingEndpoint, 2000)) {
                        if (connection != null)
                            HttpClient.SendRequest(connection, loggingHostName, "POST /api/logentries HTTP/1.1", content.ToString());
                        else
                            Debug.Print("Unable to connect");
                    }
                } catch (Exception ex) {
                    Debug.Print("ERROR in posting data: " + ex.StackTrace);
                }
            }).Start();

            new Thread(delegate {
                try {
                    WriteToSdCard(e.Data);
                } catch (Exception ex) {
                    Debug.Print("ERROR in writing to SDCard: " + ex.StackTrace);
                }
            }).Start();
        }

        static void s0PulseReceived(uint data1, uint data2, DateTime time) {
            s0Counter++;
            s0Port.ClearInterrupt();
        }

        private static void WriteToSdCard(P1Data p1Data) {
            string path = @"\SD\" + DateTime.Now.ToString("yyyyMM");
            string logFile = path + @"\" + DateTime.Now.ToString("yyyyMMdd") + ".log";

            if (!Directory.Exists(path)) {
                Directory.CreateDirectory(path);
            }

            if (!File.Exists(logFile)) {
                using (var sw = new StreamWriter(logFile, true)) {
                    sw.WriteLine("Timestamp;E1;E2;E1Retour;E2Retour;CurrentTariff;CurrentUsage;CurrentRetour;GasMeasurementMoment;GasMeasurementValue;PvProductionCounter");
                }
            }

            using (var sw = new StreamWriter(logFile, true)) {
                var logLine = new StringBuilder(512);
                logLine.Append(p1Data.LogMoment.ToString("yyyy-MM-ddTHH:mm:ss") + ";");
                logLine.Append(p1Data.E1.ToString() + ";");
                logLine.Append(p1Data.E2.ToString() + ";");
                logLine.Append(p1Data.E1Retour.ToString() + ";");
                logLine.Append(p1Data.E2Retour.ToString() + ";");
                logLine.Append(p1Data.CurrentTariff.ToString() + ";");
                logLine.Append(p1Data.CurrentUsage.ToString() + ";");
                logLine.Append(p1Data.CurrentRetour.ToString() + ";");
                logLine.Append(p1Data.LastGasTransmit + ";");
                logLine.Append(p1Data.Gas.ToString() + ";");
                logLine.Append(s0Counter.ToString());

                sw.WriteLine(logLine.ToString());
            }
        }
    }
}



