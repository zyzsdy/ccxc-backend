using Ccxc.Core.Utils;
using System;
using System.Collections.Generic;
using System.Text;

namespace ccxc_backend.Config
{
    public class Config
    {
        [OptionDescription("HTTP服务端口")]
        public int HttpPort { get; set; } = 51002;

        [OptionDescription("Redis服务器连接字符串")]
        public string RedisConnStr { get; set; } = "127.0.0.1:6379";

        [OptionDescription("数据库连接字符串")]
        public string ParamDbConn { get; set; } = "Server=localhost;User=root;Database=ccxc_db;Port=3306;Password=lp1234xy;Charset=utf8;ConvertZeroDateTime=True";

        [OptionDescription("调试模式：调试模式打开时，捕获的异常详情将通过HTTP直接返回给客户端，关闭时只返回简单错误消息和500提示码。True-打开 False-关闭，默认为False")]
        public bool DebugMode { get; set; } = false;

        [OptionDescription("用户Session有效期，单位秒，默认3600。")]
        public int UserSessionTimeout { get; set; } = 3600;

        public static Config Options { get; set; } = SystemOption.GetOption<Config>("Config/CcxcConfig.xml");
    }
}
