using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Attendance.PoemsSearch
{
    public class SearchItem
    {
        public int Id { get; set; }
        public string DisplayText { get; set; } // 用于展示在 ScrollViewer 中
        public string Type { get; set; }        // "Poem" / "Sentence" / "Writer"


    }

}
