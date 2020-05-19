using Ccxc.Core.DbOrm;
using Ccxc.Core.Utils.ExtensionFunctions;
using Newtonsoft.Json;
using System;

namespace ccxc_backend.DataModels
{
    public class answer_log
    {
        [DbColumn(IsPrimaryKey = true, IsIdentity = true, ColumnDescription = "答案记录ID")]
        public int id { get; set; }

        [JsonConverter(typeof(UnixTimestampConverter))]
        [DbColumn(ColumnDescription = "创建时间", ColumnDataType = "TIMESTAMP", Length = 6, DefaultValue = "0000-00-00 00:00:00.000000")]
        public DateTime create_time { get; set; }

        [DbColumn(ColumnDescription = "UID")]
        public int uid { get; set; }

        [DbColumn(ColumnDescription = "GID")]
        public int gid { get; set; }

        [DbColumn(ColumnDescription = "题目ID")]
        public int pid { get; set; }

        [DbColumn(ColumnDescription = "提交答案")]
        public string answer { get; set; }

        [DbColumn(ColumnDescription = "答案状态（0-保留 1-正确 2-答案错误 3-在冷却中而未判断）")]
        public byte status { get; set; }
    }

    public class AnswerLog : MysqlClient<answer_log>
    {
        public AnswerLog(string connStr) : base(connStr)
        {

        }
    }

}
