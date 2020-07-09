using Ccxc.Core.Utils;
using System;
using System.Collections.Generic;
using System.Text;

namespace ccxc_backend.Config
{
    public class Config
    {
        [OptionDescription("HTTP服务端口")]
        public int HttpPort { get; set; } = 52412; //0xCCBC

        [OptionDescription("Redis服务器连接字符串")]
        public string RedisConnStr { get; set; } = "127.0.0.1:6379";

        [OptionDescription("数据库连接字符串")]
        public string DbConnStr { get; set; } = "Server=localhost;User=root;Database=ccxc_db;Port=3306;Password=lp1234xy;Charset=utf8;ConvertZeroDateTime=True";

        [OptionDescription("调试模式：调试模式打开时，捕获的异常详情将通过HTTP直接返回给客户端，关闭时只返回简单错误消息和500提示码。True-打开 False-关闭，默认为False")]
        public bool DebugMode { get; set; } = false;

        [OptionDescription("图片存储目录，上传的图片将会存放在这里。")]
        public string ImageStorage { get; set; } = "D:/MyWorks/ccxc/static.ccxc.online/static/images";

        [OptionDescription("图片访问前缀")]
        public string ImagePrefix { get; set; } = "https://cc.minyami.net/static/images/";

        [OptionDescription("密码Hash种子1，请自由设置，设置后不要修改")]
        public string PassHashKey1 { get; set; } = "TqCoLoRdYy,25zWAITn";

        [OptionDescription("密码Hash种子2，请自由设置，设置后不要修改")]
        public string PassHashKey2 { get; set; } = "yYnjYy,qSJ;;";

        [OptionDescription("用户Session有效期，单位秒，默认3600。")]
        public int UserSessionTimeout { get; set; } = 3600;

        [OptionDescription("冷却超时时间，单位秒，默认300。")]
        public int CooldownTime { get; set; } = 300;

        [OptionDescription("默认罚时时间，单位小时，默认12.0")]
        public double PenaltyDefault { get; set; } = 12.0;

        [OptionDescription("开赛时间，Unix时间戳（毫秒），默认为2020-08-07 20:00:00 +0800 aka. 1596801600000")]
        public long StartTime { get; set; } = 1596801600000;

        [OptionDescription("完赛时间，Unix时间戳（毫秒），默认为2020-08-17 20:00:00 +0800 aka. 1597665600000")]
        public long EndTime { get; set; } = 1597665600000;

        [OptionDescription("报名截止日期，0为不限制，Unix时间戳（毫秒），默认为2020-08-01 20:00:00 +0800 aka. 1596283200000")]
        public long RegDeadline { get; set; } = 1596283200000;

        [OptionDescription("至少完成多少个区域以后才可见PreFinalMeta")]
        public int ShowFinalGroups { get; set; } = 4;

        public static Config Options { get; set; } = SystemOption.GetOption<Config>("Config/CcxcConfig.xml");
    }
}
