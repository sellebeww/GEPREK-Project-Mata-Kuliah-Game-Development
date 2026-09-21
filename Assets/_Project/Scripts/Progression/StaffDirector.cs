using System.Collections.Generic;
using Geprek.Core;
using Geprek.Data;
using Geprek.Stations;
using UnityEngine;

namespace Geprek.Progression
{
    /// <summary>
    /// Memunculkan karyawan yang sudah direkrut di lokasi ini dan memberi mereka
    /// peran masing-masing. Jumlahnya disamakan dengan daftar di kemajuan pemain
    /// setiap kali hari dimulai atau daftar karyawannya berubah.
    /// </summary>
    public class StaffDirector : MonoBehaviour
    {
        [SerializeField] GameObject staffPrefab;
        [SerializeField] PlateStation plate;
        [SerializeField] Transform idleSpot;
        [SerializeField] float spacing = 1.0f;

        readonly List<GameObject> _spawned = new();

        void OnEnable()
        {
            GameEvents.DayStarted += OnDayStarted;
            GameEvents.StaffChanged += Sync;
        }

        void OnDisable()
        {
            GameEvents.DayStarted -= OnDayStarted;
            GameEvents.StaffChanged -= Sync;
        }

        void OnDayStarted(DayPlanDef plan, int dayNumber) => Sync();

        /// <summary>Samakan karyawan di lapangan dengan daftar yang sudah direkrut.</summary>
        public void Sync()
        {
            var game = GameManager.Instance;
            if (game == null || staffPrefab == null || !gameObject.activeInHierarchy) return;

            var list = game.Progress.staff;

            // kasir tidak berkeliling, jadi tidak perlu dimunculkan sebagai NPC berjalan
            var field = new List<StaffMember>();
            foreach (var m in list) if (m.role != StaffRole.Cashier) field.Add(m);

            while (_spawned.Count > field.Count)
            {
                int last = _spawned.Count - 1;
                if (_spawned[last] != null) Destroy(_spawned[last]);
                _spawned.RemoveAt(last);
            }

            for (int i = 0; i < field.Count; i++)
            {
                if (i >= _spawned.Count) _spawned.Add(CreateStaff(i));
                var go = _spawned[i];
                if (go == null) { _spawned[i] = CreateStaff(i); go = _spawned[i]; }

                var worker = go.GetComponent<StaffWorker>();
                worker?.Configure(field[i], plate, IdleAnchor(i));
                go.name = $"{field[i].RoleName} {i + 1}";

                ApplySkin(go, game.Database, field[i]);
            }
        }

        GameObject CreateStaff(int index)
        {
            Vector3 spot = IdleAnchor(index).position;
            return Instantiate(staffPrefab, spot, Quaternion.identity, transform);
        }

        Transform IdleAnchor(int index)
        {
            string name = $"IdleSpot{index}";
            var found = transform.Find(name);
            if (found != null) return found;

            var anchor = new GameObject(name).transform;
            anchor.SetParent(transform, false);
            Vector3 basePos = idleSpot != null ? idleSpot.position : transform.position;
            anchor.position = basePos + Vector3.right * (spacing * index);
            return anchor;
        }

        /// <summary>Seragam dipilih dari perannya supaya juru masak dan pramusaji mudah dibedakan.</summary>
        static void ApplySkin(GameObject go, GameDatabase db, StaffMember member)
        {
            if (db == null || db.staffSkins == null || db.staffSkins.Count == 0) return;

            int index = member.role switch
            {
                StaffRole.Cook => 0,
                StaffRole.Server => 1,
                _ => 2
            };
            var skin = db.staffSkins[Mathf.Clamp(index, 0, db.staffSkins.Count - 1)];

            var anim = go.GetComponentInChildren<Player.CharacterAnimator>();
            anim?.SetSkin(skin);
        }
    }
}
