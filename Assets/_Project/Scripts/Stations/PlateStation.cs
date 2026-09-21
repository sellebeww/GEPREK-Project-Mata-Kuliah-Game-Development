using System.Collections.Generic;
using Geprek.Core;
using Geprek.Data;
using Geprek.Player;
using Geprek.World;
using UnityEngine;

namespace Geprek.Stations
{
    /// <summary>
    /// Meja penyajian. Komponen ditumpuk di sini; begitu susunannya cocok dengan
    /// salah satu resep yang terbuka, porsi jadi bisa diangkat.
    /// </summary>
    public class PlateStation : StationBase
    {
        [Header("Tampilan tumpukan")]
        [SerializeField] SpriteRenderer[] componentSlots;
        [SerializeField] SpriteRenderer matchedDishIcon;

        readonly List<ItemDef> _plate = new();
        RecipeDef _match;
        float _quality = 1f;
        bool _wasComplete;

        public bool IsEmptyPlate => _plate.Count == 0;
        public RecipeDef Match => _match;

        protected override void Awake()
        {
            base.Awake();
            RefreshVisual();
        }

        public override bool CanInteract(PlayerCarry carry)
        {
            if (carry == null) return false;
            if (carry.HasItem)
                return carry.Held.def != null && carry.Held.def.isPlatingComponent;
            return _plate.Count > 0;
        }

        public override string Hint(PlayerCarry carry)
        {
            if (carry != null && carry.HasItem)
            {
                if (carry.Held.def != null && carry.Held.def.isPlatingComponent)
                    return $"Tambah {carry.Held.def.displayName}";
                return "Tidak bisa ditaruh di piring";
            }
            if (_match != null) return $"Angkat {_match.displayName}";
            if (_plate.Count > 0) return "Ambil kembali bahan terakhir";
            return "Piring kosong";
        }

        public override void Interact(PlayerCarry carry)
        {
            if (!CanInteract(carry)) return;

            if (carry.HasItem)
            {
                var held = carry.Release();
                _plate.Add(held.def);
                _quality = _plate.Count == 1 ? held.quality : Mathf.Min(_quality, held.quality);
                Sfx(SfxId.Plate);
                Recalculate();
                return;
            }

            if (_match != null)
            {
                carry.TryTake(CarriedItem.FromRecipe(_match, _quality));
                Sfx(SfxId.Pickup);
                _plate.Clear();
                _quality = 1f;
                Recalculate();
                return;
            }

            // batalkan bahan terakhir supaya salah taruh masih bisa diperbaiki
            var last = _plate[^1];
            _plate.RemoveAt(_plate.Count - 1);
            carry.TryTake(CarriedItem.FromItem(last, _quality));
            Sfx(SfxId.Pickup);
            Recalculate();
        }

        /// <summary>Buang seluruh isi piring, dipakai tombol sampah.</summary>
        public void DumpPlate()
        {
            if (_plate.Count == 0) return;
            _plate.Clear();
            _quality = 1f;
            Recalculate();
        }

        void Recalculate()
        {
            _match = null;
            var game = Game;
            if (game != null && _plate.Count > 0)
            {
                foreach (var recipe in game.Database.recipes)
                {
                    if (recipe == null || !game.Progress.HasRecipe(recipe.id)) continue;
                    if (recipe.Matches(_plate)) { _match = recipe; break; }
                }
            }
            RefreshVisual();
        }

        void RefreshVisual()
        {
            bool complete = _match != null;
            if (complete && !_wasComplete)
            {
                JuiceDirector.DoSparkle(transform.position + Vector3.up * 1.0f, new Color(0.45f, 0.86f, 0.5f), 5);
                JuiceDirector.DoPunch(transform, 0.12f);
                JuiceDirector.DoPopup(_match.displayName, _match.icon,
                                      transform.position + Vector3.up * 1.35f, new Color(0.55f, 0.92f, 0.58f));
            }
            _wasComplete = complete;

            if (matchedDishIcon != null)
            {
                matchedDishIcon.sprite = complete ? _match.icon : null;
                matchedDishIcon.enabled = complete;
            }

            if (componentSlots != null)
            {
                for (int i = 0; i < componentSlots.Length; i++)
                {
                    if (componentSlots[i] == null) continue;
                    bool show = !complete && i < _plate.Count;
                    componentSlots[i].sprite = show ? _plate[i].icon : null;
                    componentSlots[i].enabled = show;
                }
            }

            ShowIcon(null);
            HideProgress();
        }
    }
}
