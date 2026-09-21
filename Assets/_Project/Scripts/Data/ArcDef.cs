using System.Collections.Generic;
using Geprek.Core;
using UnityEngine;

namespace Geprek.Data
{
    /// <summary>Satu babak cerita: 5 hari kerja dengan lokasi dan target tersendiri.</summary>
    [CreateAssetMenu(menuName = "Geprek/Arc", fileName = "Arc_")]
    public class ArcDef : ScriptableObject
    {
        [Header("Identitas")]
        public int arcNumber = 1;
        public string title = "Arc 1: Perintis";
        [TextArea] public string intro;
        [TextArea] public string outro;

        [Header("Lokasi usaha selama arc ini")]
        public LocationId businessLocation = LocationId.Warung;

        [Header("Hari kerja")]
        public List<DayPlanDef> days = new();

        [Header("Syarat lulus arc")]
        [Tooltip("Total omzet kumulatif sepanjang arc yang harus dicapai untuk lapor ke dosen.")]
        public int arcRevenueTarget = 400000;
        [Tooltip("Rata-rata kepuasan pelanggan minimal (0..1).")]
        public float minSatisfaction = 0.5f;

        public int DayCount => days.Count;
        public DayPlanDef GetDay(int index) =>
            days.Count == 0 ? null : days[Mathf.Clamp(index, 0, days.Count - 1)];
    }
}
