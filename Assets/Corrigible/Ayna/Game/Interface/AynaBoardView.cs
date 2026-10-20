using System;
using System.Linq;
using System.Text;
using Corrigible.Ayna.Core;
using Corrigible.Ayna.Presentation;
using Corrigible.Core;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Corrigible.Ayna.Game
{
    // Sahne referanslarını bağlar; bütün oyun kararları denetleyicinin komutlarından geçer.
    public sealed class AynaBoardView : MonoBehaviour
    {
        [SerializeField] AynaBoardGraphic board;
        [SerializeField] TMP_Text titleText, objectiveText, phaseText, selectedText, previewText, intentText;
        [SerializeField] Button moveButton, attackButton, abilityButton, waitButton, undoButton, endTurnButton, retreatButton;
        [SerializeField] Button[] unitButtons = Array.Empty<Button>();
        [SerializeField] TMP_Text[] unitLabels = Array.Empty<TMP_Text>();
        AynaGameController controller;
        int selectedId, hoverCell = -1, redirectedUnitId;
        BoardAction action = BoardAction.Move;
        readonly bool[] legal = new bool[64];
        UnitVm[] ownUnits = Array.Empty<UnitVm>();
        bool listenersBound;

        public void Bind(AynaGameController value)
        {
            if (controller != null) controller.Changed -= Render;
            controller = value;
            if (controller != null) controller.Changed += Render;
            if (!listenersBound)
            {
                if (board != null) { board.CellClicked += ClickCell; board.CellHovered += HoverCell; }
                Hook(moveButton, () => SelectAction((int)BoardAction.Move));
                Hook(attackButton, () => SelectAction((int)BoardAction.Attack));
                Hook(abilityButton, () => SelectAction((int)BoardAction.Ability));
                Hook(waitButton, () => SelectAction((int)BoardAction.Wait));
                Hook(undoButton, Undo); Hook(endTurnButton, EndTurn); Hook(retreatButton, Retreat);
                for (int i = 0; i < unitButtons.Length; i++)
                {
                    int index = i;
                    Hook(unitButtons[i], () => { if (index < ownUnits.Length) SelectUnit(ownUnits[index].Id); });
                }
                listenersBound = true;
            }
            Render();
        }

        static void Hook(Button button, UnityEngine.Events.UnityAction callback)
        { if (button != null) button.onClick.AddListener(callback); }

        void OnDestroy()
        {
            if (controller != null) controller.Changed -= Render;
            if (board != null) { board.CellClicked -= ClickCell; board.CellHovered -= HoverCell; }
        }

        A6BoardVm Data => controller?.Screens?.A6;
        bool CanPlay => controller?.Screens?.Phase == AynaPhase.Board && Data != null &&
            (Data.Phase == BoardPhase.Intents && Data.IntentSide == Side.Player ||
             (Data.Phase == BoardPhase.Response || Data.Phase == BoardPhase.FirstStrike) && Data.IntentSide == Side.Ayna);

        public void Render()
        {
            var data = Data;
            if (data == null)
            {
                ownUnits = Array.Empty<UnitVm>(); selectedId = redirectedUnitId = 0;
                if (board != null) board.SetBoard(null, 0, action, legal, -1, null);
                return;
            }
            ownUnits = data.Units.Where(u => u.Hp > 0 && u.Controller == Side.Player).OrderBy(u => u.Id).ToArray();
            if (!ownUnits.Any(u => u.Id == selectedId))
            { selectedId = ownUnits.FirstOrDefault()?.Id ?? 0; redirectedUnitId = 0; hoverCell = -1; }
            if (!data.Units.Any(u => u.Id == redirectedUnitId && u.Controller != Side.Player)) redirectedUnitId = 0;
            Set(titleText, Loc.F("TUR {0}/{1}", data.Turn, data.MaxTurns));
            int ours = data.Nodes.Count(n => n.Owner == Side.Player), theirs = data.Nodes.Count(n => n.Owner == Side.Ayna);
            Set(objectiveText, Loc.F("DÜĞÜMLER  ◆ {0}   ⬢ {1}   ○ {2}", ours, theirs, data.Nodes.Length - ours - theirs));
            Set(phaseText, data.Phase == BoardPhase.Finished ? Loc.T("SAVAŞ TAMAMLANDI") :
                data.Phase == BoardPhase.Intents ? Loc.T("NİYETİNİ BİLDİR") : data.Phase == BoardPhase.FirstStrike ? Loc.T("İLK VURUŞ") : Loc.T("SIRA SENDE"));
            for (int i = 0; i < unitButtons.Length; i++)
            {
                var unit = i < ownUnits.Length ? ownUnits[i] : null;
                if (unitButtons[i] != null)
                {
                    unitButtons[i].gameObject.SetActive(unit != null);
                    unitButtons[i].interactable = unit != null && CanPlay;
                    if (unitButtons[i].targetGraphic != null)
                        unitButtons[i].targetGraphic.color = unit?.Id == selectedId ? new Color(.11f, .31f, .36f, .98f) : new Color(.10f, .075f, .035f, .98f);
                }
                if (i < unitLabels.Length && unitLabels[i] != null && unit != null)
                    unitLabels[i].text = Loc.F("◆ {0}\n{1}  ·  {2}", AynaText.Unit(unit.Kind), unit.Hp,
                        unit.Moved && unit.Acted ? Loc.T("TAMAMLANDI") : Loc.T("HAZIR"));
            }
            SetEnabled(moveButton, CanPlay && selectedId != 0);
            SetEnabled(attackButton, CanPlay && selectedId != 0);
            SetEnabled(abilityButton, CanPlay && selectedId != 0);
            SetEnabled(waitButton, CanPlay && selectedId != 0);
            SetEnabled(undoButton, CanPlay); SetEnabled(endTurnButton, CanPlay); SetEnabled(retreatButton, CanPlay);
            RenderIntents(data);
            RefreshLegal();
        }

        static void Set(TMP_Text field, string value) { if (field != null) field.text = value; }
        static void SetEnabled(Button field, bool value) { if (field != null) field.interactable = value; }

        void RenderIntents(A6BoardVm data)
        {
            var text = new StringBuilder();
            foreach (var intent in data.VisibleIntents.Where(i => data.Units.Any(u => u.Id == i.UnitId && u.Controller != Side.Player)))
            {
                var unit = data.Units.First(u => u.Id == intent.UnitId);
                text.AppendLine(Loc.F("{0}. ⬢ {1} · {2} → ({3}, {4})", intent.Order + 1,
                    AynaText.Unit(unit.Kind), AynaText.Action(intent.Action), intent.To / 8 + 1, intent.To % 8 + 1));
            }
            foreach (var intent in data.DraftIntents)
                text.AppendLine(Loc.F("◆ {0} → ({1}, {2})", AynaText.Action(intent.Action), intent.To / 8 + 1, intent.To % 8 + 1));
            Set(intentText, text.Length == 0 ? Loc.T("GÖRÜNÜR NİYET YOK") : text.ToString().TrimEnd());
        }

        public void SelectUnit(int id)
        {
            if (!CanPlay || !ownUnits.Any(u => u.Id == id)) return;
            selectedId = id; redirectedUnitId = 0; hoverCell = -1; action = BoardAction.Move; Render();
        }

        public void SelectAction(int value)
        {
            if (!CanPlay || (uint)value > (uint)BoardAction.Wait || selectedId == 0) return;
            action = (BoardAction)value; redirectedUnitId = 0; hoverCell = -1;
            if (action == BoardAction.Wait)
            {
                var unit = ownUnits.FirstOrDefault(u => u.Id == selectedId);
                if (unit != null) controller.BoardCommand(new BoardCommand { UnitId = selectedId, Action = action, To = unit.Cell });
                action = BoardAction.Move;
            }
            RefreshLegal();
        }

        BoardCommand Command(int cell)
        {
            var unit = ownUnits.FirstOrDefault(u => u.Id == selectedId);
            if (unit == null || cell < 0 || cell >= 64) return null;
            if (unit.Kind == UnitKind.PhantomOrder && action == BoardAction.Ability && redirectedUnitId == 0) return null;
            return new BoardCommand { UnitId = selectedId, Action = action, To = cell, TargetUnitId = redirectedUnitId };
        }

        void RefreshLegal()
        {
            Array.Clear(legal, 0, legal.Length);
            var selected = ownUnits.FirstOrDefault(u => u.Id == selectedId);
            if (CanPlay && selected != null)
            {
                if (selected.Kind == UnitKind.PhantomOrder && action == BoardAction.Ability && redirectedUnitId == 0)
                {
                    foreach (var enemy in Data.Units.Where(u => u.Controller != Side.Player)) legal[enemy.Cell] = true;
                }
                else for (int i = 0; i < legal.Length; i++)
                {
                    var command = Command(i);
                    legal[i] = command != null && controller.Preview(command) != null;
                }
            }
            Set(selectedText, selected == null ? Loc.T("BİRİM SEÇ") : Loc.F("◆ {0}\nCAN {1}  ·  {2}", AynaText.Unit(selected.Kind), selected.Hp, AynaText.Action(action)));
            RefreshPreview();
        }

        public void ClickCell(int cell)
        {
            if (!CanPlay || cell < 0 || cell >= 64) return;
            var unit = Data.Units.FirstOrDefault(u => u.Cell == cell);
            var selected = ownUnits.FirstOrDefault(u => u.Id == selectedId);
            if (selected?.Kind == UnitKind.PhantomOrder && action == BoardAction.Ability && unit != null && unit.Controller != Side.Player)
            { redirectedUnitId = unit.Id; hoverCell = -1; RefreshLegal(); return; }
            if (unit?.Controller == Side.Player && (unit.Id != selectedId || action == BoardAction.Move))
            { SelectUnit(unit.Id); return; }
            var command = Command(cell);
            if (command == null || !legal[cell]) return;
            if (controller.BoardCommand(command)) { hoverCell = -1; redirectedUnitId = 0; Render(); }
        }

        void HoverCell(int cell)
        { if (hoverCell == cell) return; hoverCell = cell; RefreshPreview(); }

        void RefreshPreview()
        {
            var command = CanPlay && hoverCell >= 0 && hoverCell < 64 && legal[hoverCell] ? Command(hoverCell) : null;
            var preview = command == null ? null : controller.Preview(command);
            if (board != null) board.SetBoard(Data, selectedId, action, legal, hoverCell, preview == null ? null : command);
            var selected = ownUnits.FirstOrDefault(u => u.Id == selectedId);
            if (selected == null) { Set(previewText, Loc.T("Bir birim seçerek başla.")); return; }
            if (!CanPlay) { Set(previewText, Loc.T("Savaş sonucu hazır.")); return; }
            if (selected.Kind == UnitKind.PhantomOrder && action == BoardAction.Ability && redirectedUnitId == 0)
            { Set(previewText, Loc.T("Önce niyetini yönlendireceğin AYNA birimini seç.")); return; }
            if (preview == null)
            {
                Set(previewText, hoverCell >= 0 ? Loc.T("Bu hücrede bu hamle kullanılamıyor.") : Loc.T("Önizlemek için bir hücrenin üzerine gel; uygulamak için tıkla."));
                return;
            }
            var text = new StringBuilder(Loc.F("{0} → ({1}, {2})", AynaText.Action(action), hoverCell / 8 + 1, hoverCell % 8 + 1));
            foreach (var before in Data.Units)
            {
                var after = preview.Units.FirstOrDefault(u => u.Id == before.Id);
                if (after == null) text.AppendLine().Append(Loc.F("{0} etkisiz hâle gelir.", AynaText.Unit(before.Kind)));
                else if (after.Hp < before.Hp) text.AppendLine().Append(Loc.F("{0}: −{1} CAN", AynaText.Unit(before.Kind), before.Hp - after.Hp));
                else if (after.Controller != before.Controller) text.AppendLine().Append(Loc.F("{0} ele geçirilir.", AynaText.Unit(before.Kind)));
                else if (after.StunnedUntil >= preview.Turn && action == BoardAction.Ability)
                    text.AppendLine().Append(Loc.F("{0} sersemler.", AynaText.Unit(before.Kind)));
            }
            if (Data.Phase == BoardPhase.Intents) text.AppendLine().Append(Loc.T("Niyet sıraya eklenir; rakibin cevabından sonra çözülür."));
            Set(previewText, text.ToString());
        }

        public void Undo() { if (CanPlay) { controller.UndoBoard(); hoverCell = -1; Render(); } }
        public void EndTurn() { if (CanPlay) controller.EndBoardTurn(); }
        public void Retreat() { if (CanPlay) controller.Retreat(); }

        void Update()
        {
            if (!CanPlay || AynaInterfaceMotion.ModalOpen) return;
            var keyboard = Keyboard.current;
            if (keyboard == null) return;
            var focus = EventSystem.current?.currentSelectedGameObject;
            if (focus != null && (focus.GetComponent<TMP_InputField>() != null || focus.GetComponent<TMP_Dropdown>() != null)) return;
            if (keyboard.zKey.wasPressedThisFrame) Undo();
            else if (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame) EndTurn();
            else if (keyboard.mKey.wasPressedThisFrame) SelectAction((int)BoardAction.Move);
            else if (keyboard.aKey.wasPressedThisFrame) SelectAction((int)BoardAction.Attack);
            else if (keyboard.qKey.wasPressedThisFrame) SelectAction((int)BoardAction.Ability);
            else if (keyboard.digit1Key.wasPressedThisFrame && ownUnits.Length > 0) SelectUnit(ownUnits[0].Id);
            else if (keyboard.digit2Key.wasPressedThisFrame && ownUnits.Length > 1) SelectUnit(ownUnits[1].Id);
            else if (keyboard.digit3Key.wasPressedThisFrame && ownUnits.Length > 2) SelectUnit(ownUnits[2].Id);
        }
    }
}
