using Ccxc.Core.DbOrm;
using Ccxc.Core.Utils.ExtensionFunctions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace ccxc_backend.DataModels
{
    public class temp_anno
    {
        [DbColumn(IsPrimaryKey = true, ColumnDescription = "题目ID")]
        public int pid { get; set; }

        [JsonConverter(typeof(UnixTimestampConverter))]
        [DbColumn(ColumnDescription = "创建时间", ColumnDataType = "TIMESTAMP", Length = 6, DefaultValue = "0000-00-00 00:00:00.000000")]
        public DateTime create_time { get; set; }

        [DbColumn(ColumnDescription = "公告内容", ColumnDataType = "TEXT", IsNullable = true)]
        public string content { get; set; }

        [DbColumn(ColumnDescription = "公告是否已发布（0-未发布 1-已发布）")]
        public byte is_pub { get; set; }

        [DbColumn(IsIgnore = true)]
        public string puzzle_name { get; set; }
    }

    public class TempAnno : MysqlClient<temp_anno>
    {
        public TempAnno(string connStr) : base(connStr)
        {

        }
    }
}
