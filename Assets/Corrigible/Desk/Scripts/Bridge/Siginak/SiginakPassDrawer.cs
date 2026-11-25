using System;
using Corrigible.Core;
using Corrigible.Game;
using Corrigible.Game.Audio;
using UnityEngine;

namespace Corrigible.Desk.Bridge
{
    /// <summary>
    /// Kapının yanındaki çift taraflı belge çekmecesinin köprüsü (Docs/KAPI-BELGELERI-VE-YZ-MEKANLARI-TASARIMI.md §A). Dışarıdan gelen
    /// iki belge (olay tutanağı <see cref="SiginakShelterEvent"/>, sunucu ödülü <see cref="SiginakServerReward"/>) çekmecenin tablasına
    /// konur; bu bileşen çekmeceyi, lambasını ve şalterini onların durumuna göre sürer:
    /// <list type="bullet">
    /// <item>Gelen belge tablada (<see cref="ShelterEventPaperView.AtDoor"/>): lamba amber (çekmece kapalıyken yanıp söner). Çekmecenin
    /// önüne tık çekmeceyi açar ya da kapar; açıkken tabladaki belgeye tık onu ele getirir (<see cref="ShelterEventPaperView.DoorOpen"/>).
    /// Esc ile bırakılan belge tablaya döner.</item>
    /// <item>Karar verilen belge tabladadır (<see cref="ShelterEventPaperView.InOutbox"/>): lamba yeşil yanıp söner. Şaltere tık kolu
    /// çeker, çekmece kapanır ve belge duvarın öbür yanına geçer (<see cref="ShelterEventPaperView.Send"/>). Karar komutu KARAR'da
    /// verilmiştir; şalter yalnız sunumdur (kayda girmez, Core'a dokunmaz).</item>
    /// <item>Yeni belge yalnız çekmece kapalıyken ve tablada giden belge yokken gelir (<see cref="ReadyForDelivery"/>): açık ve boşta
    /// duran çekmece dışarıdan çekilip kapanır. Vardiya dosyalanırken gönderilmemiş belge kendiliğinden gider.</item>
    /// <item>Kayıttan devamda ve yeni oyunda çekmece kapalıdır; gönderilmemiş belge kaydedilmez (karar zaten kayıtta).</item>
    /// </list>
    /// </summary>
    [DefaultExecutionOrder(-5)]
    public sealed class SiginakPassDrawer : MonoBehaviour
    {
        [SerializeField] GameController game;
        [SerializeField] PassDrawer drawer;
        [SerializeField] SiginakShelterEvent shelterEvent;
        [SerializeField] SiginakServerReward serverReward;
        [Tooltip("Vardiya dosyalanırken gönderilmemiş belge kendiliğinden gider (ShiftFiling).")]
        [SerializeField] SiginakDesk desk;
        [SerializeField] HoverTag hoverTag;

        /// <summary>Etiketler (kaynak metinler; DeskTarget gösterirken çevirir, satırlar burada çevrilir).</summary>
        static readonly string DrawerTitle = Loc.N("BELGE ÇEKMECESİ"),
            DrawerIncomingTitle = Loc.N("ÇEKMECE · BELGE VAR"),
            DrawerOutgoingTitle = Loc.N("ÇEKMECE · KARAR HAZIR"),
            DrawerIdleLine = Loc.N("Duvarı geçen çekmece: dışarıdan gelen belgeler buraya konur."),
            DrawerIncomingLine = Loc.N("Dışarıdan bir belge geldi: çekmeceyi aç, belgeyi al."),
            DrawerOpenLine = Loc.N("Belgeye tıkla ya da çekmeceyi kapat."),
            DrawerOutgoingLine = Loc.N("Karar çekmecede: yanındaki şalteri çekip dışarıya bildir."),
            SwitchTitle = Loc.N("ŞALTER · DIŞARIYA BİLDİR"),
            SwitchReadyLine = Loc.N("Çekince çekmece kapanır, belge duvarın öbür yanına geçer."),
            SwitchIdleLine = Loc.N("Gönderilecek belge yok.");

