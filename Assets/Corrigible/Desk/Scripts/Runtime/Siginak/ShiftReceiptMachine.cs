using System;
using Corrigible.Core;
using TMPro;
using UnityEngine;

namespace Corrigible.Desk
{
    /// <summary>Vardiya şalterinin fiş çıkışı ve fiziksel EVRAK tepsisi; simülasyona bağlayıcı karar verir. Durum satırı şalterin
    /// pirinç plakasının ikinci satırıdır. Revizyon 3: fiş ağzının amber lambası fiş masadayken yanar.
    /// <para>
    /// Revizyon 3 · R19 (PLAN §1.5, §1.7): basılan fiş kol aşağıya varınca çıkar (<c>ShiftSwitch.LeverArrived(true)</c>; olay
    /// kaçarsa <c>Update</c> <c>ShiftSwitch.LeverDown</c>'ı yoklar). Lamba fişin durumundan türetilir: besleme başında yanar, dilde,
    /// elde, sürüklenirken ve dile dönerken yanık, kabul edilen bırakışta söner, <see cref="Reject"/>'te yeniden yanar. Sürüklenen fiş
    /// EVRAK üstündeyken tepsinin hedef ışığı yanar (ölçüt bırakılan ekran noktasının ışını). Bırakışın kabulüne <see cref="CanFile"/>
    /// karar verir; reddedilen her bırakış fişi dile döndürür (bulgu 17). Kâğıdın tık ve ses olaylarını iletir. Plakayı R19 kipinde
    /// <see cref="ShiftPlate"/> yazar (<c>status</c> boş; yalnız eski yol).
    /// </para>
    /// <para>
    /// Kâğıt ve şalter olaylarına bağlanma tembeldir (<c>OnEnable</c> ya da ilk <c>Present</c>/<c>Configure</c>): EditMode'da
    /// <c>OnEnable</c> çalışmaz.
    /// </para>
    /// </summary>
    public sealed class ShiftReceiptMachine : MonoBehaviour
    {
        [SerializeField] ShiftSwitch shiftSwitch;
        [SerializeField] ShiftReceiptPaper paper;
        [SerializeField] EvrakTray tray;
        [SerializeField] Camera view;
        [SerializeField] Transform output;
        [SerializeField] TMP_Text status;
        [Tooltip("Fiş ağzının lambası (Revizyon 3 SlipMouth/Lamp; swap 0 sönük, 1 yanık). Boşsa lamba yok.")]
        [SerializeField] PartDriver slipLamp;

        [NonSerialized] ShiftReceiptPaper wiredPaper;
        [NonSerialized] ShiftSwitch wiredSwitch;
        bool printedWithoutPaper;

        public ShiftReceiptPaper Paper => paper;
        public EvrakTray Tray => tray;
        /// <summary>EVRAK'a kabul edilen bırakış (fiş tepsiye inmeye başladı, lamba söndü): fişin kimliği.</summary>
        public event Action<string> ReceiptFiled;
        public event Action<bool> DragChanged;
        /// <summary>R19: fişe sürüklemesiz tık (kâğıdın <c>Clicked</c>'ı).</summary>
        public event Action Clicked;
        /// <summary>R19: kâğıdın ses olayları (köprü <c>Sfx</c>'e çevirir).</summary>
        public event Action<ShiftSound> Sound;

        /// <summary>
        /// R19: EVRAK üstüne bırakılan fişin kimliği dosyalanabilir mi (bağlayıcının korumaları: dosyalama sürmüyor, kimlik güncel,
        /// masa kilitli değil, pano var). false ise fiş dile döner. Boşsa her bırakış kabul.
        /// </summary>
        public Func<string, bool> CanFile { get; set; }

        /// <summary>R19: fiş ağzının lambası yanık.</summary>
        public bool LampLit { get; private set; }

        public void Configure(ShiftSwitch button, ShiftReceiptPaper sheet, EvrakTray inbox, Camera camera, Transform origin, TMP_Text label)
        {
            shiftSwitch = button; paper = sheet; tray = inbox; view = camera; output = origin; status = label;
            Wire();
        }

        /// <summary>R19: fiş ağzının lambası (tek seferlik sahne script'i ve testler).</summary>
        public void ConfigureLamp(PartDriver lamp)
        {
            slipLamp = lamp;
            ApplyLamp();
        }

        void OnEnable() { Wire(); ApplyLamp(); }

        void OnDisable()
        {
            Unwire();
            if (tray) tray.SetReceiptTarget(false);
        }

        void Update() => PollLever();

        // ------------------------------------------------------------------ olay bağları

        void Wire()
        {
            if (wiredPaper != paper)
            {
                UnwirePaper();
                wiredPaper = paper;
                if (paper)
                {
                    paper.Released += Drop;
                    paper.DragChanged += Drag;
                    paper.Dragged += Dragging;
                    paper.Clicked += PaperClicked;
                    paper.Sound += PaperSound;
                    paper.StateChanged += PaperMoved;
                }
            }
            if (wiredSwitch != shiftSwitch)
            {
                UnwireSwitch();
                wiredSwitch = shiftSwitch;
                if (shiftSwitch) shiftSwitch.LeverArrived += LeverArrived;
            }
        }

