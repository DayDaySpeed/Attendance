using Attendance.Classes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Attendance.Importer
{
    public interface IStudentImporter
    {
        List<string> ImportWithProgress(Cla targetClass, string jsonPath, IProgress<int> progress);
    }

}
