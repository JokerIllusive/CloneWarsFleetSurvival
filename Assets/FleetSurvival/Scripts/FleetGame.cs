using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

namespace FleetSurvival
{
    public sealed class FleetGame : MonoBehaviour
    {
        public readonly List<FleetShip> Ships=new List<FleetShip>();
        public readonly List<FleetBolt> Bolts=new List<FleetBolt>();
        public Faction PlayerFaction { get; private set; }
        public BattlePhase Phase { get; private set; }=BattlePhase.Menu;
        public int Wave { get; private set; }
        public int Salvage { get; private set; }
        public int Kills { get; private set; }
        public float BattleTime { get; private set; }
        public bool Paused { get; private set; }
        public int BestWave { get; private set; }
        public string Message { get; private set; }="Choose a faction to begin.";
        public Camera ViewCamera { get; private set; }
        public FleetHUD HUD { get; private set; }
        public FleetShip Flagship => Ships.FirstOrDefault(s=>s!=null && s.Alive && s.Friendly && s.IsFlagship);
        public int FriendlyCount => Ships.Count(s=>s!=null && s.Alive && s.Friendly);
        public int EnemyCount => Ships.Count(s=>s!=null && s.Alive && !s.Friendly);
        public int SelectedCount => Ships.Count(s=>s!=null && s.Alive && s.Selected);
        public bool AttackMoveMode { get; private set; }
        public bool SmokeMode { get; private set; }
        public bool IsDragging { get; private set; }
        public Vector2 DragStart { get; private set; }
        public Vector2 DragCurrent { get; private set; }
        Vector3 cameraFocus=new Vector3(0,0,0);
        float cameraDistance=103, spawnTimer, messageTimer, soundTimer;
        int enemiesRemaining, spawnIndex;
        bool bossQueued, gesture;
        Vector2 mouseStart;
        readonly Plane commandPlane=new Plane(Vector3.up,Vector3.zero);
        Transform world, shipsRoot, effectsRoot;
        AudioSource audioSource;
        AudioClip laserClip, explosionClip, waveClip;
        bool muted;

        void Awake()
        {
            Application.targetFrameRate=60;
            BestWave=PlayerPrefs.GetInt("FleetSurvival.BestWave",0);
            SmokeMode=Environment.GetCommandLineArgs().Contains("-fleet-smoke-test");
            BuildWorld();
            HUD=gameObject.AddComponent<FleetHUD>(); HUD.Initialize(this);
            SpawnDisplayFleet();
        }

        void Start() { if(SmokeMode) StartCoroutine(SmokeTest()); }

