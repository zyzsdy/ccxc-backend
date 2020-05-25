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

namespace ccxc_backend.Controllers.Invites
{
    [Export(typeof(HttpController))]
    public class InviteController : HttpController
    {
        [HttpHandler("POST", "/send-invite")]
        public async Task SendInvite(Request request, Response response)
        {
            var userSession = await CheckAuth.Check(request, response, AuthLevel.TeamLeader);
            if (userSession == null) return;

            var requestJson = request.Json<SendInviteRequest>();

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
                    await response.BadRequest($"报名截止时间 （{regDeadline:yyyy-MM-dd HH:mm:ss}） 已过。");
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

            //取得该GID已绑定人数
            var numberOfGroup = groupBindList.Count(it => it.gid == gid);

            if(numberOfGroup >= 5)
            {
                await response.BadRequest("组队人数已满，不能发出新邀请。");
                return;
            }

            //取得目标用户信息
            var userDb = DbFactory.Get<User>();
            var userList = await userDb.SelectAllFromCache();

            var sendToUser = userList.FirstOrDefault(it => it.username == requestJson.username);
            if(sendToUser == null)
            {
                await response.BadRequest("目标用户不存在或不是未报名用户。");
                return;
            }

            //判断目标用户是否为未报名用户
            if(sendToUser.roleid != 1)
            {
                await response.BadRequest("目标用户不存在或不是未报名用户。");
                return;
            }


            //插入邀请信息表
            var inviteDb = DbFactory.Get<Invite>();
            var newInvite = new invite
            {
                create_time = DateTime.Now,
                from_gid = gid,
                to_uid = sendToUser.uid,
                valid = 1
            };
            await inviteDb.SimpleDb.AsInsertable(newInvite).ExecuteCommandAsync();
            await inviteDb.InvalidateCache();

            await response.OK();
        }

        [HttpHandler("POST", "/list-sent-invites")]
        public async Task ListSentInvites(Request request, Response response)
        {
            var userSession = await CheckAuth.Check(request, response, AuthLevel.TeamLeader);
            if (userSession == null) return;

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

            //读取基础数据
            var userDb = DbFactory.Get<User>();
            var userNameDict = (await userDb.SelectAllFromCache()).ToDictionary(it => it.uid, it => it.username);

            //读取仍然为有效状态的邀请
            var inviteDb = DbFactory.Get<Invite>();
            var result = await inviteDb.SimpleDb.AsQueryable().Where(it => it.from_gid == gid && it.valid == 1).ToListAsync();

            var res = result.Select(it =>
            {
                var r = new ListSentResponse.InviteView(it);
                if (userNameDict.ContainsKey(r.to_uid))
                {
                    r.to_username = userNameDict[r.to_uid];
                }
                return r;
            }).ToList();

            await response.JsonResponse(200, new ListSentResponse
            {
                status = 1,
                result = res
            });
        }