        GameRecord shownOf;
        bool sending;

        Simulation Sim => game != null ? game.Sim : null;
        ShelterEventPaperView EventView => shelterEvent != null && shelterEvent.isActiveAndEnabled ? shelterEvent.View : null;
        ShelterEventPaperView RewardView => serverReward != null && serverReward.isActiveAndEnabled ? serverReward.View : null;

        public PassDrawer Drawer => drawer;
        /// <summary>Tablada okunmayı bekleyen belge var (ya da oraya iniyor).</summary>
        public bool Incoming => Waiting(EventView) || Waiting(RewardView);
        /// <summary>Tablada karara bağlanmış, şalteri bekleyen belge var (ya da oraya iniyor).</summary>
        public bool Outgoing => Out(EventView) || Out(RewardView);
        /// <summary>Şalter çekildi, çekmece kapanıyor: kapanınca belge gider.</summary>
        public bool Sending => sending;
        /// <summary>Çekmece kapalı, duruyor ve tablada giden belge yok: yeni belge konabilir.</summary>
        public bool ReadyForDelivery => drawer == null || !sending && !Outgoing && drawer.FullyClosed;
        /// <summary>Belge gönderildi (şalterle ya da dosyalanırken kendiliğinden; testler).</summary>
        public event Action Sent;

        static bool Waiting(ShelterEventPaperView v) => v != null && v.Data != null && v.AtDoor;
        static bool Out(ShelterEventPaperView v) => v != null && v.InOutbox;

        void OnEnable()
        {
            if (drawer != null)
            {
                drawer.DrawerClicked += ClickDrawer;
                drawer.SwitchClicked += ClickSwitch;
                drawer.Sound += Play;
                if (hoverTag != null)
                {
                    if (drawer.DrawerTarget != null && drawer.DrawerTarget.Tag == null) drawer.DrawerTarget.Tag = hoverTag;
                    if (drawer.SwitchTarget != null && drawer.SwitchTarget.Tag == null) drawer.SwitchTarget.Tag = hoverTag;
                }
            }
            // Oda baskından dönüp yeniden açılınca aynı kaydın sunumu sürer; başka kayıtta çekmece kapalı açılır
            if (Sim == null || !ReferenceEquals(Sim.Record, shownOf)) ResetDrawer();
        }

        void OnDisable()
        {
            if (drawer == null) return;
            drawer.DrawerClicked -= ClickDrawer;
            drawer.SwitchClicked -= ClickSwitch;
            drawer.Sound -= Play;
        }

        /// <summary>Kayıttan devam ve yeni oyun: çekmece animasyonsuz kapalı, şalter dinlenmede.</summary>
        public void ResetDrawer()
        {
            shownOf = Sim?.Record;
            sending = false;
            if (drawer != null) drawer.SetOpenNow(false);
        }

        /// <summary>
        /// Yeni belgenin gelişi (köprülerin Flush'ı sorar): çekmece hazırsa true. Açık ve boşta duran çekmece dışarıdan çekilip kapanır
        /// (kapanınca sonraki sorgu true döner); tablada giden belge varken ya da şalter çekilirken false.
        /// </summary>
        public bool RequestDelivery()
        {
            if (drawer == null) return true;
            if (ReadyForDelivery) return true;
            if (!sending && !Outgoing && drawer.IsOpen && !InHand() && !PaperMoving) drawer.Close();
            return false;
        }

        bool PaperMoving => EventView != null && EventView.Moving || RewardView != null && RewardView.Moving;

        bool InHand() => EventView != null && EventView.InHand || RewardView != null && RewardView.InHand;

        /// <summary>Çekmecenin önüne tık: açar ya da kapar (şalter çekilirken ve tabladaki kâğıt hareket ederken değil: kâğıdın hedefi
        /// tablanın o anki dünya pozudur).</summary>
        public void ClickDrawer()
        {
            if (drawer == null || sending || drawer.Pulling || PaperMoving) return;
            if (drawer.IsOpen) drawer.Close(); else drawer.Open();
        }

