using UnityEngine;

namespace Corrigible.Desk
{
    /// <summary>Sahte ayarlar (sandbox): değerler bellekte; dil gerçekten değişir.</summary>
    public sealed class MockDeskSettings : MonoBehaviour, IDeskSettings
    {
        [SerializeField] float[] levels = { 0.8f, 0.6f, 0.7f, 0.9f };
        [SerializeField] bool muted;

        public float Get(int channel) => levels[Mathf.Clamp(channel, 0, levels.Length - 1)];
        public void Set(int channel, float value) => levels[Mathf.Clamp(channel, 0, levels.Length - 1)] = Mathf.Clamp01(value);
        public bool Muted { get => muted; set => muted = value; }
        public DeskLang Language => DeskLoc.Instance.Lang;
        public void SetLanguage(DeskLang lang) => DeskLoc.Instance.SetLanguage(lang);
        public void SetOpen(bool open) { }
        public void Preview(int channel) { }
        public void Save() { }
    }
}
