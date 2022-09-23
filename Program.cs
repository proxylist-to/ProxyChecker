using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Leaf.xNet;
using Colorful;
using Console = Colorful.Console;
using System.Drawing;
using System.IO;
using System.Net;
using System.Threading;

namespace ProxyList.to_Proxy_Checker
{
    internal class Program
    {
        static int threads = 100;
        static int timeout = 10000;
        static string type = "";
        static ProxyType proxyType = new ProxyType();

        static int cpm = 0;
        static int ccpm = 0;
        static string prxjudgeurl = "http://azenv.net/";
        static int good = 0;

        static Queue<string> proxies = new Queue<string>();

        static void Main(string[] args)
        {
            Console.Title = "ProxyList.to Proxy Checker";
            Console.WriteAscii("ProxyList.to", Color.Cyan);
            Console.WriteLine("", Color.White);


            bool noprxtype = true;
            while (noprxtype) {
                Console.Clear();
                Console.WriteAscii("ProxyList.to", Color.Cyan);
                Console.WriteLine("", Color.White);
                Console.WriteLine("[1] HTTP/s", Color.White);
                Console.WriteLine("[2] SOCKS4", Color.White);
                Console.WriteLine("[3] SOCKS5", Color.White);


                string prxtype = Console.ReadKey().KeyChar.ToString();
                if(prxtype == "1")
                {
                    type = "http";
                    proxyType = ProxyType.HTTP;
                    noprxtype = false;
                }
                if (prxtype == "2")
                {
                    type = "socks4";
                    proxyType = ProxyType.Socks4;
                    noprxtype = false;
                }
                if (prxtype == "3")
                {
                    type = "socks5";
                    proxyType = ProxyType.Socks5;
                    noprxtype = false;
                }
            }

            bool nothreads = true;
            while (nothreads)
            {
                Console.Clear();
                Console.WriteAscii("ProxyList.to", Color.Cyan);
                Console.WriteLine("", Color.White);
                Console.Write("Threads (Max 250): ", Color.White);
                string threadstr = Console.ReadLine();
                int thdinput = int.Parse(threadstr);
                if(thdinput <= 250)
                {
                    threads = thdinput;
                    nothreads = false;
                }
            }

            bool notimeout = true;
            while (notimeout)
            {
                Console.Clear();
                Console.WriteAscii("ProxyList.to", Color.Cyan);
                Console.WriteLine("", Color.White);
                Console.Write("Timeout (Min 1000): ", Color.White);
                string timeoutstr = Console.ReadLine();
                int toinput = int.Parse(timeoutstr);
                if (toinput >= 1000)
                {
                    timeout = toinput;
                    notimeout = false;
                }
            }

            importFromSite();

        }

        static void importFromSite()
        {
            Console.Clear();
            Console.WriteAscii("ProxyList.to", Color.Cyan);
            Console.WriteLine("", Color.White);
            string apikey = "";

            if (File.Exists("api.key"))
            {
                apikey = File.ReadAllText("api.key");
                Console.WriteLine("Loaded API Key: " + apikey + " from file.", Color.White);
            }
            else
            {
                bool noapi = true;
                while (noapi)
                {
                    Console.Clear();
                    Console.WriteAscii("ProxyList.to", Color.Cyan);
                    Console.WriteLine("");
                    Console.WriteLine("[1] Get an API Key now (Free)", Color.White);
                    Console.WriteLine("[2] Input your API Key", Color.White);


                    string srctype = Console.ReadKey().KeyChar.ToString();
                    if (srctype == "1")
                    {
                        noapi = false;
                        System.Diagnostics.Process.Start("https://proxylist.to/dashboard/");
                    }
                    if (srctype == "2")
                    {
                        noapi = false;
                    }
                }
                Console.Clear();
                Console.WriteAscii("ProxyList.to", Color.Cyan);
                Console.WriteLine("", Color.White);
                Console.Write("Your API Key: ", Color.White);
                string apikeystr = Console.ReadLine();
                File.WriteAllText("api.key", apikeystr);
                apikey = apikeystr;
            }

            string prxlist = new WebClient().DownloadString("https://api.proxylist.to/" + type + "?key=" + apikey);

            if (prxlist.Contains("ERROR"))
            {
                Console.WriteLine("  " + prxlist, Color.OrangeRed);
                File.Delete("api.key");
                Console.WriteLine("Press any key to exit.", Color.White);
                Console.ReadKey();
                Environment.Exit(420);
            }
            else
            {
                string[] splitprxlist = prxlist.Split(new string[] { "\n" }, StringSplitOptions.None);
                for(int i = 0; i < splitprxlist.Length; i++)
                {
                    proxies.Enqueue(splitprxlist[i]);
                }
                Console.Write("Loaded ", Color.White);
                Console.Write(proxies.Count.ToString(), Color.Cyan);
                Console.WriteLine(" Proxies from ProxyList.to.", Color.White);
            }
            StartChecking();
        }

        static string filetitle = "";

        static void StartChecking()
        {
            Console.WriteLine("Starting Checking process.", Color.White);
            Thread.Sleep(500);
            Console.Clear();
            Console.WriteAscii("ProxyList.to", Color.Cyan);
            Console.WriteLine("", Color.White);

            new Thread(CPMT).Start();

            string date = DateTime.Now.ToString("MM-dd-yyyy HH-mm-ss");

            filetitle = "./output/" + type.ToUpper() + "-" + date + ".txt";

            if (!Directory.Exists("output"))
            {
                Directory.CreateDirectory("output");
            }

            for(int i = 0; i < threads; i++)
            {
                new Thread(CheckThread).Start();
            }
        }

        static void CPMT()
        {
            while (true)
            {
                cpm = ccpm * 10;
                ccpm = 0;
                Console.Title = "ProxyList.to | Remaining: " + proxies.Count + " | Good: " + good + " | CPM: " + cpm;
                Thread.Sleep(6000);
            }
        }

        static ReaderWriterLock locker = new ReaderWriterLock();
        static void CheckThread()
        {
            HttpRequest request = new HttpRequest { KeepAlive = false };
            request.ConnectTimeout = timeout;
            request.KeepAliveTimeout = timeout;

            while (proxies.Count > 0)
            {
                try
                {
                    string currentprx = proxies.Dequeue();

                    try
                    {

                        request.Proxy = ProxyClient.Parse(proxyType, currentprx);


                        HttpResponse httpRes = request.Get(prxjudgeurl);
                        
                        if (httpRes.IsOK && httpRes.ToString().Contains("<h1>PHP Proxy Judge</h1>"))
                        {
                            Console.WriteLine(currentprx, Color.LimeGreen);
                            Interlocked.Increment(ref good);
                            Interlocked.Increment(ref ccpm);

                            try
                            {
                                locker.AcquireWriterLock(int.MaxValue);
                                File.AppendAllText(filetitle, currentprx + Environment.NewLine);
        }
                            finally
                            {
                                locker.ReleaseWriterLock();
                            }
                        }
                        else
                        {
                            Interlocked.Increment(ref ccpm);
                        }
                    }
                    catch (HttpException ex)
                    {
                        Interlocked.Increment(ref ccpm);
                    }

                }
                catch
                {

                }
            }
        }
    }
}
