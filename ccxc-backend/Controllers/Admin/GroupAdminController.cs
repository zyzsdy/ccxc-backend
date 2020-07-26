using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ccxc.Core.HttpServer;
using ccxc_backend.DataModels;
using ccxc_backend.DataServices;

namespace ccxc_backend.Controllers.Admin
{
    [Export(typeof(HttpController))]
    public class GroupAdminController : HttpController
    {
        [HttpHandler("POST", "/admin/list-group-name")]
        public async Task ListGroupName(Request request, Response response)
        {
            var userSession = await CheckAuth.Check(request, response, AuthLevel.Organizer);
            if (userSession == null) return;

            var userGroupDb = DbFactory.Get<UserGroup>();
            var groupList = (await userGroupDb.SelectAllFromCache()).Select(it => new UserGroupNameInfo
            {
                gid = it.gid,
                groupname = it.groupname
            }).ToList();

            await response.JsonResponse(200, new UserGroupNameListResponse
            {
                status = 1,
                group_name_list = groupList
            });
        }

        [HttpHandler("POST", "/admin/add-penalty")]
        public async Task AddPenalty(Request request, Response response)
        {
            var userSession = await CheckAuth.Check(request, response, AuthLevel.Organizer);
            if (userSession == null) return;

            var requestJson = request.Json<GroupAdminRequest>();

            //判断请求是否有效
            if (!Validation.Valid(requestJson, out string reason))
            {
                await response.BadRequest(reason);
                return;
            }

            var penaltyIncrement = Config.Config.Options.PenaltyDefault;
            var progressDb = DbFactory.Get<Progress>();

            var groupProgress = await progressDb.SimpleDb.AsQueryable().Where(it => it.gid == requestJson.gid).FirstAsync();

            if (groupProgress == null)
            {
                await response.BadRequest("找不到指定队伍");
                return;
            }

            groupProgress.penalty += penaltyIncrement;

            await progressDb.SimpleDb.AsUpdateable(groupProgress).UpdateColumns(it => new {it.penalty})
                .ExecuteCommandAsync();
            await response.OK();
        }

        [HttpHandler("POST", "/admin/get-penalty")]
        public async Task GetPenalty(Request request, Response response)
        {
            var userSession = await CheckAuth.Check(request, response, AuthLevel.Organizer);
            if (userSession == null) return;

            var requestJson = request.Json<GroupAdminRequest>();

            //判断请求是否有效
            if (!Validation.Valid(requestJson, out string reason))
            {
                await response.BadRequest(reason);
                return;
            }

            var progressDb = DbFactory.Get<Progress>();

            var groupProgress = await progressDb.SimpleDb.AsQueryable().Where(it => it.gid == requestJson.gid).FirstAsync();

            if (groupProgress == null)
            {
                await response.BadRequest("找不到指定队伍");
                return;
            }

            await response.JsonResponse(200, new GetPenaltyResponse
            {
                status = 1,
                penalty = groupProgress.penalty
            });
        }
    }
}
