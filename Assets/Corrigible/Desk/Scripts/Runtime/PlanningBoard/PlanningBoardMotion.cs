using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Corrigible.Core;

namespace Corrigible.Desk
{
    /// <summary>
    /// Defter ve yedek elinin yalnız sunum animasyonları; Core durumunu değiştirmez. Revizyon 3 · Aşama 5'ten sonra geçici uGUI
    /// defteri (D6) sahneden kalktı: defter alanları boş (null) olabilir, o zaman defter kısmı sessizce hiçbir şey yapmaz (3B defter
    /// <c>RecipeBookView</c>'dir). Yedek eli kodu Aşama 10 temizliğine dek durur.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public sealed class PlanningBoardMotion : MonoBehaviour
    {
        [SerializeField] PlanningBoardView view;
        [SerializeField] Button bookButton, bookClose, previous, next;
        [SerializeField] RectTransform bookRoot, cover, leftPage, rightPage, detailsRoot;
        [SerializeField] CanvasGroup bookGroup;
        [SerializeField] TMP_Text pageNumber;
        [SerializeField] Vector2 bookHome = new Vector2(-680,-490), bookFocus = new Vector2(0,-30);
        [SerializeField] float duration = .32f, reserveStagger = .07f;
        float reserveTransitionAt;
        readonly Dictionary<PlanningBoardCard,Vector2> starts = new Dictionary<PlanningBoardCard,Vector2>();
        readonly Dictionary<PlanningBoardCard,Vector3> startScales = new Dictionary<PlanningBoardCard,Vector3>();
        readonly Dictionary<PlanningBoardCard,int> transitionOrder = new Dictionary<PlanningBoardCard,int>();
        readonly List<PlanningBoardCard> reserve = new List<PlanningBoardCard>();
        readonly Dictionary<PlanningBoardCard, Vector2> targets = new Dictionary<PlanningBoardCard, Vector2>();
        readonly Dictionary<PlanningBoardCard, PlanningBoardCard> owners = new Dictionary<PlanningBoardCard, PlanningBoardCard>();
        public void BeginEquipment()=>owners.Clear();
        public void EquipmentOwner(PlanningBoardCard item,PlanningBoardCard person)=>owners[item]=person;
        float bookProgress;
        bool bookOpen, handOpen, focused, editable;
        int page;
        public bool ReserveOpen => handOpen;
        public bool BookOpen => bookOpen;
        public int Page => page;
        public RectTransform BookRoot => bookRoot;
        public void Focus(bool on) { focused=on; if(!on){bookOpen=false;CloseReserve();} }
        /// <summary>Geçici defterin parçaları sahnede bağlı mı (Aşama 5'ten sonra değil).</summary>
        bool HasBook => bookRoot != null && bookGroup != null && cover != null;
        void OnEnable()
        {
            if(bookButton)bookButton.onClick.AddListener(ToggleBook);if(bookClose)bookClose.onClick.AddListener(CloseBook);
            if(previous)previous.onClick.AddListener(Previous);if(next)next.onClick.AddListener(Next);
        }
        void OnDisable()
        {
            if(bookButton)bookButton.onClick.RemoveListener(ToggleBook);if(bookClose)bookClose.onClick.RemoveListener(CloseBook);
            if(previous)previous.onClick.RemoveListener(Previous);if(next)next.onClick.RemoveListener(Next);
            bookOpen=handOpen=false;targets.Clear();reserve.Clear();starts.Clear();startScales.Clear();transitionOrder.Clear();
        }
        public void ToggleBook(){if(!focused||!HasBook)return;bookOpen=!bookOpen;CloseReserve();RefreshPages();}
        public void CloseBook(){bookOpen=false;if(bookGroup)bookGroup.blocksRaycasts=false;}
        public void Previous(){if(PlanningBoardCard.Dragging==null){page=Mathf.Max(0,page-1);RefreshPages();}}
        public void Next(){if(PlanningBoardCard.Dragging==null&&view!=null){page=Mathf.Min(Mathf.Max(0,(RecipeCount-1)/2),page+1);RefreshPages();}}
        int RecipeCount=>view!=null&&view.Recipes!=null?view.Recipes.Length:0;
        public void ToggleReserve(){if(!focused)return;SetReserveOpen(!handOpen);bookOpen=false;}

        void SetReserveOpen(bool open)
        {
            if(handOpen==open)return;
            handOpen=open;reserveTransitionAt=Time.unscaledTime;
            starts.Clear();startScales.Clear();transitionOrder.Clear();
            int index=0;
            foreach(var card in reserve)
            {
                if(card.IsDragging)continue;
                starts[card]=view.PlanePosition(card.transform);
                startScales[card]=card.transform.localScale;
                transitionOrder[card]=index++;
            }
            // Açılma üstten, toplanma destenin en altından başlar.
            if(!open)foreach(var card in starts.Keys)transitionOrder[card]=index-1-transitionOrder[card];
        }

        public void CloseReserve(){SetReserveOpen(false);}
        public void BeginDrag(PlanningBoardCard card)
        {if(card.Kind==PlanningCardKind.Person){CloseReserve();}if(card.Kind==PlanningCardKind.Recipe){bookOpen=false;if(bookGroup)bookGroup.blocksRaycasts=false;}}
        public void BeginReserve(bool canEdit){reserve.Clear();editable=canEdit;}
        public void AcceptReserveDrop(PlanningBoardCard card)
        {
            // Başarılı bırakma eski göreve dönmeden, bırakılan noktadan desteye yerleşir.
            card.KeepPosition(view.PlanePosition(card.transform));
            targets[card]=ClosedPosition(0);
            starts.Remove(card);startScales.Remove(card);transitionOrder.Remove(card);
        }
        public void PlaceReserve(PlanningBoardCard card,int index,int count)
        {
            reserve.Add(card);
            if(card.IsDragging)return;
            if(!targets.ContainsKey(card))
            {
                card.transform.SetParent(view.DragPlane,true);
                ((RectTransform)card.transform).anchoredPosition=ClosedPosition(index);
            }
            targets[card]=ClosedPosition(index);
        }
        Vector2 ClosedPosition(int i)=>view.PlanePosition(view.JobZone("rest").transform)+new Vector2(i%3*7,-10-i%3*6);
        public void PlaceRecipe(PlanningBoardCard card,int index)
        {
            if(card==null||card.IsDragging)return;
            bool shown=bookOpen&&index/2==page;
            card.gameObject.SetActive(shown);
            if(!shown)return;
            var parent=index%2==0?leftPage:rightPage;
            if(parent==null){card.gameObject.SetActive(false);return;}
            card.transform.SetParent(parent,false);
            var r=(RectTransform)card.transform;r.anchorMin=r.anchorMax=r.pivot=new Vector2(.5f,.5f);r.anchoredPosition=Vector2.zero;r.localScale=Vector3.one;
        }
        void RefreshPages(){if(view==null||view.Recipes==null)return;for(int i=0;i<view.Recipes.Length;i++)PlaceRecipe(view.Recipes[i],i);}
        void LateUpdate()
        {
            LateUpdateBook();
            if(view==null||reserve.Count==0)return;
            float deckX=view.PlanePosition(view.JobZone("rest").transform).x;
            float scale=Mathf.Min(1,(deckX+780f)/Mathf.Max(1,reserve.Count-1)/230f);
            for(int i=0;i<reserve.Count;i++)
            {
                var card=reserve[i];if(!card||card.IsDragging||!card.gameObject.activeSelf)continue;
                var rect=(RectTransform)card.transform;
                if(rect.parent!=view.DragPlane)rect.SetParent(view.DragPlane,true);
                
                var target=handOpen?new Vector2(deckX-(reserve.Count-1-i)*230*scale,285):ClosedPosition(i);
                bool transitioning=starts.TryGetValue(card,out var from);
                float progress=transitioning?Mathf.Clamp01((Time.unscaledTime-reserveTransitionAt-transitionOrder[card]*reserveStagger)/Mathf.Max(.05f,duration)):1;
                float eased=Mathf.SmoothStep(0,1,progress);
                if(transitioning)
                {
                    rect.anchoredPosition=Vector2.Lerp(from,target,eased);
                    rect.localScale=Vector3.Lerp(startScales[card],Vector3.one*(handOpen?scale:1),eased);
                    if(!handOpen&&progress>=1){starts.Remove(card);startScales.Remove(card);transitionOrder.Remove(card);}
                }
                else
                {
                    float settle=1-Mathf.Exp(-Time.unscaledDeltaTime*15);
                    rect.anchoredPosition=Vector2.Lerp(rect.anchoredPosition,target,settle);
                    rect.localScale=Vector3.Lerp(rect.localScale,Vector3.one*(handOpen?scale:1),settle);
                }
                var group=card.GetComponent<CanvasGroup>();group.alpha=handOpen||progress<1||i<3?1:0;
                group.blocksRaycasts=focused&&(handOpen?progress>.95f:i==0&&progress>=1);card.Draggable=focused&&editable&&(handOpen?progress>.95f:i==0&&progress>=1);
                if(handOpen||progress<1)rect.SetAsLastSibling();
            }
        }
        /// <summary>Geçici defterin animasyonu; parçalar bağlı değilse (Aşama 5 sonrası sahne) hiçbir şey yapmaz.</summary>
        void LateUpdateBook()
        {
            if(!HasBook){bookOpen=false;bookProgress=0;return;}
            float step=Time.unscaledDeltaTime/Mathf.Max(.05f,duration);
            bookProgress=Mathf.MoveTowards(bookProgress,bookOpen?1:0,step);
            float b=Mathf.SmoothStep(0,1,bookProgress);
            bookRoot.gameObject.SetActive(bookProgress>0||bookOpen);bookRoot.anchoredPosition=Vector2.Lerp(bookHome,bookFocus,b);bookRoot.localScale=Vector3.one*Mathf.Lerp(.28f,1,b);
            bookGroup.alpha=b;bookGroup.blocksRaycasts=bookOpen;bookGroup.interactable=bookOpen;
            cover.gameObject.SetActive(b<.82f);
            cover.localRotation=Quaternion.Euler(0,Mathf.Lerp(0,-165,b),0);
            int pages=Mathf.Max(0,(RecipeCount-1)/2);
            if(bookButton){bookButton.gameObject.SetActive(bookProgress<=0);bookButton.interactable=focused&&!bookOpen;}
            if(previous)previous.interactable=page>0;
            if(next)next.interactable=page<pages;
            if(pageNumber)pageNumber.text=Loc.F("SAYFA {0} / {1}",page+1,(RecipeCount+1)/2);
        }
        public void RaiseBook()
        {
            // Listenin ilk kişisi görünen ve tutulabilen en üst karttır.
            for(int i=reserve.Count-1;i>=0;i--)
            {
                var person=reserve[i];if(!person.IsDragging)person.transform.SetAsLastSibling();
                foreach(var pair in owners)if(pair.Value==person&&!pair.Key.IsDragging)pair.Key.transform.SetAsLastSibling();
            }
            var held=PlanningBoardCard.Dragging;
            if(held){held.transform.SetAsLastSibling();foreach(var pair in owners)if(pair.Value==held)pair.Key.transform.SetAsLastSibling();}
            if(detailsRoot && detailsRoot.gameObject.activeSelf)detailsRoot.SetAsLastSibling();
            if(bookOpen&&bookRoot)bookRoot.SetAsLastSibling();
        }
        public bool ContainsReserve(PlanningBoardCard card)=>reserve.Contains(card);
    }
}
