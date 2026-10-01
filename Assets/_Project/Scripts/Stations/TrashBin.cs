using Geprek.Core;
using Geprek.Player;

namespace Geprek.Stations
{
    /// <summary>Tempat sampah: membuang apa pun yang dipegang, termasuk porsi salah.</summary>
    public class TrashBin : StationBase
    {
        public override bool CanInteract(PlayerCarry carry) => carry != null && carry.HasItem;

        public override string Hint(PlayerCarry carry) =>
            carry != null && carry.HasItem ? $"Buang {carry.Held.DisplayName}" : "Tempat sampah";

        public override void Interact(PlayerCarry carry)
        {
            if (!CanInteract(carry)) return;
            var item = carry.Release();
            Sfx(SfxId.Drop);
            Game?.RecordWaste();
            if (item != null && item.IsDish)
                GameEvents.RaiseToast("Porsi dibuang. Rugi bahan!");
        }
    }
}
