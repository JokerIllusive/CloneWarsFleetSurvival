using UnityEngine;

namespace FleetSurvival
{
    public sealed class FleetMovePreview : MonoBehaviour
    {
        FleetShip ship;
        GameObject footprint;
        LineRenderer path;
        Material hologram;
        public bool Visible => footprint!=null && footprint.activeSelf;
        public Vector3 Position => footprint!=null?footprint.transform.position:Vector3.zero;
        public int ColliderCount => footprint!=null?footprint.GetComponentsInChildren<Collider>().Length:0;
        public void Initialize(FleetShip owner) { ship=owner; }
        void LateUpdate() { Refresh(); }
        public void Refresh()
        {
            bool show=ship!=null && ship.Alive && ship.Friendly && ship.HasMoveOrder && !ship.IsArriving && ship.Game.Ships.Contains(ship) && ship.Game.Phase!=BattlePhase.Menu && ship.Game.Phase!=BattlePhase.Defeat;
            if(!show) { if(footprint!=null) footprint.SetActive(false); if(path!=null) path.enabled=false; return; }
            if(footprint==null)
            {
                footprint=new GameObject("Final formation slot");ship.Game.AddCombatEffect(footprint);
                var ghost=Instantiate(ship.VisualRoot,footprint.transform,false);ghost.name="Destination ship silhouette";
                hologram=new Material(Resources.Load<Material>("FleetHologram"));hologram.color=new Color(.12f,.65f,.9f,.035f);
                foreach(var renderer in ghost.GetComponentsInChildren<MeshRenderer>(true))
                { var materials=renderer.sharedMaterials;for(int i=0;i<materials.Length;i++) materials[i]=hologram;renderer.sharedMaterials=materials;renderer.SetPropertyBlock(null);for(int i=0;i<materials.Length;i++) renderer.SetPropertyBlock(null,i); }
                foreach(var collider in ghost.GetComponentsInChildren<Collider>()) Destroy(collider);
                if(ship.Squadron!=null) for(int i=0;i<ghost.childCount;i++) { ghost.GetChild(i).localPosition=FleetSquadron.Offsets[i];ghost.GetChild(i).localRotation=Quaternion.identity; }
                ShipVisuals.Ring(footprint.transform,ship.Stats.Radius+.5f,new Color(.12f,.65f,.9f),.065f,"Arrival footprint");
                var heading=new GameObject("Arrival heading",typeof(LineRenderer)).GetComponent<LineRenderer>();heading.transform.SetParent(footprint.transform,false);
                heading.useWorldSpace=false;heading.positionCount=3;heading.SetPositions(new[]{new Vector3(-1,.15f,ship.Stats.Radius),new Vector3(0,.15f,ship.Stats.Radius+2),new Vector3(1,.15f,ship.Stats.Radius)});
                heading.sharedMaterial=ShipVisuals.Material(new Color(.12f,.65f,.9f),true);heading.startWidth=heading.endWidth=.11f;
                path=new GameObject("Movement route",typeof(LineRenderer)).GetComponent<LineRenderer>();ship.Game.AddCombatEffect(path.gameObject);
                path.sharedMaterial=ShipVisuals.Material(new Color(.08f,.28f,.4f),true);path.startWidth=path.endWidth=.045f;path.positionCount=2;
            }
            footprint.SetActive(true);path.enabled=true;
            Vector3 direction=ship.Destination-ship.transform.position;direction.y=0;
            Quaternion facing=ship.Formation!=null?ship.Formation.ArrivalRotation:direction.sqrMagnitude>.01f?Quaternion.LookRotation(direction):ship.transform.rotation;
            footprint.transform.SetPositionAndRotation(ship.Destination,facing);
            path.SetPosition(0,ship.transform.position+Vector3.up*.12f);path.SetPosition(1,ship.Destination+Vector3.up*.12f);
        }
        void OnDestroy() { if(footprint!=null) Destroy(footprint);if(path!=null) Destroy(path.gameObject);if(hologram!=null) Destroy(hologram); }
    }
}
