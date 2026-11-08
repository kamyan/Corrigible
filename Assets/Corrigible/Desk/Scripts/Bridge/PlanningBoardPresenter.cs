using System;
using System.Collections.Generic;
using System.Linq;
using Corrigible.Core;
using Corrigible.Game;
using Corrigible.Game.Audio;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Corrigible.Desk.Bridge
{
    /// <summary>
    /// Kartlı panonun komut kapısı (Revizyon 3 · Aşama 3, R15/R16). Simülasyondan anlık görüntü kurar
    /// (<see cref="PlanningBoardSnapshotBuilder"/>) ve 3B görünüme (<see cref="BoardView3D"/>) çizdirir; sürükleme denetleyicisinin
    /// (<see cref="BoardDragController"/>) bırakma, tıklama ve geri isteklerini tek Core komutuna çevirir
    /// (<see cref="BoardDropRules"/> → <see cref="GameController.ExecuteBoardCommand"/>). Çizim yalnız değişimde olur (oyun
    /// <c>Changed</c>, yükleme, yeni oyun, dil, poz, sürükleme sonu), her karede değil. Kartın görevi ve sahipliği Core durumundan
    /// okunur; seçim, eğitim konusu ve fiş yerleri sunumdur (<see cref="BoardUiState"/>, <c>planning_board_layout.json</c>).
    /// Aşama 6 (R17, D20): kapının eski uGUI kabul penceresi kalktı; kabul kapıdaki 3B istasyondadır (<see cref="SiginakAdmission"/>).
    /// <see cref="ShowAdmissions"/> ve <see cref="CloseAdmissions"/> genel API olarak kalır ve oraya gider. Dosyanın İNCELE sekmesi,
    /// kapıda bekleyen bir dış kayıt talebini iptal edecekse önce uyarır; ikinci tık komutu yazar (H2/H4, D3-a).
    /// <para>
    /// Aşama 5 (R18; PLAN §1.3, §1.12, D1 = B): 3B tarif defteri (<see cref="RecipeBookView"/>, <see cref="BoardView3D.Book"/>)
    /// her çizimde anlık görüntüden bağlanır; defter açıkken pano "defter kipi"ndedir (kart ve nesne girdisi kapalı, pano tıkı defteri
    /// kapatır). Defterden alınan tarif kopyası iş emrine <c>board_queue</c> (dolu emre <c>board_cancel</c> + <c>board_queue</c>),
    /// hammadde fişi <c>board_reserve ±1</c>, form dışına bırakılan tarif kartı <c>board_cancel</c> olur. Defterin açılımı, tarif
    /// kartlarının yeri ve yeni ürünün kayış/YENİ durumu sunumdur (yerleşim dosyası); kayda girmez. Eski (A dönemi) kayıtlarda
    /// konmuş fişler bir kez <c>board_stage(tür, 0)</c> ile stoka döner (D2).
    /// </para>
    /// </summary>
    public sealed class PlanningBoardPresenter : MonoBehaviour
    {
        [SerializeField] GameController game;
        [SerializeField] LiberationDeskView surface;
        [SerializeField] SiginakCameraRig rig;
        [Tooltip("Eski görünüm (Aşama 6'dan beri kabul penceresi yok; yalnız eski bağlayıcıların durum satırı).")]
        [SerializeField] PlanningBoardView view;
        [Tooltip("Pano erişimi (K1'den panoya tıklama); satırı plan durumunu yazar.")]
        [SerializeField] DeskTarget target;

        [Header("Revizyon 3 · kartlı pano")]
        [SerializeField] BoardView3D board3D;
        [SerializeField] BoardDragController drag;
        [Tooltip("MASAYA DÖN levhası (BoardView3D.backTarget ile aynı DeskTarget): tıklama geri adımıdır.")]
        [SerializeField] DeskTarget backSign;
        [Tooltip("Boşta destesinin tepsisi (isteğe bağlı): tıklama desteyi açar ya da toplar.")]
        [SerializeField] DeskTarget deckTray;
        [Tooltip("Hurda kutusunun ön yüzü (isteğe bağlı): tıklama seçili eşyayı hurdaya; satırı hurda önizlemesini yazar.")]
        [SerializeField] DeskTarget hurdaFront;
        [Tooltip("Kapı ve kabul istasyonu (Aşama 6): ShowAdmissions / CloseAdmissions buraya gider.")]
        [SerializeField] SiginakAdmission admission;

        readonly BoardUiState ui = new BoardUiState();
        PlanningBoardLayout layout;
        bool shownInputBlocked, shownStoryHold, renderAfterDrag, enterHeld;
        /// <summary>İNCELE sekmesinin uyarısı bu kişi için gösterildi (ikinci tık komutu yazar).</summary>
        string reviewWarnedFor;

        // Aşama 5: defter kipi (gösterilen durum) ve eski fişlerin serbest bırakılması
        bool bookModeShown, legacyReleaseBlocked;

        /// <summary>Bir bırakmanın sunucudaki sonucu: kabul (gerekirse yeniden çizim) ya da ret (Core'un iletisi).</summary>
        public readonly struct DropOutcome
        {
            public readonly bool Accepted, Rerender;
            public readonly string Reason;

            DropOutcome(bool accepted, bool rerender, string reason) { Accepted = accepted; Rerender = rerender; Reason = reason; }

            public static DropOutcome Accept(bool rerender) => new DropOutcome(true, rerender, null);
            public static DropOutcome Refuse(string reason) => new DropOutcome(false, false, reason);
        }

        Simulation Sim => game != null ? game.Sim : null;
        /// <summary>Kancadaki 3B tarif defteri (pano görünümünün bağı); yoksa null.</summary>
        RecipeBookView Book => board3D != null ? board3D.Book : null;
        /// <summary>Pano açık (K4). Aşama 6'dan beri kapının kabulü panonun parçası değil.</summary>
        public bool IsOpen => rig != null && rig.Current == SiginakPose.Shelf;
        public PlanningBoardView View => view;
        /// <summary>3B pano görünümü (testler ve sahne bağlayıcısı için).</summary>
        public BoardView3D Board3D => board3D;
        /// <summary>Sunum durumu: seçim, halkalı eğitim konusu, fiş yerleri.</summary>
        public BoardUiState Ui => ui;
        /// <summary>Seçili kişinin kimliği; seçim yoksa ya da seçili kart eşyaysa null.</summary>
        public string SelectedPersonId => Sim?.Board?.Person(ui.SelectedId) != null ? ui.SelectedId : null;
        bool InputBlocked => surface != null && surface.InputBlocked;
        bool StoryHold => game != null && game.StoryHold;
        static bool English => DeskLoc.Instance.Lang == DeskLang.En;

        public void Configure(GameController controller, LiberationDeskView desk, SiginakCameraRig cameraRig, PlanningBoardView board, DeskTarget deskTarget)
        { game = controller; surface = desk; rig = cameraRig; view = board; target = deskTarget; }

        /// <summary>3B pano bağları (sahne bağlayıcısı <c>[SerializeField]</c> ile kurar; bu yol testler içindir, OnEnable'dan önce çağrılır).</summary>
        public void ConfigureBoard(BoardView3D board, BoardDragController dragController, DeskTarget backSignTarget = null, DeskTarget deckTrayTarget = null, DeskTarget hurdaTarget = null)
        { board3D = board; drag = dragController; backSign = backSignTarget; deckTray = deckTrayTarget; hurdaFront = hurdaTarget; }

        // ------------------------------------------------------------------ yaşam döngüsü

        void OnEnable()
        {
            if (game != null) { game.Changed += Refresh; game.NewGameStarted += NewLayout; game.GameLoaded += LoadLayout; }
            DeskLoc.Instance.Changed += Relabel;
            if (rig != null) { rig.PoseChanged += PoseChanged; rig.Arrived += RigArrived; }
            WireBoard(true);
            if (view != null) view.SetCamera(rig != null ? rig.View : null);
            LoadLayout();
            UpdateInteractivity();
        }

        void OnDisable()
        {
            if (game != null) { game.Changed -= Refresh; game.NewGameStarted -= NewLayout; game.GameLoaded -= LoadLayout; }
            DeskLoc.Instance.Changed -= Relabel;
            if (rig != null) { rig.PoseChanged -= PoseChanged; rig.Arrived -= RigArrived; rig.InputSuspended = false; }
            WireBoard(false);
            if (enterHeld && board3D != null && board3D.Folder != null) board3D.Folder.EndDismissHold();
            enterHeld = renderAfterDrag = false;
            if (bookModeShown && board3D != null) board3D.SetBookMode(false);
            bookModeShown = false;
        }

        /// <summary>
        /// Görünüm, denetleyici ve sabit hedeflerin olaylarına bağlanır (ya da çözülür). Defterin ve deponun sesleri pano görünümünün
        /// <see cref="BoardView3D.Sound"/>'undan gelir (görünüm toplar; PLAN §1.1), ayrıca dinlenmez: ses iki kez çalmasın.
        /// </summary>
        void WireBoard(bool on)
        {
            if (board3D != null)
            {
                if (on)
                {
                    board3D.Sound += PlaySound;
                    board3D.ChipPose = ui.ChipPose;
                    board3D.RecipePose = ui.RecipePose;
                    board3D.NewItemPending = NewItemPending;
                    board3D.NewItemTagged = NewItemTagged;
                    board3D.NewItemSlid += NewItemSlid;
                    board3D.NewItemTouched += NewItemTouched;
                }
                else
                {
                    board3D.Sound -= PlaySound;
                    board3D.NewItemSlid -= NewItemSlid;
                    board3D.NewItemTouched -= NewItemTouched;
                }
                var book = board3D.Book;
                if (book != null)
                {
                    if (on) { book.RecipeLifted += RecipeLifted; book.SpreadChanged += SpreadChanged; book.Clicked += BookClicked; }
                    else { book.RecipeLifted -= RecipeLifted; book.SpreadChanged -= SpreadChanged; book.Clicked -= BookClicked; }
                }
                var folder = board3D.Folder;
                if (folder != null)
                {
                    if (on) { folder.Clicked += OpenFolder; folder.Review += ReviewFolder; folder.Access += AccessFolder; folder.Dismiss += DismissFolder; }
                    else { folder.Clicked -= OpenFolder; folder.Review -= ReviewFolder; folder.Access -= AccessFolder; folder.Dismiss -= DismissFolder; }
                }
                var topics = board3D.Topics;
                if (topics != null) { if (on) topics.RowClicked += TopicRowClicked; else topics.RowClicked -= TopicRowClicked; }
                var lost = board3D.LostEnvelope;
                if (lost != null) { if (on) lost.Clicked += OpenLost; else lost.Clicked -= OpenLost; }
            }
            if (drag != null)
            {
                if (on)
                {
                    drag.CanDrop = CanDrop;
                    drag.Preview = PreviewDrop;
                    drag.CanPick = PickCheck;
                    drag.DropRequested += DropRequested;
                    drag.CardClicked += CardClicked;
                    drag.CardDoubleClicked += CardDoubleClicked;
                    drag.TargetClicked += TargetClicked;
                    drag.EmptyClicked += EmptyClicked;
                    drag.DragChanged += DragChanged;
                    drag.Sound += PlaySound;
                    drag.BackgroundClicked += BookBackgroundClicked;
                }
                else
                {
                    drag.CanDrop = null;
                    drag.Preview = null;
                    drag.CanPick = null;
                    drag.DropRequested -= DropRequested;
                    drag.CardClicked -= CardClicked;
                    drag.CardDoubleClicked -= CardDoubleClicked;
                    drag.TargetClicked -= TargetClicked;
                    drag.EmptyClicked -= EmptyClicked;
                    drag.DragChanged -= DragChanged;
                    drag.Sound -= PlaySound;
                    drag.BackgroundClicked -= BookBackgroundClicked;
                    drag.Suspended = false;
                }
            }
            if (backSign != null) { if (on) backSign.Clicked += BackSignClicked; else backSign.Clicked -= BackSignClicked; }
            if (deckTray != null) { if (on) deckTray.Clicked += DeckTrayClicked; else deckTray.Clicked -= DeckTrayClicked; }
            if (hurdaFront != null) { if (on) hurdaFront.Clicked += HurdaClicked; else hurdaFront.Clicked -= HurdaClicked; }
            // Kart tutulurken panonun sabit hedefleri üzerine gelmez: ortak HoverTag'i bırakma önizlemesinden almasın, tabela kaymasın
            foreach (var t in new[] { backSign, deckTray, hurdaFront })
                if (t != null) t.BlockedOverride = on ? HeldBlocked : (Func<bool>)null;
        }

        /// <summary>Odanın girdi kilidi ya da tutulan (cevap bekleyen) kart: sabit pano hedeflerinin kendi kilidi.</summary>
        bool HeldBlocked() => DeskTarget.Blocked != null && DeskTarget.Blocked() || drag != null && drag.Held != null;

        void Update()
        {
            BookKeys();

            // Klavyeyle TAHLİYE: dosya açıkken Enter 1,2 s basılı tutulur (Tab/Enter/↑↓ panoda BoardDragController'dadır).
            var folder = board3D != null ? board3D.Folder : null;
            if (folder == null || !folder.IsOpen || !board3D.Interactive)
            {
                if (enterHeld && folder != null) folder.EndDismissHold();
                enterHeld = false;
                return;
            }
            var kb = Keyboard.current;
            if (kb == null || TextFieldSelected() || DeskTarget.Blocked != null && DeskTarget.Blocked()) return;
            if (!enterHeld && (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame)) { enterHeld = true; folder.BeginDismissHold(); }
            else if (enterHeld && !kb.enterKey.isPressed && !kb.numpadEnterKey.isPressed) { enterHeld = false; folder.EndDismissHold(); }
        }

        /// <summary>Açık defterde ← / → sayfa çevirir (B11); çevirme sürerken basış yok sayılır (B4, defterin kuralı).</summary>
        void BookKeys()
        {
            var book = Book;
            if (book == null || book.State != BookState.Open || board3D == null || !board3D.Interactive) return;
            var kb = Keyboard.current;
            if (kb == null || TextFieldSelected() || DeskTarget.Blocked != null && DeskTarget.Blocked()) return;
            if (kb.leftArrowKey.wasPressedThisFrame) book.Turn(-1);
            else if (kb.rightArrowKey.wasPressedThisFrame) book.Turn(1);
        }

        void LateUpdate()
        {
            RetireJobs();
            ReleaseLegacyStaging();
            SyncBookMode();
            if (board3D == null || Sim?.Board == null) return;
            // Olayı olmayan iki kapı (girdi kilidi, hikâye) değişince bir kez yeniden çizilir; sürükleme bitince bir kez.
            bool stale = InputBlocked != shownInputBlocked || StoryHold != shownStoryHold;
            bool dragDone = renderAfterDrag && (drag == null || !drag.Busy);
            if (dragDone) renderAfterDrag = false;
            if (stale || dragDone) RenderBoard(true);
        }

        static bool TextFieldSelected()
        {
            var es = EventSystem.current;
            return es != null && es.currentSelectedGameObject != null && es.currentSelectedGameObject.GetComponent<TMP_InputField>() != null;
        }

        // ------------------------------------------------------------------ yerleşim dosyası

        static PlanningBoardLayout NewLayoutFor(GameRecord record) =>
            new PlanningBoardLayout { Version = PlanningBoardLayoutStore.CurrentVersion, Seed = record?.seed ?? 0 };

        void NewLayout()
        {
            reviewWarnedFor = null;
            legacyReleaseBlocked = false;
            ui.Reset();
            layout = NewLayoutFor(Sim?.Record);
            ResetBook();
            RefreshBoard(false);
        }

        void LoadLayout()
        {
            reviewWarnedFor = null;
            legacyReleaseBlocked = false;
            layout = PlanningBoardLayoutStore.Load(Sim?.Record) ?? NewLayoutFor(Sim?.Record);
            PlanningBoardLayoutStore.Apply(layout, ui);
            ResetBook();
            RefreshBoard(false);
        }

        /// <summary>Oda açılışı, yeni oyun ve kayıttan devam: defter kancada kapalı, son açılımı hatırlar (açılınca oradan başlar).</summary>
        void ResetBook()
        {
            var book = Book;
            if (book == null) return;
            book.Close(false);
            book.SetSpread(Mathf.Max(0, ui.BookSpread));
            SyncBookMode();
        }

        void SaveLayout()
        {
            var record = Sim?.Record;
            if (record == null) return;
            if (layout == null || layout.Seed != record.seed) layout = NewLayoutFor(record);
            PlanningBoardLayoutStore.Capture(ui, layout);
            PlanningBoardLayoutStore.Save(record, layout);
        }

        // ------------------------------------------------------------------ çizim

        /// <summary>Panoyu yeniden kurar (oyun <c>Changed</c>; yer değiştiren kartlar süresiyle hareket eder).</summary>
        public void Refresh() => RefreshBoard(true);

        void RefreshBoard(bool animate)
        {
            if (Sim?.Board == null) return;
            RenderBoard(animate);
        }

        void Relabel() => RefreshBoard(false);

        /// <summary>
        /// Anlık görüntüyü kurar ve çizer; düzenlenebilirliği denetleyiciye, plan durumunu Pano erişimine, hurda önizlemesini hurda
        /// hedefine, tarifleri deftere yazar (form, fişler ve yeni ürün görünümün çiziminde). Saf okuma: komut yazmaz, RNG çekmez.
        /// Önce sunum durumu simülasyona uyar (<see cref="PlanningBoardSnapshotBuilder.SyncUi"/>: yeni günün ürünü bir kez
        /// sabitlenir, biten ya da iptal edilen emirlerin tarif yerleri ve artık çizilmeyen fişlerin yerleri düşer; değiştiyse dosyaya).
        /// </summary>
        void RenderBoard(bool animate)
        {
            var sim = Sim;
            if (sim?.Board == null) return;
            if (PlanningBoardSnapshotBuilder.SyncUi(sim, ui)) SaveLayout();
            if (board3D == null) return;
            PruneSelection(sim);
            bool blocked = InputBlocked, hold = StoryHold;
            var s = PlanningBoardSnapshotBuilder.Build(sim, ui, English, blocked, hold);
            shownInputBlocked = blocked;
            shownStoryHold = hold;
            if (drag != null) drag.Editable = s.Editable;
            board3D.Render(s, animate);
            var book = Book;
            if (book != null)
            {
                book.Locked = s.Locked;
                book.RecipesDraggable = s.RecipesDraggable;
                book.Bind(s.Recipes, s.Book, s.BookTitle, s.BookLine);
            }
            if (target != null) target.LineOverride = s.EntryLine;
            if (hurdaFront != null) hurdaFront.LineOverride = s.HurdaLine;
        }

        /// <summary>Seçili kart artık yoksa (tahliye, hurda, kayıttan başka oyun) seçim düşer.</summary>
        void PruneSelection(Simulation sim)
        {
            string id = ui.SelectedId;
            if (string.IsNullOrEmpty(id)) { ui.SelectedId = null; return; }
            var board = sim.Board;
            if (board.Person(id) == null && board.EquipmentItem(id) == null) ui.SelectedId = null;
        }

        /// <summary>
        /// Yeni ürünün (R18d; PLAN §1.11) kayışı henüz oynamadı mı (görünüm ilk K4 varışında sorar). Durum yerleşim dosyasındadır:
        /// kayıttan devamda oynamış kayış yeniden oynamaz; yeni gün ya da yeni kimlik sıfırdan başlar.
        /// </summary>
        bool NewItemPending(NewItemData d)
        {
            if (d == null) return false;
            var state = ui.NewItem;
            return !(state != null && state.Day == d.Day && state.Id == d.ItemId && state.Slid);
        }

        /// <summary>YENİ levhası hâlâ asılı mı: bu ürünün kartına henüz dokunulmadı.</summary>
        bool NewItemTagged(NewItemData d)
        {
            if (d == null) return false;
            var state = ui.NewItem;
            return !(state != null && state.Day == d.Day && state.Id == d.ItemId && state.Touched);
        }

        NewItemState NewItemStateFor(NewItemData d)
        {
            var state = ui.NewItem;
            if (state == null || state.Day != d.Day || state.Id != d.ItemId) ui.NewItem = state = new NewItemState { Day = d.Day, Id = d.ItemId };
            return state;
        }

        /// <summary>Kayış bitti: bir daha oynamaz (yerleşim dosyası).</summary>
        void NewItemSlid(NewItemData d)
        {
            if (d == null || string.IsNullOrEmpty(d.ItemId)) return;
            var state = NewItemStateFor(d);
            if (state.Slid) return;
            state.Slid = true;
            SaveLayout();
        }

        /// <summary>Yeni ürün kartına ilk dokunuş (üzerine gelme ya da tık): YENİ etiketi düşer; komut yok.</summary>
        void NewItemTouched(NewItemData d)
        {
            if (d == null || string.IsNullOrEmpty(d.ItemId)) return;
            var state = NewItemStateFor(d);
            if (state.Touched) return;
            state.Touched = true;
            state.Slid = true;   // dokunulabilen kart yığında durur
            SaveLayout();
        }

        /// <summary>
        /// K4'e varınca (geçiş bitmiş) kart ve hedef girdisi açılır, Pano erişimi kapanır. Defterin çarpıştırıcıları da yalnız bu
        /// durumda açıktır; K4'ten çıkışta açık defter animasyonla kapanır (B6).
        /// </summary>
        void UpdateInteractivity()
        {
            bool on = rig != null && rig.Current == SiginakPose.Shelf && !rig.InTransition && Sim?.Board != null;
            if (board3D != null) board3D.SetInteractive(on);
            var book = Book;
            if (book != null)
            {
                book.SetInteractive(on);
                if (!on && BookHoldsBoard(book.State)) book.Close();
            }
            SyncBookMode();
        }

        // ------------------------------------------------------------------ tarif defteri (Aşama 5 · R18a–b)

        /// <summary>Defter panonun girdisini tutuyor mu (defter kipi, PLAN §1.12): açılırken, açıkken, çevrilirken, tarif tutulurken.</summary>
        public static bool BookHoldsBoard(BookState state) =>
            state == BookState.Opening || state == BookState.Open || state == BookState.Turning || state == BookState.Lowered;

        /// <summary>
        /// Kart girdisi askıda mı (<see cref="BoardDragController.Suspended"/>): defter kipinde, yalnız tarif kopyası tutulurken
        /// (<see cref="BookState.Lowered"/>) değil (denetçi kopyayı sürer).
        /// </summary>
        public static bool BookSuspendsDrag(BookState state) => BookHoldsBoard(state) && state != BookState.Lowered;

        /// <summary>Defterin durumunu pano görünümüne (defter kipi) ve sürükleme denetçisine (askı) yansıtır; yalnız değişince.</summary>
        void SyncBookMode()
        {
            var book = Book;
            var state = book != null ? book.State : BookState.Closed;
            bool mode = BookHoldsBoard(state), suspend = BookSuspendsDrag(state);
            if (mode != bookModeShown)
            {
                bookModeShown = mode;
                if (board3D != null) board3D.SetBookMode(mode);
            }
            if (drag != null && drag.Suspended != suspend) drag.Suspended = suspend;
        }

        /// <summary>Kapalı deftere tık: defter kendisi öne gelir; açık dosya, KAYIPLAR ve deste kapanır, seçim düşer.</summary>
        void BookClicked()
        {
            if (board3D != null)
            {
                if (board3D.Folder != null && board3D.Folder.IsOpen) board3D.Folder.Close();
                if (board3D.LostEnvelope != null && board3D.LostEnvelope.IsOpen) board3D.LostEnvelope.Close();
                if (board3D.Deck != null && board3D.Deck.IsOpen) board3D.Deck.Close(true);
            }
            SyncBookMode();
            if (ui.SelectedId == null) return;
            ui.SelectedId = null;
            RenderBoard(true);
        }

        /// <summary>Defter kipinde pano tıkı (kilitli planda da): defter kapanır, tık tüketilir (seçim değişmez, komut yok).</summary>
        void BookBackgroundClicked()
        {
            var book = Book;
            if (book != null && BookHoldsBoard(book.State)) book.Close();
            SyncBookMode();
        }

        /// <summary>Açılım değişti: yerleşim dosyasına (defter açılınca oradan başlar).</summary>
        void SpreadChanged(int spread)
        {
            if (spread < 0 || ui.BookSpread == spread) return;
            ui.BookSpread = spread;
            SaveLayout();
        }

        /// <summary>
        /// [B] Tarif kutusundan kopya kalktı (B7): havuzdaki kopya kart sayfadaki kutunun ekrandaki yerinde, pano düzleminde belirir,
        /// defter alçalır ve denetçi kartı benimser. Kabulde defter kancaya kapanır (B8); retde ya da Esc'te kart sayfaya döner,
        /// defter öne gelir (B9).
        /// </summary>
        void RecipeLifted(string recipeId, PointerEventData e, Pose pagePose)
        {
            var book = Book;
            var snap = board3D != null ? board3D.Last : null;
            if (book == null || drag == null || snap == null) return;
            var recipe = snap.Recipes.FirstOrDefault(r => r != null && r.Id == recipeId);
            var copy = recipe != null && snap.RecipesDraggable ? board3D.AcquireRecipeCopy(recipe) : null;
            if (copy == null) { if (book.State == BookState.Lowered) book.Lower(false); SyncBookMode(); return; }
            PlaceCopy(copy, pagePose);
            int side = recipe.Index % 2;
            book.Lower(true);
            SyncBookMode();
            drag.Suspended = false;
            drag.Adopt(copy, e, () => book.RecipeBoxPose(side), accepted => RecipeCopyFinished(copy, accepted));
        }

        void RecipeCopyFinished(BoardCardView copy, bool accepted)
        {
            var book = Book;
            if (accepted)
            {
                // Kopya formun tarif kartı olur (görünüm sonraki çizimde benimser); defter kancaya
                if (book != null && BookHoldsBoard(book.State)) book.Close();
            }
            else
            {
                if (book != null && book.State == BookState.Lowered) book.Lower(false);
                if (board3D != null) board3D.ReleaseRecipeCopy(copy);
            }
            SyncBookMode();
        }

        /// <summary>Kopyayı, sayfadaki kutunun kameradan görünen yerine, pano düzleminde koyar (kart düzeyi 0).</summary>
        void PlaceCopy(BoardCardView copy, Pose pagePose)
        {
            var parent = copy.transform.parent;
            float planeZ = BoardLayout.CardZ(0);
            Vector3 local;
            if (parent == null) local = new Vector3(pagePose.position.x, pagePose.position.y, planeZ);
            else
            {
                var page = parent.InverseTransformPoint(pagePose.position);
                var cam = rig != null ? rig.View : null;
                local = cam != null ? ProjectToPlane(parent.InverseTransformPoint(cam.transform.position), page, planeZ) : new Vector3(page.x, page.y, planeZ);
            }
            copy.SetVisible(true);
            copy.Place(local, 0f);
        }

        /// <summary>
        /// Göz noktasından <paramref name="point"/>'e giden ışının yerel z = <paramref name="planeZ"/> düzlemini kestiği nokta (saf);
        /// ışın düzleme paralelse noktanın düzlemdeki dik izdüşümü.
        /// </summary>
        public static Vector3 ProjectToPlane(Vector3 eye, Vector3 point, float planeZ)
        {
            var d = point - eye;
            if (Mathf.Abs(d.z) < 1e-6f) return new Vector3(point.x, point.y, planeZ);
            float t = (planeZ - eye.z) / d.z;
            var p = eye + d * t;
            p.z = planeZ;
            return p;
        }

        void PoseChanged(SiginakPose pose)
        {
            if (pose != SiginakPose.Shelf) { ui.SelectedId = null; reviewWarnedFor = null; }
            UpdateInteractivity();
            Refresh();
        }

        void RigArrived(SiginakPose pose) => UpdateInteractivity();

        // ------------------------------------------------------------------ giriş ve geri

        public void ShowPerson(string id)
        {
            if (Sim?.Board?.Person(id) == null || surface == null || surface.InputBlocked) return;
            Show(); ui.SelectedId = id; RenderBoard(true); SaveLayout();
        }

        public void Show() { if (surface != null && !surface.InputBlocked) { rig.FocusShelter(); Refresh(); } }

        /// <summary>
        /// Kapının kabulü (genel API; Aşama 6'dan beri 3B istasyon): <see cref="SiginakAdmission.Enter"/> (K8). İstasyon bağlı değilse
        /// kamera yalnız kapıya döner.
        /// </summary>
        public void ShowAdmissions()
        {
            if (admission != null) { admission.Enter(); return; }
            if (surface != null && !surface.InputBlocked && rig != null) rig.Focus(SiginakPose.Door);
        }

        /// <summary>Kapıdan masaya döner (genel API): <see cref="SiginakAdmission.Leave"/>; istasyon yoksa K1.</summary>
        public void CloseAdmissions()
        {
            if (admission != null) { admission.Leave(); return; }
            if (rig != null) rig.ResetView();
        }

        /// <summary>MASAYA DÖN: seçim düşer, açık deste toplanır, kamera masaya döner (0,5 s).</summary>
        public void Close()
        {
            ui.SelectedId = null;
            if (board3D != null && board3D.Deck != null && board3D.Deck.IsOpen) board3D.Deck.Close(true);
            if (rig != null) { rig.InputSuspended = false; rig.ResetView(); }
            Refresh();
        }

        /// <summary>
        /// Esc, sağ tık ve MASAYA DÖN levhası (Revizyon 3, Karar K7; PLAN §1.5, Aşama 5 §1.12): tutulan kart yayla yerine döner
        /// (defterden alınan tarif kopyası sayfaya döner, defter öne gelir); yoksa açık dosya (ya da KAYIPLAR zarfı) kapanır; yoksa açık
        /// tarif defteri kancaya kapanır; yoksa masaya dönülür. Deste ve seçim Esc'i tüketmez (kapının kabulü Aşama 6'dan beri
        /// <see cref="SiginakAdmission"/>'da).
        /// Pano açık değilse false.
        /// </summary>
        public bool HandleBack()
        {
            if (!IsOpen) return false;
            if (drag != null && drag.Held != null) { drag.Cancel(); return true; }
            if (board3D != null)
            {
                if (board3D.Folder != null && board3D.Folder.IsOpen) { board3D.Folder.Close(); return true; }
                if (board3D.LostEnvelope != null && board3D.LostEnvelope.IsOpen) { board3D.LostEnvelope.Close(); return true; }
            }
            var book = Book;
            if (book != null && BookHoldsBoard(book.State)) { book.Close(); SyncBookMode(); return true; }
            Close();
            return true;
        }

        public bool HandleEscape() => HandleBack();

        void BackSignClicked() => HandleBack();
        void DeckTrayClicked() { if (drag != null) drag.ClickTarget(new BoardTarget(BoardTargetKind.Deck)); }
        void HurdaClicked() { if (drag != null) drag.ClickTarget(BoardTarget.Hurda()); }

        // ------------------------------------------------------------------ sürükle-bırak

        bool CanDrop(BoardCardView c, BoardTarget t) => c != null && BoardDropRules.Valid(c.Kind, c.Id, t, Sim, ui);

        DropPreview PreviewDrop(BoardCardView c, BoardTarget t) => c != null ? BoardPreviewText.For(c.Kind, c.Id, t, Sim, ui) : DropPreview.Invalid;

        /// <summary>Kart alınabilir mi (PLAN §1.9): null = alınır; metin = ret nedeni (ör. serbest 0'da "Serbest hammadde yetersiz.").</summary>
        string PickCheck(BoardCardView c) => c != null ? BoardDropRules.CanPick(c.Kind, c.Id, Sim) : null;

        /// <summary>
        /// Bırakma isteği → <see cref="ApplyDrop"/> → <see cref="BoardDragController.Resolve(bool, string)"/>. Ret: Core'un iletisi
        /// bırakma yerindeki etikette (denetçi, yalnız yetersiz stokta, depo kartını kırmızı yakar).
        /// </summary>
        void DropRequested(BoardCardView c, BoardTarget t)
        {
            if (drag == null) return;
            if (c == null || string.IsNullOrEmpty(c.Id)) { drag.Resolve(false); return; }
            var outcome = ApplyDrop(c.Kind, c.Id, t, drag.LastDropPose);
            if (!outcome.Accepted) { drag.Resolve(false, outcome.Reason); return; }
            drag.Resolve(true);
            if (outcome.Rerender) RenderBoard(true);   // Changed'in çizimi sıkıştırmadan önceydi: kalan fişler kendi yerlerine
        }

        /// <summary>
        /// Bırakmanın sunucu işi (sürükleme denetçisi çağırır; testler doğrudan): iş emri üstünde yer değiştiren fiş ya da [B] formdaki
        /// tarif kartı komutsuzdur (yalnız yerleşim); öteki bırakmalar <see cref="BoardDropRules.Plan"/> ile tek eyleme çözülür
        /// (<see cref="ApplyPlan"/>). <paramref name="pose"/>: bırakma pozu (pano mm x, mm y, açı).
        /// </summary>
        public DropOutcome ApplyDrop(BoardCardKind kind, string id, BoardTarget t, Vector3 pose)
        {
            var sim = Sim;
            if (game == null || sim?.Board == null || string.IsNullOrEmpty(id)) return DropOutcome.Refuse(null);
            if (BoardDropRules.IsLayoutOnly(kind, id, t, sim))
            {
                // Önce sakla: Resolve(true) kartı hemen geri verir ve pano yeniden dizilir. Formdaki tarif kartının kimliği iş emridir.
                if (kind == BoardCardKind.Recipe) ui.SetRecipePose(id, pose);
                else ui.SetChipPose(id, pose);
                SaveLayout();
                return DropOutcome.Accept(false);
            }
            return ApplyPlan(BoardDropRules.Plan(kind, id, t, sim, ui), kind, id, pose);
        }

        /// <summary>
        /// Çözülmüş bırakmayı uygular: en çok bir eylem (<see cref="BoardDropAction.Replace"/> iki kayıt), tek
        /// <c>ExecuteBoardCommand</c>. Sunum durumu komuttan <b>önce</b> yazılır (komutun eşzamanlı <c>Changed</c>'i çizer ve
        /// <see cref="PlanningBoardSnapshotBuilder.SyncUi"/> artık çizilmeyen fişlerin yerini siler) ve ret olursa geri alınır: yeni fiş
        /// (<c>Stage</c> / [B] <c>Reserve</c> +1) bırakıldığı yerde, [B] yeni emrin tarif kartı (<c>Queue</c> / <c>Replace</c>; kimlik
        /// Core'un sıradaki <c>is-N</c>'si) bırakıldığı yerde belirir; depoya geri alınan fişin yeri silinir ve aynı türün sonraki
        /// fişleri birer sıra kayar (kalanlar yerinde durur). [B] <c>Replace</c> ve <c>Cancel</c> eski emrin tarif yerini düşürür.
        /// </summary>
        public DropOutcome ApplyPlan(BoardDropPlan plan, BoardCardKind kind, string id, Vector3 pose)
        {
            var sim = Sim;
            var command = plan.Command();
            if (game == null || sim?.Board == null || command == null) return DropOutcome.Refuse(null);
            var board = sim.Board;

            // Fiş sayıları komuttan önce okunur: komutun eşzamanlı Changed'i anlık görüntüyü değiştirir
            Dictionary<string, Vector3> chipBackup = null;
            string newChipKey = null;
            bool removedChip = false;
            if (plan.Action == BoardDropAction.Stage || plan.Action == BoardDropAction.Reserve)
            {
                int chipsBefore = BoardDropRules.ChipCount(board, plan.Id);
                if (kind == BoardCardKind.Chip)
                {
                    if (BoardUiState.TryParseChipId(id, out var chipKind, out int chipIndex) && chipKind == plan.Id)
                    {
                        // Kaydırma komuttan önce: Changed'in çizimi (SyncUi) sondaki sırayı siler, kalanlar yerinde kalmalı
                        chipBackup = new Dictionary<string, Vector3>(ui.ChipPoses);
                        ui.RemoveChip(chipKind, chipIndex, chipsBefore);
                        removedChip = true;
                    }
                }
                else if (kind == BoardCardKind.Material && plan.Amount > HeldBefore(board, plan))
                {
                    chipBackup = new Dictionary<string, Vector3>(ui.ChipPoses);
                    newChipKey = BoardUiState.ChipPoseKey(plan.Id, chipsBefore);
                    ui.ChipPoses[newChipKey] = pose;
                }
            }

            // [B] Yeni emrin tarif kartı bırakıldığı yerde
            string recipeKey = null;
            bool hadRecipePose = false;
            Vector3 oldRecipePose = default;
            if (plan.Action == BoardDropAction.Queue || plan.Action == BoardDropAction.Replace)
            {
                recipeKey = NextOrderId(board);
                hadRecipePose = ui.RecipePoses.TryGetValue(recipeKey, out oldRecipePose);
                ui.SetRecipePose(recipeKey, pose);
            }

            var result = game.ExecuteBoardCommand(command);
            if (result == null || !result.Ok)
            {
                if (chipBackup != null)
                {
                    ui.ChipPoses.Clear();
                    foreach (var pair in chipBackup) ui.ChipPoses[pair.Key] = pair.Value;
                }
                if (recipeKey != null) { if (hadRecipePose) ui.RecipePoses[recipeKey] = oldRecipePose; else ui.RecipePoses.Remove(recipeKey); }
                // Run retde de Changed yayar: o çizim ön yazımı görmüş ve dosyaya yazmış olabilir; geri alınmış durum yazılır
                if (chipBackup != null || recipeKey != null) SaveLayout();
                return DropOutcome.Refuse(result?.Msg);
            }

            bool save = chipBackup != null || recipeKey != null;
            if ((plan.Action == BoardDropAction.Replace || plan.Action == BoardDropAction.Cancel) && ui.RemoveRecipePose(plan.Job)) save = true;
            if (PlanningBoardSnapshotBuilder.SyncUi(sim, ui)) save = true;   // iptal: serbest kalan fişlerin yerleri
            if (save) SaveLayout();
            // Fiş geri alındı: Changed'in çizimi kart hâlâ eldeyken oldu; kalan fişler yeni sıralarına bir kez daha
            return DropOutcome.Accept(removedChip);
        }

        /// <summary>Eylemden önceki mutlak miktar: konmuş fiş (<c>Stage</c>) ya da [B] başlamamış emrin ayrılanı (<c>Reserve</c>).</summary>
        static int HeldBefore(PlanningBoardState board, BoardDropPlan plan) =>
            plan.Action == BoardDropAction.Stage ? BoardDropRules.Staged(board, plan.Id) : BoardDropRules.Reserved(board, plan.Id);

        /// <summary>Core'un açacağı sıradaki iş emrinin kimliği (<c>QueueProduction</c>: <c>"is-" + NextOrderId</c>).</summary>
        public static string NextOrderId(PlanningBoardState board) =>
            "is-" + board.NextOrderId.ToString(System.Globalization.CultureInfo.InvariantCulture);

        /// <summary>Tutma bitti: kart hareketleri bitince pano bir kez taze anlık görüntüyle çizilir (her karede değil).</summary>
        void DragChanged(bool dragging) { if (!dragging) renderAfterDrag = true; }

        // ------------------------------------------------------------------ tıklama yolu

        /// <summary>Karta tık = seçim (sarı çerçeve, dosya kapağında ad). Denetleyici desteyi kendisi açar ya da toplar.</summary>
        void CardClicked(BoardCardView c)
        {
            if (c == null || string.IsNullOrEmpty(c.Id)) return;
            ui.SelectedId = c.Id;
            RenderBoard(true);
        }

        /// <summary>Kişi kartına çift tık = seçer ve dosyasını açar.</summary>
        void CardDoubleClicked(BoardCardView c)
        {
            if (c == null || c.Kind != BoardCardKind.Person || string.IsNullOrEmpty(c.Id)) return;
            ui.SelectedId = c.Id;
            RenderBoard(true);
            OpenFolder();
        }

        /// <summary>Boş panoya tık (plan açıkken): seçim düşer; seçimsiz dosya kendiliğinden kapanır, KAYIPLAR zarfı da kapanır.</summary>
        void EmptyClicked()
        {
            if (board3D != null && board3D.LostEnvelope != null && board3D.LostEnvelope.IsOpen) board3D.LostEnvelope.Close();
            if (ui.SelectedId == null) return;
            ui.SelectedId = null;
            RenderBoard(true);
        }

        /// <summary>
        /// Seçim varken hedefe tık (ya da klavyede Enter): seçili kart o hedefe bırakılmış gibi, en çok bir komut. Kilitli planda ya
        /// da Core reddedince seçili kart 2 mm sarsılır ve "tok". Eşya kişiye takılınca seçim o kişiye geçer; başka yere giden eşyanın
        /// seçimi düşer; kişinin seçimi kalır.
        /// </summary>
        void TargetClicked(BoardTarget t)
        {
            if (t.Kind == BoardTargetKind.Back) { HandleBack(); return; }
            var sim = Sim;
            var snap = board3D != null ? board3D.Last : null;
            if (game == null || sim?.Board == null || snap == null || string.IsNullOrEmpty(snap.SelectedId)) return;
            string id = snap.SelectedId;
            var card = board3D.CardFor(id);
            var kind = card != null ? card.Kind : snap.Person(id) != null ? BoardCardKind.Person : BoardCardKind.Item;
            if (BoardDropRules.IsLayoutOnly(kind, id, t, sim)) return;   // tıklama yolunda fişin yeni yeri yok
            var command = BoardDropRules.CommandFor(kind, id, t, sim, ui);
            if (command == null) return;
            if (!snap.Editable) { Deny(card, null); return; }
            var result = game.ExecuteBoardCommand(command);
            if (result == null || !result.Ok) { Deny(card, result?.Msg); return; }
            if (kind != BoardCardKind.Item) return;
            ui.SelectedId = t.Kind == BoardTargetKind.Person ? t.Id : null;
            RenderBoard(true);
        }

        /// <summary>Tık / klavye yolundaki ret: kart 2 mm sarsılır, "tok"; yalnız yetersiz stokta depo kartı 0,2 s kırmızı (PLAN §1.9).</summary>
        void Deny(BoardCardView card, string reason)
        {
            var theme = SiginakTheme.Active;
            if (card != null)
                card.Shake(theme != null ? theme.NumberOf("motion.board.denyShakeMm", 2f) : 2f, theme != null ? theme.NumberOf("motion.board.denyShakeS", 0.18f) : 0.18f);
            PlaySound(BoardSound.Deny);
            if (card != null && DeniesStock(card.Kind, reason) && board3D != null && board3D.Store != null) board3D.Store.DenyFlash(card.Id);
        }

        /// <summary>
        /// Tık / klavye retinde depo kartı kırmızı yanar mı: sürükleme yolunun kuralıyla aynı (<see cref="BoardDragController.FlashesStock"/>;
        /// yalnız hammadde kartı ve Core'un "Serbest hammadde yetersiz." iletisi). Kilitli plan ya da tarifin istemediği tür titrer, yanmaz.
        /// </summary>
        public static bool DeniesStock(BoardCardKind kind, string reason) => BoardDragController.FlashesStock(kind, reason);

        // ------------------------------------------------------------------ eğitim konuları

        /// <summary>Konu satırına tık: biri eğitimdeyse onu o konuya alır (<c>board_train</c>); kimse yoksa halkalı konu saklanır.</summary>
        void TopicRowClicked(int row)
        {
            var sim = Sim;
            var snap = board3D != null ? board3D.Last : null;
            if (game == null || sim?.Board == null || snap == null || !snap.Editable) return;
            if (row < 0 || row >= PlanningBoardData.Subjects.Length) return;
            var command = BoardDropRules.TopicCommand(row, sim);
            if (command != null) { game.ExecuteBoardCommand(command); return; }
            string subject = PlanningBoardData.Subjects[row];
            if (ui.TrainingTopic == subject) return;
            ui.TrainingTopic = subject;
            SaveLayout();
            RenderBoard(true);
        }

        // ------------------------------------------------------------------ kişi dosyası ve KAYIPLAR

        string FolderPerson => board3D != null && board3D.Last != null && board3D.Last.Folder != null ? board3D.Last.Folder.PersonId : null;

        /// <summary>Dosya kapağına tık (ya da çift tık): seçili kişinin dosyası okuma pozunda açılır; seçim yoksa bir şey olmaz.</summary>
        void OpenFolder()
        {
            var f = board3D != null && board3D.Last != null ? board3D.Last.Folder : null;
            if (f == null || board3D.Folder == null) return;
            if (board3D.LostEnvelope != null && board3D.LostEnvelope.IsOpen) board3D.LostEnvelope.Close();
            reviewWarnedFor = null;
            board3D.Folder.Open(f);
        }

        /// <summary>
        /// İNCELE sekmesi. Komut tek gözetim yuvasındaki başka bir işi bozacaksa (kapıdaki dış kayıt talebi, başka dosyanın bekleyen
        /// incelemesi, gözetimle çalışan kısıtlı kişi; son istek kazanır, D3-a) ilk tık yalnız uyarır (<see cref="ReviewCancelsDoorRecord"/>,
        /// not satırında); aynı dosyada ikinci tık komutu yazar.
        /// </summary>
        void ReviewFolder()
        {
            var id = FolderPerson;
            var warning = ReviewCancelsDoorRecord(Sim, id);
            if (warning != null && reviewWarnedFor != id && board3D != null && board3D.Folder != null && board3D.Folder.IsOpen)
            {
                reviewWarnedFor = id;
                board3D.Folder.ShowNote(warning);
                return;
            }
            reviewWarnedFor = null;
            FolderCommand(BoardDropRules.ReviewCommand(id));
        }

        /// <summary>
        /// Bu dosyanın İNCELE'si tek gözetim yuvasındaki başka bir işi bozar mı (D3-a, H2/H4; Core'un public durumu: <c>ObservationTarget</c>,
        /// adayın incelemeleri, dosyalar): kapıdaki dış kayıt talebi bekliyorsa "Kapıdaki dış kayıt talebi iptal olur.", yuva başka bir
        /// dosyanın bekleyen incelemesindeyse ya da gözetimle çalışan kısıtlı bir kişideyse onun uyarısı (metinler
        /// <see cref="AdmissionSnapshotBuilder.ReviewWarning"/>'ten, talep defterinin uyarılarıyla aynı). Bozmuyorsa (kişi aday, İNCELE zaten
        /// yapılamıyor, yuva boş ya da kendisinde) null. Saf okuma.
        /// </summary>
        public static string ReviewCancelsDoorRecord(Simulation sim, string personId)
        {
            var a = sim?.Admission;
            if (a == null || string.IsNullOrEmpty(personId)) return null;
            if (a.Candidate != null && personId == a.Candidate.Resident.Id) return null;
            if (sim.ResidentReviewCheck(personId) != null) return null;
            return AdmissionSnapshotBuilder.ReviewWarning(sim, personId);
        }
        void AccessFolder() => FolderCommand(BoardDropRules.AccessCommand(FolderPerson, Sim));
        void DismissFolder() => FolderCommand(BoardDropRules.DismissCommand(FolderPerson));

        /// <summary>Dosya sekmesinin komutu; Core'un iletisi sağ sayfanın not satırına yazılır (tahliyede dosya kendiliğinden kapanır).</summary>
        void FolderCommand(Func<Simulation, ActResult> command)
        {
            if (command == null || game == null) return;
            var result = game.ExecuteBoardCommand(command);
            if (result != null && board3D != null && board3D.Folder != null && board3D.Folder.IsOpen) board3D.Folder.ShowNote(result.Msg ?? string.Empty);
        }

        /// <summary>KAYIPLAR zarfına tık: kişi dosyası kapanır, kayıp listesi okuma pozunda açılır.</summary>
        void OpenLost()
        {
            var snap = board3D != null ? board3D.Last : null;
            var envelope = board3D != null ? board3D.LostEnvelope : null;
            if (snap == null || envelope == null) return;
            if (board3D.Folder != null && board3D.Folder.IsOpen) board3D.Folder.Close();
            envelope.Open(snap.Lost);
        }

        // ------------------------------------------------------------------ ses

        void PlaySound(BoardSound sound) => Sfx.Play(CueOf(sound));

        /// <summary>
        /// Pano sesi → ses olayı (PLAN §1.8, Aşama 5 §1.13; klibi olmayan ses sessiz kalır). Her değer açık yazılır: yeni bir ses
        /// <c>default</c>'a düşüp kapak sesi çalmasın.
        /// </summary>
        public static Cue CueOf(BoardSound sound)
        {
            switch (sound)
            {
                case BoardSound.Lift: return Cue.BoardLift;
                case BoardSound.Settle: return Cue.BoardMagnet;
                case BoardSound.Return: return Cue.BoardReturn;
                case BoardSound.Deny: return Cue.BoardDeny;
                case BoardSound.DeckSlide: return Cue.BoardDeckSlide;
                case BoardSound.DeckCollect: return Cue.BoardDeckCollect;
                case BoardSound.Tuck: return Cue.BoardItemTuck;
                case BoardSound.Hurda: return Cue.BoardHurda;
                case BoardSound.Padlock: return Cue.BoardPadlock;
                case BoardSound.Flap: return Cue.BoardFlap;
                case BoardSound.BookOpen: return Cue.BoardBookOpen;
                case BoardSound.PageTurn: return Cue.BoardPageTurn;
                case BoardSound.BookClose: return Cue.BoardBookClose;
                case BoardSound.RecipeLift: return Cue.BoardRecipeLift;
                case BoardSound.ProductSlide: return Cue.BoardProductSlide;
                default: return Cue.BoardFlap;
            }
        }

        // ------------------------------------------------------------------ emekli görevler (her karede; değişmedi)

        static bool RetiredJob(string job) => job == "care" || job == "gather" || job == "watch" || job == "contact";
        void RetireJobs()
        {
            if (Sim?.Board == null || Sim.Board.Locked || game.StoryHold || Sim.S.Raid != null || Sim.S.Ending != null) return;
            if(!Sim.Board.AutomaticProduction)game.ExecuteBoardCommand(s=>s.EnableAutomaticProduction());
            foreach (var person in Sim.Board.People.Where(p => p.Alive && RetiredJob(p.Job)).ToArray())
                game.ExecuteBoardCommand(s => s.AssignResident(person.Id, "rest"));
        }

        /// <summary>
        /// [B] Eski (A dönemi) kayıtlarda üretim alanına konmuş fişler (D2): <see cref="RetireJobs"/>'un koşullarında, konmuş her tür
        /// bir kez <c>board_stage(tür, 0)</c> ile stoka döner (kayıtlı, Core aynı); sonra konmuş fiş kalmadığı için sessizdir. Bir komut
        /// reddedilirse aynı oyun için yeniden denenmez (yükleme ve yeni oyun sıfırlar). Her karede çağrılır; gönderilen komut sayısını döner.
        /// </summary>
        public int ReleaseLegacyStaging()
        {
            var sim = Sim;
            if (legacyReleaseBlocked || game == null || sim?.Board == null) return 0;
            var board = sim.Board;
            if (board.Locked || game.StoryHold || sim.S.Raid != null || sim.S.Ending != null || !board.AutomaticProduction) return 0;
            if (!board.ProductionInputs.Any(m => m.Quantity > 0)) return 0;
            int sent = 0;
            foreach (var kind in board.ProductionInputs.Where(m => m.Quantity > 0).Select(m => m.Kind).Distinct().ToArray())
            {
                var result = game.ExecuteBoardCommand(s => s.StageProductionMaterial(kind, 0));
                if (result == null || !result.Ok) { legacyReleaseBlocked = true; break; }
                sent++;
            }
            return sent;
        }

        // ------------------------------------------------------------------ eski yollar

        // Eski bağlayıcılar göç sırasında derlenebilir; panodan fiş basmanın komut yolu kapalıdır.
        public void PressMachine() => Print();
        public void Print() => view?.SetStatus(Loc.T("Vardiya fişini masadaki vardiya şalterinden bas."));
    }
}
