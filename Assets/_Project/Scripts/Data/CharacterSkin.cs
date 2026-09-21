using Geprek.Core;
using UnityEngine;

namespace Geprek.Data
{
    /// <summary>
    /// Kumpulan sprite jalan satu karakter. Frame disimpan berurutan per arah
    /// (Down, Left, Right, Up), masing-masing sebanyak <see cref="framesPerDirection"/>.
    /// </summary>
    [CreateAssetMenu(menuName = "Geprek/Character Skin", fileName = "Skin_")]
    public class CharacterSkin : ScriptableObject
    {
        public string displayName = "Karakter";

        [Tooltip("Jumlah frame per arah. Character 1 = 2, Character 2 = 3.")]
        public int framesPerDirection = 2;

        [Tooltip("Urutan: Down, Left, Right, Up. Masing-masing framesPerDirection buah.")]
        public Sprite[] frames;

        public bool IsValid => frames != null && frames.Length >= framesPerDirection * 4;

        public Sprite Get(Facing facing, int frame)
        {
            if (!IsValid) return null;
            int f = framesPerDirection <= 1 ? 0 : ((frame % framesPerDirection) + framesPerDirection) % framesPerDirection;
            int index = (int)facing * framesPerDirection + f;
            return frames[Mathf.Clamp(index, 0, frames.Length - 1)];
        }

        /// <summary>Frame diam untuk sebuah arah.</summary>
        public Sprite GetIdle(Facing facing) => Get(facing, 0);
    }
}
