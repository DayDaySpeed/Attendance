using Attendance.Classes;
using ClosedXML.Excel;
using System.IO;
using System.Windows;



namespace Attendance.Importer
{
    public class ExcelStudentImporter : IStudentImporter
    {
        public List<string> Import(Cla targetClass, string excelPath)
        {
            return ImportWithProgress(targetClass, excelPath, null);
        }



        public  List<string> ImportWithProgress(Cla targetClass, string excelPath, IProgress<int> progress)
        {
            var importLog = new List<string>();
            int successCount = 0;
            int skippedCount = 0;
            var startTime = DateTime.Now;

            if (!File.Exists(excelPath))
            {
                MessageBox.Show($"文件不存在: {excelPath}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                importLog.Add($"❌ 文件不存在: {excelPath}");
                SaveLog(importLog);
                return importLog;
            }

            try
            {
                importLog.Add($"📥 导入开始时间: {startTime}");

                using var workbook = new XLWorkbook(excelPath);
                int processedRows = 0;
                int totalRows = 0;

                foreach (var worksheet in workbook.Worksheets)
                {
                    //找到表头行
                    int headerRowIndex = -1;
                    foreach (var row in worksheet.RowsUsed())
                    {
                        var cells = row.Cells().Select(c => c.GetString().Trim().ToLower()).ToList();
                        if (cells.Contains("学号") || cells.Contains("姓名"))
                        {
                            headerRowIndex = row.RowNumber();
                            break;
                        }
                    }

                    if (headerRowIndex == -1)
                    {
                        importLog.Add($"❌ 工作表 '{worksheet.Name}' 未找到表头行，跳过该表");
                        continue;
                    }

                    var headers = worksheet.Row(headerRowIndex).Cells().Select(c => c.GetString().Trim().ToLower()).ToList();
                    var dataRows = worksheet.RowsUsed().Where(r => r.RowNumber() > headerRowIndex);
                    totalRows += dataRows.Count();

                    foreach (var row in dataRows)
                    {
                        var student = new Student
                        {
                            StudentNumber = 0,
                            Gender = null,
                            ClassId = targetClass.id
                        };

                        bool hasName = false;

                        for (int i = 0; i < headers.Count; i++)
                        {
                            var header = headers[i];
                            var cellValue = row.Cell(i + 1).GetString().Trim();

                            switch (header)
                            {
                                case "姓名":
                                case "name":
                                    if (string.IsNullOrEmpty(cellValue))
                                    {
                                        student.Name = "";
                                        hasName = false;
                                        importLog.Add($"⚠️ 行 {row.RowNumber()} 列 {i + 1}: 姓名为空，已设为 \"\"");
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
                                        importLog.Add($"⚠️ 行 {row.RowNumber()} 列 {i + 1}: 学号无效 '{cellValue}'，已设为默认值 0");
                                    }
                                    break;

                                case "性别":
                                case "gender":
                                    var gender = ParseGender(cellValue);
                                    if (gender == null)
                                    {
                                        importLog.Add($"⚠️ 行 {row.RowNumber()} 列 {i + 1}: 性别无效或为空 '{cellValue}'，已设为 null");
                                    }
                                    student.Gender = gender;
                                    break;

                                default:
                                    importLog.Add($"⚠️ 行 {row.RowNumber()} 列 {i + 1}: 未识别字段 '{header}'，值为 '{cellValue}'");
                                    break;
                            }
                        }

                        if (hasName || student.StudentNumber != 0)
                        {
                            ClassStorageService.AddStudent(student);
                            targetClass.Students.Add(student);
                            successCount++;
                        }
                        else
                        {
                            importLog.Add($"⚠️ 跳过行 {row.RowNumber()}: 姓名和学号都为空或无效");
                            skippedCount++;
                        }


                        processedRows++;
                        if (progress != null && totalRows > 0)
                        {
                            int percent = (int)((double)processedRows / totalRows * 100);
                            progress.Report(percent);

                            // 模拟处理时间，让进度条有时间更新
                            System.Threading.Thread.Sleep(10);
                        }
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
