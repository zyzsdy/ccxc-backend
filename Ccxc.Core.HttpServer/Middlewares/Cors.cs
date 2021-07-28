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
                SetHeader(ctx.Response, "Server", "Ccxc-Server/1.2.0 (2021-6-22)");

                //处理OPTIONS请求
                var method = ctx.Request.Method.ToUpper();
                if (method == "OPTIONS")
                {
                    SetHeader(ctx.Response, "Access-Control-Allow-Method", "GET, POST, PUT, DELETE");
                    SetHeader(ctx.Response, "Access-Control-Allow-Headers", "Content-Type, User-Token, X-Requested-With, X-Auth-Token, Upload-Token, X-Captcha-Nonce");
                    SetHeader(ctx.Response, "Access-Control-Max-Age", "600");
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
