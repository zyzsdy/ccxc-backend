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

        [DbColumn(ColumnDescription = "手机号", IsNullable = true)]
        public string phone { get; set; }

        [DbColumn(ColumnDescription = "密码")]
        public string password { get; set; }

        /// <summary>
        /// 角色（0-被封禁 1-标准用户 2-组员 3-组长 4-出题组 5-管理员）
        /// </summary>
        [DbColumn(DefaultValue = "0")]
        public int roleid { get; set; }

        [JsonConverter(typeof(UnixTimestampConverter))]
        [DbColumn(ColumnDescription = "创建时间", ColumnDataType = "TIMESTAMP", Length = 6)]
        public DateTime create_time { get; set; }

        [JsonConverter(typeof(UnixTimestampConverter))]
        [DbColumn(ColumnDescription = "更新时间", ColumnDataType = "TIMESTAMP", Length = 6)]
        public DateTime update_time { get; set; }

        [DbColumn(ColumnDescription = "个人简介", ColumnDataType = "TEXT", IsNullable = true)]
        public string profile { get; set; }

        [DbColumn(ColumnDescription = "信息Key", IsNullable = true)]
        public string info_key { get; set; }
    }

    public class User : MysqlClient<user>
    {
        public User(string connStr) : base(connStr)
        {

        }
    }

    public class UserSession
    {
        public int uid { get; set; }
        public string username { get; set; }
        public int roleid { get; set; }

        /// <summary>
        /// User-Token
        /// </summary>
        public string token { get; set; }

        /// <summary>
        /// 秘密认证Key
        /// </summary>
        public string sk { get; set; }

        /// <summary>
        /// 上次活动时间
        /// </summary>
        [JsonConverter(typeof(UnixTimestampConverter))]
        public DateTime last_update { get; set; }

        /// <summary>
        /// 本Session有效性 1-有效 0-无效（视为没有登录）
        /// </summary>
        public int is_active { get; set; }

        /// <summary>
        /// 当Session无效时返回给前端的消息
        /// </summary>
        public string inactive_message { get; set; }
    }
}
