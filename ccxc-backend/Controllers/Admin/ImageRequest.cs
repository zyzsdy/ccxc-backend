using System;
using System.Collections.Generic;
using System.Text;

namespace ccxc_backend.Controllers.Admin
{
    public class ImageResponse : BasicResponse
    {
        public string image_path { get; set; }
    }

    public class ImagePrepareResponse : BasicResponse
    {
        public string upload_token { get; set; }
    }
}
