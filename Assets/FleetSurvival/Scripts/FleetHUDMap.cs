using System.Linq;
using TMPro;
using UnityEngine;

namespace FleetSurvival
{
    public sealed partial class FleetHUD
    {
        TextMeshProUGUI mapReadout;
        FleetMapTerrain mapTerrain;
        readonly System.Collections.Generic.Dictionary<FleetJump,RectTransform> mapJumps=new System.Collections.Generic.Dictionary<FleetJump,RectTransform>();
        readonly System.Collections.Generic.Dictionary<FleetWreck,RectTransform> mapReactors=new System.Collections.Generic.Dictionary<FleetWreck,RectTransform>();
        public Vector2 MapExtent => mapDots.rect.size*.5f-Vector2.one*10;
        public string MapReadout => mapReadout.text;
        public int MapIncomingCount => mapJumps.Count;
        public int MapReactorCount => mapReactors.Count;
        public void RefreshTacticalMap(){UpdateMarkers();mapTerrain.SetVerticesDirty();Canvas.ForceUpdateCanvases();}
        public Vector2 MapPoint(Vector3 world) => new Vector2(world.x/FleetRules.ArenaRadius*MapExtent.x,world.z/FleetRules.ArenaRadius*MapExtent.y);
        Vector3 MapWorld(Vector2 point) => new Vector3(point.x/MapExtent.x*FleetRules.ArenaRadius,0,point.y/MapExtent.y*FleetRules.ArenaRadius);
        void BuildTacticalMap(Transform screen)
        {
            var panel=Panel(screen,"Tactical map",new Vector2(1,1),new Vector2(-18,-88),new Vector2(300,332),panelColor);
            Text(panel,"GEONOSIS / TACTICAL",new Vector2(0,1),new Vector2(14,-12),new Vector2(272,25),16,cyan);
            mapDots=Panel(panel,"Ship contacts",new Vector2(.5f,.5f),new Vector2(0,5),new Vector2(272,238),new Color(.008f,.018f,.03f,.98f));
            mapDots.gameObject.AddComponent<UnityEngine.UI.RectMask2D>();mapDots.gameObject.AddComponent<FleetMinimap>().Initialize(this);
            BuildMapGrid();
            var environment=FindObjectOfType<FleetEnvironment>();
            var terrain=Rect(mapDots,"Orbital map detail",new Vector2(.5f,.5f),Vector2.zero,mapDots.sizeDelta);terrain.SetAsFirstSibling();
            mapTerrain=terrain.gameObject.AddComponent<FleetMapTerrain>();mapTerrain.Initialize(game,environment,MapExtent);
            for(int i=0;i<environment.MapLandmarks.Count;i++)
            {
                var point=MapPoint(environment.MapLandmarks[i].position)+new Vector2(0,-18);
                point.x=Mathf.Clamp(point.x,-MapExtent.x+28,MapExtent.x-28);point.y=Mathf.Clamp(point.y,-MapExtent.y+12,MapExtent.y-16);
                Text(mapDots,"YARD "+(i+1).ToString("00"),new Vector2(.5f,.5f),point,new Vector2(56,16),10,new Color(.6f,.51f,.35f,.85f),TextAlignmentOptions.Center);
            }
            Text(mapDots,"+Z",new Vector2(.5f,1),new Vector2(0,-3),new Vector2(32,18),12,mutedText,TextAlignmentOptions.Center);
            mapReadout=Text(panel,"",new Vector2(.5f,0),new Vector2(0,25),new Vector2(272,32),13,ink,TextAlignmentOptions.Center);
            Text(panel,"LEFT CLICK / DRAG: PAN   |   HOVER: INFO",new Vector2(.5f,0),new Vector2(0,7),new Vector2(272,18),12,mutedText,TextAlignmentOptions.Center);
        }
        static FleetMapShape ShapeOf(FleetShip ship)
        {
            if(ship.IsFlagship)return FleetMapShape.Command;
            if(ship.Squadron!=null)return ship.Role==SquadronRole.Interceptor?FleetMapShape.Interceptor:ship.Role==SquadronRole.Strike?FleetMapShape.Bomber:FleetMapShape.Fighter;
            return ship.Kind==ShipClass.Carrier?FleetMapShape.Carrier:FleetMapShape.Capital;
        }
        void BuildMapContact(FleetShip ship,Marker marker)
        {
            marker.Route=MapLine("Selected movement route",new Color(.22f,.8f,1,.45f));
            marker.Destination=Rect(mapDots,"Selected destination",new Vector2(.5f,.5f),Vector2.zero,Vector2.one*12);
            marker.Arrival=marker.Destination.gameObject.AddComponent<FleetMapIcon>();marker.Arrival.Configure(FleetMapShape.Destination,false,new Color(1,.85f,.25f));
            marker.Dot=Rect(mapDots,"Ship contact",new Vector2(.5f,.5f),Vector2.zero,Vector2.one*(ship.IsFlagship?17:ship.Squadron!=null?11:15));
            marker.Icon=marker.Dot.gameObject.AddComponent<FleetMapIcon>();
            UpdateMapContact(ship,marker);
        }
        void UpdateMapContact(FleetShip ship,Marker marker)
        {
            marker.Dot.anchoredPosition=MapPoint(Vector3.ClampMagnitude(ship.transform.position,FleetRules.ArenaRadius));
            marker.Dot.localRotation=Quaternion.Euler(0,0,-ship.transform.eulerAngles.y);
            marker.Dot.gameObject.SetActive(!ship.Docked);
            var tint=ship.Friendly?cyan:new Color(1,.36f,.24f);if(ship.IsArriving)tint=new Color(.7f,.5f,1);
            marker.Icon.Configure(ShapeOf(ship),ship.Friendly && ship.Selected,tint);
            bool route=ship.Friendly && ship.Selected && ship.HasMoveOrder && ship.Targetable;
            marker.Route.gameObject.SetActive(route);marker.Destination.gameObject.SetActive(route);
            if(route)
            {
                var goal=MapPoint(ship.Destination);LineBetween(marker.Route,marker.Dot.anchoredPosition,goal,1.1f);
                marker.Route.GetComponent<UnityEngine.UI.Image>().color=ship.AttackMove?new Color(1,.55f,.23f,.65f):new Color(.22f,.8f,1,.5f);
                marker.Destination.anchoredPosition=goal;
            }
            marker.Dot.SetAsLastSibling();
        }
        RectTransform MapNotice(string name,FleetMapShape shape,Vector3 position,Color tint)
        {
            var rect=Rect(mapDots,name,new Vector2(.5f,.5f),MapPoint(position),Vector2.one*18);
            rect.gameObject.AddComponent<FleetMapIcon>().Configure(shape,false,tint);return rect;
        }
        void RefreshMapExtras()
        {
            foreach(var jump in mapJumps.Keys.ToArray())if(!game.ActiveJumps.Contains(jump)){mapJumps[jump].gameObject.SetActive(false);Destroy(mapJumps[jump].gameObject);mapJumps.Remove(jump);}
            foreach(var wreck in mapReactors.Keys.ToArray())if(wreck==null || wreck.Complete || !game.ActiveWrecks.Contains(wreck)){mapReactors[wreck].gameObject.SetActive(false);Destroy(mapReactors[wreck].gameObject);mapReactors.Remove(wreck);}
            foreach(var jump in game.ActiveJumps)
            {
                if(!mapJumps.TryGetValue(jump,out var marker)){marker=MapNotice("Incoming hyperspace contact",FleetMapShape.Incoming,jump.ReservedPosition,new Color(.75f,.55f,1));mapJumps[jump]=marker;}
                marker.anchoredPosition=MapPoint(jump.ReservedPosition);marker.localScale=Vector3.one*(1+Mathf.Sin(jump.Elapsed*5)*.08f);
            }
            foreach(var wreck in game.ActiveWrecks)if(wreck!=null && wreck.Meltdown && !wreck.Complete)
            {
                if(!mapReactors.TryGetValue(wreck,out var marker)){marker=MapNotice("Reactor danger contact",FleetMapShape.Reactor,wreck.transform.position,new Color(1,.6f,.2f));mapReactors[wreck]=marker;}
                marker.anchoredPosition=MapPoint(wreck.transform.position);
            }
        }
        public void RefreshMapReadout(Vector2 screenPosition,Camera eventCamera)
        {
            FleetShip hover=null;
            if(RectTransformUtility.RectangleContainsScreenPoint(mapDots,screenPosition,eventCamera) && RectTransformUtility.ScreenPointToLocalPointInRectangle(mapDots,screenPosition,eventCamera,out var point))
            {
                float closest=14*14;
                foreach(var ship in game.Ships)
                {
                    if(ship==null || !ship.Alive || ship.Docked)continue;
                    float distance=(MapPoint(Vector3.ClampMagnitude(ship.transform.position,FleetRules.ArenaRadius))-point).sqrMagnitude;
                    if(distance<closest){closest=distance;hover=ship;}
                }
            }
            if(hover!=null){mapReadout.text=(hover.Friendly?"ALLY / ":"HOSTILE / ")+hover.Stats.Name+"\nHull "+Mathf.CeilToInt(hover.Hull)+" | Shields "+Mathf.CeilToInt(hover.Shield)+(hover.IsArriving?" | ARRIVING":"");return;}
            if(RectTransformUtility.RectangleContainsScreenPoint(mapDots,screenPosition,eventCamera) && RectTransformUtility.ScreenPointToLocalPointInRectangle(mapDots,screenPosition,eventCamera,out var noticePoint))
            {
                foreach(var pair in mapJumps)if((pair.Value.anchoredPosition-noticePoint).sqrMagnitude<14*14){mapReadout.text="INBOUND / "+FleetRules.Stats(game.PlayerFaction,pair.Key.Kind).Name+"\n"+(pair.Key.Elapsed<2.2f?(2.2f-pair.Key.Elapsed).ToString("0.0")+"s hyperspace charge":"Completing arrival");return;}
                foreach(var pair in mapReactors)if((pair.Value.anchoredPosition-noticePoint).sqrMagnitude<14*14){mapReadout.text="REACTOR FAILURE / "+pair.Key.Countdown.ToString("0.0")+"s\nOrange ring marks the blast radius";return;}
            }
            mapReadout.text="ALLY "+game.Ships.Count(s=>s!=null && s.Alive && s.Friendly && !s.Docked)+"  /  HOSTILE "+game.Ships.Count(s=>s!=null && s.Alive && !s.Friendly && !s.Docked)+"  /  INBOUND "+game.IncomingCount+
                "\n"+(MapReactorCount>0?"REACTOR WARNINGS "+MapReactorCount:"Rocks / yards: scenery   |   Gold: selected");
        }
    }
}
