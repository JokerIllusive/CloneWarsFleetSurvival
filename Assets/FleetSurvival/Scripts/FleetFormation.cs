using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FleetSurvival
{
    // A moving set of slots lets escorts keep pace while slower capitals turn.
    public sealed class FleetFormation
    {
        readonly Dictionary<FleetShip,Vector3> offsets=new Dictionary<FleetShip,Vector3>();
        readonly Quaternion rotation;
        readonly Vector3 goal;
        readonly float spacing;
        Vector3 center;
        public float Speed { get; private set; }
        public bool Arrived => (goal-center).sqrMagnitude<.01f && Speed<.05f;
        public int Count => offsets.Count;
        public Vector3 Goal => goal;
        public Quaternion ArrivalRotation => rotation;
        public FleetFormation(FleetShip[] ships,Vector3 destination,bool attackMove)
        {
            foreach(var ship in ships) center+=ship.transform.position;
            center/=ships.Length;
            Vector3 direction=destination-center; direction.y=0;
            rotation=direction.sqrMagnitude>.01f?Quaternion.LookRotation(direction):ships[0].transform.rotation;
            spacing=ships.Max(s=>s.Stats.Radius)*2+2.4f;
            int width=Mathf.CeilToInt(Mathf.Sqrt(ships.Length));
            int rows=Mathf.CeilToInt(ships.Length/(float)width);
            var slots=new List<Vector3>();
            for(int row=0;row<rows;row++)
            {
                int columns=Mathf.Min(width,ships.Length-row*width);
                for(int col=0;col<columns;col++)
                    slots.Add(rotation*new Vector3((col-(columns-1)*.5f)*spacing,0,((rows-1)*.5f-row)*spacing));
            }
            float extent=slots.Max(s=>s.magnitude)+ships.Max(s=>s.Stats.Radius)+1;
            destination.y=0;
            goal=Vector3.ClampMagnitude(destination,Mathf.Max(5,FleetRules.ArenaRadius-extent));
            // Assign nearby slots first rather than making ships cross the whole formation.
            foreach(var ship in ships.OrderByDescending(s=>s.Stats.Radius))
            {
                Vector3 slot=slots.OrderBy(s=>(center+s-ship.transform.position).sqrMagnitude).First();
                slots.Remove(slot); offsets[ship]=slot;
                ship.MoveTo(goal+slot,attackMove); ship.Formation=this;
            }
        }
        public Vector3 Slot(FleetShip ship) => center+offsets[ship];
        public bool Tick(float dt)
        {
            foreach(var ship in offsets.Keys.ToArray())
                if(ship==null || !ship.Alive || !ship.HasMoveOrder || ship.Formation!=this) offsets.Remove(ship);
            if(offsets.Count==0) return false;
            float cruise=float.MaxValue,acceleration=float.MaxValue,braking=float.MaxValue,worstError=0;
            bool engaged=false;
            foreach(var pair in offsets)
            {
                var ship=pair.Key; var handling=FleetRules.Handling(ship.Kind);
                cruise=Mathf.Min(cruise,ship.Stats.Speed*ship.Destruction.Mobility*.8f);
                acceleration=Mathf.Min(acceleration,handling.Acceleration*ship.Destruction.Mobility);
                braking=Mathf.Min(braking,handling.Braking);
                worstError=Mathf.Max(worstError,Vector3.Distance(ship.transform.position,center+pair.Value));
                var enemy=ship.AttackMove?ship.Game.NearestEnemy(ship):null;
                if(enemy!=null && Vector3.Distance(ship.transform.position,enemy.transform.position)<=ship.Stats.Range) engaged=true;
            }
            float distance=Vector3.Distance(center,goal);
            // Hold back when the group is stretched, including after a sharp new order.
            float cohesion=Mathf.Clamp01(1-(worstError-3)/Mathf.Max(10,spacing));
            float desired=engaged?0:Mathf.Min(cruise*cohesion,Mathf.Sqrt(2*braking*distance));
            Speed=Mathf.MoveTowards(Speed,desired,(desired<Speed?braking:acceleration)*dt);
            float travel=Mathf.Min(distance,Speed*dt);
            center=Vector3.MoveTowards(center,goal,travel);
            if(distance<=travel+.001f) { center=goal; Speed=0; }
            return true;
        }
    }
}
