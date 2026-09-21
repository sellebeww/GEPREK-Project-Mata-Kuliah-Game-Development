using Geprek.Player;
using UnityEngine;

namespace Geprek.Stations
{
    /// <summary>Apa pun yang bisa dituju dan ditekan pemain: stasiun, pintu, orang.</summary>
    public interface IInteractable
    {
        Transform Transform { get; }

        /// <summary>Boleh berinteraksi sekarang? Menentukan sorotan dan tombol.</summary>
        bool CanInteract(PlayerCarry carry);

        /// <summary>Teks petunjuk pendek, misal "Ambil ayam" atau "Taruh di penggorengan".</summary>
        string Hint(PlayerCarry carry);

        /// <summary>Interaksi sekali tekan.</summary>
        void Interact(PlayerCarry carry);

        /// <summary>True kalau butuh ditahan (misal mengulek).</summary>
        bool UsesHold(PlayerCarry carry);

        /// <summary>Dipanggil tiap frame selama tombol ditahan.</summary>
        void HoldTick(PlayerCarry carry, float deltaTime);

        void HoldCancelled();

        void SetHighlighted(bool highlighted);
    }
}
