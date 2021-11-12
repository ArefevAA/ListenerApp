using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.IO;
using System.Xml;

namespace ListenerApp
{
    class Program
    {
        private static readonly string _listenerUrl = "http://127.0.0.1/";
        private static readonly string _listenerPauseSettingFile = $@"{Directory.GetCurrentDirectory()}\ListenerSettings.xml";
        static void Main(string[] args)
        {
            _ = Listen();
            Console.Read();
        }

        private static async Task Listen()
        {
            HttpListener listener = new HttpListener();
            listener.Prefixes.Add(_listenerUrl);

            try
            {
                listener.Start();
                Console.WriteLine("Waiting for a new connection...");

                while (true)
                {
                    // Localhost listening:
                    HttpListenerContext context = await listener.GetContextAsync();
                    HttpListenerRequest request = context.Request;
                    Console.WriteLine($"New request: {request.RawUrl}");

                    // Forming a new response:
                    Console.WriteLine("Forming a new response...");
                    string answerFile = GetAnswerFilePath(request.QueryString);
                    HttpListenerResponse response = context.Response;
                    response.ContentType = "text/xml";
                    byte[] myFileFromLocalMachine = GetFile(answerFile);
                    response.ContentLength64 = myFileFromLocalMachine.Length;

                    // Pause settings:
                    (int, int, int) pauseSettings = GetPauseSettings(_listenerPauseSettingFile);
                    int pauseValue = NetworkPauseSimulation(pauseSettings);
                    Console.WriteLine($"Network pause value = {pauseValue}");

                    // Simulation a pause or delay in network operation:
                    System.Threading.Thread.Sleep(pauseValue);

                    // Sending a new response:
                    Stream output = response.OutputStream;
                    output.Write(myFileFromLocalMachine, 0, myFileFromLocalMachine.Length);
                    Console.WriteLine($"A new response has been sent.\n");
                    output.Close();

                }
            }
            catch (Exception exception)
            {
                Console.WriteLine($"Error in Listen function: {exception.Message}");
            }
            finally
            {
                listener.Close();
            }
        }

        private static (int, int, int) GetPauseSettings(string listenerPauseSettingFile)
        {
            int frequencyOneFrom = 1;
            int durationFrom = 100;
            int durationTo = 1000;

            if (File.Exists(listenerPauseSettingFile))
            {
                XmlDocument xmlDocument = new XmlDocument();
                xmlDocument.Load(listenerPauseSettingFile);
                XmlElement xmlElement = xmlDocument.DocumentElement;
                if (xmlElement != null && xmlElement.HasChildNodes && xmlElement.ChildNodes.Count == 2)
                {
                    foreach (XmlNode settingName in xmlElement.ChildNodes)
                    {
                        if (settingName.Name == "frequency" && settingName.Attributes != null)
                        {
                            XmlNode frequencyOneFromNode = settingName.Attributes.GetNamedItem("oneFrom");
                            if (frequencyOneFromNode != null)
                                int.TryParse(frequencyOneFromNode.Value, out frequencyOneFrom);
                        }
                        else if (settingName.Name == "duration" && settingName.Attributes != null)
                        {
                            XmlNode durationFromNode = settingName.Attributes.GetNamedItem("from");
                            if (durationFromNode != null)
                                int.TryParse(durationFromNode.Value, out durationFrom);
                            XmlNode durationToNode = settingName.Attributes.GetNamedItem("to");
                            if (durationToNode != null)
                                int.TryParse(durationToNode.Value, out durationTo);
                        }
                    }
                }
            }
            return (frequencyOneFrom, durationFrom, durationTo);
        }

        private static int NetworkPauseSimulation((int frequencyOneFrom, int durationFrom, int durationTo) pauseSettings)
        {
            Random random = new Random();
            if (pauseSettings.frequencyOneFrom > 0 &&
                pauseSettings.durationFrom > 0 &&
                pauseSettings.durationTo > 0 &&
                random.Next(pauseSettings.frequencyOneFrom) == 0)
                return random.Next(pauseSettings.durationFrom, pauseSettings.durationTo);
            return 0;
        }

