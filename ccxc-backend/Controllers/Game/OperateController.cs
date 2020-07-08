﻿using Ccxc.Core.HttpServer;
using ccxc_backend.DataModels;
using ccxc_backend.DataServices;
using SqlSugar;
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

        [HttpHandler("POST", "/check-answer")]
        public async Task CheckAnswer(Request request, Response response)
        {
            var userSession = await CheckAuth.Check(request, response, AuthLevel.Member, true);
            if (userSession == null) return;

            var requestJson = request.Json<CheckAnswerRequest>();

            //判断请求是否有效
            if (!Validation.Valid(requestJson, out string reason))
            {
                await response.BadRequest(reason);
                return;
            }

            var answerLogDb = DbFactory.Get<AnswerLog>();
            var answerLog = new answer_log
            {
                create_time = DateTime.Now,
                uid = userSession.uid,
                pid = requestJson.pid,
                answer = requestJson.answer
            };

            //取得该用户GID
            var groupBindDb = DbFactory.Get<UserGroupBind>();
            var groupBindList = await groupBindDb.SelectAllFromCache();

            var groupBindItem = groupBindList.FirstOrDefault(it => it.uid == userSession.uid);
            if (groupBindItem == null)
            {
                answerLog.status = 5;
                await answerLogDb.SimpleDb.AsInsertable(answerLog).ExecuteCommandAsync();

                await response.BadRequest("未确定组队？");
                return;
            }

            var gid = groupBindItem.gid;
            answerLog.gid = gid;

            //取得进度
            var progressDb = DbFactory.Get<Progress>();
            var progress = await progressDb.SimpleDb.AsQueryable().Where(it => it.gid == gid).FirstAsync();
            if (progress == null)
            {
                answerLog.status = 5;
                await answerLogDb.SimpleDb.AsInsertable(answerLog).ExecuteCommandAsync();

                await response.BadRequest("没有进度，请返回首页重新开始。");
                return;
            }

            var progressData = progress.data;
            if (progressData == null)
            {
                answerLog.status = 5;
                await answerLogDb.SimpleDb.AsInsertable(answerLog).ExecuteCommandAsync();

                await response.BadRequest("未找到可用存档，请联系管理员。");
                return;
            }

            //取出待判定题目
            var puzzleDb = DbFactory.Get<Puzzle>();
            var puzzleItem = (await puzzleDb.SelectAllFromCache()).Where(it => it.pid == requestJson.pid).First();

            if (puzzleItem == null)
            {
                answerLog.status = 4;
                await answerLogDb.SimpleDb.AsInsertable(answerLog).ExecuteCommandAsync();

                await response.BadRequest("题目不存在或未解锁。");
                return;
            }

            //FinalMeta需存档可见
            if (puzzleItem.answer_type == 2)
            {
                if (!progressData.IsOpenFinalMeta)
                {
                    answerLog.status = 4;
                    await answerLogDb.SimpleDb.AsInsertable(answerLog).ExecuteCommandAsync();

                    await response.BadRequest("题目不存在或未解锁。");
                    return;
                }
            }
            //需判定题目组已开放或者题目本身作为隐藏题目开放
            if (progressData.NowOpenPuzzleGroupId != puzzleItem.pgid && !progressData.FinishedGroups.Contains(puzzleItem.pgid) && !progressData.OpenedHidePuzzles.Contains(puzzleItem.pid))
            {
                answerLog.status = 4;
                await answerLogDb.SimpleDb.AsInsertable(answerLog).ExecuteCommandAsync();

                await response.BadRequest("题目不存在或未解锁。");
                return;
            }

            //取得最后一次错误答题记录
            var lastWrongTime = DateTime.MinValue;
            var lastWrongAnswer = await answerLogDb.SimpleDb.AsQueryable().Where(it => it.gid == gid && it.status != 1)
                .OrderBy(it => it.create_time, SqlSugar.OrderByType.Desc).FirstAsync();

            if(lastWrongAnswer != null)
            {
                lastWrongTime = lastWrongAnswer.create_time;
            }

            //判断是否在冷却时间内
            var coolTime = (DateTime.Now - lastWrongTime).TotalSeconds;
            if(coolTime <= Config.Config.Options.CooldownTime)
            {
                var remainTime = Config.Config.Options.CooldownTime - coolTime;
                answerLog.status = 3;
                await answerLogDb.SimpleDb.AsInsertable(answerLog).ExecuteCommandAsync();

                await response.JsonResponse(406, new AnswerResponse //使用 406 Not Acceptable 作为答案错误的专用返回码。若返回 200 OK 则为答案正确
                {
                    status = 1,
                    answer_status = 3,
                    message = $"冷却中，还有 {remainTime:F0} 秒",
                    cooldown_remain_seconds = remainTime
                });
                return;
            }

            //判断答案是否正确
            if (puzzleItem.answer != requestJson.answer)
            {
                var remainTime = Config.Config.Options.CooldownTime - coolTime;
                answerLog.status = 2;
                await answerLogDb.SimpleDb.AsInsertable(answerLog).ExecuteCommandAsync();

                await response.JsonResponse(406, new AnswerResponse //使用 406 Not Acceptable 作为答案错误的专用返回码。若返回 200 OK 则为答案正确
                {
                    status = 1,
                    answer_status = 2,
                    message = $"答案错误"
                });
                return;
            }

            //答案正确
            answerLog.status = 1;
            await answerLogDb.SimpleDb.AsInsertable(answerLog).ExecuteCommandAsync();

            //回写存档
            progress.data.FinishedPuzzles.Add(puzzleItem.pid);

            //检查是否可以完成本区域
            if (puzzleItem.pgid == progressData.NowOpenPuzzleGroupId && puzzleItem.answer_type == 1)
            {
                //当前分组的区域meta成功解出
                progress.data.IsOpenNextGroup = true;
                progress.data.FinishedGroups.Add(progressData.NowOpenPuzzleGroupId);
            }

            //检查是否符合开放FinalMeta条件
            if (progress.data.FinishedGroups.Count >= Config.Config.Options.ShowFinalGroups)
            {
                progress.data.IsOpenFinalMeta = true;
            }

            //计算分数
            //时间分数为 1000 - （开赛以来的总时长 + 罚时）
            var timeBaseScore = 1000d;
            var timeSpanHours = (DateTime.Now - Ccxc.Core.Utils.UnixTimestamp.FromTimestamp(Config.Config.Options.StartTime)).TotalHours + progress.penalty;
            var timeScore = timeBaseScore - timeSpanHours;

            var puzzleFactor = 1.0d; //题目因数
            if(puzzleItem.answer_type == 1)
            {
                puzzleFactor = 5.0d;
            }
            else if(puzzleItem.answer_type == 2)
            {
                puzzleFactor = 10.0d;
            }
            else if(puzzleItem.answer_type == 3)
            {
                puzzleFactor = 0.0d;
            }
            progress.score += timeScore * puzzleFactor; //累加本题分数

            //计算是否完赛
            if (puzzleItem.answer_type == 2)
            {
                progress.is_finish = 1;
            }

        }
    }
}