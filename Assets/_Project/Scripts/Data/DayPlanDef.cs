using System.Collections.Generic;
using UnityEngine;

namespace Geprek.Data
{
    /// <summary>Rencana satu hari kerja: seberapa ramai dan berapa target omzetnya.</summary>
    [CreateAssetMenu(menuName = "Geprek/Day Plan", fileName = "Day_")]
    public class DayPlanDef : ScriptableObject
    {
        [Header("Identitas")]
        public int dayInArc = 1;
        public string title = "Hari 1";
        [TextArea] public string briefing = "Buka warung dan layani pelanggan.";

        [Header("Target")]
        [Tooltip("Omzet minimal supaya hari ini dianggap lulus.")]
        public int targetRevenue = 60000;

        [Header("Keramaian")]
        public int totalCustomers = 8;
        [Tooltip("Jeda antar pelanggan di awal hari (detik).")]
        public float spawnIntervalStart = 9f;
        [Tooltip("Jeda antar pelanggan di akhir hari (detik). Biasanya lebih kecil = makin ramai.")]
        public float spawnIntervalEnd = 5f;

        [Header("Kesulitan")]
        [Tooltip("Pengali kesabaran semua pelanggan hari ini.")]
        public float patienceMultiplier = 1f;
        [Tooltip("Maksimal jumlah komponen menu yang boleh dipesan hari ini. 0 = tanpa batas.")]
        public int maxRecipeComplexity;

        [Header("Target tambahan")]
        [Tooltip("Selain omzet. Kosongkan kalau hari ini hanya soal omzet.")]
        public List<DayObjective> objectives = new();

        [Header("Jam sibuk")]
        public bool hasLunchRush = true;
        [Range(0f, 1f)] [Tooltip("Kapan jam sibuk mulai, dihitung dari progres hari.")]
        public float rushStart = 0.38f;
        [Range(0f, 1f)] public float rushEnd = 0.64f;
        [Tooltip("Pengali jeda kedatangan saat jam sibuk. Di bawah 1 berarti lebih cepat.")]
        public float rushSpawnScale = 0.55f;
        [Tooltip("Pengali kesabaran saat jam sibuk. Di bawah 1 berarti pelanggan lebih buru-buru.")]
        public float rushPatienceScale = 0.9f;
    }
}
