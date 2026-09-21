using Geprek.Core;
using Geprek.Data;
using UnityEngine;

namespace Geprek.Player
{
    /// <summary>
    /// Tangan pemain. Hanya muat satu benda, seperti Diner Dash / Overcooked,
    /// supaya pemain harus mengatur urutan kerja.
    /// </summary>
    public class PlayerCarry : MonoBehaviour
    {
        [SerializeField] SpriteRenderer heldIcon;
        [SerializeField] Transform heldAnchor;

        [Tooltip("Matikan untuk tangan karyawan: HUD pemain tidak boleh ikut berubah.")]
        [SerializeField] bool broadcastEvents = true;

        public CarriedItem Held { get; private set; }

        public void SetBroadcast(bool on) => broadcastEvents = on;

        public bool IsEmpty => Held == null;
        public bool HasItem => Held != null;

        void Awake() => Refresh();

        /// <summary>Ambil benda. Gagal kalau tangan sudah penuh.</summary>
        public bool TryTake(CarriedItem item)
        {
            if (item == null || Held != null) return false;
            Held = item;
            Refresh();
            return true;
        }

        /// <summary>Lepas benda yang dipegang dan kembalikan ke pemanggil.</summary>
        public CarriedItem Release()
        {
            var item = Held;
            Held = null;
            Refresh();
            return item;
        }

        public void Clear()
        {
            Held = null;
            Refresh();
        }

        void Refresh()
        {
            if (heldIcon != null)
            {
                heldIcon.sprite = Held?.Icon;
                heldIcon.enabled = Held != null && heldIcon.sprite != null;
            }
            if (broadcastEvents) GameEvents.RaiseCarryChanged(Held);
        }
    }
}