        void BuildWorld()
        {
            world=new GameObject("BattleSector").transform;
            shipsRoot=new GameObject("Fleets").transform;
            effectsRoot=new GameObject("CombatEffects").transform;
            var cameraGo=new GameObject("TacticalCamera",typeof(Camera),typeof(AudioListener));
            ViewCamera=cameraGo.GetComponent<Camera>(); cameraGo.tag="MainCamera";
            ViewCamera.backgroundColor=new Color(.014f,.025f,.065f); ViewCamera.clearFlags=CameraClearFlags.SolidColor;
            ViewCamera.fieldOfView=48; ViewCamera.nearClipPlane=.3f; ViewCamera.farClipPlane=1000;
            PositionCamera();
            var light=new GameObject("Starlight",typeof(Light)).GetComponent<Light>();
            light.type=LightType.Directional; light.intensity=1.25f; light.color=new Color(.68f,.82f,1);
            light.transform.rotation=Quaternion.Euler(42,-28,0);
            var fill=new GameObject("PlanetBounce",typeof(Light)).GetComponent<Light>();
            fill.type=LightType.Directional; fill.color=new Color(.22f,.39f,.85f); fill.intensity=.65f;
            fill.transform.rotation=Quaternion.Euler(-30,130,0);
            var cisFill=new GameObject("CIS hull fill",typeof(Light)).GetComponent<Light>();
            cisFill.type=LightType.Directional; cisFill.intensity=1.8f; cisFill.color=new Color(.9f,.95f,1);
            cisFill.cullingMask=1<<8; cisFill.transform.rotation=Quaternion.Euler(60,-35,0);
            RenderSettings.ambientLight=new Color(.23f,.3f,.43f); RenderSettings.skybox=null;
            // Stars are one mesh, rather than hundreds of separate objects.
            var starGo=new GameObject("Starfield",typeof(MeshFilter),typeof(MeshRenderer));
            starGo.transform.SetParent(world,false);
            var vertices=new List<Vector3>(); var triangles=new List<int>(); var colors=new List<Color>();
            var rng=new System.Random(7301);
            for(int i=0;i<850;i++)
            {
                float x=(float)rng.NextDouble()*500-250, z=(float)rng.NextDouble()*500-250;
                float y=-70-(float)rng.NextDouble()*60, size=.12f+(float)rng.NextDouble()*.34f;
                int n=vertices.Count;
                vertices.Add(new Vector3(x-size,y,z-size)); vertices.Add(new Vector3(x+size,y,z-size));
                vertices.Add(new Vector3(x+size,y,z+size)); vertices.Add(new Vector3(x-size,y,z+size));
                triangles.AddRange(new[]{n,n+2,n+1,n,n+3,n+2});
                float brightness=.35f+(float)rng.NextDouble()*.65f;
                for(int j=0;j<4;j++) colors.Add(new Color(brightness*.8f,brightness*.9f,brightness));
            }
            var starMesh=new Mesh { name="Sector stars",vertices=vertices.ToArray(),triangles=triangles.ToArray(),colors=colors.ToArray() };
            starMesh.RecalculateBounds(); starGo.GetComponent<MeshFilter>().sharedMesh=starMesh;
            var starMat=Resources.Load<Material>("FleetStars");
            starGo.GetComponent<Renderer>().sharedMaterial=starMat!=null?starMat:ShipVisuals.Material(new Color(.5f,.65f,.9f),true);
            var planet=GameObject.CreatePrimitive(PrimitiveType.Sphere); planet.name="Outer Rim planet";
            planet.transform.SetParent(world,false); planet.transform.position=new Vector3(135,-98,75); planet.transform.localScale=Vector3.one*120;
            Destroy(planet.GetComponent<Collider>()); planet.GetComponent<Renderer>().sharedMaterial=ShipVisuals.Material(new Color(.055f,.16f,.33f));
            var atmosphere=ShipVisuals.Ring(world,62,new Color(.18f,.5f,1),.65f,"Planet atmosphere");
            atmosphere.transform.position=planet.transform.position+new Vector3(0,2,0);
            var boundary=ShipVisuals.Ring(world,FleetRules.ArenaRadius,new Color(.06f,.2f,.32f),.08f,"Sector boundary");
            boundary.transform.position=Vector3.down;
            for(int i=-6;i<=6;i++)
            {
                GridLine(new Vector3(i*12,-1,-72),new Vector3(i*12,-1,72));
                GridLine(new Vector3(-72,-1,i*12),new Vector3(72,-1,i*12));
            }
            audioSource=gameObject.AddComponent<AudioSource>(); audioSource.spatialBlend=0; audioSource.volume=.2f;
            laserClip=Tone("Turbolaser",.12f,800,120,.12f);
            explosionClip=Tone("Reactor burst",.4f,160,25,.3f,true);
            waveClip=Tone("Incoming fleet",.5f,330,500,.2f);
        }

        void GridLine(Vector3 a,Vector3 b)
        {
            var line=new GameObject("Tactical grid",typeof(LineRenderer)).GetComponent<LineRenderer>();
            line.transform.SetParent(world); line.positionCount=2; line.SetPosition(0,a); line.SetPosition(1,b);
            line.sharedMaterial=ShipVisuals.Material(new Color(.026f,.07f,.12f),true); line.startWidth=line.endWidth=.035f;
        }

