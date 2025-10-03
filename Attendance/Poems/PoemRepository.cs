using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;

namespace Attendance.Poems
{
    public class PoemRepository
    {
        public List<Poem> AllPoems { get; private set; } = new();

        public void LoadData(string folderPath)
        {
            foreach (var file in Directory.GetFiles(folderPath, "*.json"))
            {
                var json = File.ReadAllText(file);
                var poems = JsonSerializer.Deserialize<List<Poem>>(json);
                if (poems != null) AllPoems.AddRange(poems);
            }
        }
    }
}
