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
        /// 当前启用中的题目组
        /// </summary>
        public List<int> NowOpenPuzzleGroups { get; set; } = new List<int>();

        /// <summary>
        /// 已完成的题目组
        /// </summary>
        public List<int> FinishedGroups { get; set; } = new List<int>();

        /// <summary>
        /// 已完成的题目
        /// </summary>
        public List<int> FinishedPuzzles { get; set; } = new List<int>();

        /// <summary>
        /// 已开放的隐藏题目
        /// </summary>
        public List<int> OpenedHidePuzzles { get; set; } = new List<int>();

        /// <summary>
        /// 是否可以选择开放下一个组
        /// </summary>
        public bool IsOpenNextGroup { get; set; } = false;

        /// <summary>
        /// 是否开放FinalMeta准入
        /// </summary>
        public bool IsOpenPreFinal { get; set; } = false;

        /// <summary>
        /// 是否开放FinalMeta
        /// </summary>
        public bool IsOpenFinalMeta { get; set; } = false;
    }

    public class Progress : MysqlClient<progress>
    {
        public Progress(string connStr) : base(connStr)
        {

        }
    }
}