        AudioClip Tone(string name,float duration,float start,float end,float volume,bool noise=false)
        {
            int count=(int)(22050*duration); var samples=new float[count];
            var rng=new System.Random(71); float phase=0;
            for(int i=0;i<count;i++)
            {
                float t=(float)i/count; phase+=Mathf.Lerp(start,end,t)*2*Mathf.PI/22050;
                samples[i]=(noise?(float)rng.NextDouble()*2-1:Mathf.Sin(phase))*Mathf.Pow(1-t,2)*volume;
            }
            var clip=AudioClip.Create(name,count,1,22050,false); clip.SetData(samples,0); return clip;
        }

        void SpawnDisplayFleet()
        {
            Spawn(Faction.Republic,ShipClass.Flagship,true,false,new Vector3(-13,0,-6));
            Spawn(Faction.CIS,ShipClass.Flagship,false,false,new Vector3(14,0,12));
            Spawn(Faction.Republic,ShipClass.Fighter,true,false,new Vector3(-3,0,-1));
            Spawn(Faction.CIS,ShipClass.Frigate,false,false,new Vector3(9,0,1));
        }

        public FleetShip Spawn(Faction faction,ShipClass kind,bool friendly,bool flagship,Vector3 position,float multiplier=1)
        {
            var go=new GameObject((friendly?"Allied ":"Enemy ")+FleetRules.Stats(faction,kind).Name);
            go.transform.SetParent(shipsRoot); go.transform.position=position;
            go.transform.rotation=Quaternion.Euler(0,friendly?0:180,0);
            var ship=go.AddComponent<FleetShip>(); ship.Configure(this,faction,kind,friendly,flagship,multiplier);
            Ships.Add(ship); return ship;
        }

        public void Begin(Faction faction)
        {
            ClearBattle(); PlayerFaction=faction; Wave=0; Kills=0; BattleTime=0;
            Salvage=FleetRules.StartingSalvage; Paused=false; Phase=BattlePhase.Preparation;
            cameraFocus=Vector3.zero; cameraDistance=103; PositionCamera();
            var flagship=Spawn(faction,ShipClass.Flagship,true,true,new Vector3(0,0,-12)); flagship.Selected=true;
            Spawn(faction,ShipClass.Frigate,true,false,new Vector3(-11,0,-3));
            Spawn(faction,ShipClass.Frigate,true,false,new Vector3(11,0,-3));
            Spawn(faction,ShipClass.Fighter,true,false,new Vector3(-8,0,9));
            Spawn(faction,ShipClass.Fighter,true,false,new Vector3(8,0,9));
            Notify("Fleet ready. Reinforce, position your ships, then launch the first wave.");
            HUD.Rebuild();
        }

        public void ClearBattle()
        {
            foreach(var s in Ships) if(s!=null) Destroy(s.gameObject);
            foreach(var b in Bolts) if(b!=null) Destroy(b.gameObject);
            Ships.Clear(); Bolts.Clear();
            foreach(Transform effect in effectsRoot) Destroy(effect.gameObject);
            gesture=false; IsDragging=false; AttackMoveMode=false;
        }

        public void ReturnToMenu()
        { ClearBattle(); Paused=false; Phase=BattlePhase.Menu; cameraFocus=Vector3.zero; PositionCamera(); SpawnDisplayFleet(); HUD.Rebuild(); }

        public void LaunchWave()
        {
            if(Phase!=BattlePhase.Preparation || Paused) return;
            Wave++; Phase=BattlePhase.Combat;
            enemiesRemaining=FleetRules.WaveBudget(Wave); spawnTimer=.4f; spawnIndex=0; bossQueued=Wave%5==0;
            Notify(bossQueued?"CAPITAL ASSAULT — enemy flagship inbound!":"Enemy contacts. Protect your flagship.");
            if(!muted) audioSource.PlayOneShot(waveClip);
            HUD.Rebuild();
        }

