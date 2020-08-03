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
                    data = new SaveData
                    {
                        NowOpenPuzzleGroups = new HashSet<int>(){ firstPuzzleGroup.pgid }
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
                open_group_count = progressData.NowOpenPuzzleGroups.Count,
                finished_puzzle_count = progressData.FinishedPuzzles.Count,
                is_open_next_group = progressData.IsOpenNextGroup ? 1 : 0,
                score = progress.score,
                penalty = progress.penalty
            };
            await response.JsonResponse(200, res);
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

            var progressData = progress.data;
            if (progressData == null)
            {
                await response.BadRequest("未找到可用存档，请联系管理员。");
                return;
            }

            var res = new GetPuzzleGroupResponse
            {
                is_open_next_group = progressData.IsOpenNextGroup ? 1 : 0,
                is_open_pre_final = progressData.IsOpenPreFinal ? 1 : 0,
                is_open_final_meta = progressData.IsOpenFinalMeta ? 1 : 0
            };

            //获得所有PuzzleGroup
            var puzzleGroupDb = DbFactory.Get<PuzzleGroup>();
            var puzzleGroupList = (await puzzleGroupDb.SelectAllFromCache()).OrderBy(it => it.pgid);

            if(progressData.FinishedGroups.Count <= 0)
            {
                //第一区域，只能看到当前一个区域内容
                var firstPuzzleGroup = puzzleGroupList
                    .Where(it => it.is_hide == 0 && progressData.NowOpenPuzzleGroups.Contains(it.pgid))
                    .OrderBy(it => it.pgid)
                    .ToList();
                if (firstPuzzleGroup.Count > 0)
                {
                    res.puzzle_groups = firstPuzzleGroup.Select(it => new PuzzleGroupView(it)
                    {
                        is_finish = 0,
                        is_open = 1
                    }).ToList();
                }
            }
            else
            {
                //可见所有未隐藏组
                res.puzzle_groups = puzzleGroupList.Where(it => it.is_hide == 0).OrderBy(it => it.pgid).Select(it => new PuzzleGroupView(it)
                {
                    is_finish = progressData.FinishedGroups.Contains(it.pgid) ? 1 : 0,
                    is_open = progressData.NowOpenPuzzleGroups.Contains(it.pgid) ? 1 : 0
                }).ToList();
            }

            res.status = 1;
            await response.JsonResponse(200, res);
        }

        [HttpHandler("POST", "/play/get-puzzle-list")]
        public async Task GetPuzzleList(Request request, Response response)
        {
            var userSession = await CheckAuth.Check(request, response, AuthLevel.Member, true);
            if (userSession == null) return;

            var requestJson = request.Json<GetPuzzleListRequest>();

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

            if (!progressData.NowOpenPuzzleGroups.Contains(requestJson.pgid))
            {
                await response.Unauthorized("不能访问您未打开的区域; Eno=0");
                return;
            }

            //取得题目组详情
            var puzzleGroupDb = DbFactory.Get<PuzzleGroup>();
            var puzzleGroupItem = (await puzzleGroupDb.SelectAllFromCache()).FirstOrDefault(it => it.pgid == requestJson.pgid);

            //取得题目详情
            var puzzleDb = DbFactory.Get<Puzzle>();
            var puzzleList = (await puzzleDb.SelectAllFromCache()).Where(it => it.pgid == requestJson.pgid).OrderBy(it => it.pid);

            var puzzleOverviewList = puzzleList.Select(it => new PuzzleOverview(it)
            {
                is_finish = progressData.FinishedPuzzles.Contains(it.pid) ? 1 : 0
            }).ToList();

            //返回
            await response.JsonResponse(200, new GetPuzzleListResponse
            {
                status = 1,
                puzzle_group_info = puzzleGroupItem,
                puzzle_list = puzzleOverviewList
            });
        }

        [HttpHandler("POST", "/play/get-pre-final-puzzle-list")]
        public async Task GetPreFinalPuzzleList(Request request, Response response)
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

            if (!progressData.IsOpenPreFinal)
            {
                await response.Unauthorized("不能访问您未打开的区域; Eno=1");
                return;
            }

            //取得题目详情
            var puzzleDb = DbFactory.Get<Puzzle>();
            var puzzleList = (await puzzleDb.SelectAllFromCache()).Where(it => it.answer_type == 2).OrderBy(it => it.pid); //answer_type == 2 PreFinalMeta

            var puzzleOverviewList = puzzleList.Select(it => new PuzzleOverview(it)
            {
                is_finish = progressData.FinishedPuzzles.Contains(it.pid) ? 1 : 0
            }).ToList();

            //返回
            await response.JsonResponse(200, new GetFinalMetaPuzzleListResponse
            {
                status = 1,
                puzzle_list = puzzleOverviewList
            });
        }

        [HttpHandler("POST", "/play/get-final-meta-puzzle-list")]
        public async Task GetFinalMetaPuzzleList(Request request, Response response)
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

            if (!progressData.IsOpenFinalMeta)
            {
                await response.Unauthorized("不能访问您未打开的区域; Eno=1");
                return;
            }

            //取得题目详情
            var puzzleDb = DbFactory.Get<Puzzle>();
            var puzzleList = (await puzzleDb.SelectAllFromCache()).Where(it => it.answer_type == 3).OrderBy(it => it.pid); //answer_type == 3 FinalMeta

            var puzzleOverviewList = puzzleList.Select(it => new PuzzleOverview(it)
            {
                is_finish = progressData.FinishedPuzzles.Contains(it.pid) ? 1 : 0
            }).ToList();

            //返回
            await response.JsonResponse(200, new GetFinalMetaPuzzleListResponse
            {
                status = 1,
                puzzle_list = puzzleOverviewList
            });
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
            //  FinalMeta需存档可见
            if (puzzleItem.answer_type == 2)
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
                        pgid = 0,
                        extend_content = isFinished ? puzzleItem.extend_content : "",
                        is_finish = isFinished ? 1 : 0
                    }
                };
                await response.JsonResponse(200, prePuzzleRes);
                return;
            }

            if (puzzleItem.answer_type == 3)
            {
                if (!progressData.IsOpenFinalMeta)
                {
                    await response.Unauthorized("不能访问您未打开的区域");
                    return;
                }

                var fmPuzzleRes = new GetPuzzleDetailResponse
                {
                    status = 1,
                    puzzle = new PuzzleView(puzzleItem)
                    {
                        pgid = 0,
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
                await response.BadRequest("当前题目不属于任何有效的题目组，无法打开。2");
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
            //  其他题目需当前题目组已完成或开放
            else if (!progressData.NowOpenPuzzleGroups.Contains(puzzleItem.pgid))
            {
                await response.Unauthorized("不能访问您未打开的区域");
                return;
            }

            var res = new GetPuzzleDetailResponse
            {
                status = 1,
                puzzle = new PuzzleView(puzzleItem)
                {
                    pg_name = thisPuzzleGroup.pg_name,
                    extend_content = isFinished ? puzzleItem.extend_content : "",
                    is_finish = isFinished ? 1 : 0
                }
            };

            //隐藏区域不显示分组
            if (thisPuzzleGroup.is_hide == 1)
            {
                res.puzzle.pgid = 0;
                res.puzzle.pg_name = "";
            }

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
