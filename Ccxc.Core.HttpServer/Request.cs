using Ccxc.Core.Utils.ExtensionFunctions;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Text;

namespace Ccxc.Core.HttpServer
{
    public class Request
    {
        public HttpRequest RawRequest { get; private set; }
        private dynamic pathParam;
        public dynamic Params => pathParam ?? new ExpandoObject();

        private dynamic query;
        public dynamic Query => query ?? (query = BuildDynamicQuery());

        private dynamic header;
        public dynamic Header => header ?? (header = BuildDynamicHeader());

        public IDictionary<object, object> ContextItems { get; set; }

        private string bodyString;
        public string BodyString
        {
            get
            {
                if (!string.IsNullOrEmpty(bodyString)) return bodyString;
                using (var readStream = new StreamReader(RawRequest.Body, Encoding.UTF8))
                {
                    bodyString = readStream.ReadToEnd();
                }

                return bodyString;
            }
        }

        private dynamic form;
        public dynamic Form => form ?? (form = BuildDynamicPostForm());

        public Request(HttpRequest request, dynamic pathParam = null)
        {
            RawRequest = request;
            this.pathParam = pathParam;
        }

        public T Json<T>()
        {
            if (string.IsNullOrEmpty(BodyString)) return default;
            var jsonObject = JsonConvert.DeserializeObject<T>(BodyString);

            return jsonObject;
        }

        private dynamic BuildDynamicQuery()
        {
            var queryDict = new Dictionary<string, string>();
            foreach (var kv in RawRequest.Query)
            {
                if (!queryDict.ContainsKey(kv.Key))
                {
                    queryDict.Add(kv.Key, kv.Value);
                }
            }
            return queryDict.ToDynamic();
        }

        private dynamic BuildDynamicHeader()
        {
            var headerDict = new Dictionary<string, string>();
            foreach (var kv in RawRequest.Headers)
            {
                if (!headerDict.ContainsKey(kv.Key))
                {
                    headerDict.Add(kv.Key, kv.Value);
                }
            }
            return headerDict.ToDynamic();
        }

        private dynamic BuildDynamicPostForm()
        {
            if (!RawRequest.HasFormContentType)
            {
                return new ExpandoObject();
            }

            var postFormDict = new Dictionary<string, string>();
            foreach (var kv in RawRequest.Form)
            {
                if (!postFormDict.ContainsKey(kv.Key))
                {
                    postFormDict.Add(kv.Key, kv.Value);
                }
            }
            return postFormDict.ToDynamic();
        }
    }
}