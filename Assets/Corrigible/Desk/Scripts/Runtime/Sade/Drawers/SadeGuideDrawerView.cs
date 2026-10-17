using System;
using Corrigible.Core;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Desk
{
    /// <summary>Rehberin konu menüsü ve kaydırılabilir sayfası. İlk sayfa paketin güven dili örneğidir.</summary>
    public sealed class SadeGuideDrawerView : MonoBehaviour
    {
        [Serializable] public sealed class TopicSlot { public Image Selected, Icon; public TMP_Text Title; public SadeDrawerCommandView Action; }
        [Serializable] public sealed class RowSlot { public RectTransform Root; public Image Mark; public TMP_Text Question, Title, Body; }
        [SerializeField] TopicSlot[] topics=Array.Empty<TopicSlot>();
        [SerializeField] RowSlot[] rows=Array.Empty<RowSlot>();
        [SerializeField] TMP_Text heading, subtitle, exampleTitle, exampleBody, exampleValue, pageNumber;
        [SerializeField] RectTransform content, example;
        [SerializeField] DeskButton previous, next;
        [SerializeField] SadeDrawerCommandView previousAction,nextAction;
        public void Bind(JObject data,SadeDrawerFolderView owner)
        {
            if(data==null)return;
            var menu=data["topics"] as JArray;var page=data["page"];string id=SadeDrawerData.S(page,"id","trust");int index=SadeDrawerData.I(page,"index",1),count=SadeDrawerData.I(page,"count",6);
            string name="";
            for(int i=0;i<topics.Length;i++)
            {
                var topic=menu!=null&&i<menu.Count?menu[i]:null;var s=topics[i];DeskUi.SetActive(s.Title,topic!=null);if(topic==null)continue;
                string tid=SadeDrawerData.S(topic,"id");bool current=tid==id;
                DeskUi.SetActive(s.Selected,current);SadeDrawerData.Text(s.Title,SadeDrawerData.T(SadeDrawerData.S(topic,"title")));
                if(current)name=s.Title.text;
                SadeUi.Sprite(s.Icon,"ic_"+SadeDrawerData.S(topic,"icon")+"_l");s.Action?.Bind(owner,"guideTopic",tid);
            }
            SadeDrawerData.Text(heading,DeskText.Upper(name));
            bool trust=id=="trust";
            string sub=trust?Loc.N("Her sayının yanındaki işaret, o sayıya ne kadar güvenebileceğini söyler."):Description(id);
            float y=26+SadeDrawerData.Wrapped(subtitle,SadeDrawerData.T(sub),292,35)+6;
            SadeUi.Place(subtitle,0,26,292,y-32);
            string[] states={"verified","report","suspicious","unknown","fake"};
            var help=HelpRows(id);
            for(int i=0;i<rows.Length;i++)
            {
                var s=rows[i];bool on=trust?i<states.Length:i<help.Length;DeskUi.SetActive(s.Root,on);if(!on)continue;
                string state=trust?states[i]:null;
                string rowTitle=trust?Title(state):help[i].Title;
                string rowBody=trust?Body(state):help[i].Body;
                SadeDrawerData.Text(s.Title,SadeDrawerData.T(rowTitle));
                float body=SadeDrawerData.Wrapped(s.Body,SadeDrawerData.T(rowBody),248,17);float h=Mathf.Max(54,27+body+8);
                SadeUi.Place(s.Root,0,y,292,h);SadeUi.Place(s.Body,44,27,248,body);y+=h;
                s.Title.color=ColorOf(state=="verified"?"#1A4789":state=="report"?"#524973":state=="suspicious"?"#854005":state=="fake"?"#9A1E14":"#4A4640");
                string icon=trust?(state=="verified"?"node_badge_seal_20":state=="report"?"legend_report":state=="suspicious"?"mark_suspicious":state=="fake"?"mark_fake_24":"ring_dash_24"):help[i].Icon;
                SadeUi.Sprite(s.Mark,icon);s.Mark.color=!trust||state=="unknown"?ColorOf("#4A4640"):Color.white;
                DeskUi.SetActive(s.Question,state=="suspicious"||state=="unknown");SadeDrawerData.Text(s.Question,"?");
            }
            DeskUi.SetActive(example,trust);
            if(trust)
            {
                var ex=page?["example"];string region=SadeDrawerData.S(ex,"region","DoA");
                SadeDrawerData.Text(exampleTitle,Loc.F("Örnek · {0}",SadeDrawerData.T("region."+region)));
                string text=Loc.F("Rapor {0} diyor, ekip {1} ölçtü. Halka ve soru işareti bu yüzden var.",SadeDrawerData.I(ex,"report",38),SadeDrawerData.I(ex,"field",52));
                float h=Mathf.Max(73,31+SadeDrawerData.Wrapped(exampleBody,text,216,34)+10);SadeUi.Place(example,0,y+12,292,h);SadeUi.Place(exampleBody,64,30,216,h-40);
                SadeDrawerData.Text(exampleValue,SadeDrawerData.I(ex,"report",38).ToString());y+=h+24;
            }
            SadeDrawerData.Size(content,Mathf.Max(452,y));
            SadeDrawerData.Text(pageNumber,Loc.F("{0} / {1}",index,count));
            previous?.SetLabel(SadeDrawerData.T(Loc.N("‹ önceki")));next?.SetLabel(SadeDrawerData.T(Loc.N("sonraki ›")));
            if(previous!=null)previous.interactable=index>1;if(next!=null)next.interactable=index<count;
            previousAction?.Bind(owner,index>1?"guidePage":null,(index-1).ToString());nextAction?.Bind(owner,index<count?"guidePage":null,(index+1).ToString());
        }
        public static string Title(string state)=>state switch {"verified"=>Loc.N("ONAYLANDI"),"report"=>Loc.N("RAPOR"),"suspicious"=>Loc.N("ŞÜPHELİ"),"fake"=>Loc.N("SAHTE"),_=>Loc.N("BİLİNMİYOR")};
        public static string Body(string state)=>state switch
        {
            "verified"=>Loc.N("Ekip ya da denetim doğruladı. Güvenebilirsin."),"report"=>Loc.N("Resmî kaynak, doğrulanmadı. AYNA değiştirmiş olabilir."),"suspicious"=>Loc.N("Kaynaklar çelişiyor. Önce denetim iste."),"fake"=>Loc.N("Yalan çıktı; hesaba katılmaz."),_=>Loc.N("Veri yok. İzleme ağı kur ya da ekip gönder.")
        };
        readonly struct HelpRow
        {
            public readonly string Title,Body,Icon;
            public HelpRow(string title,string body,string icon){Title=title;Body=body;Icon=icon;}
        }
        // İlk sayfa çizimden, diğer konu metinleri C rehberinin eylem ve kısayol açıklamalarından uyarlanır.
        static readonly HelpRow[] ColorRows=
        {
            new HelpRow(Loc.N("SIZMA"),Loc.N("Haritada koyulaşan kırmızı, daha yüksek sızmayı gösterir. Değeri bölge kartında incele."),"ic_globe_l"),
            new HelpRow(Loc.N("GÜVEN İŞARETİ"),Loc.N("Mavi damga ölçülmüş değeri, karbon kopya yalnızca raporu, turuncu kalem halkası şüpheli kaynağı gösterir."),"ic_check_l"),
            new HelpRow(Loc.N("KARANTİNA VE SİS"),Loc.N("Yeşil tarama karantinayı, sis taraması veri olmadığını gösterir. Renk tek başına bilgi taşımaz."),"ic_legend_l"),
        };
        static readonly HelpRow[] ActionRows=
        {
            new HelpRow(Loc.N("İZLEME AĞI · 1"),Loc.N("İzleme ağı kaynağı kalıcı olarak doğrular. Maliyetini sarı önizleme notunda kontrol et."),"ic_antenna_l"),
            new HelpRow(Loc.N("DENETİM · 2"),Loc.N("Denetim sızmayı azaltır ve tek seferlik ölçüm verir. Etki ve bedel, eylemin üstüne gelince görünür."),"ic_search_l"),
            new HelpRow(Loc.N("KARANTİNA · 3"),Loc.N("Karantina ağ üzerinden yayılmayı sınırlar. Kararı vermeden önce ekonomi ve güven bedelini kontrol et."),"ic_lock_l"),
            new HelpRow(Loc.N("SON ŞALTER · 4"),Loc.N("Bölgeyi çevrimdışı bırakır. Son Şalter onay ister; önizleme sonuçlarını gösterir."),"ic_kill_l"),
        };
        static readonly HelpRow[] TeamRows=
        {
            new HelpRow(Loc.N("GÖREVLENDİR"),Loc.N("Ekipler çekmecesinde Görevlendir'i seç, ardından haritada hedef bölgeye tıkla."),"ic_teams_l"),
            new HelpRow(Loc.N("İZLEME VE DENETİM"),Loc.N("İzleme ve denetim için ekip gönderilir; taktik baskın açılır."),"ic_scope_l"),
            new HelpRow(Loc.N("BASKIN · B"),Loc.N("Hazır ekibin kartındaki Baskın düğmesi saha operasyonunu açar. Başarı bilgisi kartta görünür."),"ic_goal_l"),
            new HelpRow(Loc.N("YOLDAKİ EKİP"),Loc.N("Kesik rota, hedef şehir ve kalan gün sayısı ekibin yolculuğunu gösterir."),"ic_clock_l"),
        };
        static readonly HelpRow[] SwitchRows=
        {
            new HelpRow(Loc.N("BÖLGE SON ŞALTERİ"),Loc.N("Bölge kartında 4 tuşu veya Son Şalter eylemi onay penceresini açar."),"ic_kill_l"),
            new HelpRow(Loc.N("KÜRESEL SON ŞALTER"),Loc.N("Sağ alttaki zarfın mührünü 3 saniye basılı tut. Onay penceresinde kararını gözden geçir."),"ic_goal_l"),
            new HelpRow(Loc.N("ÇEKİRDEK VE KOPYALAR"),Loc.N("Çekirdek yok edildikten sonra kalan kopyaları da temizlemek gerekir. Silme hedefi temiz günleri izler."),"ic_chip_l"),
        };
        static readonly HelpRow[] KeyRows=
        {
            new HelpRow(Loc.N("ÇEKMECELER"),Loc.N("R araştırma · N haberler · T saha ekipleri · L YZ günlüğü · H rehber."),"ic_book_l"),
            new HelpRow(Loc.N("BÖLGE EYLEMLERİ"),Loc.N("Bölge kartında 1–4 eylemleri uygular; B hazır ekibin baskınını açar."),"ic_scope_l"),
            new HelpRow(Loc.N("ZAMAN"),Loc.N("Boşluk duraklatır veya sürdürür. + ve − zaman hızını değiştirir."),"ic_clock_l"),
            new HelpRow(Loc.N("BÖLGE SEÇİMİ"),Loc.N("Tab sonraki, Shift+Tab önceki bölgeyi seçer ve kartını açar."),"ic_arrow_l"),
            new HelpRow(Loc.N("KAPATMA"),Loc.N("Esc en üstteki pencereyi kapatır. Açık panel yoksa duraklatma menüsünü açar."),"ic_dots_l"),
        };
        static HelpRow[] HelpRows(string id)=>id switch{"colors"=>ColorRows,"actions"=>ActionRows,"teams"=>TeamRows,"killSwitch"=>SwitchRows,"keys"=>KeyRows,_=>Array.Empty<HelpRow>()};
        static string Description(string id)=>id switch
        {
            "colors"=>Loc.N("Sızma renkleri tehdit düzeyini gösterir. Güven işaretini de kontrol et; renk tek başına bilgi taşımaz."),
            "actions"=>Loc.N("Dosyada 1–4 tuşları bölge eylemlerini uygular. Üstüne geldiğiniz eylemin etkisi sarı ÖNİZLEME notunda görünür. Son Şalter onay ister."),
            "teams"=>Loc.N("İzleme ve denetim için ekip gönderilir; taktik baskın açılır."),
            "killSwitch"=>Loc.N("Son Şalter onay ister. Küresel Son Şalter için zarfın mührünü 3 saniye basılı tut."),
            _=>Loc.N("R araştırma · N haberler · T saha ekipleri · L YZ günlüğü · H rehber · Boşluk duraklat · +/− hız · Esc en üstteki pencereyi kapatır.")
        };
        static Color ColorOf(string hex){ColorUtility.TryParseHtmlString(hex,out var c);return c;}
    }
}
