using Attendance.Classes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;


namespace Attendance.Importer
{
    public class CsvStudentImporter : IStudentImporter
    {
        public List<string> Import(Cla targetClass, string csvPath)
        {
            return ImportWithProgress(targetClass, csvPath, null);
        }

        public List<string> ImportWithProgress(Cla targetClass, string csvPath, IProgress<int> progress)
        {
            var importLog = new List<string>();
            int successCount = 0;
            int skippedCount = 0;
            var startTime = DateTime.Now;

            if (!File.Exists(csvPath))
            {
                MessageBox.Show($"文件不存在: {csvPath}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                importLog.Add($"❌ 文件不存在: {csvPath}");
                SaveLog(importLog);
                return importLog;
            }

            try
            {
                importLog.Add($"📥 导入开始时间: {startTime}");

                var lines = File.ReadAllLines(csvPath, Encoding.UTF8);
                if (lines.Length < 2)
                {
                    importLog.Add("❌ CSV 文件无有效数据");
                    SaveLog(importLog);
                    return importLog;
                }

                var headers = lines[0].Split(',').Select(h => h.Trim().ToLower()).ToList();
                var dataLines = lines.Skip(1).ToList();
                int totalRows = dataLines.Count;
                int processedRows = 0;

                foreach (var line in dataLines)
                {
                    var columns = line.Split(',');
                    var student = new Student
                    {
                        StudentNumber = 0,
                        Gender = null,
                        ClassId = targetClass.id
                    };

                    bool hasName = false;

                    for (int i = 0; i < headers.Count && i < columns.Length; i++)
                    {
                        var header = headers[i];
                        var cellValue = columns[i].Trim();

                        switch (header)
                        {
                            case "姓名":
                            case "name":
                                if (string.IsNullOrEmpty(cellValue))
                                {
                                    hasName = false;
                                }
                                else
                                {
                                    student.Name = cellValue;
                                    hasName = true;
                                }
                                break;

                            case "学号":
                            case "id":
                                if (long.TryParse(cellValue, out long studentNumber))
                                {
                                    student.StudentNumber = studentNumber;
                                }
                                else
                                {
                                    student.StudentNumber = 0;
                                    importLog.Add($"⚠️ 行 {processedRows + 2} 列 {i + 1}: 学号无效 '{cellValue}'，已设为默认值 0");
                                }
                                break;

                            case "性别":
                            case "gender":
                                var gender = ParseGender(cellValue);
                                if (gender == null)
                                {
                                    importLog.Add($"⚠️ 行 {processedRows + 2} 列 {i + 1}: 性别无效或为空 '{cellValue}'，已设为 null");
                                }
                                student.Gender = gender;
                                break;

                            default:
                                importLog.Add($"⚠️ 行 {processedRows + 2} 列 {i + 1}: 未识别字段 '{header}'，值为 '{cellValue}'");
                                break;
                        }
                    }

                    if (hasName)
                    {
                        ClassStorageService.AddStudent(student);
                        targetClass.Students.Add(student);
                        successCount++;
                    }
                    else
                    {
                        importLog.Add($"⚠️ 跳过行 {processedRows + 2}: 姓名为空");
                        skippedCount++;
                    }

                    processedRows++;
                    if (progress != null && totalRows > 0)
                    {
                        int percent = (int)((double)processedRows / totalRows * 100);
                        progress.Report(percent);
                        System.Threading.Thread.Sleep(10);
                    }
                }

                var endTime = DateTime.Now;
                importLog.Add($"✅ 成功导入学生数: {successCount}");
                importLog.Add($"⚠️ 跳过无效行数: {skippedCount}");
                importLog.Add($"📤 导入结束时间: {endTime}");
                importLog.Add($"⏱ 总耗时: {(endTime - startTime).TotalSeconds:F2} 秒");

                SaveLog(importLog);
                return importLog;
            }
            catch (Exception ex)
            {
                importLog.Add($"❌ 导入失败: {ex.Message}");
                SaveLog(importLog);
                throw;
            }
        }

        private Student.GenderEnum? ParseGender(string genderStr)
        {
            return genderStr.Trim().ToLower() switch
            {
                "男" => Student.GenderEnum.male,
                "女" => Student.GenderEnum.female,
                "male" => Student.GenderEnum.male,
                "female" => Student.GenderEnum.female,
                _ => null
            };
        }

        private void SaveLog(List<string> log)
        {
            var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var logDirectory = Path.Combine(documentsPath, "Attendlog");
            if (!Directory.Exists(logDirectory)) Directory.CreateDirectory(logDirectory);

            var logFileName = $"ImportLog_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
            var logPath = Path.Combine(logDirectory, logFileName);
            File.WriteAllLines(logPath, log);
        }
    }
}
