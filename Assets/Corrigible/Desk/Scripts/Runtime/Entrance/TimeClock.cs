using System;
using Corrigible.Core;
using TMPro;
using UnityEngine;

namespace Corrigible.Desk
{
    /// <summary>R2 puantaj saati: kayıt kartı, 80 bitmiş vardiya deliği, iki tıklı yeni harekât ve fiziksel zımba.</summary>
    public sealed class TimeClock : MonoBehaviour
    {
        [SerializeField] DeskTarget clockTarget;
        [Header("Komutanın kartı")]
        [SerializeField] DeskTarget cardTarget;
        [SerializeField] Transform card;
        [SerializeField] TMP_Text cardSummary, cardRole, cardShelter, cardFree;
        [SerializeField] GameObject[] punches = Array.Empty<GameObject>();
        [SerializeField] Transform cardTilt;
        [SerializeField] GameObject voidTag;
        [SerializeField] TMP_Text voidText;
        [Header("Boş kartlar")]
        [SerializeField] DeskTarget stackTarget;
        [SerializeField] Transform blank;
        [SerializeField] TMP_Text newCardShift, newCardFree;
        [SerializeField] GameObject freshPunch;
        [SerializeField] Transform clip;
        [SerializeField] float clipOpenDeg = -70;
        [Header("Saat")]
        [SerializeField] Transform slotAbove, slotIn, plunger;
        [SerializeField] float plungerDrop = .012f;
        [SerializeField] PartDriver clipPart, tiltPart, plungerPart, lampPart, hourPart, minutePart;
        [SerializeField] CounterWheel[] digits = Array.Empty<CounterWheel>();

        static readonly string VoidSource = Loc.N("İPTAL EDİLİR\nV.{0:000}"),
            ArmLine = Loc.N("Kıskacı kaldır, yeniden bas: eski kart iptal edilir."),
            ConfirmLine = Loc.N("2. tık onaylar · 4 sn");
        static SiginakTheme T => SiginakTheme.Active;
        bool hasCard, armed, punching, structured;
        float armedAt, clipAngle, tilt, plungerRest, punchAt = -1;
        int shift, completed, freed, regions, minute = -1;
        GameObject newHole;
        public bool HasCard => hasCard;
        public bool Armed => armed;
        public bool Punching => punching;
        public float PunchSeconds => N("cardLiftS", .12f) + T.NumberOf("motion.cardCarryS", .4f) + T.NumberOf("motion.cardPushS", .14f) + N("stampDownS", .1f) + N("stampReturnS", .2f);
        static float N(string key, float fallback) => T.NumberOf("motion.entrance." + key, fallback);
        public event Action ClockClicked, ContinueChosen, NewChosen, Stamped;
        public event Action<bool> ArmChanged;

        void Awake()
        {
            if(plunger)plungerRest=plunger.localPosition.y;
            if(clockTarget)clockTarget.Clicked+=()=>{if(hasCard&&!punching)ClockClicked?.Invoke();};
            if(cardTarget)cardTarget.Clicked+=()=>{if(hasCard&&!punching){Disarm();ContinueChosen?.Invoke();}};
            if(stackTarget)stackTarget.Clicked+=Stack;
            if(voidTag)voidTag.SetActive(false);
            if(freshPunch)freshPunch.SetActive(false);
            if(hourPart)hourPart.Set(T.NumberOf("menu.clock.hourDeg", -240));
            DeskLoc.Instance.Changed+=Relabel;
        }
        void OnDestroy()=>DeskLoc.Instance.Changed-=Relabel;