        public bool Recruit(ShipClass kind)
        {
            if(Phase!=BattlePhase.Preparation || Paused || kind==ShipClass.Flagship) return false;
            int cost=FleetRules.Stats(PlayerFaction,kind).Cost;
            if(Salvage<cost) { Notify("Insufficient salvage for that reinforcement."); return false; }
            if(FriendlyCount>=FleetRules.FleetLimit) { Notify("Fleet capacity reached (22 ships)."); return false; }
            Salvage-=cost;
            Vector3 origin=Flagship!=null?Flagship.transform.position:Vector3.zero;
            Vector3 position=FindReinforcementPosition(origin,kind);
            Spawn(PlayerFaction,kind,true,false,position);
            Notify("Reinforcement deployed: "+FleetRules.Stats(PlayerFaction,kind).Name+"."); return true;
        }

        Vector3 FindReinforcementPosition(Vector3 origin,ShipClass kind)
        {
            float radius=FleetRules.Stats(PlayerFaction,kind).Radius;
            for(int ring=0;ring<6;ring++) for(int i=0;i<12;i++)
            {
                float a=i*Mathf.PI/6;
                Vector3 position=origin+new Vector3(Mathf.Sin(a),0,Mathf.Cos(a))*(12+ring*8);
                if(position.magnitude>FleetRules.ArenaRadius-radius) continue;
                if(Ships.All(s=>s==null || !s.Alive || Vector3.Distance(s.transform.position,position)>radius+s.Stats.Radius+1)) return position;
            }
            return Vector3.zero;
        }

        public bool RepairFleet()
        {
            if(Phase!=BattlePhase.Preparation || Paused) return false;
            if(!Ships.Any(s=>s!=null && s.Friendly && s.Hull<s.MaxHull-.1f)) { Notify("All hulls are already intact."); return false; }
            const int cost=120;
            if(Salvage<cost) { Notify("Repairs require 120 salvage."); return false; }
            Salvage-=cost;
            foreach(var s in Ships) if(s!=null && s.Friendly) { s.Hull=s.MaxHull; s.Shield=s.MaxShield; s.Destruction.Repair(); }
            Notify("Fleet hulls repaired and shields restored."); return true;
        }

        public bool UpgradeWeapons()
        {
            if(Phase!=BattlePhase.Preparation || Paused) return false;
            const int cost=200;
            if(Salvage<cost) { Notify("Weapon refit requires 200 salvage."); return false; }
            Salvage-=cost;
            foreach(var s in Ships) if(s!=null && s.Friendly) { var stats=s.Stats; stats.Damage*=1.18f; s.Stats=stats; }
            Notify("Current fleet weapons improved by 18%. New ships require a later refit."); return true;
        }

        void Update()
        {
            soundTimer-=Time.unscaledDeltaTime; messageTimer-=Time.unscaledDeltaTime;
            HandleCamera(); HandleInput();
            if(Phase==BattlePhase.Menu)
            { foreach(var s in Ships) if(s!=null) s.transform.Rotate(0,Time.deltaTime*2.5f,0); return; }
            if(Paused || Phase==BattlePhase.Defeat) return;
            Tick(Time.deltaTime);
        }

