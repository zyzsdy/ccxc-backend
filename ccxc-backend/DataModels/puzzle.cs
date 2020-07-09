using Ccxc.Core.DbOrm;

namespace ccxc_backend.DataModels
{
    public class puzzle
    {
        [DbColumn(IsPrimaryKey = true, IsIdentity = true, ColumnDescription = "题目ID")]
        public int pid { get; set; }

        [DbColumn(ColumnDescription = "题目组ID", IndexGroupNameList = new string[] { "index_pgid" })]
        public int pgid { get; set; }

        /// <summary>
        /// 题目内容类型（0-图片 1-HTML）
        /// </summary>
        [DbColumn(DefaultValue = "0")]
        public byte type { get; set; }

        [DbColumn(ColumnDescription = "标题", IsNullable = true)]
        public string title { get; set; }

        [DbColumn(ColumnDescription = "题目描述", ColumnDataType = "TEXT", IsNullable = true)]
        public string content { get; set; }

        [DbColumn(ColumnDescription = "图片URL（type=0有效）", ColumnDataType = "TEXT", IsNullable = true)]
        public string image { get; set; }

        [DbColumn(ColumnDescription = "题目HTML（type=1有效）", ColumnDataType = "TEXT", IsNullable = true)]
        public string html { get; set; }

        /// <summary>
        /// 答案类型（0-小题 1-组/区域Meta 2-PreFinalMeta 3-FinalMeta 4-不计分题目）
        /// 1- 完成该区域，开放下一区域选择权
        /// 2- 开放FinalMeta
        /// 3- 完赛，记录最终成绩
        /// </summary>
        [DbColumn(ColumnDescription = "答案类型（0-小题 1-组/区域Meta 2-PreFinalMeta 3-FinalMeta 4-不计分题目）")]
        public byte answer_type { get; set; }

        [DbColumn(ColumnDescription = "答案")]
        public string answer { get; set; }
    }

    public class Puzzle : MysqlClient<puzzle>
    {
        public Puzzle(string connStr) : base(connStr)
        {

        }
    }
}