        /// <summary>Eski tek satırlı kart bağlantısı; yeni model dört ayrı TMP yuvası kullanır.</summary>
        public void ShowCard(string summary,int shifts)
        {
            structured=false;
            hasCard=true;completed=Mathf.Max(0,shifts);shift=completed+1;
            if(card)card.gameObject.SetActive(true);
            if(cardSummary)cardSummary.text=summary;
            if(cardTarget)cardTarget.LineOverride=summary;
            for(int i=0;i<punches.Length;i++)if(punches[i])punches[i].SetActive(i<completed);
            for(int i=0;i<digits.Length;i++)if(digits[i])digits[i].Set(shift/(int)Mathf.Pow(10,2-i)%10);
            StackLine();
        }
        public void ShowCard(int completedShifts,int free,int total)
        {
            freed=free;regions=total;
            ShowCard(Loc.F("V.{0:000} · özgür {1}/{2}",completedShifts+1,free,total),completedShifts);
            structured=true;
            Relabel();
        }
        void Relabel()
        {
            if(structured&&hasCard)
            {
                if(cardRole)cardRole.text=Loc.T("KOMUTAN");
                if(cardShelter)cardShelter.text=Loc.T("SIĞINAK 07");
                if(cardSummary)cardSummary.text=Loc.F("V.{0:000}",shift);
                if(cardFree)cardFree.text=Loc.F("özgür {0}/{1}",freed,regions);
                if(cardTarget)cardTarget.LineOverride=Loc.F("V.{0:000} · özgür {1}/{2}",shift,freed,regions);
            }
            if(armed&&voidText)voidText.text=Loc.F(VoidSource,shift);
            StackLine();
        }
        void StackLine(){if(stackTarget)stackTarget.LineOverride=hasCard?Loc.T(armed?ConfirmLine:ArmLine):null;}
        public void HideCard(){hasCard=false;structured=false;if(card)card.gameObject.SetActive(false);StackLine();}
        void Stack()
        {
            if(punching)return;
            if(!hasCard||armed){Disarm();NewChosen?.Invoke();return;}
            armed=true;armedAt=Time.unscaledTime;
            if(voidText)voidText.text=Loc.F(VoidSource,shift);
            if(voidTag)voidTag.SetActive(true);
            StackLine();ArmChanged?.Invoke(true);
        }
        public void Disarm()
        {
            if(!armed)return;armed=false;if(voidTag)voidTag.SetActive(false);StackLine();ArmChanged?.Invoke(false);
        }
        public void Punch(bool fresh,Action done)
        {
            var c=fresh?blank:card;
            if(!c||!slotAbove||!slotIn){done?.Invoke();return;}
            c.gameObject.SetActive(true);
            if(fresh)
            {
                if(newCardShift)newCardShift.text=Loc.F("V.{0:000}",1);
                if(newCardFree)newCardFree.text=string.Empty;
            }
            punching=true;StartCoroutine(PunchRoutine(c,fresh,done));
        }
        System.Collections.IEnumerator PunchRoutine(Transform c,bool fresh,Action done)
        {
            Vector3 p0=c.position;Quaternion r0=c.rotation;
            float lift=N("cardLiftS",.12f),carry=T.NumberOf("motion.cardCarryS",.4f),push=T.NumberOf("motion.cardPushS",.14f);
            for(float t=0;t<lift;t+=Time.unscaledDeltaTime){c.position=p0+Vector3.up*(.012f*Ease(t/lift));yield return null;}
            var raised=c.position;
            for(float t=0;t<carry;t+=Time.unscaledDeltaTime)
            {
                float k=Ease(t/carry);
                c.SetPositionAndRotation(Vector3.Lerp(raised,slotAbove.position,k)+Vector3.up*Mathf.Sin(k*Mathf.PI)*T.NumberOf("menu.clock.carryArcM",.08f),Quaternion.Slerp(r0,slotAbove.rotation,k));yield return null;
            }
            for(float t=0;t<push;t+=Time.unscaledDeltaTime){c.SetPositionAndRotation(Vector3.Lerp(slotAbove.position,slotIn.position,t/push),slotIn.rotation);yield return null;}
            c.SetPositionAndRotation(slotIn.position,slotIn.rotation);
            float down=N("stampDownS",.1f),up=N("stampReturnS",.2f);
            for(float t=0;t<down;t+=Time.unscaledDeltaTime){SetPlunger(t/down);yield return null;}
            SetPlunger(1);Stamped?.Invoke();punchAt=Time.unscaledTime;
            newHole=fresh?freshPunch:completed<punches.Length?punches[completed]:null;
            if(newHole){newHole.transform.localScale=Vector3.zero;newHole.SetActive(true);}
            for(float t=0;t<up;t+=Time.unscaledDeltaTime){SetPlunger(1-t/up);yield return null;}
            SetPlunger(0);punching=false;done?.Invoke();
        }
        void SetPlunger(float k)
        {
            if(plungerPart){plungerPart.Set(-plungerDrop*Mathf.Clamp01(k));return;}
            if(plunger){var p=plunger.localPosition;p.y=plungerRest-plungerDrop*Mathf.Clamp01(k);plunger.localPosition=p;}
        }
        void Update()
        {
            if(armed&&Time.unscaledTime-armedAt>T.NumberOf("motion.coverAutoCloseS",4))Disarm();
            float dt=Time.unscaledDeltaTime;
            clipAngle=Mathf.MoveTowards(clipAngle,armed?clipOpenDeg:0,Mathf.Abs(clipOpenDeg)/N("clipS",.15f)*dt);
            if(clipPart)clipPart.Set(clipAngle);else if(clip)clip.localRotation=Quaternion.Euler(clipAngle,0,0);
            tilt=Mathf.MoveTowards(tilt,armed?1:0,dt/N("cardTiltS",.2f));
            if(tiltPart)tiltPart.Set(-14*tilt);else if(cardTilt)cardTilt.localRotation=Quaternion.Euler(-14*tilt,0,0);
            if(lampPart)lampPart.SetSwap(punchAt>=0&&Time.unscaledTime-punchAt<N("clockLampS",.08f)?1:0);
            if(newHole)newHole.transform.localScale=Vector3.one*Mathf.Clamp01((Time.unscaledTime-punchAt)/N("punchHoleS",.3f));
            int m=(int)(Time.unscaledTime/60);
            if(minutePart&&minute!=m){minute=m;minutePart.Set(-Mathf.Repeat(T.NumberOf("menu.clock.minuteDeg",-102)*-1+minute*6,360));}
        }
        static float Ease(float t)=>t<.5f?2*t*t:1-Mathf.Pow(-2*t+2,2)/2;
    }
}
