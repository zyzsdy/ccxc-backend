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

        [HttpHandler("POST", "/admin/get-group-overview")]
        public async Task GetGroupOverview(Request request, Response response)
        {
            var userSession = await CheckAuth.Check(request, response, AuthLevel.Organizer);
            if (userSession == null) return;

            var requestJson = request.Json<GetGroupRequest>();

            //判断请求是否有效
            if (!Validation.Valid(requestJson, out string reason))
            {
                await response.BadRequest(reason);
                return;
            }

            var groupDb = DbFactory.Get<UserGroup>();
            var groupList = await groupDb.SelectAllFromCache();

            var progressDb = DbFactory.Get<Progress>();
            var progressList = await progressDb.SimpleDb.AsQueryable().ToListAsync();
            var progressDict = progressList.ToDictionary(it => it.gid, it => it);

            var resList = groupList.Select(it =>
            {
                var r = new GetGroupOverview
                {
                    gid = it.gid,
                    create_time = it.create_time,
                    groupname = it.groupname,
                    profile = it.profile
                };

                if (progressDict.ContainsKey(it.gid))
                {
                    var progress = progressDict[it.gid];
                    r.finished_puzzle_count = progress.data.FinishedPuzzles.Count;
                    r.opened_puzzle_groups = string.Join(", ", progress.data.NowOpenPuzzleGroups);
                    r.score = progress.score;
                    r.is_finish = progress.is_finish;
                    r.finish_time = progress.finish_time;
                    r.penalty = progress.penalty;

                    if (r.is_finish == 1)
                    {
                        r.total_time =
                            (progress.finish_time -
                             Ccxc.Core.Utils.UnixTimestamp.FromTimestamp(Config.Config.Options.StartTime)).TotalHours +
                            progress.penalty;
                    }
                }

                return r;
            });

            List<GetGroupOverview> res;
            if (requestJson.order == 0)
            {
                res = resList.OrderBy(it => it.gid).ToList();
            }
            else
            {
                res = resList.OrderByDescending(it => it.score).ToList();
            }

            await response.JsonResponse(200, new GetGroupOverviewResponse
            {
                status = 1,
                groups = res
            });
        }
    }
}
