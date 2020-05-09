using Ccxc.Core.DbOrm;
using Ccxc.Core.Utils.ExtensionFunctions;
using Newtonsoft.Json;
using System;

namespace ccxc_backend.DataModels
{
    public class message
    {
        [DbColumn(IsPrimaryKey = true, IsIdentity = true, ColumnDescription = "消息ID")]
        public int mid { get; set; }

        [JsonConverter(typeof(UnixTimestampConverter))]
        [DbColumn(ColumnDescription = "创建时间", ColumnDataType = "TIMESTAMP", Length = 6)]
        public DateTime create_time { get; set; }

        [DbColumn(ColumnDescription = "目标组ID", IndexGroupNameList = new string[] { "index_gid" })]
        public int gid { get; set; }

        [DbColumn(ColumnDescription = "发送用户的UID", IndexGroupNameList = new string[] { "index_uid" })]
        public int uid { get; set; }

        [DbColumn(ColumnDescription = "消息内容", ColumnDataType = "TEXT")]
        public string content { get; set; }

        /// <summary>
        /// 已读（0-未读 1-已读）
        /// </summary>
        [DbColumn(ColumnDescription = "已读（0-未读 1-已读）")]
        public byte is_read { get; set; }
    }

    public class Message : MysqlClient<message>
    {
        public Message(string connStr) : base(connStr)
        {

        }
    }
}
