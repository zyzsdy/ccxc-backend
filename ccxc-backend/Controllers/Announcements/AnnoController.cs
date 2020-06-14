using Ccxc.Core.HttpServer;
using ccxc_backend.DataModels;
using ccxc_backend.DataServices;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace ccxc_backend.Controllers.Announcements
{
    [Export(typeof(HttpController))]
    public class AnnoController : HttpController
    {
        [HttpHandler("POST", "/admin/add-announcement")]
        public async Task AddAnnouncement(Request request, Response response)
        {
            var now = DateTime.Now;

            var userSession = await CheckAuth.Check(request, response, AuthLevel.Administrator);
            if (userSession == null) return;

            var requestJson = request.Json<AddAnnoRequest>();

            //判断请求是否有效
            if (!Validation.Valid(requestJson, out string reason))
            {
                await response.BadRequest(reason);
                return;
            }

            //插入新公告
            var annoDb = DbFactory.Get<Announcement>();
            var newAnnouncement = new announcement
            {
                create_time = now,
                update_time = now,
                content = requestJson.content
            };

            await annoDb.SimpleDb.AsInsertable(newAnnouncement).ExecuteCommandAsync();
            await annoDb.InvalidateCache();

            await response.OK();
        }

        [HttpHandler("POST", "/admin/delete-announcement")]
        public async Task DeleteAnnouncement(Request request, Response response)
        {
            var userSession = await CheckAuth.Check(request, response, AuthLevel.Administrator);
            if (userSession == null) return;

            var requestJson = request.Json<DeleteAnnoRequest>();

            //判断请求是否有效
            if (!Validation.Valid(requestJson, out string reason))
            {
                await response.BadRequest(reason);
                return;
            }

            //删除它
            var annoDb = DbFactory.Get<Announcement>();
            await annoDb.SimpleDb.AsDeleteable().Where(it => it.aid == requestJson.aid).ExecuteCommandAsync();
            await annoDb.InvalidateCache();

            await response.OK();
        }

        [HttpHandler("POST", "/admin/edit-announcement")]
        public async Task EditAnnouncement(Request request, Response response)
        {
            var userSession = await CheckAuth.Check(request, response, AuthLevel.Administrator);
            if (userSession == null) return;

            var requestJson = request.Json<EditAnnoRequest>();

            //判断请求是否有效
            if (!Validation.Valid(requestJson, out string reason))
            {
                await response.BadRequest(reason);
                return;
            }

            //生成修改后对象
            var updateAnno = new announcement
            {
                aid = requestJson.aid,
                content = requestJson.content,
                update_time = DateTime.Now
            };

            var annoDb = DbFactory.Get<Announcement>();
            await annoDb.SimpleDb.AsUpdateable(updateAnno).IgnoreColumns(it => new { it.create_time }).ExecuteCommandAsync();
            await annoDb.InvalidateCache();

            await response.OK();
        }

        [HttpHandler("POST", "/get-announcement")]
        public async Task GetAnnouncement(Request request, Response response)
        {
            var requestJson = request.Json<GetAnnoRequest>();

            //判断请求是否有效
            if (!Validation.Valid(requestJson, out string reason))
            {
                await response.BadRequest(reason);
                return;
            }

            var annoDb = DbFactory.Get<Announcement>();
            var annoList = await annoDb.SelectAllFromCache();

            if(requestJson.aids != null && requestJson.aids.Count > 0)
            {
                var aidSet = new HashSet<int>(requestJson.aids);
                annoList = annoList.Where(it => aidSet.Contains(it.aid)).ToList();
            }

            await response.JsonResponse(200, new GetAnnoResponse
            {
                status = 1,
                announcements = annoList
            });
        }
    }
}