        /// <summary>
        /// Şaltere tık: tablada karara bağlanmış ve yerine oturmuş belge varsa kol çekilir, çekmece kapanır; kapanınca belge gider. Yoksa
        /// ret sesi (kol kıpırdamaz).
        /// </summary>
        public void ClickSwitch()
        {
            if (drawer == null || sending) return;
            bool ready = Settled(EventView) || Settled(RewardView);
            if (!ready) { drawer.Deny(); return; }
            sending = true;
            drawer.PullSwitch();
            drawer.Close();
        }

        static bool Settled(ShelterEventPaperView v) => v != null && v.State == ShelterEventPaperView.Where.Outbox;

        /// <summary>Tabladaki karara bağlı belgeler hemen gider (şalterin sonu; dosyalanırken kendiliğinden). Gittiyse true.</summary>
        public bool SendNow()
        {
            bool any = false;
            if (EventView != null && EventView.Send()) any = true;
            if (RewardView != null && RewardView.Send()) any = true;
            sending = false;
            if (any) { Sfx.Play(Cue.PaperSlide, .7f); Sent?.Invoke(); }
            return any;
        }

        void Update()
        {
            if (drawer == null) return;
            if (Sim != null && !ReferenceEquals(Sim.Record, shownOf)) ResetDrawer();
            // Şalter çekildi: çekmece kapanınca belge duvarın öbür yanına geçer
            if (sending && drawer.FullyClosed && !drawer.Pulling) SendNow();
            // Vardiya dosyalanırken gönderilmemiş belge kendiliğinden gider (yeni olayın tutanağı ancak bundan sonra gelir)
            if (!sending && desk != null && desk.ShiftFiling && (Settled(EventView) || Settled(RewardView)))
            {
                SendNow();
                if (drawer.IsOpen) drawer.SetOpenNow(false);
            }
        }

        void LateUpdate()
        {
            if (drawer == null) return;
            bool open = drawer.FullyOpen;
            if (EventView != null) EventView.DoorOpen = open;
            if (RewardView != null) RewardView.DoorOpen = open;
            bool outgoing = Outgoing, incoming = Incoming;
            if (outgoing) drawer.SetLamp(PassDrawer.Lamp.Green, true);
            else if (incoming) drawer.SetLamp(PassDrawer.Lamp.Amber, !drawer.IsOpen);
            else drawer.SetLamp(PassDrawer.Lamp.Off, false);
            if (drawer.DrawerTarget != null)
            {
                drawer.DrawerTarget.TitleOverride = outgoing ? DrawerOutgoingTitle : incoming ? DrawerIncomingTitle : DrawerTitle;
                drawer.DrawerTarget.LineOverride = Loc.T(outgoing ? DrawerOutgoingLine : incoming ? drawer.IsOpen ? DrawerOpenLine : DrawerIncomingLine : DrawerIdleLine);
            }
            if (drawer.SwitchTarget != null)
            {
                drawer.SwitchTarget.TitleOverride = SwitchTitle;
                drawer.SwitchTarget.LineOverride = Loc.T(outgoing ? SwitchReadyLine : SwitchIdleLine);
            }
        }

        void Play(PassDrawerSound sound) => Sfx.Play(CueOf(sound));

        /// <summary>Çekmecenin ses olayı → var olan cue (yeni klip yok).</summary>
        public static Cue CueOf(PassDrawerSound sound) => sound switch
        {
            PassDrawerSound.Open => Cue.AdmissionDrawer,
            PassDrawerSound.Close => Cue.AdmissionDrawer,
            PassDrawerSound.Switch => Cue.SwitchClack,
            PassDrawerSound.Deny => Cue.BoardDeny,
            _ => throw new ArgumentOutOfRangeException(nameof(sound), sound, null),
        };
    }
}
