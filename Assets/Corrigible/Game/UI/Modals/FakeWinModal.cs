using Corrigible.Core;
using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Game.UI
{
    /// <summary>Sessiz Zafer'den önceki sahte "TEBRİKLER" ekranı: bir süre inandırır, sonra bozulur (glitch).</summary>
    public sealed class FakeWinModal : MonoBehaviour
    {
        [SerializeField] UiTheme theme;
        [SerializeField] TMP_Text eyebrow;
        [SerializeField] Graphic border;
        [Tooltip("Sahte zaferin bozulmadan önce ekranda kalma süresi (saniye).")]
        [SerializeField, Min(0)] float holdSeconds = 3.6f;
        [Tooltip("Bozulma (glitch) süresi (saniye).")]
        [SerializeField, Min(0)] float glitchSeconds = 0.9f;
        [SerializeField, Min(0.01f)] float glitchInterval = 0.09f;
        [SerializeField] Vector2 glitchOffset = new Vector2(-6, 2);
        [SerializeField] float glitchAngle = -1;

        RectTransform rt;
        Vector2 basePos;
        Coroutine routine;

        void Awake()
        {
            rt = (RectTransform)transform;
            basePos = rt.anchoredPosition;
        }

        public void Play(int day, Action done)
        {
            eyebrow.text = Loc.F("KYGA · RESMÎ DUYURU · GÜN {0}", day);
            Stop();
            routine = StartCoroutine(Run(done));
        }

        public void Stop()
        {
            if (routine != null) { StopCoroutine(routine); routine = null; }
            if (rt == null) return;
            rt.anchoredPosition = basePos;
            rt.localRotation = Quaternion.identity;
            border.color = theme.good;
        }

        IEnumerator Run(Action done)
        {
            yield return new WaitForSecondsRealtime(holdSeconds);
            float until = Time.unscaledTime + glitchSeconds;
            bool on = false;
            while (Time.unscaledTime < until)
            {
                on = !on;
                rt.anchoredPosition = basePos + (on ? glitchOffset : Vector2.zero);
                rt.localRotation = Quaternion.Euler(0, 0, on ? glitchAngle : 0);
                border.color = on ? theme.threat : theme.good;
                yield return new WaitForSecondsRealtime(glitchInterval);
            }
            routine = null;
            Stop();
            done?.Invoke();
        }
    }
}
