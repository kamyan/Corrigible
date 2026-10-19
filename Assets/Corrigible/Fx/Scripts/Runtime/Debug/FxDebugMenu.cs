using Corrigible.Desk;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
namespace Corrigible.Fx
{
    /// <summary>Yalnız örnek FX sahnelerinin geliştirme kontrolleri. Canlı oyun veya kayıt komutu göndermez.</summary>
    [DefaultExecutionOrder(80)]
    public sealed class FxDebugMenu : FxTimedBehaviour
    {
        public const string ReducedKey="corrigible.fx.reduceMotion";
        [SerializeField] public FxDeskInteraction desk;
        [SerializeField] public FxRaidHud raid;
        [SerializeField] public FxOrbLab lab;
        [SerializeField] public FxReferenceOverlay reference;
        [SerializeField] public GameObject controls;
        [SerializeField] public Toggle reducedToggle,freezeToggle,referenceToggle;
        [SerializeField] public Button languageButton;
        static FxDebugMenu owner;static int pendingScreen;static string pendingMode;
        bool lastReduced;int shownScreen=-1;string shownPrefix;
        public static bool FunctionKeysOwned=>owner!=null&&owner.isActiveAndEnabled;
        public bool ControlsVisible=>controls.activeSelf;
        protected override void OnEnable()
        {
            base.OnEnable();owner=this;controls.SetActive(false);clock.Reduced=PlayerPrefs.GetInt(ReducedKey,clock.Reduced?1:0)!=0;lastReduced=clock.Reduced;
            reducedToggle.SetIsOnWithoutNotify(clock.Reduced);reducedToggle.onValueChanged.AddListener(SetReduced);freezeToggle.onValueChanged.AddListener(SetFrozen);referenceToggle.onValueChanged.AddListener(ShowReference);languageButton.onClick.AddListener(Language);
#if !UNITY_EDITOR && !DEVELOPMENT_BUILD
            enabled=false;
#endif
        }
        protected override void OnDisable()
        {
            if(owner==this)owner=null;reducedToggle.onValueChanged.RemoveListener(SetReduced);freezeToggle.onValueChanged.RemoveListener(SetFrozen);referenceToggle.onValueChanged.RemoveListener(ShowReference);languageButton.onClick.RemoveListener(Language);base.OnDisable();
        }
        void Start(){if(pendingScreen>0){int number=pendingScreen;pendingScreen=0;Load(pendingMode,number);}RefreshReference();}
        public void SetReduced(bool value)
        {
            clock.Reduced=value;lastReduced=value;PlayerPrefs.SetInt(ReducedKey,value?1:0);PlayerPrefs.Save();reducedToggle.SetIsOnWithoutNotify(value);
            if(lab!=null)lab.reducedToggle.SetIsOnWithoutNotify(value);reference.Tick(clock);
        }
        public void SetFrozen(bool value){if(value)clock.Freeze();else clock.Resume();freezeToggle.SetIsOnWithoutNotify(value);if(lab!=null)lab.freezeToggle.SetIsOnWithoutNotify(value);}
        public void ShowReference(bool value){RefreshReference();reference.SetVisible(value);referenceToggle.SetIsOnWithoutNotify(reference.Visible);}
        public void ToggleControls(){freezeToggle.SetIsOnWithoutNotify(clock.Frozen);reducedToggle.SetIsOnWithoutNotify(clock.Reduced);referenceToggle.SetIsOnWithoutNotify(reference.Visible);controls.SetActive(!controls.activeSelf);}
        public void Language(){DeskLoc.Instance.Toggle();if(raid!=null)raid.source.RefreshPresentation();}
        public void Load(string mode,int number)
        {
            if(mode=="desk"&&desk!=null){desk.LoadSample(number);RefreshReference();return;}
            if(mode=="raid"&&raid!=null){raid.LoadScreen(number);RefreshReference();return;}
            if(mode=="lab"&&lab!=null){RefreshReference();return;}
            pendingScreen=number;pendingMode=mode;
            string path=mode=="desk"?"Assets/Corrigible/Fx/Scenes/FxDesk_Sandbox.unity":mode=="raid"?"Assets/Corrigible/Fx/Scenes/FxRaidHud_Sandbox.unity":"Assets/Corrigible/Fx/Scenes/FxOrbLab.unity";
#if UNITY_EDITOR
            UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(path,new LoadSceneParameters(LoadSceneMode.Single));
#else
            if(Application.CanStreamedLevelBeLoaded(path))SceneManager.LoadScene(path);
#endif
        }
        public void RefreshReference()
        {
            int number=desk!=null?desk.source.ActiveSample:raid!=null?Mathf.Max(1,raid.Screen):0;string prefix=desk!=null?"F-Masa":raid!=null?"F-Baskin":"F-YZ";
            if(number==shownScreen&&prefix==shownPrefix)return;shownScreen=number;shownPrefix=prefix;reference.Select(number==0?prefix:prefix+number,desk!=null);
        }
        public override void Tick(FxClock source)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if(lastReduced!=clock.Reduced)SetReduced(clock.Reduced);RefreshReference();var k=Keyboard.current;if(k==null)return;
            if(k.ctrlKey.isPressed&&k.f12Key.wasPressedThisFrame){Load("lab",1);return;}
            if(k.ctrlKey.isPressed&&k.f8Key.wasPressedThisFrame){ToggleControls();return;}
            if(k.ctrlKey.isPressed&&k.mKey.wasPressedThisFrame)SetReduced(!clock.Reduced);
            if(k.f10Key.wasPressedThisFrame)Language();
            if(k.f12Key.wasPressedThisFrame)ShowReference(!reference.Visible);
            if(k.f11Key.wasPressedThisFrame){if(k.shiftKey.isPressed)reference.ToggleBlink();else reference.CycleOpacity();}
            if(k.ctrlKey.isPressed)return;
            for(int i=0;i<9;i++)if(k[(Key)((int)Key.F1+i)].wasPressedThisFrame){Load(k.shiftKey.isPressed?"raid":"desk",i+1);return;}
#endif
        }
    }
}
