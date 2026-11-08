using System;
using System.Collections.Generic;

namespace Corrigible.Desk
{
    /// <summary>
    /// Devir fişinin dilden bağımsız verisi (Revizyon 3 · R19c, PLAN §1.2): yalnız sayılar, kimlikler ve kişi adları; görünür
    /// sözcük taşımaz, metne <c>HandoverSlipFormat</c> o anki dilde çevirir. Köprü (<c>HandoverSlipFactsBuilder</c>) simülasyonun
    /// public, salt okunur durumundan kurar: RNG çekmez, komut yazmaz, gizli geleceği (yeni gelen, robot, sabotaj) okumaz.
    /// Dosyalanan fişin kopyası EVRAK arşivine <c>JsonUtility</c> ile yazılır (son 5; oyun kaydına girmez).
    /// <para>
    /// Dikkat: <c>JsonUtility</c> null dizgiyi boş dizgi olarak geri verir; okuyan her yer boş adı <c>string.IsNullOrEmpty</c>
    /// ile sınar (boş cep, eğitim yok, üretici yok).
    /// </para>
    /// </summary>
    [Serializable]
    public sealed class HandoverSlipFacts
    {
        /// <summary>Basılan fişin kimliği (<c>Bd.ReceiptId</c>); fişle eşleşme için, fişte yazılmaz.</summary>
        public string receiptId;
        /// <summary>Kapanan vardiyanın numarası (<c>S.Day + 1</c>): “VARDİYA 001”, “No. 001/1”.</summary>
        public int shift;
        /// <summary>Basım sırası: “No. 001/1”, “plan 001/1”. Vardiyada tek basım (Core: <c>Printed</c> tek), hep 1.</summary>
        public int copy = 1;
        /// <summary>Canlı kişi sayısı ve sığınağın yer sınırı (<c>boardResidentCapacity</c>): “8/10 kişi”.</summary>
        public int alive, capacity;
        /// <summary>Erzak: kapanıştan önce, tavan ve kapanış tahmini (<c>supplies + BoardFoodPreview</c>).</summary>
        public double supplies, suppliesCapacity, suppliesAfter;
        /// <summary>Saha kadrosu: hep üç satır, cep sırasıyla tek, emp, ana.</summary>
        public List<SlipFieldRow> field = new List<SlipFieldRow>();
        /// <summary>Sığınaktaki kişiler: cep sırası (craft, cook, train), sonra Boşta'lar deste sırasıyla.</summary>
        public List<SlipShelterRow> shelter = new List<SlipShelterRow>();
        /// <summary>KAPANIŞTA BEKLENEN · EĞİTİM.</summary>
        public SlipTraining training = new SlipTraining();
        /// <summary>KAPANIŞTA BEKLENEN · ÜRETİM.</summary>
        public SlipProduction production = new SlipProduction();
        /// <summary>KAPANIŞTA BEKLENEN · KAPI: kapıdaki açık başvuru (0 ya da 1).</summary>
        public int openApplications;
        /// <summary>Uyarı kutuları (KAPI, DOSYA); sırası kutu sırasıdır.</summary>
        public List<SlipWarning> warnings = new List<SlipWarning>();
    }

    /// <summary>
    /// Saha kadrosu satırı. <see cref="role"/>: cep görevi (tek, emp, ana; disk harfi buradan). <see cref="name"/> boşsa cep boştur
    /// (“—”). <see cref="items"/>: kişinin eşya türü kimlikleri yuva sırasıyla (kit_*, armor, emp_cell, medkit …).
    /// </summary>
    [Serializable]
    public sealed class SlipFieldRow
    {
        public string role;
        public string name;
        public List<string> items = new List<string>();
    }

    /// <summary>
    /// Sığınak satırı. <see cref="job"/>: craft | cook | train | rest (<c>BoardDropRules.PlaceOf</c>); <see cref="subject"/> yalnız
    /// train'de (eğitim konusu kimliği). <see cref="state"/> ve <see cref="stateShifts"/>: durum eki (“yatıyor 2”, “kısıtlı 2”).
    /// </summary>
    [Serializable]
    public sealed class SlipShelterRow
    {
        public string name, job, subject;
        public SlipPersonState state;
        public int stateShifts;
    }

    /// <summary>
    /// Kişinin fişteki durum eki: yaralı (can eksik, ayakta), yatıyor n (vardiya), kısıtlı n (kalan vardiya), kısıtlı · bitti
    /// (süre bu kapanışta doldu).
    /// </summary>
    public enum SlipPersonState { None, Hurt, Down, Restricted, RestrictionEnded }

    /// <summary>
    /// Eğitim tahmini: train cebindeki kişi, konu, deneyim önce/sonra (<c>boardExpertAt</c>'ta durur) ve kademe önce/sonra
    /// (kademe değişirse satır “→ EĞİTİMLİ” gibi biter). <see cref="active"/> false ise eğitim yok.
    /// </summary>
    [Serializable]
    public sealed class SlipTraining
    {
        public bool active;
        public string name, subject;
        public int xpBefore, xpAfter, tierBefore, tierAfter;
    }

    /// <summary>
    /// Üretim tahmini. <see cref="kind"/>: <see cref="SlipProductionKind.None"/> iş emri yok · çıktı yok;
    /// <see cref="SlipProductionKind.NoOutput"/> {ürün} · çıktı yok; <see cref="SlipProductionKind.Progress"/>
    /// {ürün} done/turns → after/turns · {üretici}. <see cref="itemType"/> ürünün eşya türü kimliği, <see cref="maker"/> üreticinin adı.
    /// </summary>
    [Serializable]
    public sealed class SlipProduction
    {
        public SlipProductionKind kind;
        public string itemType, maker;
        public int done, after, turns;
    }

    /// <summary>Üretim satırının biçimi (bkz. <see cref="SlipProduction"/>).</summary>
    public enum SlipProductionKind { None, NoOutput, Progress }

    /// <summary>Uyarı kutusu: tür ve (DOSYA'da) kişi adı ya da aynı türün virgülle birleşmiş adları.</summary>
    [Serializable]
    public sealed class SlipWarning
    {
        public SlipWarningKind kind;
        public string name;
    }

    /// <summary>
    /// Uyarı türü: <see cref="Door"/> kararsız kapı başvurusu bu kapanışta reddedilir; DOSYA uyarıları Core'un kabul riski
    /// sırasıyla <see cref="FileRestrictionExpired"/> (kısıt süresi doldu), <see cref="FileClues"/> (iki bağımsız bulgu),
    /// <see cref="FileReview"/> (inceleme fişi açık).
    /// </summary>
    public enum SlipWarningKind { Door, FileReview, FileRestrictionExpired, FileClues }
}
