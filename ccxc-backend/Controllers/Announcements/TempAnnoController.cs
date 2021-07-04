using Ccxc.Core.HttpServer;
using ccxc_backend.DataModels;
using ccxc_backend.DataServices;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ccxc_backend.Controllers.Announcements
{
    [Export(typeof(HttpController))]
    public class TempAnnoController : HttpController
    {
        [HttpHandler("POST", "/admin/get-tempanno")]
        public async Task GetTempAnno(Request request, Response response)
        {
            var userSession = await CheckAuth.Check(request, response, AuthLevel.Organizer);
            if (userSession == null) return;

            var tempAnnoDb = DbFactory.Get<TempAnno>();
            var tempAnnoList = await tempAnnoDb.SimpleDb.AsQueryable().ToListAsync();

            var puzzleDb = DbFactory.Get<Puzzle>();
            var puzzleList = await puzzleDb.SelectAllFromCache();
            var puzzleDict = puzzleList.ToDictionary(it => it.pid, it => it.title);


            var result = tempAnnoList.Select(it => new temp_anno
            {
                pid = it.pid,
                create_time = it.create_time,
                content = it.content,
                is_pub = it.is_pub,
                puzzle_name = puzzleDict[it.pid]
            }).OrderBy(it => it.is_pub).ThenByDescending(it => it.create_time).ToList();

            await response.JsonResponse(200, new GetTempAnnoResponse
            {
                status = 1,
                temp_anno = result
            });
        }

        [HttpHandler("POST", "/admin/convert-tempanno")]
        public async Task ConvertTempAnno(Request request, Response response)
        {
            var userSession = await CheckAuth.Check(request, response, AuthLevel.Organizer);
            if (userSession == null) return;

            var requestJson = request.Json<ConvertTempAnnoRequest>();

            //判断请求是否有效
            if (!Validation.Valid(requestJson, out string reason))
            {
                await response.BadRequest(reason);
                return;
            }

            //取得待操作对象
            var tempAnnoDb = DbFactory.Get<TempAnno>();
            var tempAnnoItem = await tempAnnoDb.SimpleDb.AsQueryable().Where(x => x.pid == requestJson.pid).FirstAsync();

            if (tempAnnoItem == null)
            {
                await response.BadRequest($"获取 pid={requestJson.pid} 的信息出错。");
                return;
            }

            //添加新公告
            var annoDb = DbFactory.Get<Announcement>();
            await annoDb.NewAnnouncement(tempAnnoItem.content);

            //更新原对象
            tempAnnoItem.is_pub = 1;
            await tempAnnoDb.SimpleDb.AsUpdateable(tempAnnoItem).UpdateColumns(x => new { x.is_pub }).ExecuteCommandAsync();

            await response.OK();
        }

        [HttpHandler("POST", "/admin/del-tempanno")]
        public async Task DelTempAnno(Request request, Response response)
        {
            var userSession = await CheckAuth.Check(request, response, AuthLevel.Organizer);
            if (userSession == null) return;

            var requestJson = request.Json<ConvertTempAnnoRequest>();

            //判断请求是否有效
            if (!Validation.Valid(requestJson, out string reason))
            {
                await response.BadRequest(reason);
                return;
            }

            //删除待操作对象
            var tempAnnoDb = DbFactory.Get<TempAnno>();
            await tempAnnoDb.SimpleDb.AsDeleteable().Where(x => x.pid == requestJson.pid).ExecuteCommandAsync();

            await response.OK();
        }
    }
}
