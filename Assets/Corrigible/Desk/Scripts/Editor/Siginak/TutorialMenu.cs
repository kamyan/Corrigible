using Corrigible.Desk.Bridge;
using UnityEditor;
using UnityEngine;

namespace Corrigible.Desk.EditorTools
{
    /// <summary>Geliştirme: bitirilen ya da atlanan eğitimi sıfırlar; bir sonraki yeni harekâtta eğitim yeniden açılır.</summary>
    public static class TutorialMenu
    {
        [MenuItem("Corrigible/Eğitim/Eğitimi sıfırla")]
        static void Reset()
        {
            SiginakTutorial.Done = false;
            SiginakTutorial.PlanningDone = false;
            PlayerPrefs.DeleteKey(SiginakTutorial.PlanningSeedKey);
            PlayerPrefs.DeleteKey(SiginakTutorial.PlanningStepKey);
            PlayerPrefs.Save();
            Debug.Log("Eğitim sıfırlandı: bir sonraki yeni harekâtta yeniden açılır.");
        }
    }
}
