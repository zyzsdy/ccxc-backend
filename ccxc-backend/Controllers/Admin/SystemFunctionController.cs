using Ccxc.Core.HttpServer;
using ccxc_backend.DataModels;
using ccxc_backend.DataServices;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ccxc_backend.Controllers.Admin
{
    [Export(typeof(HttpController))]
    public class SystemFunctionController : HttpController
    {
        [HttpHandler("POST", "/admin/overview")]
        public async Task Overview(Request request, Response response)
        {
            var userSession = await CheckAuth.Check(request, response, AuthLevel.Organizer);
            if (userSession == null) return;

            var result = new List<string>
            {
                $"服务器时间：{DateTime.Now:yyyy-MM-dd HH:mm:ss}"
            };

            var userDb = DbFactory.Get<User>();
            var userList = await userDb.SelectAllFromCache();

            result.Add($"注册用户数：{userList.Count}");

            var groupBindDb = DbFactory.Get<UserGroupBind>();
            var groupBindList = await groupBindDb.SelectAllFromCache();

            result.Add($"有效报名人数：{groupBindList.Count}");

            var groupDb = DbFactory.Get<UserGroup>();
            var groupList = await groupDb.SelectAllFromCache();

            result.Add($"报名队伍数：{groupList.Count}");

            var cache = DbFactory.GetCache();
            //登录成功
            var keyPattern = cache.GetUserSessionKey("*");
            var sessions = cache.FindKeys(keyPattern);
            var lastActionList = (await Task.WhenAll(sessions.Select(async it => await cache.Get<UserSession>(it))))
                .Where(it => it != null && it.is_active == 1)
                .GroupBy(it => it.uid)
                .Select(it => it.First() == null ? DateTime.MinValue : it.First().last_update)
                .Where(it => Math.Abs((DateTime.Now - it).TotalMinutes) < 1.1);

            result.Add($"在线人数：{lastActionList.Count()}");

            var resultString = string.Join("", result.Select(it => "<p>" + it + "</p>"));

            await response.JsonResponse(200, new
            {
                status = 1,
                result = resultString
            });
        }

        [HttpHandler("POST", "/admin/purge-cache")]
        public async Task CachePurge(Request request, Response response)
        {
            var userSession = await CheckAuth.Check(request, response, AuthLevel.Administrator);
            if (userSession == null) return;

            var requestJson = request.Json<PurgeCacheRequest>();

            //判断请求是否有效
            if (!Validation.Valid(requestJson, out string reason))
            {
                await response.BadRequest(reason);
                return;
            }

            switch (requestJson.op_key)
            {
                case "anno":
                    {
                        var db = DbFactory.Get<Announcement>();
                        await db.InvalidateCache();
                        await response.OK();
                        return;
                    }
                case "invi":
                    {
                        var db = DbFactory.Get<Invite>();
                        await db.InvalidateCache();
                        await response.OK();
                        return;
                    }
                case "mess":
                    {
                        var db = DbFactory.Get<Message>();
                        await db.InvalidateCache();
                        await response.OK();
                        return;
                    }
                case "prog":
                    {
                        var db = DbFactory.Get<Progress>();
                        await db.InvalidateCache();
                        await response.OK();
                        return;
                    }
                case "puzz":
                    {
                        var db = DbFactory.Get<Puzzle>();
                        await db.InvalidateCache();
                        await response.OK();
                        return;
                    }
                case "puzg":
                    {
                        var db = DbFactory.Get<PuzzleGroup>();
                        await db.InvalidateCache();
                        await response.OK();
                        return;
                    }
                case "user":
                    {
                        var db = DbFactory.Get<User>();
                        await db.InvalidateCache();
                        await response.OK();
                        return;
                    }
                case "useg":
                    {
                        var db = DbFactory.Get<UserGroup>();
                        await db.InvalidateCache();
                        await response.OK();
                        return;
                    }
                case "usgb":
                    {
                        var db = DbFactory.Get<UserGroupBind>();
                        await db.InvalidateCache();
                        await response.OK();
                        return;
                    }
                case "uall":
                    {
                        var db1 = DbFactory.Get<User>();
                        var db2 = DbFactory.Get<UserGroup>();
                        var db3 = DbFactory.Get<UserGroupBind>();
                        await db1.InvalidateCache();
                        await db2.InvalidateCache();
                        await db3.InvalidateCache();
                        await response.OK();
                        return;
                    }
                case "pall":
                    {
                        var db1 = DbFactory.Get<Puzzle>();
                        var db2 = DbFactory.Get<PuzzleGroup>();
                        await db1.InvalidateCache();
                        await db2.InvalidateCache();
                        await response.OK();
                        return;
                    }
                case "aall":
                    {
                        var db1 = DbFactory.Get<Announcement>();
                        var db2 = DbFactory.Get<Invite>();
                        var db3 = DbFactory.Get<Message>();
                        var db4 = DbFactory.Get<Progress>();
                        var db5 = DbFactory.Get<Puzzle>();
                        var db6 = DbFactory.Get<PuzzleGroup>();
                        var db7 = DbFactory.Get<User>();
                        var db8 = DbFactory.Get<UserGroup>();
                        var db9 = DbFactory.Get<UserGroupBind>();
                        await db1.InvalidateCache();
                        await db2.InvalidateCache();
                        await db3.InvalidateCache();
                        await db4.InvalidateCache();
                        await db5.InvalidateCache();
                        await db6.InvalidateCache();
                        await db7.InvalidateCache();
                        await db8.InvalidateCache();
                        await db9.InvalidateCache();
                        await response.OK();
                        return;
                    }
                default:
                    break;
            }

            await response.BadRequest("wrong op_key");
        }
    }
}
