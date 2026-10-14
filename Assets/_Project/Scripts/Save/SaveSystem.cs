using System;
using System.IO;
using Geprek.Progression;
using UnityEngine;

namespace Geprek.Save
{
    [Serializable]
    public class SaveData
    {
        public int version = 1;
        public string savedAtUtc;
        public PlayerProgress progress = new();
    }

    /// <summary>Simpan/muat kemajuan sebagai JSON di persistentDataPath.</summary>
    public static class SaveSystem
    {
        const string FileName = "geprek_save.json";
#if UNITY_EDITOR
        // Editor previews and smoke tests use a separate file, never the player's progress.
        public static string EditorOverridePath
        {
            get
            {
                string value = UnityEditor.SessionState.GetString("Geprek.Preview.SavePath", "");
                return string.IsNullOrEmpty(value) ? null : value;
            }
            set => UnityEditor.SessionState.SetString("Geprek.Preview.SavePath", value ?? "");
        }
        static string Path => EditorOverridePath ?? System.IO.Path.Combine(Application.persistentDataPath, FileName);
#else
        static string Path => System.IO.Path.Combine(Application.persistentDataPath, FileName);
#endif

        public static bool HasSave => File.Exists(Path) || File.Exists(Path + ".bak");

        public static bool Save(PlayerProgress progress)
        {
            try
            {
                var data = new SaveData
                {
                    progress = progress,
                    savedAtUtc = DateTime.UtcNow.ToString("o")
                };
                if (progress == null) throw new ArgumentNullException(nameof(progress));
                string path = Path;
                Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));
                string temporary = path + ".tmp";
                using (var stream = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    byte[] bytes = System.Text.Encoding.UTF8.GetBytes(JsonUtility.ToJson(data, true));
                    stream.Write(bytes, 0, bytes.Length);
                    stream.Flush(true);
                }
                if (File.Exists(path)) File.Replace(temporary, path, path + ".bak");
                else File.Move(temporary, path);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Geprek] Gagal menyimpan: {e.Message}");
                return false;
            }
        }

        public static PlayerProgress Load()
        {
            foreach (string path in new[] { Path, Path + ".bak" })
            {
                if (!File.Exists(path)) continue;
                try
                {
                    string json = File.ReadAllText(path);
                    // JsonUtility can materialize a default class for an explicit null object.
                    if (!System.Text.RegularExpressions.Regex.IsMatch(json, "\"progress\"\\s*:\\s*\\{"))
                        throw new InvalidDataException("Data kemajuan tidak ditemukan.");
                    var data = JsonUtility.FromJson<SaveData>(json);
                    var p = data?.progress;
                    if (data == null || data.version != 1 || p == null || p.level < 1 ||
                        p.arcNumber < 1 || p.dayInArc < 1 || p.unlockedRecipes == null ||
                        p.upgrades == null || p.staff == null || p.branches == null ||
                        p.parentTopicsSeen == null || p.arcStats == null ||
                        p.upgrades.Exists(x => x == null) || p.staff.Exists(x => x == null) ||
                        p.branches.Exists(x => x == null))
                        throw new InvalidDataException("Data kemajuan tidak lengkap.");
                    return p;
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[Geprek] Gagal memuat save: {e.Message}");
                }
            }
            return null;
        }

        public static void Delete()
        {
            try
            {
                foreach (string path in new[] { Path, Path + ".bak", Path + ".tmp" })
                    if (File.Exists(path)) File.Delete(path);
            }
            catch (Exception e) { Debug.LogWarning($"[Geprek] Gagal menghapus save: {e.Message}"); }
        }
    }
}
