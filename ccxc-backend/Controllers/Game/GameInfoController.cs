using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ccxc.Core.HttpServer;
using ccxc_backend.DataModels;
using ccxc_backend.DataServices;
using SqlSugar;

namespace ccxc_backend.Controllers.Game
{
    [Export(typeof(HttpController))]
    public class GameInfoController : HttpController
    {
        [HttpHandler("POST", "/play/get-last-answer-log")]
        public async Task GetLastAnswerLog(Request request, Response response)
        {
            var userSession = await CheckAuth.Check(request, response, AuthLevel.Member, true);
            if (userSession == null) return;

            var requestJson = request.Json<GetLastAnswerLogRequest>();

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

            //取得进度
            var progressDb = DbFactory.Get<Progress>();
            var progress = await progressDb.SimpleDb.AsQueryable().Where(it => it.gid == gid).FirstAsync();
            if (progress == null)
            {
                await response.BadRequest("没有进度，请返回首页重新开始。");
                return;
            }

            var progressData = progress.data;
            if (progressData == null)
            {
                await response.BadRequest("未找到可用存档，请联系管理员。");
                return;
            }

            //取得答题历史
            var answerLogDb = DbFactory.Get<AnswerLog>();
            var answerList = await answerLogDb.SimpleDb.AsQueryable()
                .Where(it =>
                    it.gid == gid && it.pid == requestJson.pid && (it.status == 1 || it.status == 2 || it.status == 3))
                .OrderBy(it => it.create_time, OrderByType.Desc)
                .Take(10)
                .ToListAsync();

            //取得用户名缓存
            var userDb = DbFactory.Get<User>();
            var userNameDict = (await userDb.SelectAllFromCache()).ToDictionary(it => it.uid, it => it.username);

            var resultList = answerList.Select(it => new AnswerLogView(it)
            {
                user_name = userNameDict.ContainsKey(it.uid) ? userNameDict[it.uid] : ""
            }).ToList();

            await response.JsonResponse(200, new GetLastAnswerLogResponse
            {
                status = 1,
                answer_log = resultList
            });
        }
    }
}
