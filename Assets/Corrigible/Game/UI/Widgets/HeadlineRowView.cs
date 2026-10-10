using System;
using Corrigible.Core;
using TMPro;
using UnityEngine;

namespace Corrigible.Game.UI
{
    /// <summary>Haber akışında bir başlık (HeadlineRow prefab'ı): gün, metin, ajans, karar ya da Doğrula düğmesi.</summary>
    public sealed class HeadlineRowView : MonoBehaviour
    {
        [SerializeField] TMP_Text day;
        [SerializeField] TMP_Text text;
        [SerializeField] TMP_Text agency;
        [SerializeField] TMP_Text verdict;
        [SerializeField] StyledButton verify;

        public event Action<string> VerifyClicked;
        string headlineId;

        void Awake() => UiUtil.OnClick(verify.Button, () => VerifyClicked?.Invoke(headlineId));

        public void Set(Headline h, bool canVerify, bool revealed, bool running, UiTheme theme)
        {
            gameObject.SetActive(h != null);
            if (h == null) return;
            headlineId = h.Id;
            day.text = Loc.F("Gün {0}", h.Day);
            text.text = h.Text;
            bool suspect = h.Suspect && h.Verified == null && running;
            agency.text = Loc.T(h.Agency) + (h.Src != null ? " · " + GameData.Region(h.Src).Name : "") + (suspect ? Loc.T(" · şüpheli kaynak") : "");
            agency.color = suspect ? theme.warn : theme.muted;
            string v = null; Color vc = theme.threat;
            if (revealed && h.Fake) v = Loc.T("SAHTEYDİ");
            else if (h.Verified == "true") { v = Loc.T("DOĞRU"); vc = theme.good; }
            else if (h.Verified == "fake") v = Loc.T("SAHTE");
            else if (h.Verified == "unknown") { v = Loc.T("DOĞRULANAMADI"); vc = theme.warn; }
            UiUtil.Show(verdict, v != null);
            verdict.text = v ?? "";
            verdict.color = vc;
            UiUtil.Show(verify, v == null && canVerify);
        }
    }
}
