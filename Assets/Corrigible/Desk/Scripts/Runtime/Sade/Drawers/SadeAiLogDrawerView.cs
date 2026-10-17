using System;
using Corrigible.Core;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Desk
{
    /// <summary>Ele geçirilmiş döküm. Karartma gerçek veriye erişmeden VM'deki açık yer tutucuya çizilir.</summary>
    public sealed class SadeAiLogDrawerView : MonoBehaviour
    {
        [Serializable] public sealed class EntrySlot
        {
            public RectTransform Root, Chip, Redaction;
            public TMP_Text Stamp, Text, Note, ChipText, NewBadgeText;
            public GameObject NewBadge, NewBackground;
            public Image ChipIcon;
            public SadeDrawerCommandView Link;
            public Image Divider;
        }
        [SerializeField] TMP_Text source, banner, warningText;
        [SerializeField] EntrySlot[] entries=Array.Empty<EntrySlot>();
        [SerializeField] RectTransform content, warning;
        public void Bind(JObject data,SadeDrawerFolderView owner)
        {
            if(data==null)return;
            SadeDrawerData.Text(source,SadeDrawerData.T(SadeDrawerData.S(data,"source")));
            SadeDrawerData.Text(banner,SadeDrawerData.T(Loc.N("AYNA · ELE GEÇİRİLEN KAYIT")));
            var rows=data["entries"] as JArray;float y=0;
            for(int i=0;i<entries.Length;i++)
            {
                var s=entries[i];var row=rows!=null&&i<rows.Count?rows[i]:null;DeskUi.SetActive(s.Root,row!=null);if(row==null)continue;
                SadeDrawerData.Text(s.Stamp,SadeDrawerData.S(row,"stamp"));SadeDrawerData.Text(s.NewBadgeText,SadeDrawerData.T(Loc.N("YENİ")));bool isNew=SadeDrawerData.B(row,"new");DeskUi.SetActive(s.NewBadge,isNew);DeskUi.SetActive(s.NewBackground,isNew);
                string text=SadeDrawerData.T(SadeDrawerData.S(row,"text"));int index=text.IndexOf("[[KARARTI]]",StringComparison.Ordinal);bool redacted=index>=0;
                string before=redacted?text.Substring(0,index):"";
                if(redacted)text=text.Replace("[[KARARTI]]","          ");
                float th=SadeDrawerData.Wrapped(s.Text,text,448,18);SadeUi.Place(s.Text,0,34,448,th);
                DeskUi.SetActive(s.Redaction,redacted);
                if(redacted)
                {
                    // TMP aynı fontun gerçek metrikleriyle siyah kutunun yerini verir; metin gövdesi okunabilir kalır.
                    var prefix=s.Text.GetPreferredValues(before);float x=prefix.x%448;SadeUi.Place(s.Redaction,x,35+Mathf.Floor(prefix.x/448)*17.2f,62,13);
                }
                string note=SadeDrawerData.S(row,"analystNote");bool hasNote=!string.IsNullOrEmpty(note);DeskUi.SetActive(s.Note,hasNote);
                float nh=hasNote?SadeDrawerData.Wrapped(s.Note,SadeDrawerData.T(note),420,24):0;
                if(hasNote)SadeUi.Place(s.Note,30,36+th,418,nh);
                float h=34+th+(hasNote?nh+8:0)+10;
                SadeUi.Place(s.Root,0,y,448,h);SadeUi.Place(s.Divider,0,h-1,448,1);y+=h;
                string region=SadeDrawerData.S(row,"region"),kind=SadeDrawerData.S(row["link"],"kind");
                string chip=!string.IsNullOrEmpty(region)?SadeDrawerData.Region(region):SadeDrawerData.T(kind=="news"?Loc.N("HABER · SAHTE"):Loc.N("ÇEKİRDEK"));
                SadeDrawerData.Text(s.ChipText,chip);float w=s.ChipText.GetPreferredValues(chip).x+28;SadeUi.Place(s.Chip,448-w,9,w,20);SadeUi.Place(s.ChipText,20,3,w-23,14);
                SadeUi.Sprite(s.ChipIcon,!string.IsNullOrEmpty(region)?"dot_12":kind=="news"?"ic_news_s":"ic_chip_s");
                s.ChipIcon.color=!string.IsNullOrEmpty(region)?SadeNewsDrawerView.RegionColor(region):ColorOf(kind=="core"?"#9A1E14":"#463F34");
                s.Link?.Bind(owner,kind=="news"?"logNews":null,SadeDrawerData.S(row["link"],"id"),region);
            }
            bool showWarning=SadeDrawerData.B(data,"deceptionWarning");DeskUi.SetActive(warning,showWarning);
            if(showWarning)
            {
                string w=SadeDrawerData.T(Loc.N("<b>AYNA aldatma profilinde.</b> Bu satırlar da yalan olabilir; sahada doğrula."));
                float h=Mathf.Max(54,SadeDrawerData.Wrapped(warningText,w,404,34)+20);SadeUi.Place(warning,0,y+12,448,h);y+=h+24;
            }
            SadeDrawerData.Size(content,y+12);
        }
        static Color ColorOf(string hex){ColorUtility.TryParseHtmlString(hex,out var c);return c;}
    }
}