        public void Tick(float dt)
        {
            if(Paused) return;
            if(Phase!=BattlePhase.Combat && Phase!=BattlePhase.Preparation) return;
            if(Phase==BattlePhase.Combat)
            {
                BattleTime+=dt; spawnTimer-=dt;
                if(spawnTimer<=0 && (enemiesRemaining>0 || bossQueued))
                {
                    Faction enemy=PlayerFaction==Faction.Republic?Faction.CIS:Faction.Republic;
                    ShipClass kind;
                    if(bossQueued) { kind=ShipClass.Flagship; bossQueued=false; }
                    else
                    {
                        if(Wave>=6 && enemiesRemaining>=6 && spawnIndex%4==0) { kind=ShipClass.Carrier; enemiesRemaining-=6; }
                        else if(Wave>=3 && enemiesRemaining>=5 && spawnIndex%3==0) { kind=ShipClass.Destroyer; enemiesRemaining-=5; }
                        else if(enemiesRemaining>=3 && (spawnIndex%3==0 || Wave>=4 && spawnIndex%2==0)) { kind=ShipClass.Frigate; enemiesRemaining-=3; }
                        else { kind=spawnIndex%2==0?ShipClass.Interceptor:ShipClass.Fighter; enemiesRemaining--; }
                    }
                    float a=(spawnIndex*137.5f+Wave*43)*Mathf.Deg2Rad;
                    Spawn(enemy,kind,false,false,new Vector3(Mathf.Sin(a),0,Mathf.Cos(a))*(FleetRules.ArenaRadius-7),FleetRules.EnemyMultiplier(Wave));
                    spawnIndex++; spawnTimer=1.6f;
                }
            }
            for(int i=Ships.Count-1;i>=0;i--) if(i<Ships.Count && Ships[i]!=null) Ships[i].Tick(dt);
            for(int i=Bolts.Count-1;i>=0;i--) if(i<Bolts.Count && Bolts[i]!=null) Bolts[i].Tick(dt);
            if(Phase==BattlePhase.Combat && enemiesRemaining<=0 && !bossQueued && EnemyCount==0)
            {
                Phase=BattlePhase.Preparation; Salvage+=FleetRules.WaveReward(Wave);
                foreach(var s in Ships) if(s!=null && s.Friendly) { s.Shield=s.MaxShield; s.ForcedTarget=null; }
                SaveBest(); Notify("Wave "+Wave+" cleared. +"+FleetRules.WaveReward(Wave)+" salvage. Reinforce for the next assault.");
                HUD.Rebuild();
            }
        }

        public FleetShip NearestEnemy(FleetShip from)
        {
            FleetShip result=null; float best=float.MaxValue;
            foreach(var ship in Ships)
            {
                if(ship==null || !ship.Alive || ship.Friendly==from.Friendly) continue;
                float score=(ship.transform.position-from.transform.position).sqrMagnitude;
                // Enemy capitals prioritize the command ship when distances are comparable.
                if(!from.Friendly && ship.IsFlagship) score*=.72f;
                if(score<best) { best=score; result=ship; }
            }
            return result;
        }

