using System;
using System.Collections.Generic;
using UnityEngine;
namespace Corrigible.Fx
{
    public enum FxPhase { Pulse, Blink, Cursor, Rotation, DashFlow, Led, Sweep, Hazard, Wave, Float, Cone, Flicker, Glitch, Redact, RingPulse, GlowPulse }
    /// <summary>Oyun hızından bağımsız tek görsel saat. Donma evreleri referans paketinden gelir.</summary>
    [DisallowMultipleComponent,DefaultExecutionOrder(-200)]
    public sealed class FxClock : MonoBehaviour
    {
        [SerializeField] FxLoopRunner runner;
        [SerializeField] bool reduced;
        [SerializeField] bool usePlayerPreferences;
        public FxLoopRunner Runner=>runner;
        public double Now {get;private set;}
        public bool Reduced {get=>reduced;set=>reduced=value;}
        public bool Frozen {get;private set;}
        public bool Static=>Reduced||Frozen;
        public bool CompleteEntrances=>Static;
        static FxClock owner;
        readonly float[] shaderPhases=new float[16];
        readonly Dictionary<FxPhase,float> phases=new Dictionary<FxPhase,float>();
        static readonly int TimeId=Shader.PropertyToID("_FxTime"), StaticId=Shader.PropertyToID("_FxStatic"), LedId=Shader.PropertyToID("_FxLedPhase"), SweepId=Shader.PropertyToID("_FxSweepPhase"), FlowId=Shader.PropertyToID("_FxFlowPhase");
        public void Configure(FxLoopRunner value)=>runner=value;
        void OnEnable() { if(owner!=null && owner!=this) { Debug.LogError("FX sahnesinde tek saat olmalı.",this);enabled=false;return; } owner=this;if(usePlayerPreferences)reduced=PlayerPrefs.GetInt("corrigible.fx.reduceMotion",reduced?1:0)!=0;Now=Time.unscaledTimeAsDouble; }
        void OnDisable() { if(owner==this) {owner=null;Shader.SetGlobalFloat(StaticId,0);} }
        void Update()=>Advance(Time.unscaledTimeAsDouble);
        public void Advance(double now)
        {
            Now=now;for(int i=0;i<shaderPhases.Length;i++)shaderPhases[i]=ReferencePhase((FxPhase)i);Shader.SetGlobalFloatArray("_FxPhases",shaderPhases); Shader.SetGlobalFloat(TimeId,(float)now);Shader.SetGlobalFloat(StaticId,Static?1:0);
            Shader.SetGlobalFloat(LedId,ReferencePhase(FxPhase.Led));Shader.SetGlobalFloat(SweepId,ReferencePhase(FxPhase.Sweep));Shader.SetGlobalFloat(FlowId,ReferencePhase(FxPhase.DashFlow));
            if(runner!=null)runner.Tick(this);
        }
        public void Freeze(IDictionary<FxPhase,float> values=null) { phases.Clear(); if(values!=null)foreach(var p in values)phases[p.Key]=Mathf.Repeat(p.Value,1); Frozen=true; }
        public void Resume() { Frozen=false;phases.Clear(); }
        public float ReferencePhase(FxPhase kind)
        {
            if(phases.TryGetValue(kind,out float p))return p;
            switch(kind) { case FxPhase.Pulse:case FxPhase.GlowPulse:case FxPhase.Cone:return .5f;case FxPhase.Led:return .45f;case FxPhase.Wave:return .3f;default:return 0; }
        }
        public float Phase(float period,float offset,FxPhase kind)=>Static?ReferencePhase(kind):Mathf.Repeat((float)(Now/Math.Max(.001,period))+offset,1);
    }
}
