using Ccxc.Core.DbOrm;

namespace ccxc_backend.DataModels
{
    public class user_group_bind
    {
        [DbColumn(IsPrimaryKey = true, ColumnDescription = "用户ID")]
        public int uid { get; set; }

        [DbColumn(ColumnDescription = "绑定的组ID", IndexGroupNameList = new string[] { "index_gid" })]
        public int gid { get; set; }

        /// <summary>
        /// 是否为组长（0-队员 1-队长）
        /// </summary>
        [DbColumn(ColumnDescription = "是否为组长（0-队员 1-队长）")]
        public byte is_leader { get; set; }
    }

    public class UserGroupBind : MysqlClient<user_group_bind>
    {
        public UserGroupBind(string connStr) : base(connStr)
        {

        }
    }
}
