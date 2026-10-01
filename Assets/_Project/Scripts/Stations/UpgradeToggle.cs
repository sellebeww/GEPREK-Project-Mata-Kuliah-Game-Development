using UnityEngine;

namespace Geprek.Stations
{
    /// <summary>
    /// Menandai objek yang baru muncul setelah upgrade dibeli, misalnya penggorengan
    /// kedua. Objek dimatikan di awal lalu dinyalakan saat upgradenya dimiliki.
    /// </summary>
    public class UpgradeToggle : MonoBehaviour
    {
        [SerializeField] GameObject[] visuals;
        [SerializeField] bool startActive;
        GameObject[] _childVisuals;
        bool[] _childInitiallyActive;

        void Awake() => SetActiveState(startActive);

        public void SetActiveState(bool on)
        {
            if (visuals != null && visuals.Length > 0)
            {
                foreach (var v in visuals) if (v != null) v.SetActive(on);
            }
            else
            {
                if (_childVisuals == null)
                {
                    _childVisuals = new GameObject[transform.childCount];
                    _childInitiallyActive = new bool[_childVisuals.Length];
                    for (int i = 0; i < _childVisuals.Length; i++)
                    {
                        _childVisuals[i] = transform.GetChild(i).gameObject;
                        _childInitiallyActive[i] = _childVisuals[i].activeSelf;
                    }
                }
                // Include TextMesh labels, while preserving initially hidden effects.
                // Renderer.enabled remains owned by cooking, highlights and progress bars.
                for (int i = 0; i < _childVisuals.Length; i++)
                    if (_childVisuals[i] != null)
                        _childVisuals[i].SetActive(on && _childInitiallyActive[i]);
            }

            var col = GetComponent<Collider2D>();
            if (col != null) col.enabled = on;
        }
    }
}
