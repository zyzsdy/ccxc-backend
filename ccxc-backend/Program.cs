using System;

namespace ccxc_backend
{
    class Program
    {
        static void Main(string[] args)
        {
            var startUp = new Startup();
            startUp.Run();
            startUp.Wait();
        }
    }
}
