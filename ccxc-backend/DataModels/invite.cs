using Ccxc.Core.DbOrm;
using Ccxc.Core.Utils.ExtensionFunctions;
using Newtonsoft.Json;
using System;

namespace ccxc_backend.DataModels
{
    public class invite
    {
        [DbColumn(IsPrimaryKey = true, IsIdentity = true, ColumnDescription = "邀请ID")]
        public int iid { get; set; }

        [JsonConverter(typeof(UnixTimestampConverter))]
        [DbColumn(ColumnDescription = "创建时间", ColumnDataType = "TIMESTAMP", Length = 6)]
        public DateTime create_time { get; set; }

        [DbColumn(ColumnDescription = "发出邀请的组ID", IndexGroupNameList = new string[] { "index_gid" })]
        public int from_gid { get; set; }

        [DbColumn(ColumnDescription = "接受邀请的用户ID", IndexGroupNameList = new string[] { "index_uid" })]
        public int to_uid { get; set; }

        /// <summary>
        /// 邀请有效性（0-已被撤回 1-有效 2-已被拒绝）
        /// </summary>
        [DbColumn(ColumnDescription = "邀请有效性（0-已被撤回 1-有效 2-已被拒绝）")]
        public byte valid { get; set; }
    }

    public class Invite : MysqlClient<invite>
    {
        public Invite(string connStr) : base(connStr)
        {

        }
    }
}
