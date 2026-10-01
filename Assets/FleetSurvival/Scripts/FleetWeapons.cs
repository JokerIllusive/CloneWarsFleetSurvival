using System.Collections.Generic;
using UnityEngine;

namespace FleetSurvival
{
    public sealed class FleetWeapons : MonoBehaviour
    {
        struct Shot { public Vector3 Origin; public float Delay,Damage; public FleetShip Target; public int Fighter; }
        readonly List<Shot> queued=new List<Shot>();
        readonly List<Vector3> mounts=new List<Vector3>();
        FleetShip ship;
        public int BarrelCount => mounts.Count;
        public void Clear() {queued.Clear();}
        public Color BoltColor => ship.Faction==Faction.Republic?new Color(.12f,.52f,1):new Color(1,.08f,.025f);
        public void Initialize(FleetShip owner)
        {
            ship=owner;
            if(ship.Kind==ShipClass.Fighter || ship.Kind==ShipClass.Interceptor)
            {
                float wing=ship.Faction==Faction.Republic && ship.Kind==ShipClass.Fighter?1.65f:.8f;
                foreach(var offset in FleetSquadron.Offsets)
                { if(ship.Role==SquadronRole.Strike) mounts.Add(offset+new Vector3(0,.3f,1)*FleetSquadron.CraftScale);else {mounts.Add(offset+new Vector3(-wing,.45f,.7f)*FleetSquadron.CraftScale); mounts.Add(offset+new Vector3(wing,.45f,.7f)*FleetSquadron.CraftScale);} }
                return;
            }
            if(ship.Faction==Faction.CIS && ship.Kind==ShipClass.Carrier)
            {
                for(int i=0;i<8;i++) { float a=i*Mathf.PI/4; Twin(new Vector3(Mathf.Sin(a)*7.2f,1.2f,Mathf.Cos(a)*7.2f)); }
                return;
            }
            int pairs=ship.Kind==ShipClass.Flagship?(ship.Faction==Faction.Republic?4:5):ship.Kind==ShipClass.Destroyer?4:ship.Kind==ShipClass.Escort?2:3;
            float halfLength=ship.Kind==ShipClass.Flagship?4:ship.Kind==ShipClass.Destroyer?3:2;
            float width=ship.Kind==ShipClass.Flagship?2.7f:ship.Kind==ShipClass.Destroyer?2.1f:ship.Kind==ShipClass.Escort?1.2f:1.65f;
            for(int i=0;i<pairs;i++)
            {
                float z=Mathf.Lerp(-halfLength,halfLength*.35f,i/(float)Mathf.Max(1,pairs-1));
                Twin(new Vector3(-width,1.3f,z)); Twin(new Vector3(width,1.3f,z));
            }
        }
        void Twin(Vector3 position) { mounts.Add(position+Vector3.left*.12f); mounts.Add(position+Vector3.right*.12f); }
        public void FireVolley(FleetShip target,float damage)
        {
            if(target==null || !target.Targetable || queued.Count>0) return;
            int barrels=ship.Squadron!=null?ship.Squadron.ActiveCount*(ship.Role==SquadronRole.Strike?1:2):mounts.Count;
            if(barrels==0) return;
            for(int i=0;i<mounts.Count;i++)
            {
                int craft=ship.Role==SquadronRole.Strike?i:i/2;
                if(ship.Squadron==null || ship.Squadron.IsActive(craft)) queued.Add(new Shot{Origin=mounts[i],Delay=i*.018f,Damage=damage/barrels,Target=target,Fighter=craft});
            }
        }
        public void Tick(float dt)
        {
            for(int i=queued.Count-1;i>=0;i--)
            {
                var shot=queued[i]; shot.Delay-=dt;
                if(shot.Delay>0) { queued[i]=shot; continue; }
                queued.RemoveAt(i);
                if(shot.Target==null || !shot.Target.Targetable) continue;
                if(ship.Squadron!=null && !ship.Squadron.IsActive(shot.Fighter)) continue;
                Vector3 muzzle=ship.Squadron!=null?ship.Squadron.Muzzle(shot.Fighter,(shot.Origin-FleetSquadron.Offsets[shot.Fighter])/FleetSquadron.CraftScale):transform.TransformPoint(shot.Origin);
                ship.Game.SpawnBolt(ship,shot.Target,shot.Damage,muzzle,BoltColor);
            }
        }
    }
}
