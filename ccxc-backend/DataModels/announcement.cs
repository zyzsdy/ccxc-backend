using Ccxc.Core.DbOrm;
using Ccxc.Core.Utils.ExtensionFunctions;
using Newtonsoft.Json;
using System;

namespace ccxc_backend.DataModels
{
    public class announcement
    {
        [DbColumn(IsPrimaryKey = true, IsIdentity = true, ColumnDescription = "公告ID")]
        public int aid { get; set; }

        [JsonConverter(typeof(UnixTimestampConverter))]
        [DbColumn(ColumnDescription = "更新时间", ColumnDataType = "TIMESTAMP", Length = 6, DefaultValue = "0000-00-00 00:00:00.000000")]
        public DateTime update_time { get; set; }

        [JsonConverter(typeof(UnixTimestampConverter))]
        [DbColumn(ColumnDescription = "创建时间", ColumnDataType = "TIMESTAMP", Length = 6, DefaultValue = "0000-00-00 00:00:00.000000")]
        public DateTime create_time { get; set; }

        [DbColumn(ColumnDescription = "公告内容", ColumnDataType = "TEXT", IsNullable = true)]
        public string content { get; set; }
    }

    public class Announcement : MysqlClient<announcement>
    {
        public Announcement(string connStr) : base(connStr)
        {

        }
    }
}
