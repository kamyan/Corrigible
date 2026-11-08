namespace Corrigible.Desk.Bridge
{
    /// <summary>
    /// Kabul istasyonunun kayda girmeyen sunum durumu (Revizyon 3 · Aşama 6, PLAN §1.2; eleştiri 7, 8, 20): belge tezgâha alındı mı (aday
    /// kimliğine bağlı), hangi talimat sürümünün değişim animasyonu görüldü, hangi adayın dış kayıt sonucu fişi ağızdan çıktı.
    /// <c>AdmissionSnapshotBuilder</c> (W4) okur, <c>SiginakAdmission</c> (W5) yazar ve <c>liberation_desk.json</c>'a
    /// (<see cref="SiginakDeskStore.AdmissionSeen"/>) saklar. Yeni oyunda ve yan dosya yokken <see cref="SeenOrdersVersion"/> güncel sürüme
    /// eşitlenir (v1 hiç "yeni" sayılmaz).
    /// </summary>
    public sealed class AdmissionUi
    {
        /// <summary>Belgesi çekmeceden tezgâha alınmış adayın kimliği; aday değişince geçersiz.</summary>
        public string DocumentOnCounterFor;
        /// <summary>Değişim animasyonu oynamış son talimat sürümü (0 = bilinmiyor).</summary>
        public int SeenOrdersVersion;
        /// <summary>Dış kayıt sonuç fişinin ağızdan çıkışı oynamış adayın kimliği.</summary>
        public string SeenResultFor;

        /// <summary>Bu adayın belgesi tezgâhta mı.</summary>
        public bool DocumentOnCounter(string candidateId) => !string.IsNullOrEmpty(candidateId) && DocumentOnCounterFor == candidateId;

        public void Reset()
        {
            DocumentOnCounterFor = null;
            SeenOrdersVersion = 0;
            SeenResultFor = null;
        }

        /// <summary>Yan dosyaya yazılacak biçim.</summary>
        public SiginakDeskStore.AdmissionSeen ToSeen() => new SiginakDeskStore.AdmissionSeen
        {
            ordersVersion = SeenOrdersVersion, resultFor = SeenResultFor ?? "", documentFor = DocumentOnCounterFor ?? "",
        };

        /// <summary>Yan dosyadan okunan durum; null ya da boş girdi (sürüm 0) false döner ve durumu değiştirmez.</summary>
        public bool Apply(SiginakDeskStore.AdmissionSeen seen)
        {
            if (seen == null || seen.ordersVersion <= 0) return false;
            SeenOrdersVersion = seen.ordersVersion;
            SeenResultFor = string.IsNullOrEmpty(seen.resultFor) ? null : seen.resultFor;
            DocumentOnCounterFor = string.IsNullOrEmpty(seen.documentFor) ? null : seen.documentFor;
            return true;
        }
    }
}
