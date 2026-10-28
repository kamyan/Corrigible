using System.Collections.Generic;

namespace Corrigible.Desk
{
    /// <summary>Kapı kâğıdındaki bir düğme: seçenek kimliği, yazısı, altındaki bedel ya da gerekçe ve seçilebilir mi.</summary>
    public sealed class DoorButtonData
    {
        public string Id, Label, Detail;
        public bool Enabled = true;
        /// <summary>Koyu mürekkep düğme (kapıyı aç, kapıyı kapat); öteki düğmeler çerçeveli kâğıt.</summary>
        public bool Primary;
    }

    /// <summary>Kapı kâğıdının gösterime hazır hâli: <see cref="DoorCardView"/> yalnız bunu çizer.</summary>
    public sealed class DoorCardData
    {
        public string Head, Code, Title, Body, Hint;
        /// <summary>Kırmızı damga (yalnız harekâtı bitiren kararda); yoksa null.</summary>
        public string Stamp;
        public readonly List<DoorButtonData> Buttons = new List<DoorButtonData>();
    }
}
