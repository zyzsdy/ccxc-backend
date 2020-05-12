using Ccxc.Core.DbOrm;
using Ccxc.Core.Utils.ExtensionFunctions;
using Newtonsoft.Json;
using System;

namespace ccxc_backend.DataModels
{
    public class login_log
    {
        [DbColumn(IsPrimaryKey = true, IsIdentity = true, ColumnDescription = "记录ID")]
        public int id { get; set; }

        [JsonConverter(typeof(UnixTimestampConverter))]
        [DbColumn(ColumnDescription = "记录时间", ColumnDataType = "TIMESTAMP", Length = 6)]
        public DateTime create_time { get; set; }

        [DbColumn(ColumnDescription = "登录名")]
        public string username { get; set; }

        [DbColumn(ColumnDescription = "UID（若登录成功）")]
        public int uid { get; set; }

        [DbColumn(ColumnDescription = "登录状态（0-保留 1-用户名或密码错误 2-没有登录权限 3-登录成功）")]
        public byte status { get; set; }

        [DbColumn(ColumnDescription = "IP")]
        public string ip { get; set; }

        [DbColumn(ColumnDescription = "用户浏览器UA", ColumnDataType = "TEXT")]
        public string useragent { get; set; }
    }

    public class LoginLog : MysqlClient<login_log>
    {
        public LoginLog(string connStr) : base(connStr)
        {

        }
    }
}
