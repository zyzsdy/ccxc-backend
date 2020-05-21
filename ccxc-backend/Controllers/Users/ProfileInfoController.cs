using Ccxc.Core.HttpServer;
using ccxc_backend.DataModels;
using ccxc_backend.DataServices;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ccxc_backend.Controllers.Users
{
    [Export(typeof(HttpController))]
    public class ProfileInfoController : HttpController
    {
        [HttpHandler("POST", "/get-profileInfo")]
        public async Task UserReg(Request request, Response response)
        {
            var userSession = await CheckAuth.Check(request, response, AuthLevel.Normal);
            if (userSession == null) return;

            var userDb = DbFactory.Get<User>();
            var userDict = (await userDb.SelectAllFromCache()).ToDictionary(it => it.uid, it => it);

            var res = new MyProfileResponse();
            if (!userDict.ContainsKey(userSession.uid))
            {
                await response.Unauthorized("服务器提出了一个问题：你为何存在？");
                return;
            }

            //取出当前用户信息
            res.user_info = new UserInfo(userDict[userSession.uid]);

            //读取分组信息
            var groupDb = DbFactory.Get<UserGroup>();
            var groupDict = (await groupDb.SelectAllFromCache()).ToDictionary(it => it.gid, it => it);

            var groupBindDb = DbFactory.Get<UserGroupBind>();
            var groupBindList = await groupBindDb.SelectAllFromCache();
            var groupBindDict = groupBindList.GroupBy(it => it.gid).ToDictionary(it => it.Key, it => it.ToList());

            var groupBindItem = groupBindList.FirstOrDefault(it => it.uid == userSession.uid);
            if(groupBindItem != null)
            {
                var gid = groupBindItem.gid;
                if (groupDict.ContainsKey(gid))
                {
                    res.group_info = new GroupInfo(groupDict[gid])
                    {
                        member_list = new List<UserInfo>()
                    };

                    if (groupBindDict.ContainsKey(gid))
                    {
                        res.group_info.member_list = groupBindDict[gid].Select(it =>
                        {
                            var memberUid = it.uid;
                            if (userDict.ContainsKey(memberUid))
                            {
                                var sUser = new UserInfo(userDict[memberUid]);
                                sUser.email = "";
                                sUser.phone = "";
                                return sUser;
                            }
                            else
                            {
                                return new UserInfo
                                {
                                    uid = memberUid
                                };
                            }
                        }).ToList();
                    }
                }
            }

            res.status = 1;
            await response.JsonResponse(200, res);
        }
    }
}
