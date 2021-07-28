using Ccxc.Core.Utils.ExtensionFunctions;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ccxc.Core.HttpServer
{
    public class Response
    {
        public HttpResponse RawResponse { get; private set; }
        public Response(HttpResponse response)
        {
            RawResponse = response;
        }

        public void SetHeader(string header, string value)
        {
            if (RawResponse.Headers.Any(h => h.Key == header))
            {
                RawResponse.Headers[header] = value;
            }
            else
            {
                RawResponse.Headers.Add(header, value);
            }
        }

        public Task JsonResponse(int statusCode, object jsonObject)
        {
            var jsonString = JsonConvert.SerializeObject(jsonObject, new FrontendLongConverter());
            return SimpleResponse(RawResponse, statusCode, jsonString, "application/json");
        }

        public Task StringResponse(int statusCode, string responseContent, string responseType = "text/html")
        {
            return SimpleResponse(RawResponse, statusCode, responseContent, responseType);
        }

        public Task BinaryResponse(int statusCode, byte[] responseContent, string responseType = "application/octet-stream")
        {
            RawResponse.ContentType = responseType;
            RawResponse.StatusCode = statusCode;
            return RawResponse.Body.WriteAsync(responseContent, 0, responseContent.Length);
        }

        public static Task SimpleResponse(HttpResponse response, int httpStatusCode = 404, string responseText = "", string responseType = "text/plain; charset=utf-8")
        {
            response.ContentType = responseType;
            response.StatusCode = httpStatusCode;
            return response.WriteAsync(responseText, Encoding.UTF8);
        }
    }
}