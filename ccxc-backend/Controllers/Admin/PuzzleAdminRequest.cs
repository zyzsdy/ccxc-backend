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
        public string extend_content { get; set; }
        public string extend_data { get; set; }

        public string tips1 { get; set; }
        public string tips2 { get; set; }
        public string tips3 { get; set; }
        public string tips1title { get; set; }
        public string tips2title { get; set; }
        public string tips3title { get; set; }
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
        public string extend_content { get; set; }
        public string extend_data { get; set; }

        public string tips1 { get; set; }
        public string tips2 { get; set; }
        public string tips3 { get; set; }
        public string tips1title { get; set; }
        public string tips2title { get; set; }
        public string tips3title { get; set; }
    }

    public class GetPuzzleResponse : BasicResponse
    {
        public List<puzzle> puzzle { get; set; }
    }

    public class GetAdditionalAnswerResponse : BasicResponse
    {
        public List<additional_answer> additional_answer { get; set; }
    }

    public class DeleteAdditionalAnswerRequest
    {
        public int aaid { get; set; }
    }
}
