using System;
using System.Threading.Tasks;

namespace Ccxc.Core.HttpServer
{
    public class HttpHandler
    {
        public string Method { get; set; }
        public string Path { get; set; }
        public Func<Request, Response, Task> Handler { get; set; }
    }
}