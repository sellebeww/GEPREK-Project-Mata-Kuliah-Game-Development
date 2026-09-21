using UnityEngine;

namespace Geprek.Stations
{
    /// <summary>
    /// Menandai objek yang baru muncul setelah upgrade dibeli, misalnya penggorengan
    /// kedua. Objek dimatikan di awal lalu dinyalakan saat upgradenya dimiliki.
    /// </summary>
    public class UpgradeToggle : MonoBehaviour
    {
        [SerializeField] GameObject[] visuals;
        [SerializeField] bool startActive;

        void Awake() => SetActiveState(startActive);

        public void SetActiveState(bool on)
        {
            if (visuals != null && visuals.Length > 0)
            {
                foreach (var v in visuals) if (v != null) v.SetActive(on);
            }
            else
            {
                foreach (var r in GetComponentsInChildren<SpriteRenderer>(true)) r.enabled = on;
            }

            var col = GetComponent<Collider2D>();
            if (col != null) col.enabled = on;
        }
    }
}
