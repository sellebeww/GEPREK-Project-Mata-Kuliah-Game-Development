using Geprek.World;
using UnityEngine;

namespace Geprek.Customers
{
    /// <summary>Balon pesanan di atas kepala pelanggan: ikon menu + sisa kesabaran.</summary>
    public class OrderBubble : MonoBehaviour
    {
        [SerializeField] GameObject root;
        [SerializeField] SpriteRenderer background;
        [SerializeField] SpriteRenderer dishIcon;
        [SerializeField] SpriteRenderer moodIcon;
        [SerializeField] WorldProgressBar patienceBar;

        [Header("Ikon suasana hati")]
        [SerializeField] Sprite moodHappy;
        [SerializeField] Sprite moodNeutral;
        [SerializeField] Sprite moodAnnoyed;
        [SerializeField] Sprite moodAngry;

        void Awake() => Hide();

        public void Hide()
        {
            if (root != null) root.SetActive(false);
            patienceBar?.SetVisible(false);
        }

        public void ShowOrder(Sprite dish)
        {
            if (root != null) root.SetActive(true);
            if (dishIcon != null) { dishIcon.sprite = dish; dishIcon.enabled = dish != null; }
            if (background != null) background.enabled = true;
            patienceBar?.SetVisible(true);
            SetPatience(1f);
        }

        public void SetPatience(float value01)
        {
            patienceBar?.SetValue(value01);
            if (moodIcon == null) return;

            Sprite mood = value01 > 0.6f ? moodHappy
                        : value01 > 0.35f ? moodNeutral
                        : value01 > 0.15f ? moodAnnoyed
                        : moodAngry;
            moodIcon.sprite = mood;
            moodIcon.enabled = mood != null;
        }

        /// <summary>Tampilkan satu ikon saja, misalnya saat pelanggan marah atau senang.</summary>
        public void ShowReaction(Sprite icon)
        {
            if (root != null) root.SetActive(true);
            if (background != null) background.enabled = true;
            if (dishIcon != null) { dishIcon.sprite = icon; dishIcon.enabled = icon != null; }
            if (moodIcon != null) moodIcon.enabled = false;
            patienceBar?.SetVisible(false);
        }
    }
}
