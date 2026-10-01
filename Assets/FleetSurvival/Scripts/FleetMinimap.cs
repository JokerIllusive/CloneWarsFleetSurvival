using UnityEngine.EventSystems;

namespace FleetSurvival
{
    public sealed class FleetMinimap : UnityEngine.MonoBehaviour, IPointerDownHandler, IDragHandler
    {
        FleetHUD hud;
        public void Initialize(FleetHUD owner) { hud=owner; }
        public void OnPointerDown(PointerEventData e)
        { if(e.button==PointerEventData.InputButton.Left) hud.PanMap(e.position,e.pressEventCamera); }
        public void OnDrag(PointerEventData e)
        { if(e.button==PointerEventData.InputButton.Left) hud.PanMap(e.position,e.pressEventCamera); }
    }
}
