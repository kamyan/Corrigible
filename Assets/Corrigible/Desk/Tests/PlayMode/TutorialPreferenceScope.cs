using System;
using Corrigible.Desk.Bridge;
using UnityEngine;
namespace Corrigible.Desk.Tests
{
    /// <summary>Testin rehber tercihleri kullanıcıya sızmaz; normal pano testlerinde rehber kapalıdır.</summary>
    public sealed class TutorialPreferenceScope:IDisposable
    {
        readonly bool hadDone=PlayerPrefs.HasKey(SiginakTutorial.PlanningDoneKey),hadSeed=PlayerPrefs.HasKey(SiginakTutorial.PlanningSeedKey),hadStep=PlayerPrefs.HasKey(SiginakTutorial.PlanningStepKey);
        readonly int done=PlayerPrefs.GetInt(SiginakTutorial.PlanningDoneKey),step=PlayerPrefs.GetInt(SiginakTutorial.PlanningStepKey);
        readonly string seed=PlayerPrefs.GetString(SiginakTutorial.PlanningSeedKey);
        public TutorialPreferenceScope(bool enabled=false){PlayerPrefs.SetInt(SiginakTutorial.PlanningDoneKey,enabled?0:1);PlayerPrefs.DeleteKey(SiginakTutorial.PlanningStepKey);PlayerPrefs.DeleteKey(SiginakTutorial.PlanningSeedKey);}
        public void Dispose()
        {
            if(hadDone)PlayerPrefs.SetInt(SiginakTutorial.PlanningDoneKey,done);else PlayerPrefs.DeleteKey(SiginakTutorial.PlanningDoneKey);
            if(hadStep)PlayerPrefs.SetInt(SiginakTutorial.PlanningStepKey,step);else PlayerPrefs.DeleteKey(SiginakTutorial.PlanningStepKey);
            if(hadSeed)PlayerPrefs.SetString(SiginakTutorial.PlanningSeedKey,seed);else PlayerPrefs.DeleteKey(SiginakTutorial.PlanningSeedKey);PlayerPrefs.Save();
        }
    }
}
