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
        [DbColumn(ColumnDescription = "记录时间", ColumnDataType = "TIMESTAMP", Length = 6, DefaultValue = "0000-00-00 00:00:00.000000")]
        public DateTime create_time { get; set; }

        [DbColumn(ColumnDescription = "登录名")]
        public string username { get; set; }

        [DbColumn(ColumnDescription = "UID（若用户存在）")]
        public int uid { get; set; }

        /// <summary>
        /// 登录状态（0-保留 1-登录成功 2-请求无效 3-用户名错误 4-密码错误 5-没有登录权限）
        /// </summary>
        [DbColumn(ColumnDescription = "登录状态（0-保留 1-登录成功 2-请求无效 3-用户名错误 4-密码错误 5-没有登录权限）")]
        public byte status { get; set; }

        [DbColumn(ColumnDescription = "IP")]
        public string ip { get; set; }

        [DbColumn(ColumnDescription = "代理服务器传递的原始IP")]
        public string proxy_ip { get; set; }

        [DbColumn(ColumnDescription = "用户浏览器UA", ColumnDataType = "TEXT")]
        public string useragent { get; set; }

        [DbColumn(ColumnDescription = "用户浏览器识别码")]
        public long userid { get; set; }
    }

    public class LoginLog : MysqlClient<login_log>
    {
        public LoginLog(string connStr) : base(connStr)
        {

        }
    }
}