        [HttpHandler("POST", "/invalidate-invite")]
        public async Task InvalidateInvite(Request request, Response response)
        {
            var userSession = await CheckAuth.Check(request, response, AuthLevel.TeamLeader);
            if (userSession == null) return;

            var requestJson = request.Json<IidInviteRequest>();

            //判断请求是否有效
            if (!Validation.Valid(requestJson, out string reason))
            {
                await response.BadRequest(reason);
                return;
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

            //读取目标iid
            var inviteDb = DbFactory.Get<Invite>();
            var inviteItem = inviteDb.SimpleDb.GetById(requestJson.iid);

            if(inviteItem == null)
            {
                await response.BadRequest("无效邀请");
                return;
            }

            if(inviteItem.from_gid != gid)
            {
                await response.BadRequest("无修改权限");
            }

            //将目标置为无效
            inviteItem.valid = 0;

            await inviteDb.SimpleDb.AsUpdateable(inviteItem).ExecuteCommandAsync();
            await inviteDb.InvalidateCache();

            await response.OK();
        }

        [HttpHandler("POST", "/list-my-invite")]
        public async Task ListMyInvite(Request request, Response response)
        {
            var userSession = await CheckAuth.Check(request, response, AuthLevel.Normal);
            if (userSession == null) return;

            //读取基础数据
            var userDb = DbFactory.Get<User>();
            var userNameDict = (await userDb.SelectAllFromCache()).ToDictionary(it => it.uid, it => it.username);

            //读取仍然为有效状态的邀请
            var inviteDb = DbFactory.Get<Invite>();
            var result = await inviteDb.SimpleDb.AsQueryable().Where(it => it.to_uid == userSession.uid && it.valid == 1).ToListAsync();

            var res = result.Select(it =>
            {
                var r = new ListSentResponse.InviteView(it);
                if (userNameDict.ContainsKey(r.to_uid))
                {
                    r.to_username = userNameDict[r.to_uid];
                }
                return r;
            }).ToList();

            await response.JsonResponse(200, new ListSentResponse
            {
                status = 1,
                result = res
            });
        }

        [HttpHandler("POST", "/decline-invite")]
        public async Task DeclineInvite(Request request, Response response)
        {
            var userSession = await CheckAuth.Check(request, response, AuthLevel.Normal);
            if (userSession == null) return;

            var requestJson = request.Json<IidInviteRequest>();

            //判断请求是否有效
            if (!Validation.Valid(requestJson, out string reason))
            {
                await response.BadRequest(reason);
                return;
            }

            //读取目标iid
            var inviteDb = DbFactory.Get<Invite>();
            var inviteItem = inviteDb.SimpleDb.GetById(requestJson.iid);

            if (inviteItem == null)
            {
                await response.BadRequest("无效邀请");
                return;
            }

            if (inviteItem.to_uid != userSession.uid)
            {
                await response.BadRequest("无修改权限");
            }

            //将目标置为无效
            inviteItem.valid = 2;

            await inviteDb.SimpleDb.AsUpdateable(inviteItem).ExecuteCommandAsync();
            await inviteDb.InvalidateCache();

            await response.OK();
        }

        [HttpHandler("POST", "/accept-invite")]
        public async Task AcceptInvite(Request request, Response response)
        {
            var userSession = await CheckAuth.Check(request, response, AuthLevel.Normal);
            if (userSession == null) return;

            var requestJson = request.Json<IidInviteRequest>();

            //判断请求是否有效
            if (!Validation.Valid(requestJson, out string reason))
            {
                await response.BadRequest(reason);
                return;
            }

            //读取用户设置
            var userDb = DbFactory.Get<User>();
            var userList = await userDb.SelectAllFromCache();

            var user = userList.FirstOrDefault(it => it.uid == userSession.uid);

            if (user == null)
            {
                await response.BadRequest("活见鬼了");
                return;
            }

            //读取目标iid
            var inviteDb = DbFactory.Get<Invite>();
            var inviteItem = inviteDb.SimpleDb.GetById(requestJson.iid);

            if (inviteItem == null || inviteItem.valid != 1)
            {
                await response.BadRequest("无效邀请");
                return;
            }

            if (inviteItem.to_uid != userSession.uid)
            {
                await response.BadRequest("无修改权限");
            }

            //将目标置为无效
            inviteItem.valid = 3;

            await inviteDb.SimpleDb.AsUpdateable(inviteItem).ExecuteCommandAsync();
            await inviteDb.InvalidateCache();

            //取得该GID已绑定人数
            var groupBindDb = DbFactory.Get<UserGroupBind>();
            var groupBindList = await groupBindDb.SelectAllFromCache();

            var numberOfGroup = groupBindList.Count(it => it.gid == inviteItem.from_gid);

            if (numberOfGroup >= 5)
            {
                await response.BadRequest("组队人数已满，无法加入。");
                return;
            }

            //插入组绑定
            var newGroupBindDb = new user_group_bind
            {
                uid = user.uid,
                gid = inviteItem.from_gid,
                is_leader = 0
            };
            await groupBindDb.SimpleDb.AsInsertable(newGroupBindDb).ExecuteCommandAsync();
            await groupBindDb.InvalidateCache();

            //修改用户设置
            user.roleid = 2;
            user.update_time = DateTime.Now;

            await userDb.SimpleDb.AsUpdateable(user).ExecuteCommandAsync();
            await userDb.InvalidateCache();

            await response.OK();
        }
    }
}
