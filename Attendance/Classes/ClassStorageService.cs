using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace Attendance.Classes
{
    public static class ClassStorageService
    {
        private static readonly string DbFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "db");
        private static readonly string DbPath = Path.Combine(DbFolder, "classes.db");

        //加载班级和学生数据
        public static ObservableCollection<Cla> Load()
        {
            var result = new ObservableCollection<Cla>();
            if (!File.Exists(DbPath)) return result;

            using var conn = new SqliteConnection($"Data Source={DbPath}");
            conn.Open();

            var classCmd = new SqliteCommand("SELECT Id, Name FROM Classes", conn);
            using var classReader = classCmd.ExecuteReader();

            var classList = new List<Cla>();
            while (classReader.Read())
            {
                var cla = new Cla
                {
                    id = classReader.GetInt32(0),
                    Name = classReader.GetString(1),
                    Students = new ObservableCollection<Student>()
                };
                classList.Add(cla);
            }

            var studentCmd = new SqliteCommand("SELECT Id, Name, StudentNumber, Gender, ClassId FROM Students", conn);
            using var studentReader = studentCmd.ExecuteReader();

            while (studentReader.Read())
            {
                var genderStr = studentReader.GetString(3);
                Student.GenderEnum? gender = string.IsNullOrEmpty(genderStr) ? null : Enum.TryParse(genderStr, out Student.GenderEnum g) ? g : null;

                var student = new Student
                {
                    id = studentReader.GetInt32(0),
                    Name = studentReader.GetString(1),
                    StudentNumber = studentReader.GetInt64(2),
                    Gender = gender,
                    ClassId = studentReader.GetInt64(4)
                };

                var cla = classList.FirstOrDefault(c => c.id == student.ClassId);
                cla?.Students.Add(student);
            }

            foreach (var cla in classList)
                result.Add(cla);

            return result;
        }

        //添加班级
        public static void AddClass(Cla cla)
        {
            using var conn = new SqliteConnection($"Data Source={DbPath}");
            conn.Open();
            using var transaction = conn.BeginTransaction();

            try
            {
                var cmd = new SqliteCommand("INSERT INTO Classes (Name) VALUES (@Name)", conn, transaction);
                cmd.Parameters.AddWithValue("@Name", cla.Name ?? "");
                cmd.ExecuteNonQuery();

                var idCmd = new SqliteCommand("SELECT last_insert_rowid();", conn, transaction);
                cla.id = Convert.ToInt32(idCmd.ExecuteScalar());

                transaction.Commit();
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                Console.WriteLine($"[ERROR] 添加班级失败: {ex.Message}");
                throw;
            }
        }

        //添加学生
        public static void AddStudent(Student student)
        {
            using var conn = new SqliteConnection($"Data Source={DbPath}");
            conn.Open();
            using var transaction = conn.BeginTransaction();

            try
            {
                var cmd = new SqliteCommand(@"
            INSERT INTO Students (Name, StudentNumber, Gender, ClassId) 
            VALUES (@Name, @StudentNumber, @Gender, @ClassId)", conn, transaction);

                cmd.Parameters.AddWithValue("@Name", student.Name ?? "");
                cmd.Parameters.AddWithValue("@StudentNumber", student.StudentNumber);
                cmd.Parameters.AddWithValue("@Gender", student.Gender?.ToString() ?? "");
                cmd.Parameters.AddWithValue("@ClassId", student.ClassId);
                cmd.ExecuteNonQuery();

                var idCmd = new SqliteCommand("SELECT last_insert_rowid();", conn, transaction);
                student.id = Convert.ToInt32(idCmd.ExecuteScalar());

                transaction.Commit();
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                Console.WriteLine($"[ERROR] 添加学生失败: {ex.Message}");
                throw;
            }
        }

        //删除学生
        public static void DeleteStudent(Student student)
        {
            using var conn = new SqliteConnection($"Data Source={DbPath}");
            conn.Open();

            var cmd = new SqliteCommand("DELETE FROM Students WHERE Id = @Id", conn);
            cmd.Parameters.AddWithValue("@Id", student.id);
            cmd.ExecuteNonQuery();
        }
        //删除班级及其学生
        public static void DeleteClass(Cla cla)
        {
            using var conn = new SqliteConnection($"Data Source={DbPath}");
            conn.Open();
            using var transaction = conn.BeginTransaction();

            try
            {
                var deleteStudents = new SqliteCommand("DELETE FROM Students WHERE ClassId = @ClassId", conn, transaction);
                deleteStudents.Parameters.AddWithValue("@ClassId", cla.id);
                deleteStudents.ExecuteNonQuery();

                var deleteClass = new SqliteCommand("DELETE FROM Classes WHERE Id = @Id", conn, transaction);
                deleteClass.Parameters.AddWithValue("@Id", cla.id);
                deleteClass.ExecuteNonQuery();

                transaction.Commit();
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                Console.WriteLine($"[ERROR] 删除班级失败: {ex.Message}");
                throw;
            }
        }
        //更新班级信息
        public static void UpdateClass(Cla cla)
        {
            using var conn = new SqliteConnection($"Data Source={DbPath}");
            conn.Open();

            var cmd = new SqliteCommand("UPDATE Classes SET Name = @Name WHERE Id = @Id", conn);
            cmd.Parameters.AddWithValue("@Name", cla.Name ?? "");
            cmd.Parameters.AddWithValue("@Id", cla.id);
            cmd.ExecuteNonQuery();
        }
        //更新学生信息
        public static void UpdateStudent(Student student)
        {
            using var conn = new SqliteConnection($"Data Source={DbPath}");
            conn.Open();

            var cmd = new SqliteCommand(@"
        UPDATE Students 
        SET Name = @Name, 
            StudentNumber = @StudentNumber, 
            Gender = @Gender, 
            ClassId = @ClassId 
        WHERE Id = @Id", conn);

            cmd.Parameters.AddWithValue("@Name", student.Name ?? "");
            cmd.Parameters.AddWithValue("@StudentNumber", student.StudentNumber);
            cmd.Parameters.AddWithValue("@Gender", student.Gender?.ToString() ?? "");
            cmd.Parameters.AddWithValue("@ClassId", student.ClassId);
            cmd.Parameters.AddWithValue("@Id", student.id);
            cmd.ExecuteNonQuery();
        }

        //保存所有班级和学生数据（覆盖式保存）
        public static void Save(ObservableCollection<Cla> classes)
        {
            using var conn = new SqliteConnection($"Data Source={DbPath}");
            conn.Open();
            using var transaction = conn.BeginTransaction();

            try
            {
                new SqliteCommand("DELETE FROM Students", conn, transaction).ExecuteNonQuery();
                new SqliteCommand("DELETE FROM Classes", conn, transaction).ExecuteNonQuery();

                foreach (var cla in classes)
                {
                    var insertClass = new SqliteCommand("INSERT INTO Classes (Name) VALUES (@Name)", conn, transaction);
                    insertClass.Parameters.AddWithValue("@Name", cla.Name ?? "");
                    insertClass.ExecuteNonQuery();

                    var classIdCmd = new SqliteCommand("SELECT last_insert_rowid();", conn, transaction);
                    cla.id = Convert.ToInt32(classIdCmd.ExecuteScalar());

                    foreach (var student in cla.Students)
                    {
                        var insertStudent = new SqliteCommand(@"
                    INSERT INTO Students (Name, StudentNumber, Gender, ClassId) 
                    VALUES (@Name, @StudentNumber, @Gender, @ClassId)", conn, transaction);

                        insertStudent.Parameters.AddWithValue("@Name", student.Name ?? "");
                        insertStudent.Parameters.AddWithValue("@StudentNumber", student.StudentNumber);
                        insertStudent.Parameters.AddWithValue("@Gender", student.Gender?.ToString() ?? "");
                        insertStudent.Parameters.AddWithValue("@ClassId", cla.id);
                        insertStudent.ExecuteNonQuery();

                        var studentIdCmd = new SqliteCommand("SELECT last_insert_rowid();", conn, transaction);
                        student.id = Convert.ToInt32(studentIdCmd.ExecuteScalar());
                    }
                }

                transaction.Commit();
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                Console.WriteLine($"[ERROR] 保存失败，已回滚: {ex.Message}");
                throw;
            }
        }

    }
}
