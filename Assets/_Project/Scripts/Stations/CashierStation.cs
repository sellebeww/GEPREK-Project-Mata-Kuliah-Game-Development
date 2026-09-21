using Geprek.Core;
using Geprek.Player;
using Geprek.Utils;
using Geprek.World;
using UnityEngine;

namespace Geprek.Stations
{
    /// <summary>
    /// Meja kasir. Tidak dipakai untuk transaksi (pelanggan membayar di mejanya),
    /// tapi jadi tempat pemain mengecek rekap hari ini tanpa membuka menu jeda.
    /// </summary>
    public class CashierStation : StationBase
    {
        [SerializeField] Sprite reportIcon;

        public override bool CanInteract(PlayerCarry carry) =>
            Game != null && Game.State is GameState.DayOperating or GameState.DayClosing;

        public override string Hint(PlayerCarry carry) => "Cek rekap hari ini";

        public override void Interact(PlayerCarry carry)
        {
            if (!CanInteract(carry)) return;

            var today = Game.Today;
            Sfx(SfxId.Click);

            string pesan = $"Omzet {MathUtil.ToRupiah(today.revenue)} dari target {MathUtil.ToRupiah(today.targetRevenue)} " +
                           $"· {today.served} dilayani · {today.abandoned + today.turnedAway} hilang";
            GameEvents.RaiseToast(pesan, reportIcon);

            JuiceDirector.DoPopup(MathUtil.ToRupiah(today.revenue), reportIcon,
                                  transform.position + Vector3.up * 1.3f,
                                  today.TargetMet ? new Color(0.5f, 0.9f, 0.5f) : new Color(1f, 0.85f, 0.4f));
        }
    }
}
