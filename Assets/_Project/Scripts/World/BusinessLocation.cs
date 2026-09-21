using System.Collections.Generic;
using Geprek.Core;
using Geprek.Customers;
using Geprek.Stations;
using UnityEngine;

namespace Geprek.World
{
    /// <summary>
    /// Menandai sebuah lokasi sebagai tempat usaha dan mengumpulkan komponen yang
    /// dipakai alur hari: spawner pelanggan, meja penyajian, dan karyawan.
    /// Warung, ruko, dan restoran masing-masing punya satu komponen ini, sehingga
    /// GameManager cukup bertanya ke lokasi yang sedang aktif.
    /// </summary>
    public class BusinessLocation : MonoBehaviour
    {
        [SerializeField] LocationId id = LocationId.Warung;
        [SerializeField] string displayName = "Warung";
        [SerializeField] CustomerSpawner spawner;
        [SerializeField] PlateStation plateStation;
        [SerializeField] Progression.StaffDirector staffDirector;
        [Tooltip("Penggorengan tambahan yang baru terbuka lewat upgrade.")]
        [SerializeField] List<UpgradeToggle> upgradeFryers = new();

        public LocationId Id => id;
        public string DisplayName => displayName;
        public CustomerSpawner Spawner => spawner;
        public PlateStation Plate => plateStation;
        public Progression.StaffDirector Staff => staffDirector;

        /// <summary>Nyalakan penggorengan tambahan sebanyak yang sudah dibeli.</summary>
        public void RefreshFryers(int extraUnlocked)
        {
            for (int i = 0; i < upgradeFryers.Count; i++)
                if (upgradeFryers[i] != null) upgradeFryers[i].SetActiveState(i < extraUnlocked);
        }
    }
}
