using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ccxc.Core.HttpServer;
using ccxc_backend.DataModels;
using ccxc_backend.DataServices;
using SqlSugar;

namespace ccxc_backend.Controllers.Admin
{
    [Export(typeof(HttpController))]
    public class LoginLogController : HttpController
    {
        [HttpHandler("POST", "/admin/get-l-user-list")]
        public async Task GetLUserList(Request request, Response response)
        {
            var userSession = await CheckAuth.Check(request, response, AuthLevel.Organizer);
            if (userSession == null) return;

            var userDb = DbFactory.Get<User>();
            var uidItems = (await userDb.SelectAllFromCache()).Select(it => new UidItem
            {
                uid = it.uid,
                user_name = it.username
            }).OrderBy(it => it.uid).ToList();

            await response.JsonResponse(200, new GetUserListResponse
            {
                status = 1,
                uid_item = uidItems
            });
        }

        [HttpHandler("POST", "/admin/query-login-log")]
        public async Task QueryLoginLog(Request request, Response response)
        {
            var userSession = await CheckAuth.Check(request, response, AuthLevel.Organizer);
            if (userSession == null) return;

            var requestJson = request.Json<QueryLoginLogRequest>();

            //判断请求是否有效
            if (!Validation.Valid(requestJson, out string reason))
            {
                await response.BadRequest(reason);
                return;
            }

            var loginLogDb = DbFactory.Get<LoginLog>();

            const int pageSize = 20;
            var page = requestJson.page;
            if (page <= 0) page = 1;
            var totalCount = new RefAsync<int>(0);

            var orderType = requestJson.order == 0 ? OrderByType.Desc : OrderByType.Asc;

            var queryable = loginLogDb.SimpleDb.AsQueryable();
            if (requestJson.uid != null && requestJson.uid.Count > 0)
            {
                queryable = queryable.In(it => it.uid, requestJson.uid);
            }

            if (requestJson.status != null && requestJson.status.Count > 0)
            {
                queryable = queryable.In(it => it.status, requestJson.status);
            }

            if (!string.IsNullOrEmpty(requestJson.email))
            {
                queryable = queryable.Where(it => it.email.Contains(requestJson.email));
            }

            if (!string.IsNullOrEmpty(requestJson.ip))
            {
                queryable = queryable.Where(it => it.ip.Contains(requestJson.ip));
            }

            if (!string.IsNullOrEmpty(requestJson.userid))
            {
                queryable = queryable.Where(it => SqlFunc.Contains(SqlFunc.ToString(it.userid), requestJson.userid));
            }

            var loginLogList = await queryable.OrderBy(it => it.create_time, orderType)
                .ToPageListAsync(page, pageSize, totalCount);

            await response.JsonResponse(200, new QueryLoginLogResponse
            {
                status = 1,
                page = requestJson.page,
                page_size = pageSize,
                total_count = totalCount.Value,
                login_log = loginLogList
            });
        }
    }
}
