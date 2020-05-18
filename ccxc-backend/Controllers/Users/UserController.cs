using Ccxc.Core.HttpServer;
using ccxc_backend.DataModels;
using ccxc_backend.DataServices;
using ccxc_backend.Functions;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading.Tasks;

namespace ccxc_backend.Controllers.Users
{
    [Export(typeof(HttpController))]
    public class UserController : HttpController
    {
        [HttpHandler("POST", "/user-reg")]
        public async Task UserReg(Request request, Response response)
        {
            var requestJson = request.Json<UserRegRequest>();

            //判断请求是否有效
            if (!Validation.Valid(requestJson, out string reason))
            {
                await response.BadRequest(reason);
                return;
            }

            //数据库对象
            var userDb = DbFactory.Get<User>();
            var userList = await userDb.SelectAllFromCache();

            //判断是否重复
            var userNameSet = new HashSet<string>(userList.Select(it => it.username));
            if (userNameSet.Contains(requestJson.username))
            {
                await response.BadRequest("用户名已被使用，请选择其他用户名。");
                return;
            }

            var emailSet = new HashSet<string>(userList.Select(it => it.username));
            if (emailSet.Contains(requestJson.email))
            {
                await response.BadRequest("E-mail已被使用，请使用其他邮箱。");
                return;
            }

            //插入数据库并清除缓存
            var user = new user
            {
                username = requestJson.username,
                email = requestJson.email,
                password = CryptoUtils.GetLoginHash(requestJson.pass),
                roleid = 1,
                create_time = DateTime.Now,
                update_time = DateTime.Now,
            };
            await userDb.SimpleDb.AsInsertable(user).ExecuteCommandAsync();
            await userDb.InvalidateCache();

            //返回
            await response.OK();
        }

        [HttpHandler("POST", "/user-login")]
        public async Task UserLogin(Request request, Response response)
        {
            var loginLogDb = DbFactory.Get<LoginLog>();
            var loginLog = new login_log
            {
                create_time = DateTime.Now,
                ip = request.ContextItems["RealIp"].ToString(),
                proxy_ip = request.ContextItems["ForwardIp"].ToString(),
                useragent = request.ContextItems["UserAgent"].ToString()
            };

            var requestJson = request.Json<UserLoginRequest>();
            loginLog.username = requestJson.username;

            //判断请求是否有效
            if (!Validation.Valid(requestJson, out string reason))
            {
                loginLog.status = 2;
                await loginLogDb.SimpleDb.AsInsertable(loginLog).ExecuteCommandAsync();

                await response.BadRequest(reason);
                return;
            }

            //数据库对象
            var userDb = DbFactory.Get<User>();
            var userDict = (await userDb.SelectAllFromCache()).ToDictionary(it => it.username, it => it);

            if (!userDict.ContainsKey(requestJson.username))
            {
                //用户不存在
                loginLog.status = 3;
                await loginLogDb.SimpleDb.AsInsertable(loginLog).ExecuteCommandAsync();

                await response.BadRequest("用户名或密码错误");
                return;
            }


            var user = userDict[requestJson.username];
            loginLog.uid = user.uid;

            var hashedPass = CryptoUtils.GetLoginHash(requestJson.pass);
            if (hashedPass != user.password)
            {
                //密码错误
                loginLog.status = 4;
                await loginLogDb.SimpleDb.AsInsertable(loginLog).ExecuteCommandAsync();

                await response.BadRequest("用户名或密码错误");
                return;
            }

            if(user.roleid < 1)
            {
                //被封禁
                loginLog.status = 5;
                await loginLogDb.SimpleDb.AsInsertable(loginLog).ExecuteCommandAsync();

                await response.BadRequest("您的账号目前无法登录");
                return;
            }

            var cache = DbFactory.GetCache();
            //登录成功
            //查询该用户已登录Session并置为无效
            var keyPattern = cache.GetUserSessionKey("*");
            var sessions = cache.FindKeys(keyPattern);

            foreach (var session in sessions)
            {
                var oldSession = await cache.Get<UserSession>(session);
                if (oldSession != null && oldSession.username == user.username)
                {
                    oldSession.is_active = 0;
                    oldSession.last_update = DateTime.Now;
                    oldSession.inactive_message = $"您的帐号已于 {DateTime.Now:yyyy-MM-dd HH:mm:ss} 在其他设备登录。";

                    await cache.Put(session, oldSession, Config.Config.Options.UserSessionTimeout * 1000);
                }
            }

            //创建新Session
            var uuid = Guid.NewGuid().ToString("n");
            var sk = CryptoUtils.GetRandomKey();

            var newSession = new UserSession
            {
                uid = user.uid,
                username = user.username,
                roleid = user.roleid,
                token = uuid,
                sk = sk,
                last_update = DateTime.Now,
                is_active = 1
            };

            //保存当前Session
            var sessionKey = cache.GetUserSessionKey(uuid);
            await cache.Put(sessionKey, newSession, Config.Config.Options.UserSessionTimeout * 1000);

            loginLog.status = 1;
            await loginLogDb.SimpleDb.AsInsertable(loginLog).ExecuteCommandAsync();

            //将uid, username, roleid, token, sk返回给前端
            await response.JsonResponse(200, new UserLoginResponse
            {
                status = 1,
                user_login_info = new UserLoginResponse.UserLoginInfo
                {
                    uid = user.uid,
                    username = user.username,
                    roleid = user.roleid,
                    token = uuid,
                    sk = sk
                }
            });
        }

        [HttpHandler("POST", "/user-logout")]
        public async Task UserLogout(Request request, Response response)
        {
            var userSession = await CheckAuth.Check(request, response, AuthLevel.Normal);
            if (userSession == null) return;

            var cache = DbFactory.GetCache();
            var sessionKey = cache.GetUserSessionKey(userSession.token);
            await cache.Delete(sessionKey);

            await response.OK();
        }
    }
}
