using Ccxc.Core.DbOrm;
using Ccxc.Core.Utils.ExtensionFunctions;
using Newtonsoft.Json;
using System;

namespace ccxc_backend.DataModels
{
    public class user
    {
        [DbColumn(IsPrimaryKey = true, IsIdentity = true, ColumnDescription = "用户ID")]
        public int uid { get; set; }

        [DbColumn(ColumnDescription = "用户名", IndexGroupNameList = new string[] { "index_name" })]
        public string username { get; set; }

        [DbColumn(ColumnDescription = "E-mail")]
        public string email { get; set; }

        [DbColumn(ColumnDescription = "手机号")]
        public string phone { get; set; }

        [DbColumn(ColumnDescription = "密码")]
        public string password { get; set; }

        [DbColumn(ColumnDescription = "角色（0-被封禁 1-标准用户 2-组员 3-组长 4-出题组 5-管理员）", DefaultValue = "0")]
        public int roleid { get; set; } = 0;

        [JsonConverter(typeof(UnixTimestampConverter))]
        [DbColumn(ColumnDescription = "创建时间", ColumnDataType = "TIMESTAMP", Length = 6)]
        public DateTime create_time { get; set; }

        [JsonConverter(typeof(UnixTimestampConverter))]
        [DbColumn(ColumnDescription = "更新时间", ColumnDataType = "TIMESTAMP", Length = 6)]
        public DateTime update_time { get; set; }

        [DbColumn(ColumnDescription = "个人简介", ColumnDataType = "TEXT")]
        public string profile { get; set; }

        [DbColumn(ColumnDescription = "信息Key")]
        public string info_key { get; set; }
    }

    public class User : MysqlClient<user>
    {
        public User(string connStr) : base(connStr)
        {

        }
    }
}
