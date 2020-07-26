using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ccxc.Core.HttpServer;
using ccxc_backend.Controllers.Mail;
using ccxc_backend.DataModels;
using ccxc_backend.DataServices;
using SqlSugar;

namespace ccxc_backend.Controllers.Admin
{
    [Export(typeof(HttpController))]
    public class MessageAdminController : HttpController
    {

        [HttpHandler("POST", "/admin/add-message")]
        public async Task AddMessage(Request request, Response response)
        {
            var userSession = await CheckAuth.Check(request, response, AuthLevel.Organizer);
            if (userSession == null) return;

            var requestJson = request.Json<AddMessageRequest>();

            //判断请求是否有效
            if (!Validation.Valid(requestJson, out string reason))
            {
                await response.BadRequest(reason);
                return;
            }

            if (requestJson.gid <= 0)
            {
                await response.BadRequest("发送目标不正确");
                return;
            }

            //写入新消息
            var newMessage = new message
            {
                content = requestJson.content,
                update_time = DateTime.Now,
                create_time = DateTime.Now,
                gid = requestJson.gid,
                uid = userSession.uid,
                is_read = 0,
                direction = 1
            };

            var messageDb = DbFactory.Get<Message>();
            await messageDb.SimpleDb.AsInsertable(newMessage).ExecuteCommandAsync();
            await response.OK();
        }


        [HttpHandler("POST", "/admin/query-message")]
        public async Task QueryMessage(Request request, Response response)
        {
            var userSession = await CheckAuth.Check(request, response, AuthLevel.Organizer);
            if (userSession == null) return;

            var requestJson = request.Json<QueryMessageAdminRequest>();

            //判断请求是否有效
            if (!Validation.Valid(requestJson, out string reason))
            {
                await response.BadRequest(reason);
                return;
            }

            var messageDb = DbFactory.Get<Message>();

            const int pageSize = 20;
            var page = requestJson.page;
            if (page <= 0) page = 1;
            var totalCount = new RefAsync<int>(0);

            var orderType = requestJson.order == 0 ? OrderByType.Desc : OrderByType.Asc;

            var readType = requestJson.read switch
            {
                2 => 0,
                1 => 1,
                _ => 0
            };

            List<message> resultList;
            if (requestJson.gid == 0)
            {
                if (requestJson.read == 0)
                {
                    resultList = await messageDb.SimpleDb.AsQueryable()
                        .OrderBy(it => it.create_time, orderType).ToPageListAsync(page, pageSize, totalCount);
                }
                else
                {
                    resultList = await messageDb.SimpleDb.AsQueryable().Where(it => it.is_read == readType)
                        .OrderBy(it => it.create_time, orderType).ToPageListAsync(page, pageSize, totalCount);
                }
            }
            else
            {
                if (requestJson.read == 0)
                {
                    resultList = await messageDb.SimpleDb.AsQueryable().Where(it => it.gid == requestJson.gid)
                        .OrderBy(it => it.create_time, orderType).ToPageListAsync(page, pageSize, totalCount);
                }
                else
                {
                    resultList = await messageDb.SimpleDb.AsQueryable().Where(it => it.is_read == readType && it.gid == requestJson.gid)
                        .OrderBy(it => it.create_time, orderType).ToPageListAsync(page, pageSize, totalCount);
                }
            }

            //生成结果
            var userDb = DbFactory.Get<User>();
            var userDict = (await userDb.SelectAllFromCache()).ToDictionary(it => it.uid, it => it);

            var groupDb = DbFactory.Get<UserGroup>();
            var groupNameDict = (await groupDb.SelectAllFromCache()).ToDictionary(it => it.gid, it => it.groupname);
            var resList = resultList.Select(it =>
            {
                var r = new MessageView(it);
                if (!userDict.ContainsKey(r.uid)) return r;

                var u = userDict[r.uid];
                r.user_name = u.username;
                r.roleid = u.roleid;

                if (!groupNameDict.ContainsKey(r.gid)) return r;
                r.group_name = groupNameDict[r.gid];
                return r;
            }).ToList();

            var res = new GetMailResponse
            {
                status = 1,
                page = requestJson.page,
                page_size = pageSize,
                total_count = totalCount.Value,
                messages = resList
            };
            await response.JsonResponse(200, res);
        }

        [HttpHandler("POST", "/admin/delete-message")]
        public async Task DeleteMessage(Request request, Response response)
        {
            var userSession = await CheckAuth.Check(request, response, AuthLevel.Organizer);
            if (userSession == null) return;

            var requestJson = request.Json<MessageAdminRequest>();

            //判断请求是否有效
            if (!Validation.Valid(requestJson, out string reason))
            {
                await response.BadRequest(reason);
                return;
            }

            var messageDb = DbFactory.Get<Message>();

            await messageDb.SimpleDb.AsDeleteable().Where(it => it.mid == requestJson.mid).ExecuteCommandAsync();
            await response.OK();
        }

        [HttpHandler("POST", "/admin/set-read-message")]
        public async Task SetReadMessage(Request request, Response response)
        {
            var userSession = await CheckAuth.Check(request, response, AuthLevel.Organizer);
            if (userSession == null) return;

            var requestJson = request.Json<MessageAdminRequest>();

            //判断请求是否有效
            if (!Validation.Valid(requestJson, out string reason))
            {
                await response.BadRequest(reason);
                return;
            }

            var messageDb = DbFactory.Get<Message>();

            await messageDb.SimpleDb.AsUpdateable(new message
            {
                mid = requestJson.mid,
                is_read = 1
            }).UpdateColumns(it => new {it.is_read}).WhereColumns(it => it.mid).ExecuteCommandAsync();
            await response.OK();
        }
    }
}
