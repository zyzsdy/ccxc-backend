using Ccxc.Core.HttpServer;
using ccxc_backend.DataModels;
using ccxc_backend.DataServices;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ccxc_backend.Controllers.Users
{
    [Export(typeof(HttpController))]
    public class QueryUserController : HttpController
    {
        [HttpHandler("POST", "/search-no-group-user")]
        public async Task SearchNoGroupUser(Request request, Response response)
        {
            var userSession = await CheckAuth.Check(request, response, AuthLevel.TeamLeader);
            if (userSession == null) return;

            var requestJson = request.Json<SearchNoGroupUserRequest>();

            //判断请求是否有效
            if (!Validation.Valid(requestJson, out string reason))
            {
                await response.BadRequest(reason);
                return;
            }

            var keyword = requestJson.kw_uname;

            //读入roleid == 1（没有组队）的用户列表
            var userDb = DbFactory.Get<User>();
            var userList = await userDb.SelectAllFromCache();

            var res = userList.Where(it => it.roleid == 1 && (string.IsNullOrEmpty(keyword) || it.username.Contains(keyword)))
                              .Select(it => new SearchNoGroupUserResponse.UserSearchResult(it)).ToList();

            await response.JsonResponse(200, new SearchNoGroupUserResponse
            {
                status = 1,
                result = res
            });
        }
    }
}
