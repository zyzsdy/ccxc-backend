using System;
using System.Collections.Generic;
using System.Text;
using ccxc_backend.DataModels;

namespace ccxc_backend.Controllers.Admin
{
    public class QueryAnswerLogRequest
    {
        public List<int> uid { get; set; }
        public List<int> gid { get; set; }
        public List<int> pid { get; set; }
        public List<int> status { get; set; }
        public string answer { get; set; }
        public int order { get; set; } //0-倒序 1-正序
        public int page { get; set; }
    }

    public class QueryAnswerLogResponse : BasicResponse
    {
        public int page { get; set; }
        public int page_size { get; set; }
        public int total_count { get; set; }
        public List<AnswerLogView> answer_log { get; set; }
    }

    public class AnswerLogView : answer_log
    {
        public AnswerLogView(answer_log a)
        {
            id = a.id;
            create_time = a.create_time;
            uid = a.uid;
            gid = a.gid;
            pid = a.pid;
            answer = a.answer;
            status = a.status;
        }
    }

    public class GetUserListResponse : BasicResponse
    {
        public List<UidItem> uid_item { get; set; }
        public List<GidItem> gid_item { get; set; }
        public List<PidItem> pid_item { get; set; }
    }

    public class UidItem
    {
        public int uid { get; set; }
        public string user_name { get; set; }
    }

    public class GidItem
    {
        public int gid { get; set; }
        public string group_name { get; set; }
    }

    public class PidItem
    {
        public int pid { get; set; }
        public int pgid { get; set; }
        public string title { get; set; }
    }
}
