using UnityEngine;

namespace Geprek.Core
{
    /// <summary>
    /// Singleton sederhana untuk manager yang hanya ada satu di scene.
    /// Tidak membuat objek sendiri: instance diambil dari objek yang sudah ada di scene,
    /// supaya semua referensi tetap bisa diatur lewat Inspector.
    /// </summary>
    public abstract class Singleton<T> : MonoBehaviour where T : Singleton<T>
    {
        static T _instance;

        public static T Instance
        {
            get
            {
                if (_instance == null) _instance = FindAnyObjectByType<T>(FindObjectsInactive.Include);
                return _instance;
            }
        }

        public static bool Exists => Instance != null;

        protected virtual void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = (T)this;
        }

        protected virtual void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }
    }
}
