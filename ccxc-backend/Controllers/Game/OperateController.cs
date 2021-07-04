using Ccxc.Core.HttpServer;
using ccxc_backend.DataModels;
using ccxc_backend.DataServices;
using Newtonsoft.Json;
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

            var answer = requestJson.answer.ToLower().Replace(" ", "");

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

            //取得队伍名称
            var groupDb = DbFactory.Get<UserGroup>();
            var groupList = await groupDb.SelectAllFromCache();

            var groupItem = groupList.FirstOrDefault(it => it.gid == gid);

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
            var puzzleList = await puzzleDb.SelectAllFromCache();

            //1. 判定是否可以激活隐藏题目
            //取出隐藏关键字
            var jumpKeyWords = puzzleList.Where(it => !string.IsNullOrEmpty(it.jump_keyword))
                .GroupBy(it => it.jump_keyword.ToLower().Replace(" ", ""))
                .ToDictionary(it => it.Key, it => it.First());

            if (jumpKeyWords.ContainsKey(answer))
            {
                var jumpTarget = jumpKeyWords[answer];

                answerLog.status = 6;
                await answerLogDb.SimpleDb.AsInsertable(answerLog).ExecuteCommandAsync();

                //回写存档
                progress.data.OpenedHidePuzzles.Add(jumpTarget.pid);
                await progressDb.SimpleDb.AsUpdateable(progress).IgnoreColumns(it => new { it.finish_time }).ExecuteCommandAsync();

                //返回
                await response.JsonResponse(200, new AnswerResponse
                {
                    status = 3,
                    answer_status = 6,
                    message = "好像发现了什么奇妙空间。",
                    location = $"/clue/{jumpTarget.pid}"
                });
                return;
            }

            //2. 判定题目可见性
            var puzzleItem = puzzleList.Where(it => it.pid == requestJson.pid).First();

            if (puzzleItem == null)
            {
                answerLog.status = 4;
                await answerLogDb.SimpleDb.AsInsertable(answerLog).ExecuteCommandAsync();

                await response.BadRequest("题目不存在或未解锁。");
                return;
            }

            
            //取得普通小题已经打开的区域（1~3）
            var cache = DbFactory.GetCache();
            var openedGroupKey = cache.GetDataKey("opened-groups");
            var openedGroup = await cache.Get<int>(openedGroupKey);

            //prefinal需存档可见
            if (puzzleItem.pgid == 4)
            {
                if (!progressData.IsOpenPreFinal)
                {
                    answerLog.status = 4;
                    await answerLogDb.SimpleDb.AsInsertable(answerLog).ExecuteCommandAsync();

                    await response.BadRequest("题目不存在或未解锁。");
                    return;
                }
            }
            //final区域需存档可见
            else if (puzzleItem.pgid == 5)
            {
                if (!progressData.IsOpenFinalStage)
                {
                    answerLog.status = 4;
                    await answerLogDb.SimpleDb.AsInsertable(answerLog).ExecuteCommandAsync();

                    await response.BadRequest("题目不存在或未解锁。");
                    return;
                }
            }
            //非prefinal或final区域：需判定题目组已开放或者题目本身作为隐藏题目开放
            else if (puzzleItem.pgid > openedGroup && !progressData.OpenedHidePuzzles.Contains(puzzleItem.pid))
            {
                answerLog.status = 4;
                await answerLogDb.SimpleDb.AsInsertable(answerLog).ExecuteCommandAsync();

                await response.BadRequest("题目不存在或未解锁。");
                return;
            }

            //取得最后一次错误答题记录
            var lastWrongTime = DateTime.MinValue;
            var lastWrongAnswer = await answerLogDb.SimpleDb.AsQueryable().Where(it => it.gid == gid && it.status != 1 && it.status != 3 && it.status != 6)
                .OrderBy(it => it.create_time, OrderByType.Desc).FirstAsync();

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
            var trueAnswer = puzzleItem.answer.ToLower().Replace(" ", "");
            if (!string.Equals(trueAnswer, answer, StringComparison.CurrentCultureIgnoreCase))
            {
                //答案错误，判断是否存在附加提示
                var addAnswerDb = DbFactory.Get<AdditionalAnswer>();
                var addAnswerListAll = await addAnswerDb.SelectAllFromCache();
                var addAnswerDict = addAnswerListAll.Where(x => x.pid == puzzleItem.pid).ToDictionary(x => x.answer.ToLower().Replace(" ", ""), x => x.message);

                var message = "答案错误";
                if (addAnswerDict.ContainsKey(answer))
                {
                    message = $"答案错误，但是获得了一些信息：{addAnswerDict[answer]}";
                }


                answerLog.status = 2;
                await answerLogDb.SimpleDb.AsInsertable(answerLog).ExecuteCommandAsync();

                await response.JsonResponse(406, new AnswerResponse //使用 406 Not Acceptable 作为答案错误的专用返回码。若返回 200 OK 则为答案正确
                {
                    status = 1,
                    answer_status = 2,
                    message = message
                });
                return;
            }

            //答案正确
            answerLog.status = 1;
            await answerLogDb.SimpleDb.AsInsertable(answerLog).ExecuteCommandAsync();


            //计算是否为首杀
            var tempAnnoDb = DbFactory.Get<TempAnno>();
            var c = await tempAnnoDb.SimpleDb.AsQueryable().Where(x => x.pid == puzzleItem.pid).CountAsync();
            if (c == 0)
            {
                //触发首杀逻辑
                var extraInfo = "";
                //判断是否可以全局解锁新区域
                if (puzzleItem.answer_type == 1 && puzzleItem.pgid < 3) //只有pgid是1和2的分区meta可以触发
                {
                    if (openedGroup < puzzleItem.pgid + 1)
                    {
                        await cache.Put(openedGroupKey, puzzleItem.pgid + 1);
                        extraInfo = @"**<span style=""color: red"">【在他们出色的解开了谜题的同时，有新的线索出现了。】</span>**";
                    }
                }

                //写入首杀公告
                var newTempAnno = new temp_anno
                {
                    pid = puzzleItem.pid,
                    create_time = DateTime.Now,
                    content = $"【首杀公告】恭喜队伍 {groupItem?.groupname ?? ""} 于 {DateTime.Now:yyyy-MM-dd HH:mm:ss} 首个解出了题目 **#{puzzleItem.title}** 。{extraInfo}",
                    is_pub = 0
                };

                try
                {
                    await tempAnnoDb.SimpleDb.AsInsertable(newTempAnno).ExecuteCommandAsync();
                }
                catch (Exception e)
                {
                    Ccxc.Core.Utils.Logger.Error($"首杀数据写入失败，原因可能是：{e.Message}，附完整数据：{JsonConvert.SerializeObject(newTempAnno)}，详细信息：" + e.ToString());
                    //写入不成功可能是产生了竞争或者主键已存在。总之这里忽略掉这个异常。
                }
            }



            //==============更新存档=====================


            //若解出的题目是分区Meta，则标记为该分区完成
            if (puzzleItem.answer_type == 1)
            {
                progress.data.FinishedGroups.Add(puzzleItem.pgid);
            }

            //检查是否可以打开新区域
            var successMessage = "OK";
            //检查是否可以开放PreFinal（条件：M1-M3全部回答正确时该值变为True，可展示M4）
            if (progress.data.FinishedGroups.Contains(1) && progress.data.FinishedGroups.Contains(2) && progress.data.FinishedGroups.Contains(3))
            {
                progress.data.IsOpenPreFinal = true;
            }

            //检查是否可以开放最终Meta区域（条件：M4回答正确时该值变为True，可展示M5-M8、FM）
            if (progress.data.FinishedGroups.Contains(4))
            {
                progress.data.IsOpenFinalStage = true;
            }

            if (!string.IsNullOrEmpty(puzzleItem.extend_content))
            {
                successMessage += " 好像发现了什么线索（请注意题目页面多出来的新内容）。";
            }

            //计算分数
            //得分为 时间分数 * 系数
            //时间分数为 1000 - （开赛以来的总时长 + 使用过的提示币数量）

            if (!progressData.FinishedPuzzles.Contains(puzzleItem.pid))
            {
                const double timeBaseScore = 1000d;
                var timeSpanHours =
                    (DateTime.Now - Ccxc.Core.Utils.UnixTimestamp.FromTimestamp(Config.Config.Options.StartTime))
                    .TotalHours + progress.penalty;
                var timeScore = timeBaseScore - timeSpanHours;

                var puzzleFactor = 1.0d; //题目得分系数
                if (puzzleItem.answer_type == 1)
                {
                    puzzleFactor = 10.0d;
                }
                else if (puzzleItem.answer_type == 3)
                {
                    puzzleFactor = 1000.0d;
                }
                else if (puzzleItem.answer_type == 4)
                {
                    puzzleFactor = 0.0d;
                }

                if (progress.is_finish == 1)
                {
                    puzzleFactor *= 0.5; //完赛后继续答题题目分数减半
                }


                progress.score += timeScore * puzzleFactor; //累加本题分数
            }

            var extendFlag = string.IsNullOrEmpty(puzzleItem.extend_content) ? 0 : 16; //如果存在扩展，extend_flag应为16，此时前端需要刷新，如果需要跳转final，extend_flag应为1。否则应为0。

            //本题目标记为已完成
            progress.data.FinishedPuzzles.Add(puzzleItem.pid);

            //回写存档

            //计算是否完赛
            if (puzzleItem.answer_type == 3 && progress.is_finish != 1)
            {
                progress.is_finish = 1;
                progress.finish_time = DateTime.Now;
                extendFlag = 1;

                await progressDb.SimpleDb.AsUpdateable(progress).ExecuteCommandAsync();
            }
            else
            {
                await progressDb.SimpleDb.AsUpdateable(progress).IgnoreColumns(it => new { it.finish_time }).ExecuteCommandAsync();
            }

            //返回回答正确
            await response.JsonResponse(200, new AnswerResponse
            {
                status = 1,
                answer_status = 1,
                extend_flag = extendFlag,
                message = successMessage
            });
        }
    }
}
