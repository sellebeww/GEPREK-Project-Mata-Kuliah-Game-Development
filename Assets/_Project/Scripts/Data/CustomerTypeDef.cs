using UnityEngine;

namespace Geprek.Data
{
    /// <summary>Jenis pelanggan: menentukan tampilan, kesabaran, dan kemurahan hatinya.</summary>
    [CreateAssetMenu(menuName = "Geprek/Customer Type", fileName = "Customer_")]
    public class CustomerTypeDef : ScriptableObject
    {
        public string id = "customer";
        public string displayName = "Pelanggan";

        [Tooltip("Set sprite jalan 4 arah untuk pelanggan ini.")]
        public CharacterSkin skin;

        [Header("Perilaku")]
        [Tooltip("Pengali waktu sabar. >1 lebih sabar, <1 lebih cepat marah.")]
        public float patienceMultiplier = 1f;

        [Tooltip("Pengali uang yang dibayarkan, termasuk tip.")]
        public float payMultiplier = 1f;

        [Tooltip("Bobot kemunculan relatif terhadap jenis pelanggan lain.")]
        public float spawnWeight = 1f;

        [Tooltip("Arc paling awal jenis pelanggan ini bisa muncul.")]
        public int minArc = 1;

        [Header("Tamu istimewa")]
        [Tooltip("Tamu istimewa tidak muncul acak. Kedatangannya diumumkan dan dampaknya ke reputasi besar.")]
        public bool isSpecialGuest;
        [Tooltip("Tambahan reputasi kalau tamu istimewa ini dilayani dengan sempurna.")]
        public float specialReputationGain = 0.10f;
        [Tooltip("Pengurangan reputasi kalau tamu istimewa ini kecewa.")]
        public float specialReputationLoss = 0.08f;
    }
}
