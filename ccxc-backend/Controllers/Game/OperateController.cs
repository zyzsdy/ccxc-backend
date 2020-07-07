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
    public class OperateController : HttpController
    {
        [HttpHandler("POST", "/unlock-group")]
        public async Task UnlockGroup(Request request, Response response)
        {
            var userSession = await CheckAuth.Check(request, response, AuthLevel.Member, true);
            if (userSession == null) return;

            var requestJson = request.Json<UnlockGroupRequest>();

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

            if (!progressData.IsOpenNextGroup)
            {
                await response.BadRequest("还不能开放下一个区域");
                return;
            }

            if(progressData.NowOpenPuzzleGroupId == requestJson.unlock_puzzle_group_id || progressData.FinishedGroups.Contains(requestJson.unlock_puzzle_group_id))
            {
                await response.BadRequest("选择的区域已开放");
                return;
            }

            //取得分组列表
            var groupDb = DbFactory.Get<PuzzleGroup>();
            var groupIdSet = new HashSet<int>((await groupDb.SelectAllFromCache()).Where(it => it.is_hide == 0).Select(it => it.pgid));

            if (!groupIdSet.Contains(requestJson.unlock_puzzle_group_id))
            {
                await response.BadRequest("选择的区域不存在");
                return;
            }

            //修改当前启用的分组
            progress.data.IsOpenNextGroup = false;
            progress.data.NowOpenPuzzleGroupId = requestJson.unlock_puzzle_group_id;
            progress.update_time = DateTime.Now;

            await progressDb.SimpleDb.AsUpdateable(progress).ExecuteCommandAsync();
            await response.OK();
        }
    }
}
