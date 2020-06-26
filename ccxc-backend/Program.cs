using System;

namespace ccxc_backend
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var startUp = new Startup();
            startUp.Run();
            startUp.Wait();
        }
    }
}
