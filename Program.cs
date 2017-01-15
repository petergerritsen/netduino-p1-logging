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

namespace netduino_p1_logging
{
    public class Program
    {
        private static IPEndPoint loggingEndpoint;
        private static InterruptPort s0Port;

        private const string configFilename = @"\sd\NetduinoP1.config";
        private const string valueCacheFilename = @"\sd\ValueCache.txt";

        private static string loggingHostName = "genergyloggingfunctions.azurewebsites.net";
        private static int loggingPortNumber = 80;
        private static string loggingPath = "POST /api/LogEntry?code=code HTTP/1.1";
        private static string apiKey = "apikey";
        private static int webserverPortnumber = 9080;
        private static int s0Counter = 0;

        public static void Main()
        {
            try
            {
                var timeSet = false;
                NTP.UpdateTimeFromNtpServer("pool.ntp.org", 1);

                ReadConfiguration();
                ReadValueCache(System.DateTime.Today.ToString("ddMMyyyy"));

                loggingEndpoint = HttpClient.GetIPEndPoint(loggingHostName, loggingPortNumber);

                s0Port = new InterruptPort(Pins.GPIO_PIN_D12, false, Port.ResistorMode.Disabled, Port.InterruptMode.InterruptEdgeLow);
                s0Port.OnInterrupt += new NativeEventHandler(S0PulseReceived);
                s0Port.EnableInterrupt();

                var messageReader = new P1MessageReader();
                messageReader.MessageReceived += new P1MessageReader.MessageReceivedDelegate(messageReader_MessageReceived);
                messageReader.Start();

                while (true)
                {
                    Thread.Sleep(60000);
                                        
                    // Resync time and s0Counter at 3 o'clock at night                  
                    if (!timeSet && System.DateTime.Now.Hour == 3)
                    {
                        timeSet = NTP.UpdateTimeFromNtpServer("pool.ntp.org", 1);
                        s0Counter = 0;
                    }
                    else if (timeSet && System.DateTime.Now.Hour > 3)
                    {
                        timeSet = false;
                    }

                    CacheValuesOnSd(System.DateTime.Today.ToString("ddMMyyyy"));
                }
            }
            catch (Exception ex)
            {
                Debug.Print(ex.ToString());
            }
        }

        static void S0PulseReceived(uint data1, uint data2, DateTime time)
        {
            s0Counter++;
            s0Port.ClearInterrupt();
        }

        private static void CacheValuesOnSd(string logdate)
        {
            new Thread(delegate
            {
                try
                {
                    var values = new Hashtable();
                    values.Add("logdate", logdate);
                    values.Add("s0Counter", s0Counter);

                    WriteKeyValueFile(valueCacheFilename, values);
                }
                catch (Exception ex)
                {
                    Debug.Print("ERROR in caching values: " + ex.StackTrace);
                }
            }).Start();
        }

        private static void ReadValueCache(string logdate)
        {
            try
            {
                var values = ReadKeyValueFile(valueCacheFilename);
                if (!values.Contains("logdate") || values["logdate"] as string != logdate)
                    return;
                if (values.Contains("s0Counter"))
                    s0Counter = int.Parse(values["s0Counter"] as string);
            }
            catch (Exception ex)
            {
                Debug.Print("ERROR in ReadValueCache: " + ex.StackTrace);
            }
        }

        private static void ReadConfiguration()
        {
            try
            {
                var values = ReadKeyValueFile(configFilename);

                if (values.Contains("loggingHostname"))
                    loggingHostName = values["loggingHostname"] as string;
                if (values.Contains("loggingPortNumber"))
                    loggingPortNumber = int.Parse(values["loggingPortNumber"] as string);
                if (values.Contains("apiKey"))
                    apiKey = values["apiKey"] as string;
                if (values.Contains("loggingPath"))
                    loggingPath = values["loggingPath"] as string;
                if (values.Contains("webServerPort"))
                    webserverPortnumber = int.Parse(values["webServerPort"] as string);

            }
            catch (Exception ex)
            {
                Debug.Print("ERROR in ReadConfiguration: " + ex.StackTrace);
            }
        }

        private static Hashtable ReadKeyValueFile(string keyValueFilename)
        {
            var values = new Hashtable();
            try
            {
                using (var sr = new StreamReader(keyValueFilename))
                {
                    while (true)
                    {
                        var line = sr.ReadLine();
                        if (line == null)
                            break;

                        var index = line.IndexOf("=");
                        var key = line.Substring(0, index);
                        var value = line.Substring(index + 1);

                        values.Add(key, value);
                    }
                }
            }
            catch (Exception)
            {
                // Do nothing, don't care, program for fallback if empty hashtable received
            }

            return values;
        }

        private static void WriteKeyValueFile(string keyValueFilename, Hashtable values)
        {
            using (var sw = new StreamWriter(keyValueFilename, false))
            {
                foreach (var key in values.Keys)
                {
                    sw.WriteLine(key + "=" + values[key]);
                }
            }
        }

        static void messageReader_MessageReceived(object sender, P1MessageReader.MessageReceivedEventArgs e)
        {
            new Thread(delegate
            {
                try
                {
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
                    new Thread(delegate()
                           {
                               try
                               {
                                   using (Socket connection = HttpClient.Connect(loggingEndpoint, 2000))
                                   {
                                       if (connection != null)
                                       {
                                           HttpClient.SendRequest(connection, loggingHostName, loggingPath, content.ToString());
                                       }
                                       else
                                       {
                                           Debug.Print("Unable to connect");
                                       }
                                   }
                               }
                               catch (Exception ex) {
                                   Debug.Print("Error in sending data: " + ex.StackTrace);
                               }
                           }).Start();
                }
                catch (Exception ex)
                {
                    Debug.Print("ERROR in posting data: " + ex.StackTrace);
                }
            }).Start();

            new Thread(delegate
            {
                try
                {
                    WriteToSdCard(e.Data);
                }
                catch (Exception ex)
                {
                    Debug.Print("ERROR in writing to SDCard: " + ex.StackTrace);
                }
            }).Start();
        }

        private static void WriteToSdCard(P1Data p1Data)
        {
            string path = @"\SD\" + DateTime.Now.ToString("yyyyMM");
            string logFile = path + @"\" + DateTime.Now.ToString("yyyyMMdd") + ".log";

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            if (!File.Exists(logFile))
            {
                using (var sw = new StreamWriter(logFile, true))
                {
                    sw.WriteLine("Timestamp;E1;E2;E1Retour;E2Retour;CurrentTariff;CurrentUsage;CurrentRetour;GasMeasurementMoment;GasMeasurementValue;PvProductionCounter");
                }
            }

            using (var sw = new StreamWriter(logFile, true))
            {
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



