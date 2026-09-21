using UnityEngine;

namespace Geprek.World
{
    /// <summary>
    /// Urutan gambar top-down: objek yang lebih bawah digambar di depan.
    /// Objek diam cukup dihitung sekali, objek bergerak diperbarui tiap frame.
    /// </summary>
    [ExecuteAlways]
    public class SortingByY : MonoBehaviour
    {
        [SerializeField] SpriteRenderer[] renderers;
        [SerializeField] bool isStatic = true;
        [Tooltip("Geser titik acuan, biasanya ke kaki objek.")]
        [SerializeField] float pivotOffset;
        [SerializeField] int orderBias;

        const int Scale = 100;

        void Awake() => Cache();
        void OnEnable() => Apply();
        void OnValidate() { Cache(); Apply(); }

        void Cache()
        {
            if (renderers == null || renderers.Length == 0)
                renderers = GetComponentsInChildren<SpriteRenderer>(true);
        }

        void LateUpdate()
        {
            if (!isStatic) Apply();
        }

        public void Apply()
        {
            if (renderers == null) return;
            int order = Mathf.RoundToInt(-(transform.position.y + pivotOffset) * Scale) + orderBias;
            for (int i = 0; i < renderers.Length; i++)
                if (renderers[i] != null) renderers[i].sortingOrder = order + i;
        }

        public void SetDynamic(bool dynamic) => isStatic = !dynamic;
    }
}
