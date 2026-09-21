using System;
using System.Collections.Generic;
using Geprek.Core;
using UnityEngine;

namespace Geprek.World
{
    /// <summary>Satu lokasi yang bisa ditampilkan: warung, rumah, dan seterusnya.</summary>
    [Serializable]
    public class LocationEntry
    {
        public LocationId id;
        public Transform root;
        public Transform playerSpawn;
        public Vector2 cameraMin;
        public Vector2 cameraMax;

        [Tooltip("Ukuran ortografis kamera untuk lokasi ini. Ruangan lebih kecil butuh angka lebih kecil.")]
        public float cameraSize = 4.4f;
    }

    /// <summary>
    /// Menyalakan satu lokasi dan mematikan sisanya, lalu memindahkan pemain
    /// serta batas kamera. Semua lokasi hidup di scene yang sama.
    /// </summary>
    public class LocationManager : MonoBehaviour
    {
        [SerializeField] List<LocationEntry> locations = new();
        [SerializeField] Transform player;
        [SerializeField] CameraFollow cameraFollow;

        public LocationId Current { get; private set; } = LocationId.Warung;

        /// <summary>Tempat usaha yang sedang aktif. Null kalau pemain sedang di rumah.</summary>
        public BusinessLocation ActiveBusiness { get; private set; }

        readonly Dictionary<LocationId, BusinessLocation> _businesses = new();

        void Awake()
        {
            foreach (var e in locations)
            {
                if (e?.root == null) continue;
                var biz = e.root.GetComponentInChildren<BusinessLocation>(true);
                if (biz != null) _businesses[e.id] = biz;
            }
        }

        public BusinessLocation BusinessOf(LocationId id) =>
            _businesses.TryGetValue(id, out var b) ? b : null;

        public void Show(LocationId id, bool snapPlayer)
        {
            LocationEntry target = null;
            for (int i = 0; i < locations.Count; i++)
            {
                var e = locations[i];
                if (e?.root == null) continue;
                bool on = e.id == id;
                e.root.gameObject.SetActive(on);
                if (on) target = e;
            }

            if (target == null)
            {
                Debug.LogWarning($"[Geprek] Lokasi {id} tidak terdaftar di LocationManager.");
                return;
            }

            Current = id;
            ActiveBusiness = BusinessOf(id);

            if (snapPlayer && player != null && target.playerSpawn != null)
            {
                player.position = target.playerSpawn.position;
                var rb = player.GetComponent<Rigidbody2D>();
                if (rb != null) rb.linearVelocity = Vector2.zero;
            }

            if (cameraFollow != null)
            {
                if (target.cameraSize > 0f) cameraFollow.SetSize(target.cameraSize);
                cameraFollow.SetBounds(target.cameraMin, target.cameraMax);
                cameraFollow.SnapToTarget();
            }
        }

        public Transform SpawnOf(LocationId id)
        {
            for (int i = 0; i < locations.Count; i++)
                if (locations[i] != null && locations[i].id == id) return locations[i].playerSpawn;
            return null;
        }
    }
}
