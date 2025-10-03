using Attendance.Classes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Attendance.Importer
{
    public static class StudentImportManager
    {
        public static IStudentImporter GetImporter(string filePath)
        {
            var extension = Path.GetExtension(filePath).ToLower();

            return extension switch
            {
                ".xlsx" or ".xls" => new ExcelStudentImporter(),
                ".csv" => new CsvStudentImporter(),
                _ => throw new NotSupportedException($"不支持的文件类型: {extension}")
            };
        }

        public static List<string> ImportStudents(Cla targetClass, string filePath, IProgress<int> progress)
        {
            try {
                var importer = GetImporter(filePath);
                return importer.ImportWithProgress(targetClass, filePath, progress);
            } catch(Exception ex) { 
                throw;
            }
        }

        
        public static int ExtractCount(List<string> log, string keyword)
        {
            var line = log.FirstOrDefault(l => l.Contains(keyword));
            if (line != null)
            {
                var match = System.Text.RegularExpressions.Regex.Match(line, @"\d+");
                if (match.Success && int.TryParse(match.Value, out int count))
                {
                    return count;
                }
            }
            return 0;
        }

    }

}
