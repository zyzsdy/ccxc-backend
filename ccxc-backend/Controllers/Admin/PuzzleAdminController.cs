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
    public class PuzzleAdminController : HttpController
    {
        [HttpHandler("POST", "/admin/add-puzzle")]
        public async Task AddPuzzle(Request request, Response response)
        {
            var userSession = await CheckAuth.Check(request, response, AuthLevel.Organizer);
            if (userSession == null) return;

            var requestJson = request.Json<AddPuzzleRequest>();

            //判断请求是否有效
            if (!Validation.Valid(requestJson, out string reason))
            {
                await response.BadRequest(reason);
                return;
            }

            //插入新题目
            var puzzleDb = DbFactory.Get<Puzzle>();
            var newPuzzle = new puzzle
            {
                pgid = requestJson.pgid,
                type = requestJson.type,
                title = requestJson.title,
                content = requestJson.content,
                image = requestJson.image,
                html = requestJson.html,
                answer_type = requestJson.answer_type,
                answer = requestJson.answer,
                jump_keyword = requestJson.jump_keyword,
                extend_content = requestJson.extend_content
            };

            await puzzleDb.SimpleDb.AsInsertable(newPuzzle).ExecuteCommandAsync();
            await puzzleDb.InvalidateCache();

            await response.OK();
        }

        [HttpHandler("POST", "/admin/delete-puzzle")]
        public async Task DeletePuzzle(Request request, Response response)
        {
            var userSession = await CheckAuth.Check(request, response, AuthLevel.Organizer);
            if (userSession == null) return;

            var requestJson = request.Json<DeletePuzzleRequest>();

            //判断请求是否有效
            if (!Validation.Valid(requestJson, out string reason))
            {
                await response.BadRequest(reason);
                return;
            }

            //删除它
            var puzzleDb = DbFactory.Get<Puzzle>();
            await puzzleDb.SimpleDb.AsDeleteable().Where(it => it.pid == requestJson.pid).ExecuteCommandAsync();
            await puzzleDb.InvalidateCache();

            await response.OK();
        }

        [HttpHandler("POST", "/admin/edit-puzzle")]
        public async Task EditPuzzle(Request request, Response response)
        {
            var userSession = await CheckAuth.Check(request, response, AuthLevel.Organizer);
            if (userSession == null) return;

            var requestJson = request.Json<EditPuzzleRequest>();

            //判断请求是否有效
            if (!Validation.Valid(requestJson, out string reason))
            {
                await response.BadRequest(reason);
                return;
            }

            //生成修改后对象
            var updatePuzzle = new puzzle
            {
                pid = requestJson.pid,
                pgid = requestJson.pgid,
                type = requestJson.type,
                title = requestJson.title,
                content = requestJson.content,
                image = requestJson.image,
                html = requestJson.html,
                answer_type = requestJson.answer_type,
                answer = requestJson.answer,
                jump_keyword = requestJson.jump_keyword,
                extend_content = requestJson.extend_content
            };

            var puzzleDb = DbFactory.Get<Puzzle>();
            await puzzleDb.SimpleDb.AsUpdateable(updatePuzzle).ExecuteCommandAsync();
            await puzzleDb.InvalidateCache();

            await response.OK();
        }

        [HttpHandler("POST", "/admin/get-puzzle")]
        public async Task GetPuzzle(Request request, Response response)
        {
            var userSession = await CheckAuth.Check(request, response, AuthLevel.Organizer);
            if (userSession == null) return;

            var puzzleDb = DbFactory.Get<Puzzle>();
            var puzzleList = (await puzzleDb.SelectAllFromCache()).OrderBy(it => it.pgid).ThenBy(it => it.pid);

            await response.JsonResponse(200, new GetPuzzleResponse
            {
                status = 1,
                puzzle = puzzleList.ToList()
            });
        }
    }
}
