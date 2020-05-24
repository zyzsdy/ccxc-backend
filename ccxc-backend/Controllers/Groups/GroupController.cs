using Ccxc.Core.HttpServer;
using Ccxc.Core.Utils;
using ccxc_backend.DataModels;
using ccxc_backend.DataServices;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ccxc_backend.Controllers.Groups
{
    [Export(typeof(HttpController))]
    public class GroupController : HttpController
    {
        [HttpHandler("POST", "/create-group")]
        public async Task CreateGroup(Request request, Response response)
        {
            var userSession = await CheckAuth.Check(request, response, AuthLevel.Normal);
            if (userSession == null) return;

            var requestJson = request.Json<CreateGroupRequest>();

            //判断请求是否有效
            if (!Validation.Valid(requestJson, out string reason))
            {
                await response.BadRequest(reason);
                return;
            }

            //判断当前在允许新建组队的时间范围内
            var now = DateTime.Now;

            if(Config.Config.Options.RegDeadline > 0)
            {
                var regDeadline = UnixTimestamp.FromTimestamp(Config.Config.Options.RegDeadline);
                if(now > regDeadline)
                {
                    await response.BadRequest($"报名截止时间 （{regDeadline:yyyy-MM-dd HH:mm:ss}） 已过。");
                }
            }

            //判断当前用户不属于任何组队
            var groupBindDb = DbFactory.Get<UserGroupBind>();
            var groupBindList = await groupBindDb.SelectAllFromCache();

            var groupBindItem = groupBindList.FirstOrDefault(it => it.uid == userSession.uid);
            if(groupBindItem != null)
            {
                await response.BadRequest("Emmmmm, 请勿重复新建组队。");
                return;
            }

            //取得用户数据
            var userDb = DbFactory.Get<User>();
            var userDict = (await userDb.SelectAllFromCache()).ToDictionary(it => it.uid, it => it);

            if (!userDict.ContainsKey(userSession.uid))
            {
                await response.Unauthorized("服务器提出了一个问题：你为何存在？");
                return;
            }

            //新建组队
            var newGroup = new user_group
            {
                groupname = requestJson.groupname,
                profile = requestJson.profile,
                create_time = now,
                update_time = now
            };

            var groupDb = DbFactory.Get<UserGroup>();
            var newGid = await groupDb.SimpleDb.AsInsertable(newGroup).ExecuteReturnIdentityAsync();
            await groupDb.InvalidateCache();

            //修改用户组队绑定
            var user = userDict[userSession.uid];
            user.roleid = 3;
            user.update_time = now;
            await userDb.SimpleDb.AsUpdateable(user).ExecuteCommandAsync();
            await userDb.InvalidateCache();

            var newGroupBind = new user_group_bind
            {
                uid = userSession.uid,
                gid = newGid,
                is_leader = 1
            };
            await groupBindDb.SimpleDb.AsInsertable(newGroupBind).ExecuteCommandAsync();
            await groupBindDb.InvalidateCache();

            await response.OK();
        }

        [HttpHandler("POST", "/edit-group")]
        public async Task EditGroup(Request request, Response response)
        {
            var userSession = await CheckAuth.Check(request, response, AuthLevel.TeamLeader);
            if (userSession == null) return;

            var requestJson = request.Json<CreateGroupRequest>();

            //判断请求是否有效
            if (!Validation.Valid(requestJson, out string reason))
            {
                await response.BadRequest(reason);
                return;
            }

            //判断当前在允许新建组队的时间范围内
            var now = DateTime.Now;

            if (Config.Config.Options.RegDeadline > 0)
            {
                var regDeadline = UnixTimestamp.FromTimestamp(Config.Config.Options.RegDeadline);
                if (now > regDeadline)
                {
                    await response.BadRequest($"报名截止时间 （{regDeadline:yyyy-MM-dd HH:mm:ss}） 已过，现在不能修改队伍信息。");
                }
            }

            //取得该用户GID
            var groupBindDb = DbFactory.Get<UserGroupBind>();
            var groupBindList = await groupBindDb.SelectAllFromCache();

            var groupBindItem = groupBindList.FirstOrDefault(it => it.uid == userSession.uid);
            if (groupBindItem == null)
            {
                await response.BadRequest("未确定组队？");
                return;
            }

            var gid = groupBindItem.gid;

            //取出组队
            var groupDb = DbFactory.Get<UserGroup>();
            var groupList = await groupDb.SelectAllFromCache();

            var groupItem = groupList.FirstOrDefault(it => it.gid == gid);
            if(groupItem == null)
            {
                await response.BadRequest("幽灵组队出现了！！！！！！！！！");
                return;
            }

            //编辑并保存
            groupItem.groupname = requestJson.groupname;
            groupItem.profile = requestJson.profile;
            groupItem.update_time = DateTime.Now;

            await groupDb.SimpleDb.AsUpdateable(groupItem).ExecuteCommandAsync();
            await groupDb.InvalidateCache();

            await response.OK();
        }
    }
}
