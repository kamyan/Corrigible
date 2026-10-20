using System;
using System.Linq;
using System.Text;
using Corrigible.Ayna.Core;
using Corrigible.Ayna.Mind;
using Corrigible.Ayna.Presentation;
using Corrigible.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Ayna.Game
{
    public sealed class AynaDebugView : MonoBehaviour
    {
        [SerializeField] AynaGameController controller;
        [SerializeField] AynaDebugTheme theme;
        [SerializeField] GameObject startPage, planningPage, flowPage, boardPage, rosterPage;
        [SerializeField] TMP_Text title, status, startInfo, mainInfo, queueInfo, flowInfo, boardInfo;
        [SerializeField] TMP_Dropdown architecture, startRegion, learning, orderKind, orderRegion, army, family, unitKind, repairUnit, boardUnit, boardAction, boardTarget;
        [SerializeField] TMP_InputField seed, amount;
        [SerializeField] Button startButton, continueButton, saveButton, loadButton, homeButton, languageButton, eraseButton, confirmEraseButton, addButton, removeButton, clearButton, lockButton, rosterButton, autoButton, commandButton, backButton, undoButton, endTurnButton, retreatButton, previewButton, applyBoardButton;
        [SerializeField] Button[] tabs, regionButtons, cells;
        [SerializeField] TMP_Text[] regionTexts, cellTexts, rosterTexts, captions;
        [SerializeField] int[] captionKeys;
        [SerializeField] Toggle[] rosterToggles;
        int tab, selectedCell; int[] armyIds = Array.Empty<int>(), repairIds = Array.Empty<int>(), boardIds = Array.Empty<int>(), targetIds = Array.Empty<int>();
        string oldLanguage; bool bound;
        public void Bind(AynaGameController game)
        {
            if (bound) return; bound = true; controller = game; oldLanguage = Loc.Lang;
            controller.Changed += Render;
            startButton.onClick.AddListener(() => Guard(() => { if (!uint.TryParse(seed.text, out uint value)) throw new ArgumentException("seed"); controller.NewGame((Architecture)architecture.value, AynaWorldData.Regions[startRegion.value].Id, value); }));
            learning.onValueChanged.AddListener(v => Guard(() => controller.SetLearning((LearningLevel)v)));
            continueButton.onClick.AddListener(() => Guard(controller.Continue)); saveButton.onClick.AddListener(() => Guard(controller.SaveGame)); loadButton.onClick.AddListener(() => Guard(controller.LoadGame)); homeButton.onClick.AddListener(() => Guard(controller.Home));
            eraseButton.onClick.AddListener(controller.RequestEraseMemory); confirmEraseButton.onClick.AddListener(() => Guard(() => controller.EraseMemory()));
            languageButton.onClick.AddListener(ToggleLanguage); addButton.onClick.AddListener(AddOrder); removeButton.onClick.AddListener(controller.UndoQueue); clearButton.onClick.AddListener(controller.ClearQueue); lockButton.onClick.AddListener(() => Guard(() => controller.LockPlan()));
            rosterButton.onClick.AddListener(controller.ChooseRoster); autoButton.onClick.AddListener(() => Guard(() => controller.AutoBattle())); commandButton.onClick.AddListener(() => Guard(() => controller.CommandBattle(controller.Screens.A5.Available.Where((u, i) => i < rosterToggles.Length && rosterToggles[i].isOn).Select(u => u.Id).ToArray()))); backButton.onClick.AddListener(controller.CancelRoster);
            undoButton.onClick.AddListener(controller.UndoBoard); endTurnButton.onClick.AddListener(() => Guard(controller.EndBoardTurn)); retreatButton.onClick.AddListener(() => Guard(controller.Retreat)); previewButton.onClick.AddListener(Preview); applyBoardButton.onClick.AddListener(() => Guard(() => { if (!controller.BoardCommand(Command())) status.text = Loc.T("Bu hamle geçerli değil."); }));
            for (int i = 0; i < tabs.Length; i++) { int index = i; tabs[i].onClick.AddListener(() => { tab = index; Render(); }); }
            for (int i = 0; i < regionButtons.Length; i++) { int index = i; regionButtons[i].onClick.AddListener(() => { orderRegion.SetValueWithoutNotify(index); tab = 0; Render(); }); }
            for (int i = 0; i < cells.Length; i++) { int index = i; cells[i].onClick.AddListener(() => { selectedCell = index; Render(); Preview(); }); }
            army.onValueChanged.AddListener(_ => Render()); orderRegion.onValueChanged.AddListener(_ => Render());
            Options(architecture, Enum.GetValues(typeof(Architecture)).Cast<Architecture>().Select(AynaText.Architecture).ToArray());
            Options(startRegion, AynaWorldData.Regions.Select(r => AynaText.Region(r.Id)).ToArray());
            Options(learning, Enum.GetValues(typeof(LearningLevel)).Cast<LearningLevel>().Select(AynaText.Level).ToArray());
            architecture.SetValueWithoutNotify(2); startRegion.SetValueWithoutNotify(2); learning.SetValueWithoutNotify(2); seed.text = "3101026"; amount.text = "1";
            confirmEraseButton.gameObject.SetActive(false); Render();
        }
        void Guard(Action action) { try { action(); } catch (Exception) { status.text = Loc.T("İşlem tamamlanamadı. Seçimi veya kayıt dosyasını kontrol et."); } }
        void ToggleLanguage()
        {
            string language = Loc.IsSource ? Loc.English : Loc.Turkish; var table = Loc.ParseTable(Resources.Load<TextAsset>("Localization/strings").text);
            Loc.SetLanguage(language, language == Loc.English ? table[Loc.English] : null); Render();
        }
        void Update()
        {
            var keyboard=UnityEngine.InputSystem.Keyboard.current;if(!bound||keyboard==null)return;
            var selected=UnityEngine.EventSystems.EventSystem.current?.currentSelectedGameObject;
            if(selected!=null&&(selected.GetComponent<TMP_InputField>()!=null||selected.GetComponent<TMP_Dropdown>()!=null))return;
            if(keyboard.zKey.wasPressedThisFrame)controller.UndoBoard();
            if(keyboard.escapeKey.wasPressedThisFrame)controller.CancelRoster();
            if(keyboard.enterKey.wasPressedThisFrame && (selected==null || selected.GetComponent<Button>()==null))
            {
                if(controller.Phase==AynaPhase.Start)startButton.onClick.Invoke();
                else if(controller.Phase==AynaPhase.Planning)Guard(()=>controller.LockPlan());
                else if(controller.Phase==AynaPhase.Board)Guard(controller.EndBoardTurn);
                else Guard(controller.Continue);
            }
        }
        void OnDestroy()
        {
            if (controller != null) controller.Changed -= Render;
            if (oldLanguage != null && Loc.Lang != oldLanguage) { var table = Loc.ParseTable(Resources.Load<TextAsset>("Localization/strings").text); Loc.SetLanguage(oldLanguage, oldLanguage == Loc.English ? table[Loc.English] : null); }
        }
        static void Options(TMP_Dropdown dropdown, string[] labels)
        {
            int value = dropdown.value; dropdown.ClearOptions(); dropdown.AddOptions(labels.Length == 0 ? new System.Collections.Generic.List<string> { "—" } : labels.ToList()); dropdown.SetValueWithoutNotify(Math.Min(value, Math.Max(0, labels.Length - 1))); dropdown.RefreshShownValue();
        }
        void AddOrder()
        {
            Guard(() => { int.TryParse(amount.text, out int count); controller.Queue(new Order { Kind = (OrderKind)orderKind.value, Region = orderKind.value == (int)OrderKind.Train || orderKind.value == (int)OrderKind.StartSingularity || orderKind.value == (int)OrderKind.Wait || orderKind.value == (int)OrderKind.Produce || orderKind.value == (int)OrderKind.Repair ? null : AynaWorldData.Regions[orderRegion.value].Id, Amount = count, ArmyId = armyIds.ElementAtOrDefault(army.value), Family = (DataFamily)family.value, Unit = (UnitKind)unitKind.value, UnitId = repairIds.ElementAtOrDefault(repairUnit.value) }); });
        }
        BoardCommand Command() => new BoardCommand { UnitId = boardIds.ElementAtOrDefault(boardUnit.value), Action = (BoardAction)boardAction.value, To = selectedCell, TargetUnitId = targetIds.ElementAtOrDefault(boardTarget.value) };
        void Preview()
        {
            var v = controller.Preview(Command()); boardInfo.text = v == null ? Loc.T("Bu hamle geçerli değil.") : Loc.F("Önizleme: {0} birim ayakta, gürültü {1:0.#}.", v.Units.Count, v.Noise[(int)Side.Player]);
        }
        public void Render()
        {
            if (controller?.Screens == null) return; var s = controller.Screens;
            for (int i = 0; i < captions.Length; i++) captions[i].text = AynaText.Caption(captionKeys[i]);
            Options(architecture, Enum.GetValues(typeof(Architecture)).Cast<Architecture>().Select(AynaText.Architecture).ToArray()); Options(startRegion, AynaWorldData.Regions.Select(r => AynaText.Region(r.Id)).ToArray()); Options(learning, Enum.GetValues(typeof(LearningLevel)).Cast<LearningLevel>().Select(AynaText.Level).ToArray());
            learning.SetValueWithoutNotify((int)s.A1.Learning);
            title.text = Loc.F("AYNA · {0}", AynaText.Phase(s.Phase)); status.text = controller.Status;
            bool starting = s.Phase == AynaPhase.Start, planning = s.Phase == AynaPhase.Planning, board = s.Phase == AynaPhase.Board;
            startPage.SetActive(starting); planningPage.SetActive(planning); boardPage.SetActive(board); flowPage.SetActive(!starting && !planning && !board); rosterPage.SetActive(s.Phase == AynaPhase.Roster);
            confirmEraseButton.gameObject.SetActive(controller.EraseConfirmationPending);
            saveButton.interactable = !starting; loadButton.interactable = controller.HasSave; eraseButton.interactable = starting || s.Phase == AynaPhase.End;
            startInfo.text = Loc.F("AYNA v{0} · {1} oyun · skor {2:0.#}\nÖğrenme: {3}\nMimarini, başlangıç bölgeni ve tohumu seç.", s.A1.Generation, s.A1.Games, s.A1.Score, AynaText.Level(s.A1.Learning));
            if (starting) return;
            if (s.A11.Announced) status.text = Loc.F("{0}\n{1}", controller.Status, Loc.F("AYNA Tekillik ilan etti: {0}/{1}. Olası veri hedefleri: {2}", s.A11.Progress, s.A11.Required, string.Join(", ", s.A11.PossibleTargets.Select(AynaText.Region))));
            title.text = Loc.F("AYNA · Döngü {0} · {1} · İşlem {2:0.#} · Farkındalık {3:0.#} · Okunabilirlik %{4:0}", s.A2.Cycle, AynaText.Phase(s.Phase), s.A2.Compute, s.A2.Awareness, s.A2.Readability);
            if (planning)
            {
                Options(orderKind, Enum.GetValues(typeof(OrderKind)).Cast<OrderKind>().Select(AynaText.Order).ToArray()); Options(orderRegion, AynaWorldData.Regions.Select(r => AynaText.Region(r.Id)).ToArray()); Options(family, Enum.GetValues(typeof(DataFamily)).Cast<DataFamily>().Select(AynaText.Family).ToArray()); Options(unitKind, Enum.GetValues(typeof(UnitKind)).Cast<UnitKind>().Select(AynaText.Unit).ToArray());
                armyIds = s.A9.Armies.Select(a => a.Id).ToArray(); Options(army, s.A9.Armies.Select(a => Loc.F("Birlik {0} · {1}", a.Id, AynaText.Region(a.Region))).ToArray());
                var own = s.A9.Armies.ElementAtOrDefault(army.value); repairIds = own?.Units.Select(u => u.Id).ToArray() ?? Array.Empty<int>(); Options(repairUnit, own?.Units.Select(u => Loc.F("{0} #{1} · {2:0.#} can", AynaText.Unit(u.Kind), u.Id, u.Hp)).ToArray() ?? Array.Empty<string>());
                for (int i = 0; i < regionTexts.Length; i++) { regionTexts[i].fontSize = theme.RegionFontSize; var r = s.A2.Regions.First(x => x.Id == AynaWorldData.Regions[i].Id); string enemy = r.Knowledge == Knowledge.Exact ? Loc.Num(r.EnemyMin) : r.Knowledge == Knowledge.Unknown ? "?" : Loc.F("≈{0}–{1}", r.EnemyMin, r.EnemyMax); regionTexts[i].text = Loc.F("{0}\nSen {1} · AYNA {2} / {3}", AynaText.Region(r.Id), r.Own, enemy, r.Capacity); }
                queueInfo.text = Loc.F("Plan: {0} emir · {1:0.#} İşlem · {2:0.#} gürültü\n{3}", s.A2.Queue.Length, s.A2.Cost, s.A2.Noise, string.Join("\n", s.A2.Queue.Select((o, i) => Loc.F("{0}. {1} · {2} · {3}", i + 1, AynaText.Order(o.Kind), AynaText.Region(o.Region), AynaText.Rejection(s.A2.Preview[i].Rejection)))));
                mainInfo.text = TabText(s);
            }
            if (board) RenderBoard(s);
            if (!starting && !planning && !board)
            {
                continueButton.gameObject.SetActive(s.Phase == AynaPhase.Reveal || s.Phase == AynaPhase.Results || s.Phase == AynaPhase.BoardResult || s.Phase == AynaPhase.End);
                rosterButton.gameObject.SetActive(s.Phase == AynaPhase.Conflict); autoButton.gameObject.SetActive(s.Phase == AynaPhase.Conflict || s.Phase == AynaPhase.Roster); commandButton.gameObject.SetActive(s.Phase == AynaPhase.Roster); backButton.gameObject.SetActive(s.Phase == AynaPhase.Roster);
                flowInfo.text = FlowText(s);
                if (s.Phase == AynaPhase.End) { int index = Array.IndexOf(captionKeys,1); if(index>=0) captions[index].text=AynaText.Caption(33); }
                if (s.Phase == AynaPhase.Roster) for (int i = 0; i < rosterToggles.Length; i++) { bool visible = i < s.A5.Available.Length; rosterToggles[i].gameObject.SetActive(visible); if (visible) { var u = s.A5.Available[i]; rosterTexts[i].text = Loc.F("{0} #{1} · {2:0.#} can", AynaText.Unit(u.Kind), u.Id, u.Hp); } }
            }
        }
        string TabText(AynaScreens s)
        {
            if (tab == 1) return string.Join("\n", s.A8.Nodes.Select(n => Loc.F("{0} {1} · veri {2}/{3} · bedel {4:0} · {5}", AynaText.Family(n.Family), n.Tier, n.Servers, n.Required, n.Cost, n.Complete ? Loc.T("Tamamlandı") : n.Running ? Loc.T("Eğitimde") : n.Available ? Loc.T("Açık") : Loc.T("Kilitli"))));
            if (tab == 2) return string.Join("\n", s.A9.Armies.Select(a => Loc.F("Birlik {0} · {1}\n{2}", a.Id, AynaText.Region(a.Region), string.Join(" · ", a.Units.Select(u => Loc.F("{0} #{1}: {2:0.#} can", AynaText.Unit(u.Kind), u.Id, u.Hp)))))) + "\n" + string.Join("\n", s.A9.Production.Select(p => Loc.F("{0}: {1:0} İşlem · {2}", AynaText.Unit(p.Kind), p.Cost, p.Available ? Loc.T("Açık") : Loc.T("Veri hattı yok"))));
            if (tab == 3) return Loc.F("AYNA v{0} · öğrenme {1}\n{2}\nSon okunabilirlik: {3}\n{4}", s.A10.Generation, AynaText.Level(s.A10.Learning), string.Join("\n", ForecastText.TopThree(s.A10.TopThree)), string.Join(" · ", s.A10.Readability.Select(v => Loc.Num(v))), ForecastText.SurpriseExplanation);
            if (tab == 4) return string.Join("\n", s.A3.OwnOrders.Select(r => Loc.F("Sen: {0} · {1} · {2}", AynaText.Order(r.Kind), AynaText.Region(r.Region), AynaText.Rejection(r.Rejection))).Concat(s.A3.EnemySignals.Select(o => o.Verified && o.RevealedOrder.HasValue ? Loc.F("AYNA: {0} · {1}", AynaText.Order(o.RevealedOrder.Value), AynaText.Region(o.Region)) : Loc.F("Hareket algılandı · {0} · doğrulanmadı", AynaText.Region(o.Region)))));
            var region = s.A2.Regions.First(r => r.Id == AynaWorldData.Regions[orderRegion.value].Id);
            return Loc.F("{0} · {1}\nSenin sunucun: {2}/{3}\nGelir: +{4:0.#}/döngü · Bu plan: −{5:0.#}\n\n{6}\n\n{7}", AynaText.Region(region.Id), AynaText.Family(region.Family), region.Own, region.Capacity, s.A2.Income, s.A2.Cost, string.Join("\n", ForecastText.TopThree(s.A2.Predictions)), s.A11.Announced ? Loc.F("AYNA Tekillik ilan etti: {0}/{1}. Olası veri hedefleri: {2}", s.A11.Progress, s.A11.Required, string.Join(", ", s.A11.PossibleTargets.Select(AynaText.Region))) : Loc.T("Planını hazırla; sonra iki tarafın emirleri birlikte açılır."));
        }
        string FlowText(AynaScreens s)
        {
            if (s.Phase == AynaPhase.End) return Loc.F("OYUN SONU · {0}\nKazanan: {1}\nTahmin: {2}/{3} · Sürpriz: {4}\nNesil: {5} · Profil: {6}", AynaText.End(s.A12.Outcome.Reason), Winner(s.A12.Outcome.Winner), s.A12.Hits, s.A12.Decisions, s.A12.Surprises, s.A12.Generation, s.A12.ProfileCommitted ? Loc.T("Kaydedildi") : Loc.T("Değişmedi"));
            if (s.Phase == AynaPhase.BoardResult) return Loc.F("SAVAŞ SONUCU\nKazanan: {0}\nDüğümler: sen {1} / AYNA {2}\nGürültü: sen {3:0.#} / AYNA {4:0.#}", Winner(s.A7.Winner), s.A7.Nodes[0], s.A7.Nodes[1], s.A7.Noise[0], s.A7.Noise[1]);
            if (s.Phase == AynaPhase.Conflict || s.Phase == AynaPhase.Roster) return Loc.F("ÇATIŞMA · {0}\nKadro sınırı: {1}\nBilinen AYNA: {2}\nBilinmeyen: {3}\nKomuta et veya daha düşük ödüllü otomatik çözümü seç.", AynaText.Region(s.A4.Region), s.A5.Slots, string.Join(", ", s.A5.KnownEnemy.Select(u => AynaText.Unit(u.Kind))), s.A5.UnknownEnemy);
            return Loc.F("{0}\n{1}\n\n{2}", AynaText.Phase(s.Phase), string.Join("\n", s.A3.OwnOrders.Select(r => Loc.F("Sen: {0} · {1} · {2}", AynaText.Order(r.Kind), AynaText.Region(r.Region), AynaText.Rejection(r.Rejection))).Concat(s.A3.EnemySignals.Select(o => o.Verified && o.RevealedOrder.HasValue ? Loc.F("AYNA: {0} · {1}", AynaText.Order(o.RevealedOrder.Value), AynaText.Region(o.Region)) : Loc.F("Hareket algılandı · {0} · doğrulanmadı", AynaText.Region(o.Region))))), s.A3.Surprise ? Loc.T("SÜRPRİZ · ana taahhüt beklenmiyordu.") : Loc.T("Devam ederek çözümü veya sonraki döngüyü aç."));
        }
        static string Winner(Side side) => side == Side.Player ? Loc.T("Sen") : side == Side.Ayna ? Loc.T("AYNA") : Loc.T("Berabere");
        void RenderBoard(AynaScreens s)
        {
            var b = s.A6; boardIds = b.Units.Where(u => u.Controller == Side.Player).Select(u => u.Id).ToArray(); Options(boardUnit, b.Units.Where(u => u.Controller == Side.Player).Select(u => Loc.F("{0} #{1} · {2:0.#} can", AynaText.Unit(u.Kind), u.Id, u.Hp)).ToArray());
            targetIds = new[] { 0 }.Concat(b.Units.Select(u => u.Id)).ToArray(); Options(boardTarget, new[] { Loc.T("Hedef birim yok") }.Concat(b.Units.Select(u => Loc.F("{0} #{1}", AynaText.Unit(u.Kind), u.Id))).ToArray()); Options(boardAction, Enum.GetValues(typeof(BoardAction)).Cast<BoardAction>().Select(AynaText.Action).ToArray());
            for (int i = 0; i < cells.Length; i++)
            {
                var u = b.Units.FirstOrDefault(x => x.Cell == i); var node = b.Nodes.FirstOrDefault(n => n.Cell == i); string tile = b.Tiles[i] == BoardTile.Rack ? "#" : b.Tiles[i] == BoardTile.Civilian ? "S" : "·";
                cellTexts[i].text = Loc.F("{0},{1}\n{2}", i / 8, i % 8, u != null ? Loc.F("{0} #{1}\n{2:0.#}", u.Side == Side.Player ? Loc.T("Sen") : Loc.T("YZ"), u.Id, u.Hp) : node != null ? "N" : b.VisibleTraps.Any(t => t.Cell == i) ? "!" : b.CoreCells.Contains(i) ? "C" : tile);
                cells[i].image.color = u != null ? u.Side == Side.Player ? theme.Own : theme.Enemy : node != null ? node.Owner == Side.Player ? theme.Own : node.Owner == Side.Ayna ? theme.Enemy : theme.Node : b.Tiles[i] == BoardTile.Rack ? theme.Rack : theme.Floor;
            }
            boardInfo.text = Loc.F("Tur {0}/{7} · {1}\nSeçilen hedef: {2},{3}\n{4}\nGörünen tuzak: {5}\nNiyet tarafı: {6}", b.Turn, b.Phase == BoardPhase.Intents ? Loc.T("Niyetini bildir") : b.Phase == BoardPhase.FirstStrike ? Loc.T("İlk vuruş") : Loc.T("Cevap ver"), selectedCell / 8, selectedCell % 8, string.Join("\n", b.VisibleIntents.Select(i => Loc.F("{0}. #{1} {2} → {3},{4}", i.Order + 1, i.UnitId, AynaText.Action(i.Action), i.To / 8, i.To % 8))), b.VisibleTraps.Length, Winner(b.IntentSide), b.MaxTurns);
            boardInfo.text = Loc.F("{0}\nDüğümler: sen {1} / AYNA {2} / boş {3}", boardInfo.text, b.Nodes.Count(n=>n.Owner==Side.Player), b.Nodes.Count(n=>n.Owner==Side.Ayna), b.Nodes.Count(n=>n.Owner==Side.None));
            if (b.DraftIntents.Length > 0) boardInfo.text = Loc.F("{0}\nNiyet kuyruğu: {1}", boardInfo.text, b.DraftIntents.Length);
            int key = Array.IndexOf(captionKeys, 22); if (key >= 0) captions[key].text = AynaText.Caption(b.Phase == BoardPhase.Intents ? 31 : 22);
        }
    }
}
