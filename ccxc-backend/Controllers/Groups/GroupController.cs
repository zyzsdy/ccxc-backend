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

            //自动拒绝该用户所有未拒绝的邀请
            var inviteDb = DbFactory.Get<Invite>();
            await inviteDb.Db.Updateable<invite>().SetColumns(it => it.valid == 2)
                .Where(it => it.to_uid == userSession.uid && it.valid == 1).ExecuteCommandAsync();
            await inviteDb.InvalidateCache();


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

        [HttpHandler("POST", "/delete-group")]
        public async Task DeleteGroup(Request request, Response response)
        {
            var userSession = await CheckAuth.Check(request, response, AuthLevel.TeamLeader);
            if (userSession == null) return;

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
            if (groupItem == null)
            {
                await response.BadRequest("幽灵组队出现了！！！！！！！！！");
                return;
            }

            //取出组队所有成员并置为无组队状态
            var groupUids = groupBindList.Where(it => it.gid == gid).Select(it => it.uid).ToList();

            var userDb = DbFactory.Get<User>();
            var userDict = (await userDb.SelectAllFromCache()).ToDictionary(it => it.uid, it => it);

            var updateUser = new List<user>();
            foreach(var uid in groupUids)
            {
                if (userDict.ContainsKey(uid))
                {
                    var user = userDict[uid];
                    user.roleid = 1;
                    user.update_time = now;
                    updateUser.Add(user);
                }
            }
            await userDb.SimpleDb.AsUpdateable(updateUser).ExecuteCommandAsync();
            await userDb.InvalidateCache();

            //自动撤销本组发出的所有未拒绝的邀请
            var inviteDb = DbFactory.Get<Invite>();
            await inviteDb.Db.Updateable<invite>().SetColumns(it => it.valid == 0)
                .Where(it => it.from_gid == gid && it.valid == 1).ExecuteCommandAsync();
            await inviteDb.InvalidateCache();

            //删除组队信息
            await groupDb.SimpleDb.AsDeleteable().Where(it => it.gid == gid).ExecuteCommandAsync();
            await groupDb.InvalidateCache();

            //删除组队绑定信息
            await groupBindDb.SimpleDb.AsDeleteable().Where(it => it.gid == gid).ExecuteCommandAsync();
            await groupBindDb.InvalidateCache();

            await response.OK();
        }

        [HttpHandler("POST", "/exit-group")]
        public async Task ExitGroup(Request request, Response response)
        {
            var userSession = await CheckAuth.Check(request, response, AuthLevel.Member);
            if (userSession == null) return;

            if(userSession.roleid != (int)AuthLevel.Member)
            {
                await response.BadRequest("只有队员才可退出组队");
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
            if (groupItem == null)
            {
                await response.BadRequest("幽灵组队出现了！！！！！！！！！");
                return;
            }

            //修改用户权限信息
            var userDb = DbFactory.Get<User>();
            var userList = await userDb.SelectAllFromCache();

            var user = userList.FirstOrDefault(it => it.uid == userSession.uid);
            if(user == null)
            {
                await response.BadRequest("Emmmmm, 见鬼了！");
                return;
            }

            user.roleid = 1;
            user.update_time = now;

            await userDb.SimpleDb.AsUpdateable(user).ExecuteCommandAsync();
            await userDb.InvalidateCache();

            //删除组队绑定
            await groupBindDb.SimpleDb.AsDeleteable().Where(it => it.uid == userSession.uid).ExecuteCommandAsync();
            await groupBindDb.InvalidateCache();

            await response.OK();
        }

        [HttpHandler("POST", "/remove-group-member")]
        public async Task RemoveGroupMember(Request request, Response response)
        {
            var userSession = await CheckAuth.Check(request, response, AuthLevel.TeamLeader);
            if (userSession == null) return;

            var requestJson = request.Json<RemoveGroupRequest>();

            //判断请求是否有效
            if (!Validation.Valid(requestJson, out string reason))
            {
                await response.BadRequest(reason);
                return;
            }

            if(requestJson.uid == userSession.uid)
            {
                await response.BadRequest("目标不能是自己");
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

            //取得GID
            var groupBindDb = DbFactory.Get<UserGroupBind>();
            var groupBindList = await groupBindDb.SelectAllFromCache();

            var groupBindItem = groupBindList.FirstOrDefault(it => it.uid == userSession.uid);
            if (groupBindItem == null)
            {
                await response.BadRequest("未确定组队？");
                return;
            }

            var gid = groupBindItem.gid;

            //取得目标用户的gid

            var targetUserBindItem = groupBindList.FirstOrDefault(it => it.uid == requestJson.uid);
            if(targetUserBindItem == null)
            {
                await response.BadRequest("什么人类？？？？？");
                return;
            }

            var targetGid = targetUserBindItem.gid;

            if(gid != targetGid)
            {
                await response.Unauthorized("权限不足。无法操作其他用户");
                return;
            }

            //取出组队
            var groupDb = DbFactory.Get<UserGroup>();
            var groupList = await groupDb.SelectAllFromCache();

            var groupItem = groupList.FirstOrDefault(it => it.gid == gid);
            if (groupItem == null)
            {
                await response.BadRequest("幽灵组队出现了！！！！！！！！！");
                return;
            }

            //修改用户权限信息
            var userDb = DbFactory.Get<User>();
            var userList = await userDb.SelectAllFromCache();

            var user = userList.FirstOrDefault(it => it.uid == requestJson.uid);
            if (user == null)
            {
                await response.BadRequest("Emmmmm, 见鬼了！");
                return;
            }

            user.roleid = 1;
            user.update_time = now;

            await userDb.SimpleDb.AsUpdateable(user).ExecuteCommandAsync();
            await userDb.InvalidateCache();

            //删除组队绑定
            await groupBindDb.SimpleDb.AsDeleteable().Where(it => it.uid == requestJson.uid).ExecuteCommandAsync();
            await groupBindDb.InvalidateCache();

            await response.OK();
        }
    }
}
