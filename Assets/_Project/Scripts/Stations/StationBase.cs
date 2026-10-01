using Geprek.Audio;
using Geprek.Core;
using Geprek.Data;
using Geprek.Player;
using Geprek.World;
using UnityEngine;

namespace Geprek.Stations
{
    /// <summary>
    /// Perilaku bersama semua stasiun: sorotan saat didekati, tempat menaruh ikon
    /// benda yang sedang diproses, dan bilah progres.
    /// </summary>
    public abstract class StationBase : MonoBehaviour, IInteractable
    {
        [Header("Tampilan stasiun")]
        [SerializeField] protected SpriteRenderer bodyRenderer;
        [SerializeField] protected SpriteRenderer itemIcon;
        [SerializeField] protected WorldProgressBar progressBar;
        [Tooltip("Cincin di kaki objek yang menyala saat pemain cukup dekat untuk memakainya.")]
        [SerializeField] protected SpriteRenderer highlightGlow;
        [SerializeField] protected Color highlightTint = new(1f, 0.93f, 0.68f);

        [Header("Label")]
        [SerializeField] protected string stationName = "Stasiun";

        Color _baseColor = Color.white;
        bool _highlighted;

        public Transform Transform => transform;
        public string StationName => stationName;

        protected virtual void Awake()
        {
            if (bodyRenderer == null) bodyRenderer = GetComponentInChildren<SpriteRenderer>();
            if (bodyRenderer != null) _baseColor = bodyRenderer.color;
            ShowIcon(null);
            progressBar?.SetVisible(false);
            if (highlightGlow != null) highlightGlow.enabled = false;
        }

        public virtual void SetHighlighted(bool highlighted)
        {
            if (_highlighted == highlighted) return;
            _highlighted = highlighted;
            if (bodyRenderer != null)
                bodyRenderer.color = highlighted ? highlightTint : _baseColor;
            if (highlightGlow != null) highlightGlow.enabled = highlighted;
        }

        protected void ShowIcon(Sprite sprite)
        {
            if (itemIcon == null) return;
            itemIcon.sprite = sprite;
            itemIcon.enabled = sprite != null;
        }

        protected void ShowProgress(float value01)
        {
            if (progressBar == null) return;
            progressBar.SetVisible(true);
            progressBar.SetValue(value01);
        }

        protected void HideProgress() => progressBar?.SetVisible(false);

        protected static void Sfx(SfxId id) => AudioManager.Play(id);

        protected static GameManager Game => GameManager.Instance;

        // ---- kontrak yang diisi turunan ----
        public abstract bool CanInteract(PlayerCarry carry);
        public abstract string Hint(PlayerCarry carry);
        public abstract void Interact(PlayerCarry carry);

        public virtual bool UsesHold(PlayerCarry carry) => false;
        public virtual void HoldTick(PlayerCarry carry, float deltaTime) { }
        public virtual void HoldCancelled() { }
    }
}
