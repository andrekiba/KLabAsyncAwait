using System;
using System.Threading.Tasks;
using Nito.AsyncEx;
using TinyIoC;

namespace AsyncOOP
{
    class AsyncOOP
    {
        static void Main(string[] args)
        {
            //Init();
            AsyncContext.Run(Init);

            Console.WriteLine("End");

            Console.ReadLine();
        }


        private static async Task Init()
        {
            //Async Init
            await AsyncClass1.CreateAsync();

            //Async Init Dependecy Injection
            Bootstrap.Register();
            IAsyncClass2 instance2 = TinyIoCContainer.Current.Resolve<IAsyncClass2>();
            var instanceAsyncInit = instance2 as IAsyncInitialization;
            if (instanceAsyncInit != null)
                await instanceAsyncInit.Initialization;

            Console.WriteLine("Initialization End");
        }
    }

    #region Async Init

    class AsyncClass1
    {
        private AsyncClass1()
        {
        }

        private async Task<AsyncClass1> InitializeAsync()
        {
            await Task.Delay(TimeSpan.FromSeconds(3));
            return this;
        }

        public static Task<AsyncClass1> CreateAsync()
        {
            var result = new AsyncClass1();
            return result.InitializeAsync();
        }
    }

    #endregion

    #region Async Init Dependecy Injection

    public interface IAsyncClass2
    {
    }

    public interface IAsyncInitialization
    {
        Task Initialization { get; }
    }

    public class AsyncClass2 : IAsyncClass2, IAsyncInitialization
    {
        public Task Initialization { get; }

        public AsyncClass2()
        {
            Initialization = InitializeAsync();
        }

        private async Task InitializeAsync()
        {
            await Task.Delay(TimeSpan.FromSeconds(3));
        }
    }

    public static class Bootstrap
    {
        public static void Register()
        {
            TinyIoCContainer.Current.Register<IAsyncClass2, AsyncClass2>();
        }
    }

    #endregion
}
