namespace Corrigible.Desk.Bridge
{
    /// <summary>
    /// Olay tutanağının sunum durumu (KOS aşama 4): işaretlenen satır ve son ret gerekçesi. Kayda ve yan dosyaya girmez; seçim yalnız
    /// aynı kâğıtta (<see cref="ForKey"/>) geçerlidir, kâğıt değişince kendiliğinden düşer.
    /// </summary>
    public sealed class ShelterEventUi
    {
        /// <summary>İşaretli seçeneğin Core kimliği; yoksa null.</summary>
        public string SelectedId;
        /// <summary>Seçimin ve son retin ait olduğu kâğıdın anahtarı.</summary>
        public string ForKey;
        /// <summary>Son ret gerekçesi (kapalı satır, seçimsiz KARAR, Core reddi); not satırına yazılır.</summary>
        public string LastDenied;

        public void Reset()
        {
            SelectedId = ForKey = LastDenied = null;
        }

        /// <summary>Seçim ve ret bu kâğıda ait değilse silinir.</summary>
        public void Align(string key)
        {
            if (ForKey == key) return;
            SelectedId = LastDenied = null;
            ForKey = key;
        }
    }
}
