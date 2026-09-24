using System;

namespace FedAndFound.Core
{
    public interface IRng
    {
        float Value();                 // [0,1)
        int Range(int minInclusive, int maxExclusive);
    }

    public sealed class SystemRng : IRng
    {
        readonly Random _r;
        public SystemRng(int seed) { _r = new Random(seed); }
        public SystemRng() { _r = new Random(); }
        public float Value() => (float)_r.NextDouble();
        public int Range(int min, int max) => _r.Next(min, max);
    }

    public static class RngExt
    {
        public static bool Chance(this IRng rng, float p) => rng.Value() < p;
        public static T Pick<T>(this IRng rng, System.Collections.Generic.IList<T> list) => list[rng.Range(0, list.Count)];
    }
}
