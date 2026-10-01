using UnityEngine;

namespace Geprek.World
{
    /// <summary>Bilah progres kecil di atas objek dunia (penggorengan, cobek, kesabaran pelanggan).</summary>
    public class WorldProgressBar : MonoBehaviour
    {
        [SerializeField] SpriteRenderer background;
        [SerializeField] SpriteRenderer fill;
        [SerializeField] float width = 0.8f;

        [Header("Warna sesuai nilai")]
        [SerializeField] bool colorByValue;
        [SerializeField] Color highColor = new(0.35f, 0.82f, 0.4f);
        [SerializeField] Color midColor = new(0.98f, 0.76f, 0.24f);
        [SerializeField] Color lowColor = new(0.9f, 0.31f, 0.28f);

        float _value = -1f;

        void Awake() => SetVisible(false);

        public void SetVisible(bool visible)
        {
            if (background != null) background.enabled = visible;
            if (fill != null) fill.enabled = visible;
        }

        public void SetValue(float value01)
        {
            value01 = Mathf.Clamp01(value01);
            if (Mathf.Approximately(_value, value01)) return;
            _value = value01;

            if (fill != null)
            {
                var s = fill.transform.localScale;
                fill.transform.localScale = new Vector3(width * value01, s.y, s.z);
                // jangkar di kiri
                fill.transform.localPosition = new Vector3(-width * 0.5f + width * value01 * 0.5f,
                                                           fill.transform.localPosition.y,
                                                           fill.transform.localPosition.z);
                if (colorByValue)
                    fill.color = value01 > 0.55f ? highColor : value01 > 0.25f ? midColor : lowColor;
            }
        }

        public void SetColor(Color c) { if (fill != null) fill.color = c; }
    }
}
