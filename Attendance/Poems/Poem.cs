using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Attendance.Poems
{


    public class Poem
    {
        public string Title { get; set; }
        public string Dynasty { get; set; }
        public string Writer { get; set; }
        public string Content { get; set; }
        public string[] Type { get; set; }
        public string Remark { get; set; }
        public string Shangxi { get; set; }
        public string Translation { get; set; }
    }

    public class Writer
    {
        public string Id { get; set; }          // 对应 _id
        public string Name { get; set; }        // 作者姓名
        public string SimpleIntro { get; set; } // 简要介绍
        public string DetailIntro { get; set; } // 详细介绍
    }

    public class Sentence
    {
        public string Id { get; set; }     // 对应 JSON 里的 id
        public string Name { get; set; }   // 名句内容
        public string From { get; set; }   // 出处
    }



}
