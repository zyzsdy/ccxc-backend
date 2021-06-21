using Ccxc.Core.DbOrm;
using Ccxc.Core.Utils.ExtensionFunctions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace ccxc_backend.DataModels
{
    public class progress
    {
        [DbColumn(IsPrimaryKey = true, ColumnDescription = "组ID")]
        public int gid { get; set; }

        [DbColumn(ColumnDescription = "存档数据", IsJson = true, ColumnDataType = "JSON")]
        public SaveData data { get; set; } = new SaveData();

        [DbColumn(ColumnDescription = "得分")]
        public double score { get; set; }

        [JsonConverter(typeof(UnixTimestampConverter))]
        [DbColumn(ColumnDescription = "更新时间", ColumnDataType = "TIMESTAMP", Length = 6, DefaultValue = "0000-00-00 00:00:00.000000")]
        public DateTime update_time { get; set; }

        [DbColumn(ColumnDescription = "是否完赛（0-未完赛 1-完赛）")]
        public byte is_finish { get; set; }

        [JsonConverter(typeof(UnixTimestampConverter))]
        [DbColumn(ColumnDescription = "完成时间", ColumnDataType = "TIMESTAMP", Length = 6, DefaultValue = "0000-00-00 00:00:00.000000")]
        public DateTime finish_time { get; set; }

        [DbColumn(ColumnDescription = "罚时（单位小时）")]
        public double penalty { get; set; }
    }

    public class SaveData
    {
        /// <summary>
        /// 已完成的题目
        /// </summary>
        public HashSet<int> FinishedPuzzles { get; set; } = new HashSet<int>();

        /// <summary>
        /// 已开放的隐藏题目
        /// </summary>
        public HashSet<int> OpenedHidePuzzles { get; set; } = new HashSet<int>();

        /// <summary>
        /// 是否开放PreFinal（条件：M1-M3全部回答正确时该值变为True，可展示M4）
        /// </summary>
        public bool IsOpenPreFinal { get; set; } = false;

        /// <summary>
        /// 是否开放最终Meta区域（条件：M4回答正确时该值变为True，可展示M5-M8、FM）
        /// </summary>
        public bool IsOpenFinalStage { get; set; } = false;

        /// <summary>
        /// 已完成的分组ID（只检查1~4）
        /// </summary>
        public HashSet<int> FinishedGroups { get; set; } = new HashSet<int>();

        /// <summary>
        /// 已兑换过的提示
        /// </summary>
        public Dictionary<int, HashSet<int>> OpenedHints { get; set; } = new Dictionary<int, HashSet<int>>();
    }

    public class Progress : MysqlClient<progress>
    {
        public Progress(string connStr) : base(connStr)
        {

        }
    }
}
