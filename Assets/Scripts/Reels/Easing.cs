using UnityEngine;

namespace SlotGame.Reels
{
    public static class Easing
    {
        public static float InQuad(float t) => t * t;

        public static float OutCubic(float t)
        {
            float inv = 1f - t;
            return 1f - inv * inv * inv;
        }

        public static float InOutSine(float t) => -(Mathf.Cos(Mathf.PI * t) - 1f) * 0.5f;

        public static float OutBack(float t, float overshoot = 1.70158f)
        {
            float c3 = overshoot + 1f;
            float x = t - 1f;
            return 1f + c3 * x * x * x + overshoot * x * x;
        }
    }
}
