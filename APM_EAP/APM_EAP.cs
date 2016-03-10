using System;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Nito.AsyncEx;

namespace APM_EAP
{
    class APM_EAP
    {
        static void Main(string[] args)
        { 
            AsyncContext.Run(() => MainAsync(args));
           
            Console.WriteLine("End");
            Console.ReadLine();
        }

        static async void MainAsync(string[] args)
        {
            //EAP Event-based Asynchronous Pattern (metodo + evento)
            DumpWebPage();

            //APM Asynchronous Programming Model (2 metodi Begin End)
            LookupHostName1();

            //Aasync/Await
            //await LookupHostName3();

            //var dump = await DumpWebPageAsync(new WebClient(), new Uri("http://www.elfo.net"));
            //Console.WriteLine(Regex.Match(dump, @"<title>(.*?)</title>"));

            //var ipArray = await LookupHostNameAsync("www.elfo.net");
            //Console.WriteLine(ipArray.First());
        }

        #region  EAP Event-based Asynchronous Pattern (metodo + evento)

        //il metodo termina con il suffisso Async e l'evento con Completed
        //nell'argomento dell'evento c'è il risultato dell'operazione

        //1. il chiamante si iscrive all'evento
        //2. il chiamante chiama il metodo async
        //3. il metodo viene eseguito in un thread separato
        //4. al termine dell'operazione viene sollevato l'evento e il chiamante avvertito 

        private static void DumpWebPage()
        {
            var uri = new Uri("http://www.elfo.net");
            var webClient = new WebClient();
            webClient.DownloadStringCompleted += OnDownloadStringCompleted;
            webClient.DownloadStringAsync(uri);
        }

        private static void OnDownloadStringCompleted(object sender, DownloadStringCompletedEventArgs eventArgs)
        {
            var dump = eventArgs.Result;
            Console.WriteLine(Regex.Match(dump, @"<title>(.*?)</title>"));
        }

        #endregion

        #region APM Asynchronous Programming Model (2 metodi Begin End)

        //si basa su IAsyncResult
        
        //1. il chiamante invoca il metodo Begin passando una callback
        //2. l'operazione avviene in un thread separato
        //3. al termine dell'operazione viene richiamata la callback
        //4. il chiamante esegue il metodo End che restituisce il risultato 

        private static void LookupHostName1()
        {
            Dns.BeginGetHostAddresses("www.elfo.net", OnHostNameResolved, null);
        }
        private static void OnHostNameResolved(IAsyncResult ar)
        {
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

        #region AsyncAwait

        static async Task LookupHostName3()
        {
            Console.WriteLine("LookupHostName4");
            await Task.Delay(TimeSpan.FromSeconds(2));
            var task = Dns.GetHostAddressesAsync("www.elfo.net");
            var result = await task;
            result.ToList().ForEach(Console.WriteLine);
        }

        #endregion

        #region Wrap EAP

        private static Task<string> DumpWebPageAsync(WebClient client, Uri uri)
        {
            //si utilizza TaskCompletionSource che gestisce un task figlio
            var tcs = new TaskCompletionSource<string>();

            DownloadStringCompletedEventHandler handler = null;

            handler = (sender, args) =>
            {
                client.DownloadStringCompleted -= handler;

                if (args.Cancelled)
                    tcs.TrySetCanceled();
                else if (args.Error != null)
                    tcs.TrySetException(args.Error);
                else
                    tcs.TrySetResult(args.Result);
            };

            client.DownloadStringCompleted += handler;
            client.DownloadStringAsync(uri);

            return tcs.Task;
        }

        #endregion

        #region Wrap APM

        private static Task<IPAddress[]> LookupHostNameAsync(string hostName)
        {
            //si utilizza uno degli overload di TaskFactory.FromAsync

            //come primo parametro si passa il metodo Begin
            //come secondo si passa il metodo End
            //si passano in ordine tutti i parametri che verrebbero passati al metodo begin
            //si passa null come state obejct
            return Task<IPAddress[]>.Factory.FromAsync(Dns.BeginGetHostAddresses, Dns.EndGetHostAddresses, hostName, null);
        }

        #endregion
    }
}
