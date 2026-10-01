using Geprek.Core;
using Geprek.Player;
using Geprek.Progression;
using UnityEngine;

namespace Geprek.Stations
{
    /// <summary>Objek di rumah yang memicu aktivitas malam: ibu, ayah, handphone, meja, kasur.</summary>
    public class NightStation : StationBase
    {
        [SerializeField] NightManager nightManager;
        [SerializeField] NightAction action = NightAction.TalkMother;
        [SerializeField] string hintText = "Ngobrol";

        [Tooltip("Hanya bisa dipakai saat malam hari di rumah.")]
        [SerializeField] bool nightOnly = true;

        public override bool CanInteract(PlayerCarry carry)
        {
            if (nightManager == null) return false;
            if (!nightOnly) return true;
            return Game != null && Game.State == GameState.NightHome;
        }

        public override string Hint(PlayerCarry carry) => hintText;

        public override void Interact(PlayerCarry carry)
        {
            if (!CanInteract(carry)) return;
            Sfx(SfxId.Click);
            nightManager.Do(action);
        }
    }
}
