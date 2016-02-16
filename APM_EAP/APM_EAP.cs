using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Nito.AsyncEx;

namespace APM_EAP
{
    class APM_EAP
    {
        static void Main(string[] args)
        {
            try
            {
                AsyncContext.Run(() => MainAsync(args));
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
            }

            Console.WriteLine("End");
            Console.ReadLine();
        }

        static async Task MainAsync(string[] args)
        {
            //EAP Event-based Asynchronous Pattern (metodo + evento)
            //DumpWebPage();

            //APM Asynchronous Programming Model (2 metodi Begin End)
            //LookupHostName1();
            //LookupHostName2();
            //LookupHostName3();
            //LookupHostName4();
            await LookupHostName5();
        }

        #region  EAP Event-based Asynchronous Pattern (metodo + evento)

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

        #endregion

        #region APM Asynchronous Programming Model (2 metodi Begin End)

        private static void LookupHostName1()
        {
            object obj = "ciao!";
            Dns.BeginGetHostAddresses("www.elfo.net", OnHostNameResolved, obj);
        }
        private static void OnHostNameResolved(IAsyncResult ar)
        {
            object obj = ar.AsyncState;
            Dns.EndGetHostAddresses(ar).ToList().ForEach(Console.WriteLine);
        }

        private static void LookupHostName2()
        {
            //le variaibli vengono catturate
            //si perde però ogni possibilità gestire eventuali accezioni

            Dns.BeginGetHostAddresses("www.elfo.net", ar =>
            {
                Dns.EndGetHostAddresses(ar).ToList().ForEach(Console.WriteLine);
            }, 
            null);
        }

        #endregion

        static void LookupHostName3()
        {
            Console.WriteLine("LookupHostName3");
            var task = Dns.GetHostAddressesAsync("www.elfo.net");
            task.ContinueWith(x =>
            {
                var result = x.Result;
                result.ToList().ForEach(Console.WriteLine);
            });
        }

        static async void LookupHostName4()
        {
            Console.WriteLine("LookupHostName4");
            await Task.Delay(TimeSpan.FromSeconds(2));
            var task = Dns.GetHostAddressesAsync("www.elfo.net");
            var result = await task;
            result.ToList().ForEach(Console.WriteLine);
        }

        static async Task LookupHostName5()
        {
            Console.WriteLine("LookupHostName5");
            await Task.Delay(TimeSpan.FromSeconds(2));
            var task = Dns.GetHostAddressesAsync("www.elfo.net");
            var result = await task;
            result.ToList().ForEach(Console.WriteLine);
        }
    }
}
