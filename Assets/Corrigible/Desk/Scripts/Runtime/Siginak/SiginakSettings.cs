using UnityEngine;

namespace Corrigible.Desk
{
    /// <summary>
    /// Sığınak sunumunun oyuncuya açık ayarları. Oda, ışıklar ve harita ölçeği sahnede saklanır (Aşama 10'da oda kurucusu
    /// ve eski görünüm kaldırıldı; DeskGame yalnız sığınak odasıyla açılır).
    /// </summary>
    [CreateAssetMenu(menuName = "Corrigible/Siginak Settings", fileName = "SiginakSettings")]
    public sealed class SiginakSettings : ScriptableObject
    {
        [Tooltip("Özgür bölge sayısına bağlı umut işaretleri (K1 notu; ayrı ve kapatılabilir).")]
        public bool hopeMarks = true;

        static SiginakSettings active;
        public static SiginakSettings Active
        {
            get { if (active == null) active = Resources.Load<SiginakSettings>("SiginakSettings"); return active; }
            set => active = value;
        }
    }
}
