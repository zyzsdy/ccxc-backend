using Ccxc.Core.HttpServer;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Text;
using System.Threading.Tasks;

namespace ccxc_backend.Controllers.System
{
    [Export(typeof(HttpController))]
    public class SystemController : HttpController
    {
        [HttpHandler("POST", "/get-default-setting")]
        public async Task GetDefaultSetting(Request request, Response response)
        {
            await response.JsonResponse(200, new DefaultSettingResponse
            {
                status = 1,
                start_time = Config.Config.Options.StartTime
            });
        }
    }
}
