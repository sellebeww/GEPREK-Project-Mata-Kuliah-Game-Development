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

        WorldLabel _fallback;

        void Awake()
        {
            if (background != null) background.transform.localScale *= 1.35f;
            if (dishIcon != null)
            {
                dishIcon.transform.localScale *= 1.85f;
                dishIcon.transform.localPosition = new Vector3(-0.08f, 0.13f, 0f);
            }
            if (moodIcon != null) moodIcon.transform.localPosition = new Vector3(0.42f, 0.27f, 0f);
            if (patienceBar != null)
            {
                patienceBar.transform.localPosition = new Vector3(0f, -0.24f, 0f);
                patienceBar.transform.localScale = new Vector3(1.35f, 1f, 1f);
            }
            if (root != null)
            {
                _fallback = WorldLabel.Create(root.transform, "?", new Vector3(-0.08f, .13f, 0f),
                    .12f, new Color(.28f, .16f, .1f), 1005);
                _fallback.gameObject.SetActive(false);
            }
            Hide();
        }

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
            if (_fallback != null) _fallback.gameObject.SetActive(dish == null);
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
            if (_fallback != null) _fallback.gameObject.SetActive(icon == null);
            if (root != null) root.SetActive(true);
            if (background != null) background.enabled = true;
            if (dishIcon != null) { dishIcon.sprite = icon; dishIcon.enabled = icon != null; }
            if (moodIcon != null) moodIcon.enabled = false;
            patienceBar?.SetVisible(false);
        }
    }
}
