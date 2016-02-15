using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

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
        private static void OnHostNameResolved(IAsyncResult ar)
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

        private static async void LookupHostName4()
        {
            var task = Dns.GetHostAddressesAsync("www.elfo.net");
            var result = await task;
            result.ToList().ForEach(Console.WriteLine);
        }

        //Wrap EAP

        private static Task<string> DumpWebPageAsync(WebClient client, Uri uri)
        {
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

        //wrap APM

        private static Task<IPAddress[]> LookupHostNameAsync(string hostName)
        {
            //si utilizza uno degli overload di TaskFactory.FromAsync

            //come prima parametro si passa il metodo Begin
            //come secondo si passa il metodo End
            //si passano in ordine tutti i parametri che verrebbero passati al metodo begin
            //si passa null come state obejct
            return Task<IPAddress[]>.Factory.FromAsync(Dns.BeginGetHostAddresses, Dns.EndGetHostAddresses, hostName, null);

        }
    }
}
