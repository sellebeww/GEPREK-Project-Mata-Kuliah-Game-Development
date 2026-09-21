using Geprek.Player;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Geprek.UI
{
    /// <summary>Tombol interaksi layar yang juga mendukung tahan, untuk menggeprek.</summary>
    public class HoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        public void OnPointerDown(PointerEventData e) => GeprekInput.Instance?.PressInteract();
        public void OnPointerUp(PointerEventData e) => GeprekInput.Instance?.ReleaseInteract();
        void OnDisable() => GeprekInput.Instance?.ReleaseInteract();
    }
}
