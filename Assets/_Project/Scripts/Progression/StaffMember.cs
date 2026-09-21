using System;
using Geprek.Core;
using UnityEngine;

namespace Geprek.Progression
{
    /// <summary>
    /// Data satu karyawan yang direkrut. Disimpan di save file, jadi isinya hanya
    /// tipe sederhana. Peran menentukan pekerjaan apa yang diambil di lapangan.
    /// </summary>
    [Serializable]
    public class StaffMember
    {
        public string id;
        public string displayName;
        public StaffRole role = StaffRole.Server;
        public int level = 1;

        [Tooltip("Gaji harian, dipotong otomatis saat warung tutup.")]
        public int salary = 15000;

        /// <summary>Pengali kecepatan kerja. Naik seiring level.</summary>
        public float Speed => 1f + (level - 1) * 0.18f;

        /// <summary>Peluang mengerjakan tugas lebih rapi, dipakai peran Cook.</summary>
        public float Efficiency => Mathf.Clamp01(0.55f + (level - 1) * 0.12f);

        public int UpgradeCost => Mathf.RoundToInt(45000 * Mathf.Pow(1.7f, level - 1) / 5000f) * 5000;

        public string RoleName => role switch
        {
            StaffRole.Cook => "Juru Masak",
            StaffRole.Server => "Pramusaji",
            StaffRole.Cashier => "Kasir",
            _ => "Karyawan"
        };

        public string RoleDetail => role switch
        {
            StaffRole.Cook => "Menggoreng ayam sendiri dan menaruh hasilnya di cobek.",
            StaffRole.Server => "Mengantar porsi yang sudah jadi ke pelanggan.",
            StaffRole.Cashier => "Menjaga kasir; pelanggan membayar lebih cepat dan lebih royal.",
            _ => ""
        };
    }
}
