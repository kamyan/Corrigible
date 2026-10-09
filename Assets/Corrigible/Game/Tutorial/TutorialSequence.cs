using System;
using System.Collections.Generic;
using UnityEngine;

namespace Corrigible.Game.Tutorial
{
    /// <summary>Eğitimin işaret edebileceği ekran bölgeleri. TutorialController'da her biri sahnedeki bir RectTransform'a bağlanır.</summary>
    public enum TutorialTarget
    {
        None, Gauges, TrustGauge, DependencyGauge, SingularityGauge, Map, RegionPanel, RegionActions,
        News, SpeedControls, ResearchPanel, KillSwitch, GiftModal, AlignmentResearch, ResearchReserve, RegionField, TeamsCard,
    }

    /// <summary>Oyun durumundan okunan koşullar. Adımın ne zaman görüneceğini ve ne zaman tamamlanacağını belirler.</summary>
    public enum TutorialCondition
    {
        [Tooltip("Hemen / koşulsuz")] None,
        [Tooltip("Oyuncu İleri'ye basınca")] NextButton,
        RegionSelected, MonitorBuilt, TimeRunning, AiDetected, InfiltrationVisible, AlignmentStarted, AuditUsed,
        CoreKnown, GiftOffered, VeilTorn, DependencyAbove40, TrustBelow30, ResearchCompleted,
        [Tooltip("En az bir gözlem noktası var (YZ'nin tespit edildiği izlenen bölge).")] ObservationPoint,
        [Tooltip("Araştırma birkaç gündür fonun tamamını yutuyor ve acil durum fonu ayarlanmamış.")] ResearchStarved,
        [Tooltip("Bir saha ekibi karargâhtan yola çıktı (ekipler kapalıysa her zaman sağlanır).")] TeamDispatched,
        [Tooltip("Bir bölgede baskın yapılabilir (deneysel mod).")] RaidAvailable,
    }

    [Serializable]
    public sealed class TutorialStep
    {
        public string title;
        [TextArea(3, 8)] public string body;
        public TutorialTarget target;
        [Tooltip("Adım bu koşul sağlanınca görünür (None: hemen).")]
        public TutorialCondition showWhen = TutorialCondition.None;
        [Tooltip("Adım bu koşul sağlanınca tamamlanır. NextButton: İleri düğmesi gösterilir.")]
        public TutorialCondition completeWhen = TutorialCondition.NextButton;
        [Tooltip("Adım ekrandayken oyun zamanı durur.")]
        public bool pauseGame = true;
        [Tooltip("Adım açılınca sağ paneldeki sekme (-1: değiştirme, 0: Araştırma, 1: YZ Günlüğü, 2: Rehber).")]
        public int openTab = -1;
    }

    [Serializable]
    public sealed class TutorialTip
    {
        [Tooltip("Görüldü kaydı için kalıcı kimlik; değiştirirsen ipucu yeniden gösterilir.")]
        public string id;
        public string title;
        [TextArea(3, 8)] public string body;
        public TutorialTarget target;
        public TutorialCondition trigger;
        public bool pauseGame = true;
    }

    /// <summary>
    /// Eğitimin içeriği: rehberli ilk oyunun adımları ve mekanikler ilk kez ortaya çıktığında gösterilen ipuçları.
    /// Metinler, hedefler ve koşullar Inspector'dan düzenlenir.
    /// </summary>
    [CreateAssetMenu(menuName = "Corrigible/Tutorial Sequence", fileName = "TutorialSequence")]
    public sealed class TutorialSequence : ScriptableObject
    {
        public List<TutorialStep> steps = new List<TutorialStep>();
        public List<TutorialTip> tips = new List<TutorialTip>();
    }
}
