using Ccxc.Core.HttpServer;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ccxc_backend.DataModels;
using ccxc_backend.DataServices;

namespace ccxc_backend.Controllers.Admin
{
    [Export(typeof(HttpController))]
    public class UserAdminController : HttpController
    {
        [HttpHandler("POST", "/admin/get-user")]
        public async Task GetUser(Request request, Response response)
        {
            var userSession = await CheckAuth.Check(request, response, AuthLevel.Organizer);
            if (userSession == null) return;

            var cache = DbFactory.GetCache();
            //登录成功
            var keyPattern = cache.GetUserSessionKey("*");
            var sessions = cache.FindKeys(keyPattern);
            var lastActionDict = (await Task.WhenAll(sessions.Select(async it => await cache.Get<UserSession>(it))))
                .Where(it => it != null && it.is_active == 1)
                .ToDictionary(it => it.uid, it => it.last_update);

            var userDb = DbFactory.Get<User>();
            var userData = (await userDb.SelectAllFromCache()).Select(it =>
            {
                var ret = new UserView(it);
                if (lastActionDict.ContainsKey(it.uid))
                {
                    ret.last_action_time = lastActionDict[it.uid];
                }

                ret.is_beta_user = (it.info_key == "beta_user") ? 1 : 0;

                return ret;
            }).OrderBy(it => it.uid);

            await response.JsonResponse(200, new GetAllUserResponse
            {
                status = 1,
                users = userData.ToList()
            });
        }

        [HttpHandler("POST", "/admin/set-beta-user")]
        public async Task SetBetaUser(Request request, Response response)
        {
            var userSession = await CheckAuth.Check(request, response, AuthLevel.Organizer);
            if (userSession == null) return;

            var requestJson = request.Json<AdminUidRequest>();

            //判断请求是否有效
            if (!Validation.Valid(requestJson, out var reason))
            {
                await response.BadRequest(reason);
                return;
            }

            var userDb = DbFactory.Get<User>();
            var userDict = (await userDb.SelectAllFromCache()).ToDictionary(it => it.uid, it => it);

            if (!userDict.ContainsKey(requestJson.uid))
            {
                await response.BadRequest("请求的UID不存在");
                return;
            }

            //修改用户
            var user = userDict[requestJson.uid];
            user.info_key = "beta_user";

            await userDb.SimpleDb.AsUpdateable(user).ExecuteCommandAsync();
            await userDb.InvalidateCache();
            await response.OK();
        }

        [HttpHandler("POST", "/admin/remove-beta-user")]
        public async Task RemoveBetaUser(Request request, Response response)
        {
            var userSession = await CheckAuth.Check(request, response, AuthLevel.Organizer);
            if (userSession == null) return;

            var requestJson = request.Json<AdminUidRequest>();

            //判断请求是否有效
            if (!Validation.Valid(requestJson, out var reason))
            {
                await response.BadRequest(reason);
                return;
            }

            var userDb = DbFactory.Get<User>();
            var userDict = (await userDb.SelectAllFromCache()).ToDictionary(it => it.uid, it => it);

            if (!userDict.ContainsKey(requestJson.uid))
            {
                await response.BadRequest("请求的UID不存在");
                return;
            }

            //修改用户
            var user = userDict[requestJson.uid];
            user.info_key = "";

            await userDb.SimpleDb.AsUpdateable(user).ExecuteCommandAsync();
            await userDb.InvalidateCache();
            await response.OK();
        }

        [HttpHandler("POST", "/admin/set-ban-user")]
        public async Task SetBanUser(Request request, Response response)
        {
            var userSession = await CheckAuth.Check(request, response, AuthLevel.Organizer);
            if (userSession == null) return;

            var requestJson = request.Json<AdminUidRequest>();

            //判断请求是否有效
            if (!Validation.Valid(requestJson, out var reason))
            {
                await response.BadRequest(reason);
                return;
            }

            var userDb = DbFactory.Get<User>();
            var userDict = (await userDb.SelectAllFromCache()).ToDictionary(it => it.uid, it => it);

            if (!userDict.ContainsKey(requestJson.uid))
            {
                await response.BadRequest("请求的UID不存在");
                return;
            }

            //修改用户
            var user = userDict[requestJson.uid];
            user.roleid = 0;

            //查询该用户已登录Session并置为无效
            var cache = DbFactory.GetCache();
            var keyPattern = cache.GetUserSessionKey("*");
            var sessions = cache.FindKeys(keyPattern);

            foreach (var session in sessions)
            {
                var oldSession = await cache.Get<UserSession>(session);
                if (oldSession == null || oldSession.uid != user.uid) continue;
                oldSession.roleid = 0;
                oldSession.is_active = 0;
                oldSession.last_update = DateTime.Now;
                oldSession.inactive_message = $"您的帐号已于 {DateTime.Now:yyyy-MM-dd HH:mm:ss} 被封禁，请与管理员联系。";

                await cache.Put(session, oldSession, Config.Config.Options.UserSessionTimeout * 1000);
            }

            await userDb.SimpleDb.AsUpdateable(user).ExecuteCommandAsync();
            await userDb.InvalidateCache();
            await response.OK();
        }

        [HttpHandler("POST", "/admin/remove-ban-user")]
        public async Task RemoveBanUser(Request request, Response response)
        {
            var userSession = await CheckAuth.Check(request, response, AuthLevel.Organizer);
            if (userSession == null) return;

            var requestJson = request.Json<AdminUidRequest>();

            //判断请求是否有效
            if (!Validation.Valid(requestJson, out var reason))
            {
                await response.BadRequest(reason);
                return;
            }

            var userDb = DbFactory.Get<User>();
            var userDict = (await userDb.SelectAllFromCache()).ToDictionary(it => it.uid, it => it);

            if (!userDict.ContainsKey(requestJson.uid))
            {
                await response.BadRequest("请求的UID不存在");
                return;
            }

            var groupBindDb = DbFactory.Get<UserGroupBind>();
            var userLeaderDict = (await groupBindDb.SelectAllFromCache()).ToDictionary(it => it.uid, it => it.is_leader);

            //修改用户
            var user = userDict[requestJson.uid];

            if (userLeaderDict.ContainsKey(user.uid))
            {
                var isLeader = userLeaderDict[user.uid];
                user.roleid = isLeader == 1 ? 3 : 2;
            }
            else
            {
                user.roleid = 1;
            }

            await userDb.SimpleDb.AsUpdateable(user).ExecuteCommandAsync();
            await userDb.InvalidateCache();
            await response.OK();
        }
    }
}
