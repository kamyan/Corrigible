namespace Corrigible.Desk.Bridge
{
    /// <summary>
    /// Kabul istasyonunun kayda girmeyen sunum durumu (KOS aşama 2): dolu sığınakta grup fişinde işaretlenen yollanacak kişi.
    /// <c>AdmissionSnapshotBuilder</c> okur, <c>SiginakAdmission</c> yazar. Yan dosyaya yazılmaz (yüklemede seçim sıfırdır). Eski tek adaylı
    /// kabulün görülen durumu (belge tezgâhta, görülen talimat sürümü, dış kayıt sonucu) aşama 7'de söküldü.
    /// </summary>
    public sealed class AdmissionUi
    {
        /// <summary>Dolu sığınakta işaretlenen yollanacak kişi ve ait olduğu grup; grup değişince geçersiz.</summary>
        public string ReplaceId, ReplaceForGroup;

        /// <summary>Bu grup için işaretli yollanacak kişi; yoksa null.</summary>
        public string Replace(string groupKey) => !string.IsNullOrEmpty(groupKey) && ReplaceForGroup == groupKey && !string.IsNullOrEmpty(ReplaceId) ? ReplaceId : null;

        public void Reset()
        {
            ReplaceId = ReplaceForGroup = null;
        }
    }
}
