using Ccxc.Core.DbOrm;
using System;
using System.Collections.Generic;
using System.Text;

namespace ccxc_backend.DataModels
{
    public class puzzle_group
    {
        [DbColumn(IsPrimaryKey = true, IsIdentity = true, ColumnDescription = "题目组ID")]
        public int pgid { get; set; }

        [DbColumn(ColumnDescription = "题目组名")]
        public string pg_name { get; set; }

        [DbColumn(ColumnDescription = "题目组描述", ColumnDataType = "TEXT", IsNullable = true)]
        public string pg_desc { get; set; }

        [DbColumn(ColumnDescription = "是否为隐藏区域（0-不是 1-是）", DefaultValue = "0")]
        public byte is_hide { get; set; } = 0;
    }

    public class PuzzleGroup : MysqlClient<puzzle_group>
    {
        public PuzzleGroup(string connStr) : base(connStr)
        {

        }
    }
}
