using System;
using System.Linq;
using System.Net;

namespace APM_EAP
{
    class Program
    {
        static void Main(string[] args)
        {
            //DumpWebPage();
            LookupHostName2();
            Console.ReadLine();
        }

        //EAP Event-based Asynchronous Pattern (metodo + evento)

        private static void DumpWebPage()
        {
            Uri uri = new Uri("http://www.google.com");
            WebClient webClient = new WebClient();
            webClient.DownloadStringCompleted += OnDownloadStringCompleted;
            webClient.DownloadStringAsync(uri);
        }

        private static void OnDownloadStringCompleted(object sender, DownloadStringCompletedEventArgs eventArgs)
        {
            Console.WriteLine(eventArgs.Result);
        }

        //APM Asynchronous Programming Model (2 metodi Begin End)

        private static void LookupHostName1()
        {
            object obj = "ciao!";
            Dns.BeginGetHostAddresses("www.elfo.net", OnHostNameResolved, obj);
        }
        private static  void OnHostNameResolved(IAsyncResult ar)
        {
            object obj = ar.AsyncState;
            Dns.EndGetHostAddresses(ar).ToList().ForEach(Console.WriteLine);

        }

        private static void LookupHostName2()
        {
            object obj = "ciao!";
            //le variaibli vengono catturate
            //si perde però ogni possibilità gestire eventuali accezioni

            Dns.BeginGetHostAddresses("www.elfo.net", ar =>
            {
                Dns.EndGetHostAddresses(ar).ToList().ForEach(Console.WriteLine);
            }, 
            obj);
        }


        private static void LookupHostName3()
        {
            var task = Dns.GetHostAddressesAsync("www.elfo.net");
            task.ContinueWith(x =>
            {
                var result = x.Result;
                result.ToList().ForEach(Console.WriteLine);
            });
        }
    }
}
