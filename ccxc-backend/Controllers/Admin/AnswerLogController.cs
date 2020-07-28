using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Ccxc.Core.HttpServer;
using ccxc_backend.DataModels;
using ccxc_backend.DataServices;
using SqlSugar;

namespace ccxc_backend.Controllers.Admin
{
    [Export(typeof(HttpController))]
    public class AnswerLogController : HttpController
    {
        [HttpHandler("POST", "/admin/get-user-list")]
        public async Task GetUserList(Request request, Response response)
        {
            var userSession = await CheckAuth.Check(request, response, AuthLevel.Organizer);
            if (userSession == null) return;

            var userDb = DbFactory.Get<User>();
            var uidItems = (await userDb.SelectAllFromCache()).Select(it => new UidItem
            {
                uid = it.uid,
                user_name = it.username
            }).OrderBy(it => it.uid).ToList();

            var groupDb = DbFactory.Get<UserGroup>();
            var gidItems = (await groupDb.SelectAllFromCache()).Select(it => new GidItem
            {
                gid = it.gid,
                group_name = it.groupname
            }).OrderBy(it => it.gid).ToList();

            var puzzleDb = DbFactory.Get<Puzzle>();
            var pidItems = (await puzzleDb.SelectAllFromCache()).Select(it => new PidItem
            {
                pid = it.pid,
                pgid = it.pgid,
                title = it.title
            }).OrderBy(it => it.pgid).ThenBy(it => it.pid).ToList();

            await response.JsonResponse(200, new GetUserListResponse
            {
                status = 1,
                uid_item = uidItems,
                gid_item = gidItems,
                pid_item = pidItems
            });
        }


        [HttpHandler("POST", "/admin/query-answer-log")]
        public async Task QueryAnswerLog(Request request, Response response)
        {
            var userSession = await CheckAuth.Check(request, response, AuthLevel.Organizer);
            if (userSession == null) return;

            var requestJson = request.Json<QueryAnswerLogRequest>();

            //判断请求是否有效
            if (!Validation.Valid(requestJson, out string reason))
            {
                await response.BadRequest(reason);
                return;
            }

            var answerLogDb = DbFactory.Get<AnswerLog>();

            const int pageSize = 20;
            var page = requestJson.page;
            if (page <= 0) page = 1;
            var totalCount = new RefAsync<int>(0);

            var orderType = requestJson.order == 0 ? OrderByType.Desc : OrderByType.Asc;

            var queryable = answerLogDb.SimpleDb.AsQueryable();
            if (requestJson.gid != null && requestJson.gid.Count > 0)
            {
                queryable = queryable.In(it => it.gid, requestJson.gid);
            }

            if (requestJson.uid != null && requestJson.uid.Count > 0)
            {
                queryable = queryable.In(it => it.uid, requestJson.uid);
            }

            if (requestJson.pid != null && requestJson.pid.Count > 0)
            {
                queryable = queryable.In(it => it.pid, requestJson.pid);
            }

            if (requestJson.status != null && requestJson.status.Count > 0)
            {
                queryable = queryable.In(it => it.status, requestJson.status);
            }

            if (!string.IsNullOrEmpty(requestJson.answer))
            {
                queryable = queryable.Where(it => it.answer.Contains(requestJson.answer));
            }

            var answerLogList = await queryable.OrderBy(it => it.create_time, orderType)
                .ToPageListAsync(page, pageSize, totalCount);

            var res = answerLogList.Select(it => new AnswerLogView(it)).ToList();
            await response.JsonResponse(200, new QueryAnswerLogResponse
            {
                status = 1,
                page = requestJson.page,
                page_size = pageSize,
                total_count = totalCount.Value,
                answer_log = res
            });
        }
    }
}
