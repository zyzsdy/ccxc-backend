using Ccxc.Core.HttpServer;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Text;
using System.Threading.Tasks;

namespace ccxc_backend.Controllers.Game
{
    [Export(typeof(HttpController))]
    public class SpecialGameBackendController : HttpController
    {
        [HttpHandler("POST", "/puzzle-backend/29")]
        public async Task Start(Request request, Response response)
        {
            var userSession = await CheckAuth.Check(request, response, AuthLevel.Member, true);
            if (userSession == null) return;

            var now = DateTime.Now;
            var unixTimestamp = Ccxc.Core.Utils.UnixTimestamp.GetTimestampSecond(now);
            var minuteTag = (unixTimestamp / 60) % 16;

            var resultTag = minuteTag switch
            {
                0 => ("#FFC0CB", 1),
                1 => ("#F0F8FF", 1),
                2 => ("#4169E1", 0),
                3 => ("#4682B4", 0),
                4 => ("#808000", 0),
                5 => ("#FFDEAD", 1),
                6 => ("#A0522D", 0),
                7 => ("#7FFF00", 1),
                8 => ("#FF69B4", 0),
                9 => ("#F0FFFF", 1),
                10 => ("#FFE4E1", 1),
                11 => ("#EEEEEE", 1),
                12 => ("#7CFC00", 1),
                13 => ("#EEEEEE", 1),
                14 => ("#FFA500", 1),
                15 => ("#000080", 0),
                _ => ("#1E1E1E", 0)
            };

            await response.JsonResponse(200, new
            {
                status = 1,
                result = resultTag.Item1,
                theme = resultTag.Item2
            });
        }
    }
}
