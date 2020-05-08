using Microsoft.AspNetCore.Http;
using System.Linq;

namespace Ccxc.Core.HttpServer.Middlewares
{
    public static class Cors
    {
        public static Server UseCors(this Server server)
        {
            return server.Use(async (ctx, next) =>
            {
                //启用简易CORS
                SetHeader(ctx.Response, "Access-Control-Allow-Origin", "*");

                //处理OPTIONS请求
                var method = ctx.Request.Method.ToUpper();
                if (method == "OPTIONS")
                {
                    SetHeader(ctx.Response, "Access-Control-Allow-Method", "GET, POST, PUT, DELETE");
                    SetHeader(ctx.Response, "Access-Control-Allow-Headers", "Content-Type, User-Token, X-Requested-With");
                    ctx.Response.StatusCode = 204;
                }
                else
                {
                    await next.Invoke();
                }
            });
        }

        internal static void SetHeader(HttpResponse rawResponse, string header, string value)
        {
            if (rawResponse.Headers.Any(h => h.Key == header))
            {
                rawResponse.Headers[header] = value;
            }
            else
            {
                rawResponse.Headers.Add(header, value);
            }
        }
    }
}
