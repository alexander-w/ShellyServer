﻿using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.ServiceModel.Web;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ShellyServer
{
    class Program
    {
        static Stopwatch boilerUsage = new Stopwatch();
        static System.Timers.Timer switchTimer;
        static Dictionary<string, int> TRVPositions = new Dictionary<string, int>();


        static string myIP;
        static string switchIP;
        static string getIPBase() {
            return string.Join(".", myIP.Split('.').Take(3));
        }

        static void log(string text) {
            Console.Out.WriteLine(DateTime.Now + " " + text);
        }

        static void turnOff() {
            log("turning off boiler");
            new WebClient().DownloadString("http://"+ switchIP +"/relay/0?turn=off");
            boilerUsage.Stop();
        }

        static void turnon()
        {
            log("turning on boiler");
            new WebClient().DownloadString("http://" + switchIP + "/relay/0?turn=on");
            boilerUsage.Start();
        }

        static void showheatingtime() {
            log("boiler has been on for " + Math.Floor(TimeSpan.FromMilliseconds(boilerUsage.ElapsedMilliseconds).TotalHours) + " hours and " + TimeSpan.FromMilliseconds(boilerUsage.ElapsedMilliseconds).Minutes + " minutes");
        }

        static void Main(string[] args)
        {
            var json = File.ReadAllText("config.json");
            JObject jsobj = JsonConvert.DeserializeObject<JObject>(json);
            myIP = (string)jsobj["serverip"];
            switchIP = (string)jsobj["switchip"];

            string hostName = Dns.GetHostName();
            var ips = Dns.GetHostEntry(hostName).AddressList.Where(x => x.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork).Select(x => x.ToString());
            if (!ips.Contains(myIP))
            {
                Console.WriteLine("Discovered IPs: " + String.Join(", ", ips));
                Console.WriteLine("IP in textfile does not match any local IP. Exiting now");
                Console.ReadLine();
                Environment.Exit(3);
            }
            
            var host = new ServiceHost(typeof(WcfShellyService), new Uri[]{ });
            WebHttpBinding binding = new WebHttpBinding(WebHttpSecurityMode.None);
            ServiceEndpoint endpoint = new ServiceEndpoint(ContractDescription.GetContract(typeof(WcfShellyService)), binding, new EndpointAddress("http://localhost:8081/wcf"));
            WebHttpBehavior webBehaviour = new WebHttpBehavior();
            endpoint.EndpointBehaviors.Add(webBehaviour);
            host.AddServiceEndpoint(endpoint);
            host.Open();
            Console.WriteLine("wcf Service Started!!!");

            switchTimer = new System.Timers.Timer();
            switchTimer.Interval = 6 * 60 * 1000;
            switchTimer.Elapsed += SwitchTimer_Elapsed;
            switchTimer.AutoReset = true;
            switchTimer.Enabled = true;

            while (true) {
                Thread.Sleep(200);
            }

        }

        private static void SwitchTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            log("There were no successful requests in the last 6 minutes so ensure boiler is off");
            turnOff();
        }

        private static void ProcessSwitchRequest()
        {

            if (TRVPositions.Sum(x => x.Value) >= 100)
            {
                log("Sum of TRV positions is >= 100");
                turnon();
            }
            else
            {
                log("Sum of TRV positions is < 100");
                turnOff();
            }
            showheatingtime();
            //reset timer;
            switchTimer.Stop();
            switchTimer.Start();
        }

        [ServiceContract]
        public class WcfShellyService
        {
            [WebGet(BodyStyle = WebMessageBodyStyle.Bare, UriTemplate = "/trv/{iplast}")]
            [OperationContract]
            public void notifySwitch(string iplast) {
                var ip = getIPBase() + "." + iplast;
                log("boiler request turn ON from " + ip);
                try
                {
                    var json = new WebClient().DownloadString("http://" + ip + "/status");
                    JObject jsobj = JsonConvert.DeserializeObject<JObject>(json);
                    var valvepos = (int)jsobj["thermostats"][0]["pos"];

                    log("Valve pos: " + valvepos);

                    if (!TRVPositions.ContainsKey(iplast))
                    {
                        TRVPositions.Add(iplast, valvepos);
                    }
                    else {
                        TRVPositions[iplast] = valvepos;
                    }

                    ProcessSwitchRequest();
                }
                catch (Exception e){
                    log(e.ToString());
                }
            }

            [WebGet(BodyStyle = WebMessageBodyStyle.Bare, UriTemplate = "/trvoff/{iplast}")]
            [OperationContract]
            public void notifySwitchoff(string iplast)
            {
                var ip = getIPBase() + "." + iplast;
                log("boiler request turn OFF from " + ip);
                if (TRVPositions.ContainsKey(iplast))
                {
                    TRVPositions[iplast] = 0;
                }
                ProcessSwitchRequest();
                //reset timer
                switchTimer.Stop();
                switchTimer.Start();
            }

        }

    }
}
