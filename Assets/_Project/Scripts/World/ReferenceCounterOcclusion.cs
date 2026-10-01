using UnityEngine;

namespace Geprek.World
{
    /// <summary>Reuses the painting's worktops as foreground layers, hiding feet behind counters.</summary>
    public sealed class ReferenceCounterOcclusion : MonoBehaviour
    {
        [SerializeField] Sprite painting;
        readonly System.Collections.Generic.List<Sprite> _sprites = new();

        void Awake()
        {
            if (painting == null) return;
            // Coordinates are in the 1673 x 940 reference, measured from its upper left.
            Rect[] crops = {
                new(153,352,66,102), new(397,356,129,105), new(690,354,144,108),
                new(945,349,153,112), new(1248,346,149,116), new(1495,392,86,117)
            };
            float scaleX = painting.texture.width / 1673f, scaleY = painting.texture.height / 940f;
            for (int i = 0; i < crops.Length; i++)
            {
                var crop = crops[i];
                var rect = new Rect(crop.x * scaleX, (940 - crop.yMax) * scaleY, crop.width * scaleX, crop.height * scaleY);
                var sprite = Sprite.Create(painting.texture, rect, new(.5f, 0), 100 * scaleX, 0, SpriteMeshType.FullRect);
                sprite.name = "Counter foreground " + i;
                _sprites.Add(sprite);
                var go = new GameObject(sprite.name);
                go.transform.SetParent(transform, false);
                go.transform.localPosition = new((crop.center.x - 836.5f) / 100, (470 - crop.yMax) / 100, 0);
                var renderer = go.AddComponent<SpriteRenderer>();
                renderer.sprite = sprite;
                renderer.sortingOrder = Mathf.RoundToInt(-go.transform.position.y * 100);
            }
        }

        void OnDestroy()
        {
            foreach (var sprite in _sprites) if (sprite != null) Destroy(sprite);
        }
    }
}
