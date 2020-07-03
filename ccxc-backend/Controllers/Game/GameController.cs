using Ccxc.Core.HttpServer;
using ccxc_backend.DataModels;
using ccxc_backend.DataServices;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ccxc_backend.Controllers.Game
{
    [Export(typeof(HttpController))]
    public class GameController : HttpController
    {
        [HttpHandler("POST", "/start")]
        public async Task Start(Request request, Response response)
        {
            var userSession = await CheckAuth.Check(request, response, AuthLevel.Member, true);
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

            //取得进度
            var progressDb = DbFactory.Get<Progress>();
            var progress = await progressDb.SimpleDb.AsQueryable().Where(it => it.gid == gid).FirstAsync();
            if (progress == null)
            {
                //初始化——取得第一个区权限
                var puzzleGroupDb = DbFactory.Get<PuzzleGroup>();
                var puzzleGroupList = await puzzleGroupDb.SelectAllFromCache();
                var firstPuzzleGroup = puzzleGroupList.OrderBy(it => it.pgid).FirstOrDefault();

                if(firstPuzzleGroup == null)
                {
                    await response.BadRequest("没有题目区域，无法开始答题。");
                    return;
                }

                var progressItem = new progress
                {
                    data = new SaveData
                    {
                        NowOpenPuzzleGroupId = firstPuzzleGroup.pgid
                    },
                    score = 0,
                    update_time = DateTime.Now,
                    is_finish = 0,
                    penalty = 0
                };

                await progressDb.SimpleDb.AsInsertable(progressItem).IgnoreColumns(it => new { it.finish_time }).ExecuteCommandAsync();
                await progressDb.InvalidateCache();
            }

            await response.OK();
        }

        [HttpHandler("POST", "/play/get-puzzle-group")]
        public async Task GetPuzzleGroup(Request request, Response response)
        {
            var userSession = await CheckAuth.Check(request, response, AuthLevel.Member, true);
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

            //取得进度
            var progressDb = DbFactory.Get<Progress>();
            var progress = await progressDb.SimpleDb.AsQueryable().Where(it => it.gid == gid).FirstAsync();
            if (progress == null)
            {
                await response.BadRequest("没有进度，请返回首页重新开始。");
                return;
            }

            //获得所有PuzzleGroup
            var puzzleGroupDb = DbFactory.Get<PuzzleGroup>();
            var puzzleGroupList = (await puzzleGroupDb.SelectAllFromCache()).OrderBy(it => it.pgid);
        }
    }
}
