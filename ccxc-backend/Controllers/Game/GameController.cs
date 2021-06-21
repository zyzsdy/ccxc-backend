using Ccxc.Core.HttpServer;
using ccxc_backend.DataModels;
using ccxc_backend.DataServices;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Runtime.CompilerServices;
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
                var firstPuzzleGroup = puzzleGroupList.Where(it => it.is_hide == 0).OrderBy(it => it.pgid).FirstOrDefault();

                if(firstPuzzleGroup == null)
                {
                    await response.BadRequest("没有题目区域，无法开始答题。");
                    return;
                }

                var progressItem = new progress
                {
                    gid = gid,
                    data = new SaveData(),
                    score = 0,
                    update_time = DateTime.Now,
                    is_finish = 0,
                    penalty = 0
                };

                await progressDb.SimpleDb.AsInsertable(progressItem).IgnoreColumns(it => new { it.finish_time }).ExecuteCommandAsync();
                await progressDb.InvalidateCache();
            }

            //登录信息存入Redis
            var ticket = $"0x{Guid.NewGuid():n}";

            var cache = DbFactory.GetCache();
            var ticketKey = cache.GetTempTicketKey(ticket);
            var ticketSession = new PuzzleLoginTicketSession
            {
                token = userSession.token
            };
            await cache.Put(ticketKey, ticketSession, 15000); //15秒内登录完成有效

            await response.JsonResponse(200, new PuzzleStartResponse
            {
                status = 1,
                ticket = ticket
            });
        }

        [HttpHandler("POST", "/play/get-prologue")]
        public async Task GetPrologue(Request request, Response response)
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

            var progressData = progress.data;
            if (progressData == null)
            {
                await response.BadRequest("未找到可用存档，请联系管理员。");
                return;
            }

            var groupDb = DbFactory.Get<PuzzleGroup>();
            var prologueGroup = (await groupDb.SelectAllFromCache()).First(it => it.pg_name == "prologue");

            var prologueResult = "";
            if (prologueGroup != null)
            {
                prologueResult = prologueGroup.pg_desc;
            }

            await response.JsonResponse(200, new BasicResponse
            {
                status = 1,
                message = prologueResult
            });
        }

        [HttpHandler("POST", "/play/get-corridor")]
        public async Task GetCorridor(Request request, Response response)
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

            var progressData = progress.data;
            if (progressData == null)
            {
                await response.BadRequest("未找到可用存档，请联系管理员。");
                return;
            }

            var groupDb = DbFactory.Get<PuzzleGroup>();
            var prologueGroup = (await groupDb.SelectAllFromCache()).First(it => it.pg_name == "corridor");

            var prologueResult = "";
            if (prologueGroup != null)
            {
                prologueResult = prologueGroup.pg_desc;
            }

            await response.JsonResponse(200, new BasicResponse
            {
                status = 1,
                message = prologueResult
            });
        }

        [HttpHandler("POST", "/play/get-game-info")]
        public async Task GetGameInfo(Request request, Response response)
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

            var progressData = progress.data;
            if (progressData == null)
            {
                await response.BadRequest("未找到可用存档，请联系管理员。");
                return;
            }

            var res = new GetGameInfoResponse
            {
                status = 1,
                score = progress.score,
                penalty = progress.penalty
            };
            await response.JsonResponse(200, res);
        }

        [HttpHandler("POST", "/play/get-clue-matrix")]
        public async Task GetClueMatrix(Request request, Response response)
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

            var progressData = progress.data;
            if (progressData == null)
            {
                await response.BadRequest("未找到可用存档，请联系管理员。");
                return;
            }

            var cache = DbFactory.GetCache();
            var openedGroupKey = cache.GetDataKey("opened-groups");

            var openedGroup = await cache.Get<int>(openedGroupKey);
            if (openedGroup < 1) openedGroup = 1;

            var puzzleDb = DbFactory.Get<Puzzle>();
            var avaliablePuzzleList = await puzzleDb.SimpleDb.AsQueryable().Where(it => it.pgid <= openedGroup && it.answer_type == 0).ToListAsync();

            var simpleList = avaliablePuzzleList.Select(it =>
            {
                var coord = it.extend_data.Split(",");
                int.TryParse(coord[0], out int x);
                int.TryParse(coord[1], out int y);

                var r = new SimplePuzzle
                {
                    pid = it.pid,
                    title = it.title,
                    x = x,
                    y = y,
                    is_finished = progressData.FinishedPuzzles.Contains(it.pid) ? 1 : 0
                };

                return r;
            }).ToList();

            var res = new GetClueMatrixResponse
            {
                status = 1,
                simple_puzzles = simpleList
            };
            await response.JsonResponse(200, res);
        }

        [HttpHandler("POST", "/play/get-puzzle-detail")]
        public async Task GetPuzzleDetail(Request request, Response response)
        {
            var userSession = await CheckAuth.Check(request, response, AuthLevel.Member, true);
            if (userSession == null) return;

            var requestJson = request.Json<GetPuzzleDetailRequest>();

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

            //题目组信息
            var puzzleGroupDb = DbFactory.Get<PuzzleGroup>();
            var puzzleGroupDict = (await puzzleGroupDb.SelectAllFromCache()).ToDictionary(it => it.pgid, it => it);

            //取得题目详情
            var puzzleDb = DbFactory.Get<Puzzle>();
            var puzzleItem = (await puzzleDb.SelectAllFromCache()).FirstOrDefault(it => it.pid == requestJson.pid);

            var isFinished = progressData.FinishedPuzzles.Contains(requestJson.pid);

            if (puzzleItem == null)
            {
                await response.Unauthorized("不能访问您未打开的区域");
                return;
            }

            //检查是否可见
            //prefinal区域需要存档已开放
            if (puzzleItem.pgid == 4)  //pgid == 4, 中间存档开放
            {
                if (!progressData.IsOpenPreFinal)
                {
                    await response.Unauthorized("不能访问您未打开的区域");
                    return;
                }

                var prePuzzleRes = new GetPuzzleDetailResponse
                {
                    status = 1,
                    puzzle = new PuzzleView(puzzleItem)
                    {
                        extend_content = isFinished ? puzzleItem.extend_content : "",
                        is_finish = isFinished ? 1 : 0
                    }
                };
                await response.JsonResponse(200, prePuzzleRes);
                return;
            }

            //final区域需要验证存档已开放
            if (puzzleItem.pgid == 5) //pgid == 5, 最终部分开放
            {
                if (!progressData.IsOpenFinalStage)
                {
                    await response.Unauthorized("不能访问您未打开的区域");
                    return;
                }

                var fmPuzzleRes = new GetPuzzleDetailResponse
                {
                    status = 1,
                    puzzle = new PuzzleView(puzzleItem)
                    {
                        extend_content = isFinished ? puzzleItem.extend_content : "",
                        is_finish = isFinished ? 1 : 0
                    }
                };
                await response.JsonResponse(200, fmPuzzleRes);
                return;
            }

            //取得当前题目组
            if (!puzzleGroupDict.ContainsKey(puzzleItem.pgid))
            {
                await response.BadRequest("当前题目不属于任何有效的题目组，无法打开。");
                return;
            }
            var thisPuzzleGroup = puzzleGroupDict[puzzleItem.pgid];
            if (thisPuzzleGroup == null)
            {
                await response.BadRequest("当前题目不属于任何有效的题目组，无法打开。code: 2");
                return;
            }
            //  隐藏区域需已获得条件开放
            if (thisPuzzleGroup.is_hide == 1)
            {
                if (!progressData.OpenedHidePuzzles.Contains(puzzleItem.pid))
                {
                    await response.Unauthorized("不能访问您未打开的区域; Eno=3");
                    return;
                }
            }


            //取得普通小题已经打开的区域（1~3）
            var cache = DbFactory.GetCache();
            var openedGroupKey = cache.GetDataKey("opened-groups");

            var openedGroup = await cache.Get<int>(openedGroupKey);

            if (puzzleItem.pgid > openedGroup)
            {
                await response.Unauthorized("不能访问您未打开的区域");
                return;
            }



            var res = new GetPuzzleDetailResponse
            {
                status = 1,
                puzzle = new PuzzleView(puzzleItem)
                {
                    extend_content = isFinished ? puzzleItem.extend_content : "",
                    is_finish = isFinished ? 1 : 0
                }
            };

            await response.JsonResponse(200, res);
        }

        [HttpHandler("POST", "/play/get-final-info")]
        public async Task GetFinalInfo(Request request, Response response)
        {
            var userSession = await CheckAuth.Check(request, response, AuthLevel.Member, true);
            if (userSession == null) return;

            var requestJson = request.Json<GetPuzzleDetailRequest>();

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

            //取得Final题目
            var puzzleDb = DbFactory.Get<Puzzle>();
            var puzzleItem = (await puzzleDb.SelectAllFromCache()).First(it => it.answer_type == 3);

            var finalInfo = "";

            if (puzzleItem != null)
            {
                if (progressData.FinishedPuzzles.Contains(puzzleItem.pid))
                {
                    //题目组信息
                    var puzzleGroupDb = DbFactory.Get<PuzzleGroup>();
                    var finalGroup = (await puzzleGroupDb.SelectAllFromCache()).First(it => it.pgid == puzzleItem.pgid);

                    if (finalGroup != null)
                    {
                        finalInfo = finalGroup.pg_desc;
                    }
                }
            }

            await response.JsonResponse(200, new GetFinalInfoResponse
            {
                status = 1,
                desc = finalInfo
            });
        }
    }
}
