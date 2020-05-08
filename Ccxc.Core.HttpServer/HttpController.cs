using System;

namespace Ccxc.Core.HttpServer
{
    public class HttpController
    {
    }

    /// <summary>
    /// HTTP Handler特性，继承HttpController方法并为其中作为接口的方法带上本特性
    /// </summary>
    public class HttpHandlerAttribute : Attribute
    {
        /// <summary>
        /// HTTP方法，如 GET POST PUT等
        /// </summary>
        public string Method { get; set; }

        /// <summary>
        /// 路径，必须以“/”开头
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// HTTP Handler特性，继承HttpController方法并为其中作为接口的方法带上本特性
        /// </summary>
        /// <param name="method">HTTP方法，如 GET POST PUT等</param>
        /// <param name="path">路径，必须以“/”开头</param>
        public HttpHandlerAttribute(string method, string path)
        {
            Method = method;
            Path = path;
        }
    }

    [Serializable]
    internal class HttpHandlerRegisterException : Exception
    {
        public HttpHandlerRegisterException()
        {
        }

        public HttpHandlerRegisterException(string message) : base(message)
        {
        }

        public HttpHandlerRegisterException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected HttpHandlerRegisterException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}