        void Unwire() { UnwirePaper(); UnwireSwitch(); }

        void UnwirePaper()
        {
            if (wiredPaper)
            {
                wiredPaper.Released -= Drop;
                wiredPaper.DragChanged -= Drag;
                wiredPaper.Dragged -= Dragging;
                wiredPaper.Clicked -= PaperClicked;
                wiredPaper.Sound -= PaperSound;
                wiredPaper.StateChanged -= PaperMoved;
            }
            wiredPaper = null;
        }

        void UnwireSwitch()
        {
            if (wiredSwitch) wiredSwitch.LeverArrived -= LeverArrived;
            wiredSwitch = null;
        }

        Camera Cam => view != null ? view : paper != null ? paper.View : null;

        void Drag(bool on)
        {
            if (!on && tray) tray.SetReceiptTarget(false);
            DragChanged?.Invoke(on);
        }

        void Dragging(Vector2 screen)
        {
            if (!tray) return;
            tray.SetReceiptTarget(paper != null && !paper.ReadOnly && tray.ContainsReceiptDrop(screen, Cam));
        }

        void PaperClicked() => Clicked?.Invoke();
        void PaperSound(ShiftSound s) => Sound?.Invoke(s);
        void PaperMoved(ShiftReceiptPaper.Where where) => ApplyLamp();

        void LeverArrived(bool down)
        {
            if (down && paper && paper.FeedPending) paper.BeginFeed();
        }

        /// <summary>Bekleyen besleme kol aşağıdayken başlar (olay kaçtıysa ya da kol zaten inikse; şalter yoksa hemen).</summary>
        void PollLever()
        {
            if (paper && paper.FeedPending && (shiftSwitch == null || shiftSwitch.LeverDown)) paper.BeginFeed();
        }

        /// <summary>
        /// Bırakış (bulgu 17): EVRAK üstünde ve <see cref="CanFile"/> izin veriyorsa fiş tepsiye iner, lamba söner,
        /// <see cref="ReceiptFiled"/> çıkar; değilse fiş dile döner. Hedef ışığı her durumda söner.
        /// </summary>
        void Drop(Vector2 screen)
        {
            if (tray) tray.SetReceiptTarget(false);
            if (paper == null || paper.ReadOnly || string.IsNullOrEmpty(paper.ReceiptId)) return;
            string id = paper.ReceiptId;
            bool over = tray != null && tray.ContainsReceiptDrop(screen, Cam);
            if (over && (CanFile == null || CanFile(id)))
            {
                paper.FileToTray();
                ApplyLamp();
                ReceiptFiled?.Invoke(id);
                return;
            }
            paper.ReturnToTongue();
            ApplyLamp();
        }

        void ApplyLamp()
        {
            bool lit = paper ? paper.LiveSlipOut : printedWithoutPaper;
            LampLit = lit;
            if (slipLamp) slipLamp.SetSwap(lit ? 1 : 0);
        }

        // ------------------------------------------------------------------ sunum

        /// <summary>Eski yol: başlık ve gövde metniyle (plaka satırını <c>status</c> yazar).</summary>
        public void Present(string id, string title, string text)
        {
            Wire();
            bool printed = !string.IsNullOrEmpty(id);
            printedWithoutPaper = printed;
            if (shiftSwitch) shiftSwitch.SetAwaitingReceipt(printed);
            if (paper) paper.Present(id, title, text);
            if (status) status.text = printed ? Loc.T("fişi EVRAK’a koy") : Loc.T("kapağı kaldır");
            PollLever();
            ApplyLamp();
        }

        /// <summary>
        /// R19: devir fişini gösterir. <paramref name="feed"/> true: fiş kol aşağıya varınca ağızdan çıkar (basım anı);
        /// false: dilde anında, şalter fiş bekliyor duruşuna anında oturur (kayıttan yükleme, dil değişimi). Aynı kimlikli fiş kol
        /// inişini bekliyorsa <paramref name="feed"/> false onu bozmaz (bekleme sürer, şalter anında oturmaz). Boş kimlik fişi gizler.
        /// </summary>
        public void Present(string id, HandoverSlipFacts facts, bool feed)
        {
            Wire();
            bool printed = !string.IsNullOrEmpty(id);
            printedWithoutPaper = printed;
            bool pendingSame = printed && paper && paper.FeedPending && paper.ReceiptId == id;
            if (shiftSwitch) shiftSwitch.SetAwaitingReceipt(printed, printed && !feed && !pendingSame);
            if (paper) paper.Present(id, facts, feed ? ShiftReceiptPaper.Entry.AfterLever : ShiftReceiptPaper.Entry.Instant);
            PollLever();
            ApplyLamp();
        }

        /// <summary>R19: dosyalama Core'da reddedildi; fiş tepsiden (ya da bulunduğu yerden) dile döner, ağız lambası yeniden yanar.</summary>
        public void Reject()
        {
            Wire();
            if (tray) tray.SetReceiptTarget(false);
            if (paper) paper.ReturnToTongue();
            ApplyLamp();
        }
    }
}
