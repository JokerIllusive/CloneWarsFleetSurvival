using System.Linq;
using UnityEngine;

namespace FleetSurvival
{
    public static class FleetTactics
    {
        public static float DamageModifier(FleetShip from,FleetShip target)
        {
            if(from.Squadron==null) return target.Squadron!=null?.6f:1;
            if(from.Role==SquadronRole.Strike) return target.Squadron==null?1.9f:.35f;
            if(from.Role==SquadronRole.Interceptor) return target.Squadron!=null?1.6f:.55f;
            return target.Squadron!=null?1.2f:.85f;
        }
        public static float TargetPreference(FleetShip from,FleetShip target)
        {
            if(from.Squadron!=null)
            {
                if(from.Role==SquadronRole.Interceptor) return target.Squadron!=null?.3f:1.8f;
                if(from.Role==SquadronRole.Strike) return target.Hangar!=null?.45f:target.Squadron==null?.7f:2.5f;
                return target.Squadron!=null?(target.Role==SquadronRole.Strike?.35f:.65f):1.2f;
            }
            if(from.Kind==ShipClass.Escort && target.Squadron!=null) return .4f;
            return !from.Friendly && target.IsFlagship?.8f:1;
        }
        public static FleetShip ScreenAnchor(FleetShip from)
        {return from.Game.Ships.Where(s=>s!=null && s!=from && s.Alive && s.Targetable && s.Friendly==from.Friendly && s.Hangar!=null).OrderBy(s=>(s.transform.position-from.transform.position).sqrMagnitude).FirstOrDefault();}
        public static bool BattlePosition(FleetShip ship,FleetShip target,out Vector3 point)
        {
            point=ship.transform.position;
            if(ship.Friendly || ship.HasMoveOrder || ship.ForcedTarget!=null || target==null) return false;
            if(ship.Hangar!=null)
            {
                Vector3 away=ship.transform.position-target.transform.position;away.y=0;
                if(away.sqrMagnitude<.01f) away=-target.transform.forward;
                point=target.transform.position+away.normalized*ship.Stats.Range*.9f;return true;
            }
            if(ship.Kind==ShipClass.Escort || ship.Squadron!=null && ship.Role==SquadronRole.Fighter)
            {
                var anchor=ScreenAnchor(ship);
                if(anchor!=null && (target.transform.position-anchor.transform.position).magnitude>ship.Stats.Range+10)
                {Vector3 toward=target.transform.position-anchor.transform.position;toward.y=0;point=anchor.transform.position+toward.normalized*(anchor.Stats.Radius+9)+Vector3.Cross(Vector3.up,toward.normalized)*6;return true;}
            }
            if(ship.Squadron==null && (ship.Kind==ShipClass.Frigate || ship.Kind==ShipClass.Destroyer))
            {
                Vector3 toward=target.transform.position-ship.transform.position;toward.y=0;
                Vector3 side=Vector3.Cross(Vector3.up,toward.normalized)*(ship.transform.position.x>=0?1:-1);
                point=target.transform.position-toward.normalized*ship.Stats.Range*.78f+side*ship.Stats.Range*.35f;return true;
            }
            return false;
        }
        public static string RoleText(FleetShip ship)
        {return ship.Squadron==null?(ship.Hangar!=null?"Carrier / ranged support":ship.Kind==ShipClass.Escort?"Escort / fighter screen":"Capital / heavy fire"):ship.Role==SquadronRole.Strike?"Strike / torpedoes vs capitals":ship.Role==SquadronRole.Interceptor?"Interceptor / hunts fighters":"Fighter / fleet screen";}
    }
}
