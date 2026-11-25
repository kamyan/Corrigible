using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Corrigible.Core
{
    public sealed partial class WorldMap
    {
        /// <summary>
        /// Paketlerin kademe etiketi: 0 karargâh (hep "weu", <c>guvenli</c>), 1 erken, 2 orta, 3 geç. Paketler karargâhtan BFS
        /// uzaklık katmanlarına bu sırayla dağıtılır (yakın bölge erken paketi alır; açılış karıştırması için bkz. <c>MixChance</c>).
        /// C2 bot ölçümüyle (200 tohum, sıradan arınmış paket zorluğu; Docs/DURUM.md › üretilmiş harita) kurulmuştur: kolay
        /// paketler (af, sa, jk) erken; orta zorluk ve sivili en çok kıran (me, in, sea, na) orta; zor paketler (cn, eeu, ru, oc) geç.
        /// Değiştirmek haritaları değiştirir (<see cref="GeneratorVersion"/> ve kayıt sürümü).
        /// </summary>
        public static readonly IReadOnlyDictionary<string, int> PackageStage = new ReadOnlyDictionary<string, int>(new Dictionary<string, int>
        {
            ["weu"] = 0,
            ["af"] = 1, ["sa"] = 1, ["jk"] = 1,
            ["me"] = 2, ["in"] = 2, ["sea"] = 2, ["na"] = 2,
            ["cn"] = 3, ["eeu"] = 3, ["ru"] = 3, ["oc"] = 3,
        });
    }
}
