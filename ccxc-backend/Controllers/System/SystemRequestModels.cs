using System;
using System.Collections.Generic;
using System.Text;

namespace ccxc_backend.Controllers.System
{
    public class DefaultSettingResponse : BasicResponse
    {
        public long start_time { get; set; }
    }

    public class ScoreBoardItem
    {
        public int gid { get; set; }
        public string group_name { get; set; }
        public string group_profile { get; set; }

        /// <summary>
        /// 总用时（小时）（完赛时间-开赛时间）+ 罚时
        /// </summary>
        public double total_time { get; set; }
        public double score { get; set; }
        public int finished_puzzle_count { get; set; }
        public int is_finish { get; set; }
    }

    public class ScoreBoardResponse : BasicResponse
    {
        public List<ScoreBoardItem> finished_groups { get; set; }
        public List<ScoreBoardItem> groups { get; set; }
    }
}
