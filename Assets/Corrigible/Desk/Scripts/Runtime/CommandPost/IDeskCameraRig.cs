namespace Corrigible.Desk
{
    /// <summary>
    /// Masa kamerasının sunum katmanına açtığı ortak yüz. Eski komuta odası (<see cref="CommandPostCamera"/>) ve sığınak
    /// odası (<see cref="SiginakCameraRig"/>) aynı çağrılarla sürülür; hangisinin etkin olduğunu sunum anahtarı seçer.
    /// </summary>
    public interface IDeskCameraRig
    {
        /// <summary>Kamera harita (planlama) pozunda ya da oraya gidiyor.</summary>
        bool IsFocused { get; }
        void FocusMap();
        void FocusShelter();
        void FocusOperations();
        void ResetView();
    }
}
