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
        static string Path => System.IO.Path.Combine(Application.persistentDataPath, FileName);

        public static bool HasSave => File.Exists(Path);

        public static void Save(PlayerProgress progress)
        {
            try
            {
                var data = new SaveData
                {
                    progress = progress,
                    savedAtUtc = DateTime.UtcNow.ToString("o")
                };
                File.WriteAllText(Path, JsonUtility.ToJson(data, true));
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Geprek] Gagal menyimpan: {e.Message}");
            }
        }

        public static PlayerProgress Load()
        {
            try
            {
                if (!File.Exists(Path)) return null;
                var data = JsonUtility.FromJson<SaveData>(File.ReadAllText(Path));
                return data?.progress;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Geprek] Gagal memuat save: {e.Message}");
                return null;
            }
        }

        public static void Delete()
        {
            try { if (File.Exists(Path)) File.Delete(Path); }
            catch (Exception e) { Debug.LogWarning($"[Geprek] Gagal menghapus save: {e.Message}"); }
        }
    }
}