        public void Fire(FleetShip from,FleetShip target,float damage)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube); go.name="Turbolaser bolt";
            Destroy(go.GetComponent<Collider>()); go.transform.SetParent(effectsRoot);
            go.transform.position=from.transform.position+Vector3.up*.9f+from.transform.forward*from.Stats.Radius*.5f;
            go.transform.localScale=new Vector3(.14f,.14f,from.Kind==ShipClass.Fighter?1.3f:2.8f);
            go.GetComponent<Renderer>().sharedMaterial=ShipVisuals.Material(from.Faction==Faction.Republic?new Color(.25f,.65f,1):new Color(.3f,1,.35f),true);
            var bolt=go.AddComponent<FleetBolt>(); bolt.Game=this; bolt.Target=target; bolt.Damage=damage; Bolts.Add(bolt);
            if(soundTimer<=0 && !muted) { audioSource.PlayOneShot(laserClip); soundTimer=.12f; }
        }

        public void RemoveBolt(FleetBolt bolt) { Bolts.Remove(bolt); if(bolt!=null) Destroy(bolt.gameObject); }
        public void AddCombatEffect(GameObject effect) { effect.transform.SetParent(effectsRoot,true); }

        public void ShipDestroyed(FleetShip ship)
        {
            ship.Destruction.BreakApart();
            Burst(ship.transform.position,ship.Stats.Radius);
            if(!ship.Friendly) { Salvage+=ship.Stats.Salvage; Kills++; }
            bool lostFlagship=ship.Friendly && ship.IsFlagship;
            Ships.Remove(ship); Destroy(ship.gameObject);
            if(lostFlagship)
            {
                Phase=BattlePhase.Defeat; Paused=false; SaveBest();
                Notify("Command ship lost. Your fleet has been defeated."); HUD.Rebuild();
            }
        }

        void Burst(Vector3 position,float radius)
        {
            var go=new GameObject("Reactor explosion",typeof(ParticleSystem)); go.transform.position=position;
            AddCombatEffect(go);
            var system=go.GetComponent<ParticleSystem>(); var main=system.main;
            main.duration=.8f; main.loop=false; main.startLifetime=.8f; main.startSpeed=radius*5; main.startSize=radius*.4f;
            main.startColor=new ParticleSystem.MinMaxGradient(new Color(1,.32f,.08f),new Color(1,.85f,.2f)); main.maxParticles=70;
            var emission=system.emission; emission.rateOverTime=0; emission.SetBursts(new[]{new ParticleSystem.Burst(0,50)});
            var shape=system.shape; shape.shapeType=ParticleSystemShapeType.Sphere; shape.radius=.5f;
            var size=system.sizeOverLifetime; size.enabled=true; size.size=new ParticleSystem.MinMaxCurve(1,new AnimationCurve(new Keyframe(0,1),new Keyframe(1,0)));
            var renderer=go.GetComponent<ParticleSystemRenderer>(); renderer.sharedMaterial=Resources.Load<Material>("FleetParticle");
            system.Play(); Destroy(go,2);
            if(!muted) audioSource.PlayOneShot(explosionClip);
        }

        void SaveBest()
        {
            if(SmokeMode) return;
            int cleared=Phase==BattlePhase.Defeat?Mathf.Max(0,Wave-1):Wave;
            if(cleared<=BestWave) return;
            BestWave=cleared; PlayerPrefs.SetInt("FleetSurvival.BestWave",BestWave); PlayerPrefs.Save();
        }

        public void Notify(string text) { Message=text; messageTimer=8; }
        public void TogglePause()
        {
            if(Phase==BattlePhase.Menu || Phase==BattlePhase.Defeat) return;
            Paused=!Paused; gesture=false; IsDragging=false; HUD.Rebuild();
        }
        public void ToggleMute() { muted=!muted; audioSource.mute=muted; HUD.SetMute(muted); }
        public void SelectAll() { foreach(var s in Ships) if(s!=null && s.Alive && s.Friendly) s.Selected=true; }
        public void SelectFlagship()
        { foreach(var s in Ships) if(s!=null) s.Selected=s.Friendly && s.IsFlagship; if(Flagship!=null) { cameraFocus=Flagship.transform.position; PositionCamera(); } }

        void HandleInput()
        {
            if(Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Space)) TogglePause();
            if(Phase==BattlePhase.Menu || Phase==BattlePhase.Defeat || Paused || SmokeMode) return;
            if(Input.GetKeyDown(KeyCode.Tab)) SelectAll();
            if(Input.GetKeyDown(KeyCode.Q)) SelectFlagship();
            if(Input.GetKeyDown(KeyCode.F)) { AttackMoveMode=!AttackMoveMode; Notify(AttackMoveMode?"Attack-move ready. Right-click a destination.":"Move orders ready."); }
            if(Input.GetKeyDown(KeyCode.Alpha1)) Recruit(ShipClass.Fighter);
            if(Input.GetKeyDown(KeyCode.Alpha2)) Recruit(ShipClass.Frigate);
            if(Input.GetKeyDown(KeyCode.Alpha3)) Recruit(ShipClass.Destroyer);
            if(Input.GetKeyDown(KeyCode.R)) RepairFleet();
            bool overUI=EventSystem.current!=null && EventSystem.current.IsPointerOverGameObject();
            if(Input.GetMouseButtonDown(0) && !overUI) { mouseStart=Input.mousePosition; DragStart=mouseStart; gesture=true; }
            if(gesture && Input.GetMouseButton(0)) { DragCurrent=Input.mousePosition; IsDragging=(DragCurrent-mouseStart).magnitude>9; }
            if(gesture && Input.GetMouseButtonUp(0))
            {
                bool additive=Input.GetKey(KeyCode.LeftShift)||Input.GetKey(KeyCode.RightShift);
                if(!additive) foreach(var s in Ships) if(s!=null) s.Selected=false;
                if(IsDragging)
                {
                    Rect rect=ScreenRect(mouseStart,Input.mousePosition);
                    foreach(var s in Ships) if(s!=null && s.Alive && s.Friendly)
                    { var p=ViewCamera.WorldToScreenPoint(s.transform.position); if(p.z>0 && rect.Contains(p)) s.Selected=true; }
                }
                else if(Physics.Raycast(ViewCamera.ScreenPointToRay(Input.mousePosition),out var hit,500))
                {
                    var ship=hit.collider.GetComponent<FleetShip>();
                    if(ship!=null && ship.Friendly) ship.Selected=additive?!ship.Selected:true;
                }
                gesture=false; IsDragging=false;
            }
            if(Input.GetMouseButtonDown(1) && !overUI)
            {
                var ray=ViewCamera.ScreenPointToRay(Input.mousePosition);
                FleetShip target=null;
                if(Physics.Raycast(ray,out var hit,500)) target=hit.collider.GetComponent<FleetShip>();
                if(target!=null && !target.Friendly)
                {
                    foreach(var s in Ships) if(s!=null && s.Selected) { s.ForcedTarget=target; s.HasMoveOrder=false; }
                    Notify("Focus fire: "+target.Stats.Name+".");
                }
                else if(commandPlane.Raycast(ray,out float enter))
                {
                    Vector3 destination=ray.GetPoint(enter);
                    destination=Vector3.ClampMagnitude(destination,FleetRules.ArenaRadius-7);
                    OrderMove(destination,AttackMoveMode); AttackMoveMode=false;
                }
            }
        }

        public void OrderMove(Vector3 destination,bool attackMove)
        {
            var selected=Ships.Where(s=>s!=null && s.Alive && s.Selected && s.Friendly).ToArray();
            if(selected.Length==0) { Notify("Select allied ships first, or press Tab to select the fleet."); return; }
            int width=Mathf.CeilToInt(Mathf.Sqrt(selected.Length));
            for(int i=0;i<selected.Length;i++)
            {
                Vector3 offset=new Vector3((i%width-(width-1)*.5f)*10,0,(i/width)*10);
                selected[i].MoveTo(Vector3.ClampMagnitude(destination+offset,FleetRules.ArenaRadius-5),attackMove);
            }
            var marker=ShipVisuals.Ring(effectsRoot,2.2f,new Color(.2f,.9f,1),.16f,"Move destination"); marker.transform.position=destination; Destroy(marker.gameObject,1.5f);
            Notify(attackMove?"Attack-move order issued.":"Formation move order issued.");
        }

        public static Rect ScreenRect(Vector2 a,Vector2 b) => Rect.MinMaxRect(Mathf.Min(a.x,b.x),Mathf.Min(a.y,b.y),Mathf.Max(a.x,b.x),Mathf.Max(a.y,b.y));

        void HandleCamera()
        {
            if(SmokeMode) return;
            if(Phase!=BattlePhase.Menu)
            {
                float x=Input.GetAxisRaw("Horizontal"), z=Input.GetAxisRaw("Vertical");
                cameraFocus+=new Vector3(x,0,z)*Time.unscaledDeltaTime*cameraDistance*.45f;
                if(Input.GetMouseButton(2)) cameraFocus+=new Vector3(-Input.GetAxis("Mouse X"),0,-Input.GetAxis("Mouse Y"))*cameraDistance*.025f;
                cameraFocus=Vector3.ClampMagnitude(cameraFocus,FleetRules.ArenaRadius);
                if(EventSystem.current==null || !EventSystem.current.IsPointerOverGameObject()) cameraDistance=Mathf.Clamp(cameraDistance-Input.mouseScrollDelta.y*6,48,185);
            }
            PositionCamera();
        }
        void PositionCamera()
        { ViewCamera.transform.position=cameraFocus+new Vector3(0,.82f,-.57f).normalized*cameraDistance; ViewCamera.transform.LookAt(cameraFocus); }

        IEnumerator SmokeTest()
        {
            yield return null;
            var assertions=new List<string>();
            try
            {
                foreach(var faction in new[]{Faction.Republic,Faction.CIS})
                {
                    Begin(faction);
                    Check(FriendlyCount==5 && Flagship!=null,"Starting fleet "+faction,assertions);
                    int initial=Salvage;
                    Check(Recruit(ShipClass.Fighter),"Recruit fighter "+faction,assertions);
                    Check(Salvage==initial-FleetRules.Stats(faction,ShipClass.Fighter).Cost,"Recruitment cost "+faction,assertions);
                    Flagship.Damage(Flagship.Shield+Flagship.MaxHull*.4f);
                    Check(Flagship.Destruction.DetachedSections==1,"Procedural hull damage "+faction,assertions);
                    Check(RepairFleet() && Flagship.Hull==Flagship.MaxHull && Flagship.Destruction.DetachedSections==0,"Fleet repair restores sections "+faction,assertions);
                    Check(!Recruit(ShipClass.Flagship),"No flagship purchase "+faction,assertions);
                    SelectAll(); OrderMove(new Vector3(5,0,5),false);
                    Check(Ships.All(s=>s.HasMoveOrder),"Formation orders "+faction,assertions);
                    foreach(var s in Ships) s.HasMoveOrder=false;
                    LaunchWave(); Check(!Recruit(ShipClass.Fighter),"Combat shop blocked "+faction,assertions);
                    TogglePause(); float time=BattleTime; Tick(.5f); Check(Paused && BattleTime==time,"Pause freezes simulation "+faction,assertions); TogglePause();
                    int steps=0;
                    while(Phase==BattlePhase.Combat && steps++<6000) Tick(.04f);
                    Check(Phase==BattlePhase.Preparation && Kills>0,"Complete survival wave "+faction,assertions);
                    LaunchWave(); var flagship=Flagship; flagship.Damage(flagship.Shield+flagship.Hull+1);
                    Check(Phase==BattlePhase.Defeat,"Flagship defeat "+faction,assertions);
                }
                ReturnToMenu(); Check(Phase==BattlePhase.Menu,"Return to faction menu",assertions);
                File.WriteAllText(Path.Combine(Application.persistentDataPath,"fleet-smoke-test.json"),JsonUtility.ToJson(new SmokeReport{passed=true,checks=assertions.ToArray()},true));
                Debug.Log("FLEET_SMOKE_PASS "+assertions.Count+" assertions");
            }
            catch(Exception ex)
            {
                Debug.LogException(ex);
                File.WriteAllText(Path.Combine(Application.persistentDataPath,"fleet-smoke-test.json"),JsonUtility.ToJson(new SmokeReport{passed=false,error=ex.ToString(),checks=assertions.ToArray()},true));
                Application.Quit(1); yield break;
            }
            // Leave a combat scene visible briefly for a screenshot during automated verification.
            Begin(Faction.Republic); LaunchWave();
            for(int i=0;i<300;i++) Tick(.04f);
            yield return null;
            yield return new WaitForEndOfFrame();
            HUD.CapturePreview(Path.Combine(Application.persistentDataPath,"fleet-smoke-preview.png"));
            Begin(Faction.Republic); ClearBattle();
            Spawn(Faction.Republic,ShipClass.Flagship,true,true,new Vector3(-20,0,3));
            var breached=Spawn(Faction.Republic,ShipClass.Destroyer,true,false,new Vector3(0,0,3));
            breached.Damage(breached.Shield+breached.MaxHull*.43f,new Vector3(0,0,8));
            var doomed=Spawn(Faction.Republic,ShipClass.Destroyer,true,false,new Vector3(20,0,3));
            doomed.Damage(doomed.Shield+doomed.Hull+1);
            cameraFocus=new Vector3(0,0,3); cameraDistance=84; PositionCamera();
            Notify("PROCEDURAL DESTRUCTION: intact ship / hull breach / destroyed hull debris");
            yield return new WaitForSecondsRealtime(.6f);
            HUD.CapturePreview(Path.Combine(Application.persistentDataPath,"fleet-destruction-preview.png"));
            yield return new WaitForSecondsRealtime(.3f);
            Application.Quit(0);
        }
        static void Check(bool condition,string name,List<string> checks) { if(!condition) throw new Exception("Smoke test failed: "+name); checks.Add(name); }
        [Serializable] public sealed class SmokeReport { public bool passed; public string error; public string[] checks; }
    }
}
