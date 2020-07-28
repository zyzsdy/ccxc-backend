using System;
using System.Collections.Generic;
using System.Text;
using Ccxc.Core.HttpServer;
using ccxc_backend.DataModels;

namespace ccxc_backend.Controllers.Admin
{
    public class AddPuzzleRequest
    {
        public int pgid { get; set; }
        public byte type { get; set; }

        [Required(Message = "标题不能为空")]
        public string title { get; set; }

        public string content { get; set; }
        public string image { get; set; }
        public string html { get; set; }
        public byte answer_type { get; set; }

        [Required(Message = "答案不能为空")]
        public string answer { get; set; }
        public string jump_keyword { get; set; }
    }

    public class DeletePuzzleRequest
    {
        public int pid { get; set; }
    }

    public class EditPuzzleRequest
    {
        public int pid { get; set; }
        public int pgid { get; set; }
        public byte type { get; set; }

        [Required(Message = "标题不能为空")]
        public string title { get; set; }

        public string content { get; set; }
        public string image { get; set; }
        public string html { get; set; }
        public byte answer_type { get; set; }

        [Required(Message = "答案不能为空")]
        public string answer { get; set; }
        public string jump_keyword { get; set; }
    }

    public class GetPuzzleResponse : BasicResponse
    {
        public List<puzzle> puzzle { get; set; }
    }
}
