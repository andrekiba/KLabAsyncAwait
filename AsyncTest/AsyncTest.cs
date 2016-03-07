using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Utility;

namespace AsyncTest
{
    [TestClass]
    public class AsyncTest
    {
        [TestMethod]
        public async Task ThePump()
        {
            var tokenSource = new CancellationTokenSource();
            tokenSource.CancelAfter(TimeSpan.FromSeconds(1));
            var token = tokenSource.Token;

            var pumpTask = Task.Run(async () =>
            {
                while (!token.IsCancellationRequested)
                {
                    #region Output

                    "Pumping...".Output();

                    #endregion

                    await HandleMessage().ConfigureAwait(false);
                }
            });

            await pumpTask.ConfigureAwait(false);

            tokenSource.Dispose();
        }

        static Task HandleMessage()
        {
            return Task.Delay(1000);
        }
    }
}