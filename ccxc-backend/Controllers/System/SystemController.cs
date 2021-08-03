using Ccxc.Core.HttpServer;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Text;
using System.Threading.Tasks;
using ccxc_backend.DataModels;
using ccxc_backend.DataServices;
using System.Linq;

namespace ccxc_backend.Controllers.System
{
    [Export(typeof(HttpController))]
    public class SystemController : HttpController
    {
        [HttpHandler("POST", "/get-default-setting")]
        public async Task GetDefaultSetting(Request request, Response response)
        {
            await response.JsonResponse(200, new DefaultSettingResponse
            {
                status = 1,
                start_time = Config.Config.Options.StartTime
            });
        }

        [HttpHandler("POST", "/heartbeat")]
        public async Task HeartBeat(Request request, Response response)
        {
            var userSession = await CheckAuth.Check(request, response, AuthLevel.Normal);
            if (userSession == null) return;

            await response.JsonResponse(200, new
            {
                status = 1,
            });
        }

        [HttpHandler("POST", "/heartbeat-puzzle")]
        public async Task HeartBeatPuzzle(Request request, Response response)
        {
            var userSession = await CheckAuth.Check(request, response, AuthLevel.Normal);
            if (userSession == null) return;

            var cache = DbFactory.GetCache();
            var maxIdKey = "/ccxc-backend/datacache/last_announcement_id";
            var maxId = await cache.Get<int>(maxIdKey);

            var userReadKey = cache.GetCacheKey($"max_read_anno_id_for_{userSession.uid}");
            var userRead = await cache.Get<int>(userReadKey);

            var unread = maxId - userRead;

            var newMessage = 0; //新消息数目
            //取得该用户GID
            var groupBindDb = DbFactory.Get<UserGroupBind>();
            var groupBindList = await groupBindDb.SelectAllFromCache();

            var groupBindItem = groupBindList.FirstOrDefault(it => it.uid == userSession.uid);
            if (groupBindItem != null)
            {
                var gid = groupBindItem.gid;
                var messageDb = DbFactory.Get<Message>();
                newMessage = await messageDb.SimpleDb.AsQueryable()
                    .Where(it => it.gid == gid && it.direction == 1 && it.is_read == 0).CountAsync();
            }



            await response.JsonResponse(200, new
            {
                status = 1,
                unread,
                new_message = newMessage
            });
        }


        [HttpHandler("POST", "/get-scoreboard-info")]
        public async Task GetScoreBoardInfo(Request request, Response response)
        {
            var groupDb = DbFactory.Get<UserGroup>();
            var groupList = await groupDb.SelectAllFromCache();

            var progressDb = DbFactory.Get<Progress>();
            var progressList = await progressDb.SimpleDb.AsQueryable().ToListAsync();
            var progressDict = progressList.ToDictionary(it => it.gid, it => it);

            var scoreBoardList = groupList.Select(it =>
            {
                var r = new ScoreBoardItem
                {
                    gid = it.gid,
                    group_name = it.groupname,
                    group_profile = it.profile
                };

                if (progressDict.ContainsKey(it.gid))
                {
                    var progress = progressDict[it.gid];
                    r.is_finish = progress.is_finish;

                    if (r.is_finish == 1)
                    {
                        r.total_time =
                            (progress.finish_time -
                             Ccxc.Core.Utils.UnixTimestamp.FromTimestamp(Config.Config.Options.StartTime)).TotalHours;
                    }

                    r.score = progress.score;
                    r.finished_puzzle_count = progress.data.FinishedPuzzles.Count;
                }

                return r;
            }).ToList();

            var res = new ScoreBoardResponse
            {
                status = 1,
                finished_groups = scoreBoardList.Where(it => it.is_finish == 1).OrderBy(it => it.total_time).ThenBy(it => it.finished_puzzle_count).ToList(),
                groups = scoreBoardList.Where(it => it.is_finish != 1).OrderByDescending(it => it.score).ThenBy(it => it.gid).ToList()
            };

            await response.JsonResponse(200, res);
        }
    }
}
