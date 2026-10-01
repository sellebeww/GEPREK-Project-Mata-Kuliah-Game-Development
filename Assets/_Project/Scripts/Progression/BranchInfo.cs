using System;
using UnityEngine;

namespace Geprek.Progression
{
    /// <summary>
    /// Satu cabang yang dibuka pada Arc 3. Cabang tidak dimainkan langsung —
    /// hasilnya masuk sebagai pemasukan harian, besarnya tergantung tingkat cabang,
    /// jumlah karyawan yang ditempatkan, dan reputasi bisnis secara keseluruhan.
    /// </summary>
    [Serializable]
    public class BranchInfo
    {
        public string id;
        public string name = "Cabang";
        public int level = 1;
        public int staffAssigned = 1;

        public const int BaseIncome = 32000;
        public const int MaxLevel = 4;

        public int UpgradeCost => Mathf.RoundToInt(120000 * Mathf.Pow(1.8f, level - 1) / 5000f) * 5000;
        public int StaffCost => 25000 * (staffAssigned + 1);

        /// <summary>Pemasukan harian cabang ini.</summary>
        public int DailyIncome(float reputation)
        {
            float staffFactor = 0.6f + 0.25f * staffAssigned;
            float repFactor = 0.7f + reputation * 0.8f;
            return Mathf.RoundToInt(BaseIncome * level * staffFactor * repFactor / 1000f) * 1000;
        }

        /// <summary>Biaya operasional harian cabang, dipotong bersama gaji.</summary>
        public int DailyCost => 8000 * level + 6000 * staffAssigned;
    }
}
