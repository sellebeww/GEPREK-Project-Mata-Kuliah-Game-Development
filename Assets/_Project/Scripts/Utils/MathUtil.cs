using System.Globalization;
using Geprek.Core;
using UnityEngine;

namespace Geprek.Utils
{
    public static class MathUtil
    {
        static readonly CultureInfo Id = CultureInfo.GetCultureInfo("id-ID");

        /// <summary>Format detik menjadi "mm:ss".</summary>
        public static string ToClock(float seconds)
        {
            if (seconds < 0f) seconds = 0f;
            int total = Mathf.FloorToInt(seconds);
            return $"{total / 60:00}:{total % 60:00}";
        }

        /// <summary>Format rupiah singkat: 12500 -> "Rp12.500".</summary>
        public static string ToRupiah(int amount) => "Rp" + amount.ToString("N0", Id);

        /// <summary>Progres hari (0..1) menjadi jam dinding "HH:MM".</summary>
        public static string ToGameClock(float t01, int openHour, int closeHour)
        {
            float hours = Mathf.Lerp(openHour, closeHour, Mathf.Clamp01(t01));
            int h = Mathf.FloorToInt(hours);
            int m = Mathf.FloorToInt((hours - h) * 60f);
            return $"{h:00}:{m:00}";
        }

        /// <summary>Ubah vektor gerak menjadi arah hadap 4 penjuru.</summary>
        public static Facing ToFacing(Vector2 dir, Facing fallback)
        {
            if (dir.sqrMagnitude < 0.0001f) return fallback;
            if (Mathf.Abs(dir.x) >= Mathf.Abs(dir.y))
                return dir.x < 0f ? Facing.Left : Facing.Right;
            return dir.y < 0f ? Facing.Down : Facing.Up;
        }
    }
}
