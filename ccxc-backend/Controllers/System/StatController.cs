using Ccxc.Core.HttpServer;
using ccxc_backend.DataModels;
using ccxc_backend.DataServices;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ccxc_backend.Controllers.System
{
    [Export(typeof(HttpController))]
    public class StatController : HttpController
    {
        [HttpHandler("GET", "/get-puzzle-stat")]
        public async Task GetPuzzleStat(Request request, Response response)
        {
            var userSession = await CheckAuth.Check(request, response, AuthLevel.Organizer);
            if (userSession == null) return;

            var puzzleDb = DbFactory.Get<Puzzle>();
            var puzzleList = await puzzleDb.SelectAllFromCache();

            var groupDb = DbFactory.Get<UserGroup>();
            var groupList = await groupDb.SelectAllFromCache();
            var groupNameDict = groupList.ToDictionary(it => it.gid, it => it.groupname);

            var progessDb = DbFactory.Get<Progress>();
            var progessList = await progessDb.SimpleDb.AsQueryable().ToListAsync();

            var result = new List<StatPuzzleItem>();
            foreach (var puzzle in puzzleList)
            {
                var finishGroups = progessList.Where(progress => progress.data.FinishedPuzzles.Contains(puzzle.pid)).Select(progress => progress.gid).ToList();

                var r = new StatPuzzleItem
                {
                    pid = puzzle.pid,
                    puzzle_title = puzzle.title,
                    finished_count = finishGroups.Count
                };

                r.finished_groups = string.Join("/", finishGroups.Select(x =>
                {
                    if (groupNameDict.ContainsKey(x)) return groupNameDict[x];
                    else return "?";
                }));

                result.Add(r);
            }

            result = result.OrderBy(x => x.pid).ToList();

            Console.WriteLine("PID,题目名称,完成人数,完成队伍名（斜杠分隔）");
            foreach (var l in result)
            {
                Console.WriteLine($"{l.pid},{l.puzzle_title},{l.finished_count},{l.finished_groups}");
            }

            await response.JsonResponse(200, new
            {
                status = 1,
                result
            });
        }
    }

    public class StatPuzzleItem
    {
        public int pid { get; set; }
        public string puzzle_title { get; set; }
        public int finished_count { get; set; }
        public string finished_groups { get; set; }
    }
}