        private static byte[] GetFile(string file)
        {
            byte[] byteArray = null;

            using (FileStream fileStream = File.OpenRead(file))
            {
                byteArray = new byte[fileStream.Length];
                fileStream.Read(byteArray, 0, byteArray.Length);
                string tempString = System.Text.Encoding.Default.GetString(byteArray)
                    .Replace("ResponseDateValue", DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ssZ"));
                byteArray = System.Text.Encoding.Default.GetBytes(tempString);
            }

            return byteArray;
        }

        private static string GetAnswerFilePath(NameValueCollection keyCollection)
        {
            if (!keyCollection.AllKeys.Contains("verb")) 
                return $@"{Directory.GetCurrentDirectory()}\SiteContent\BadKey.xml";
            else
            {
                switch (keyCollection["verb"])
                {
                    case "Identify":
                        return $@"{Directory.GetCurrentDirectory()}\SiteContent\Identify.xml";
                    case "ListMetadataFormats":
                        return $@"{Directory.GetCurrentDirectory()}\SiteContent\ListMetadataFormats.xml";
                    case "ListSets":
                        return $@"{Directory.GetCurrentDirectory()}\SiteContent\ListSets.xml";
                    case "ListRecords":
                        if (!keyCollection.AllKeys.Contains("resumptionToken"))
                        {
                            if (keyCollection.AllKeys.Contains("metadataPrefix") && keyCollection["metadataPrefix"] == "oai_dc")
                            {
                                if (keyCollection.AllKeys.Contains("set"))
                                {
                                    switch (keyCollection["set"])
                                    {
                                        case "col_AF":
                                            return $@"{Directory.GetCurrentDirectory()}\SiteContent\col_AF.xml";
                                        default:
                                            return $@"{Directory.GetCurrentDirectory()}\SiteContent\BadSet.xml";
                                    }
                                }
                                return $@"{Directory.GetCurrentDirectory()}\SiteContent\ListRecordsWithoutSet.xml";
                            }
                            return $@"{Directory.GetCurrentDirectory()}\SiteContent\BadMetadataPrefix.xml";
                        }
                        else
                        {
                            switch (keyCollection["resumptionToken"])
                            {
                                case "101at392833266283162":
                                    return $@"{Directory.GetCurrentDirectory()}\SiteContent\col_AF\101at392833266283162.xml";
                                case "201at392833266283162":
                                    return $@"{Directory.GetCurrentDirectory()}\SiteContent\col_AF\201at392833266283162.xml";
                                case "301at392833266283162":
                                    return $@"{Directory.GetCurrentDirectory()}\SiteContent\col_AF\301at392833266283162.xml";
                                case "401at392833266283162":
                                    return $@"{Directory.GetCurrentDirectory()}\SiteContent\col_AF\401at392833266283162.xml";
                                case "501at392833266283162":
                                    return $@"{Directory.GetCurrentDirectory()}\SiteContent\col_AF\501at392833266283162.xml";
                                case "601at392833266283162":
                                    return $@"{Directory.GetCurrentDirectory()}\SiteContent\col_AF\601at392833266283162.xml";
                                case "701at392833266283162":
                                    return $@"{Directory.GetCurrentDirectory()}\SiteContent\col_AF\701at392833266283162.xml";
                                case "801at392833266283162":
                                    return $@"{Directory.GetCurrentDirectory()}\SiteContent\col_AF\801at392833266283162.xml";
                                case "901at392833266283162":
                                    return $@"{Directory.GetCurrentDirectory()}\SiteContent\col_AF\901at392833266283162.xml";
                                case "1001at392833266283162":
                                    return $@"{Directory.GetCurrentDirectory()}\SiteContent\col_AF\1001at392833266283162.xml";
                                case "1101at392833266283162":
                                    return $@"{Directory.GetCurrentDirectory()}\SiteContent\col_AF\1101at392833266283162.xml";
                                case "1201at392833266283162":
                                    return $@"{Directory.GetCurrentDirectory()}\SiteContent\col_AF\1201at392833266283162.xml";
                                case "1301at392833266283162":
                                    return $@"{Directory.GetCurrentDirectory()}\SiteContent\col_AF\1301at392833266283162.xml";
                                case "1401at392833266283162":
                                    return $@"{Directory.GetCurrentDirectory()}\SiteContent\col_AF\1401at392833266283162.xml";
                                case "1501at392833266283162":
                                    return $@"{Directory.GetCurrentDirectory()}\SiteContent\col_AF\1501at392833266283162.xml";
                                case "1601at392833266283162":
                                    return $@"{Directory.GetCurrentDirectory()}\SiteContent\col_AF\1601at392833266283162.xml";
                                case "1701at392833266283162":
                                    return $@"{Directory.GetCurrentDirectory()}\SiteContent\col_AF\1701at392833266283162.xml";
                                case "1801at392833266283162":
                                    return $@"{Directory.GetCurrentDirectory()}\SiteContent\col_AF\1801at392833266283162.xml";
                                case "1901at392833266283162":
                                    return $@"{Directory.GetCurrentDirectory()}\SiteContent\col_AF\1901at392833266283162.xml";
                                case "2001at392833266283162":
                                    return $@"{Directory.GetCurrentDirectory()}\SiteContent\col_AF\2001at392833266283162.xml";
                                case "2101at392833266283162":
                                    return $@"{Directory.GetCurrentDirectory()}\SiteContent\col_AF\2101at392833266283162.xml";
                                case "2201at392833266283162":
                                    return $@"{Directory.GetCurrentDirectory()}\SiteContent\col_AF\2201at392833266283162.xml";
                                case "2301at392833266283162":
                                    return $@"{Directory.GetCurrentDirectory()}\SiteContent\col_AF\2301at392833266283162.xml";
                                case "2401at392833266283162":
                                    return $@"{Directory.GetCurrentDirectory()}\SiteContent\col_AF\2401at392833266283162.xml";
                                case "2501at392833266283162":
                                    return $@"{Directory.GetCurrentDirectory()}\SiteContent\col_AF\2501at392833266283162.xml";
                                default:
                                    return $@"{Directory.GetCurrentDirectory()}\SiteContent\BadKey.xml";
                            }
                        }
                    default:
                        return $@"{Directory.GetCurrentDirectory()}\SiteContent\BadKey.xml";
                }
            }
        }
    }
}
