using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Attendance.Theme
{
    public static class UserThemeStorage
    {
        private static readonly string ConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "user_theme.json");

        public static void Save(UserThemeConfig config)
        {
            var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(ConfigPath, json);
        }

        public static UserThemeConfig Load()
        {
            if (!File.Exists(ConfigPath)) return new UserThemeConfig();

            var json = File.ReadAllText(ConfigPath);
            return JsonSerializer.Deserialize<UserThemeConfig>(json) ?? new UserThemeConfig();
        }
    }
}
