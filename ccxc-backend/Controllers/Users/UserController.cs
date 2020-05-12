using Ccxc.Core.HttpServer;
using ccxc_backend.DataModels;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Text;
using System.Threading.Tasks;

namespace ccxc_backend.Controllers.Users
{
    [Export(typeof(HttpController))]
    public class UserController : HttpController
    {
        [HttpHandler("POST", "/reg")]
        public async Task reg(Request request, Response response)
        {
            var requestJson = request.Json<user>();
            await response.JsonResponse(401, new
            {
                status = "OK",
                data = requestJson
            });
        }
    }
}
