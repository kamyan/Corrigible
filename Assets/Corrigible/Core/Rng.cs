using System;
using System.Collections.Generic;

namespace Corrigible.Core
{
    /// <summary>
    /// Tohumlu rastgele sayı üreteci (mulberry32). Tarayıcı prototipindeki üreteçle bit düzeyinde aynıdır:
    /// aynı tohum + aynı komutlar = aynı oyun.
    /// </summary>
    [Serializable]
    public sealed class Rng
    {
        public uint State;

        public Rng(uint seed) { State = seed; }

        public double Next()
        {
            unchecked
            {
                State += 0x6D2B79F5u;
                uint t = (State ^ (State >> 15)) * (1u | State);
                t = (t + (t ^ (t >> 7)) * (61u | t)) ^ t;
                return (t ^ (t >> 14)) / 4294967296.0;
            }
        }

        public double Range(double a, double b) => a + Next() * (b - a);

        public T Pick<T>(IList<T> list) => list[(int)Math.Floor(Next() * list.Count)];

        /// <summary>
        /// Harekât tohumu ve bir tuzdan birbirinden bağımsız alt tohum (splitmix tarzı karıştırma). Ayrı akışlar (tesis planı
        /// <c>FacilityMaps</c>, üretilmiş dünya haritası <c>WorldMap</c>) <c>S.Rng</c>'den tüketmeden buradan kurulur.
        /// Değiştirirsen bu akışların bütün çıktıları değişir (kayıt sürümü).
        /// </summary>
        public static uint Mix(uint seed, uint index)
        {
            unchecked
            {
                uint h = seed * 0x9E3779B1u ^ (index + 0x7F4A7C15u) * 0x85EBCA77u;
                h ^= h >> 16; h *= 0x7FEB352Du; h ^= h >> 15; h *= 0x846CA68Bu; h ^= h >> 16;
                return h;
            }
        }
    }

    /// <summary>JavaScript'in Math.round davranışı (yarımlar yukarı); C#'ın bankacı yuvarlamasıyla karışmasın diye.</summary>
    public static class Js
    {
        public static double Round(double v) => Math.Floor(v + 0.5);
    }
}
