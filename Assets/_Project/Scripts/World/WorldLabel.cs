using UnityEngine;

namespace Geprek.World
{
    /// <summary>
    /// Label teks kecil di dunia, dipakai untuk menamai stasiun dapur dan nomor meja.
    /// Memakai TextMesh bawaan supaya tidak bergantung pada paket font tambahan.
    /// </summary>
    [ExecuteAlways, RequireComponent(typeof(TextMesh))]
    public class WorldLabel : MonoBehaviour
    {
        [SerializeField] TextMesh textMesh;
        MeshRenderer _renderer;
        SpriteRenderer _backing;

        void Awake()
        {
            if (textMesh == null) textMesh = GetComponent<TextMesh>();
        }

        void OnEnable() => RefreshSorting();
        void LateUpdate() => RefreshSorting();

        /// <summary>Keep text above its plaque even when the group is sorted by Y.</summary>
        public void RefreshSorting()
        {
            if (_renderer == null) _renderer = GetComponent<MeshRenderer>();
            if (_backing == null && transform.parent != null)
                _backing = transform.parent.GetComponent<SpriteRenderer>();
            if (_renderer == null || _backing == null) return;
            if (_renderer.sortingLayerID != _backing.sortingLayerID)
                _renderer.sortingLayerID = _backing.sortingLayerID;
            if (_renderer.sortingOrder != _backing.sortingOrder + 1)
                _renderer.sortingOrder = _backing.sortingOrder + 1;
        }

        public void SetText(string value)
        {
            if (textMesh == null) textMesh = GetComponent<TextMesh>();
            if (textMesh != null) textMesh.text = value;
        }

        /// <summary>Siapkan TextMesh dengan font bawaan Unity dan urutan gambar yang benar.</summary>
        public static WorldLabel Create(Transform parent, string text, Vector3 localPosition,
                                        float characterSize, Color color, int sortingOrder,
                                        TextAnchor anchor = TextAnchor.MiddleCenter, FontStyle style = FontStyle.Bold)
        {
            var go = new GameObject("Label");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;

            var tm = go.AddComponent<TextMesh>();
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                       ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            tm.font = font;
            tm.text = text;
            tm.color = color;
            tm.anchor = anchor;
            tm.alignment = TextAlignment.Center;
            tm.fontStyle = style;
            tm.fontSize = 48;                 // resolusi tinggi lalu dikecilkan lewat characterSize
            tm.characterSize = characterSize;
            tm.richText = false;

            var mr = go.GetComponent<MeshRenderer>();
            if (font != null) mr.sharedMaterial = font.material;
            mr.sortingOrder = sortingOrder;

            return go.AddComponent<WorldLabel>();
        }
    }
}
