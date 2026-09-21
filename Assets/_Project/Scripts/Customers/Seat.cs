using UnityEngine;

namespace Geprek.Customers
{
    /// <summary>Satu tempat duduk pelanggan, lengkap dengan titik hampiran dan titik duduk.</summary>
    public class Seat : MonoBehaviour
    {
        [SerializeField] Transform sitPoint;
        [SerializeField] Transform approachPoint;
        [Tooltip("Kursi tambahan dari upgrade, awalnya tidak aktif.")]
        [SerializeField] bool isUpgradeSeat;
        [SerializeField] GameObject[] visuals;

        public Customer Occupant { get; private set; }
        public bool IsUpgradeSeat => isUpgradeSeat;
        public bool Unlocked { get; private set; } = true;
        public bool IsFree => Unlocked && Occupant == null;

        public Vector3 SitPosition => sitPoint != null ? sitPoint.position : transform.position;
        public Vector3 ApproachPosition => approachPoint != null ? approachPoint.position : SitPosition;

        public void SetUnlocked(bool on)
        {
            Unlocked = on;
            if (visuals != null)
                foreach (var v in visuals) if (v != null) v.SetActive(on);
        }

        public void Assign(Customer customer) => Occupant = customer;
        public void Free() => Occupant = null;
    }
}
