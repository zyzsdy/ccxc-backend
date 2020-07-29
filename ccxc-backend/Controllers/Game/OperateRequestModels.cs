using System;
using System.Collections.Generic;
using System.Text;

namespace ccxc_backend.Controllers.Game
{
    public class UnlockGroupRequest
    {
        public int unlock_puzzle_group_id { get; set; }
    }

    public class CheckAnswerRequest
    {
        public int pid { get; set; }
        public string answer { get; set; }
    }

    public class AnswerResponse : BasicResponse
    {
        public int answer_status { get; set; }

        //0-什么都不做 16-重新载入页面
        public int extend_flag { get; set; }
        public double cooldown_remain_seconds { get; set; }
    }
}
