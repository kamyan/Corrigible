using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Corrigible.Desk
{
    /// <summary>
    /// Ekip figürünü sol tuşla taşıma (PROMPT §2, Ekip-figuru.md): bas, 6 px sürükle ya da 150 ms basılı tut → figür
    /// kalkar; imleç harita düzlemine ışınla izlenir (<see cref="Plane.Raycast"/>); bırakınca <see cref="Dropped"/>.
    /// Eşiğin altında bırakılırsa tıktır. Taşırken kamera bakışı ve tekerlek kapalıdır. Geçerliliği ve sonucu bağlayıcı
    /// belirler (mevcut komutlar); bileşen yalnız girdiyi okur. Figürde en az 44 px'lik kutu çarpıştırıcı gerekir.
    /// </summary>
    public sealed class TeamFigureDrag : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField] TeamFigure figure;
        [SerializeField] LiberationMapLayer map;
        [SerializeField] Camera view;
        [SerializeField] SiginakCameraRig rig;

        bool pressed, carrying;
        Vector2 downAt;
        float downTime;
        Vector3 origin, point;
        string over;

        /// <summary>Alınabilir mi (yaralı ekip, kilit); false ise <see cref="Refused"/>.</summary>
        public Func<bool> CanPick;
        public event Action Picked, Refused, Clicked, Cancelled;
        /// <summary>Bırakılan dünya noktası.</summary>
        public event Action<Vector3> Dropped;
        /// <summary>Taşırken altındaki bölge değişti (null = bölge yok).</summary>
        public event Action<string> Over;

        public bool Carrying => carrying;
        public Vector3 Origin => origin;

        public void Configure(TeamFigure teamFigure, LiberationMapLayer layer, Camera camera, SiginakCameraRig cameraRig)
        {
            figure = teamFigure; map = layer; view = camera; rig = cameraRig;
        }

        public void OnPointerDown(PointerEventData e)
        {
            if (e.button != PointerEventData.InputButton.Left || carrying) return;
            pressed = true;
            downAt = e.position;
            downTime = Time.unscaledTime;
        }

        public void OnPointerUp(PointerEventData e)
        {
            if (e.button != PointerEventData.InputButton.Left || !pressed || carrying) return;
            pressed = false;
            Clicked?.Invoke();
        }

        void Update()
        {
            var mouse = Mouse.current;
            if (mouse == null) return;
            var position = mouse.position.ReadValue();
            if (pressed && !carrying)
            {
                var t = SiginakTheme.Active;
                if ((position - downAt).magnitude >= t.NumberOf("motion.team.dragThresholdPx", 6) ||
                    Time.unscaledTime - downTime >= t.NumberOf("motion.team.holdS", 0.15f))
                    Begin();
                if (!mouse.leftButton.isPressed) pressed = false;
            }
            if (!carrying) return;
            if (Track(position, out var p))
            {
                point = p;
                figure.Follow(p);
                string region = map.RegionAt(p);
                if (region != over) { over = region; Over?.Invoke(region); }
            }
            if (mouse.leftButton.wasReleasedThisFrame || !mouse.leftButton.isPressed) Drop();
        }

        bool Track(Vector2 screen, out Vector3 world)
        {
            world = default;
            if (view == null || map == null) return false;
            var ray = view.ScreenPointToRay(screen);
            if (!map.MapPlane.Raycast(ray, out float distance)) return false;
            world = ray.GetPoint(distance);
            return true;
        }

        void Begin()
        {
            pressed = false;
            if (CanPick != null && !CanPick()) { Refused?.Invoke(); return; }
            carrying = true;
            origin = figure.Destination;
            point = origin;
            over = null;
            figure.SetCarrying(true);
            if (rig != null) rig.InputSuspended = true;
            Picked?.Invoke();
        }

        void Drop()
        {
            End();
            Dropped?.Invoke(point);
        }

        /// <summary>Esc: taşımayı bırakır, figür eski yerine kayar. Bir şey yaptıysa true.</summary>
        public bool Cancel()
        {
            if (!carrying) return false;
            End();
            figure.SlideBack(origin);
            Cancelled?.Invoke();
            return true;
        }

        void End()
        {
            carrying = false;
            pressed = false;
            figure.SetCarrying(false);
            if (rig != null) rig.InputSuspended = false;
            if (over != null) { over = null; Over?.Invoke(null); }
        }

        void OnDisable()
        {
            if (carrying) Cancel();
            pressed = false;
        }
    }
}
