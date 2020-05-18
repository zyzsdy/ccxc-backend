using Ccxc.Core.HttpServer;
using Ccxc.Core.Utils;
using ccxc_backend.DataModels;
using ccxc_backend.DataServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ccxc_backend.Controllers
{
    public enum AuthLevel
    {
        Banned = 0,
        Normal = 1,
        Member = 2,
        TeamLeader = 3,
        Organizer = 4,
        Administrator = 5
    }

    public static class CheckAuth
    {
        /// <summary>
        /// authLevel 认证等级（0-被封禁 1-标准用户 2-组员 3-组长 4-出题组 5-管理员）
        /// </summary>
        /// <param name="request"></param>
        /// <param name="response"></param>
        /// <param name="authLevel"></param>
        /// <returns></returns>
        public async static Task<UserSession> Check(Request request, Response response, AuthLevel authLevel)
        {
            IDictionary<string, object> headers = request.Header;
            if (!headers.ContainsKey("User-Token"))
            {
                await response.BadRequest("请求格式不完整：User-Token 不可为空。");
                return null;
            }

            var token = headers["User-Token"].ToString();

            if (!headers.ContainsKey("X-Auth-Token"))
            {
                await response.BadRequest("请求格式不完整：X-Auth-Token 不可为空。");
                return null;
            }

            var xAuthToken = headers["X-Auth-Token"].ToString();
            var xAuth = xAuthToken.Split(" ").Select(it => it.Trim()).ToList();

            if(xAuth.Count != 3)
            {
                await response.BadRequest("请求格式错误：X-Auth-Token 结构不正确。");
                return null;
            }

            if(xAuth[0] != "Ccxc-Auth")
            {
                await response.BadRequest("请求格式错误：X-Auth-Token 认证失败。");
                return null;
            }

            var ts = xAuth[1];
            var sign = xAuth[2];

            //ts判断，客户端与前端钟差不能大于5min
            long.TryParse(ts, out long tsNum);
            var signedTime = UnixTimestamp.FromTimestamp(tsNum);
            var diff = Math.Abs((DateTime.Now - signedTime).TotalMinutes);
            if(diff > 5)
            {
                await response.Unauthorized("禁止重放。");
                return null;
            }


            //从缓存中取出Session
            var cache = DbFactory.GetCache();

            var sessionKey = cache.GetUserSessionKey(token);
            var userSession = await cache.Get<UserSession>(sessionKey);

            if(userSession == null) //Session不存在
            {
                await response.Unauthorized("登录已经过期，请重新登录。");
                return null;
            }

            if(userSession.is_active != 1) //Session无效
            {
                await response.Unauthorized(userSession.inactive_message);
                return null;
            }

            //计算签名
            var sk = userSession.sk;
            var unsingedString = $"token={token}&ts={ts}&bodyString={request.BodyString}";
            var calcedSign = HashTools.HmacSha1Base64(unsingedString, sk);

            if(sign != calcedSign) //签名不匹配
            {
                await response.Unauthorized("认证失败");
                return null;
            }

            //判断用户权限等级是否满足
            var authLevelNumber = (int)authLevel;
            if(userSession.roleid < authLevelNumber)
            {
                await response.Unauthorized("权限不足");
                return null;
            }

            //认证通过，Session续期
            userSession.last_update = DateTime.Now;
            await cache.Put(sessionKey, userSession, Config.Config.Options.UserSessionTimeout * 1000);

            return userSession;
        }
    }
}
