using Ccxc.Core.HttpServer;
using ccxc_backend.DataModels;
using ccxc_backend.DataServices;
using ccxc_backend.Functions;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
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
            await response.JsonResponse(200, new BasicResponse
            {
                status = 1
            });
        }
    }
}
