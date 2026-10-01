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
        readonly List<FleetFormation> formations=new List<FleetFormation>();
        readonly List<FleetJump> jumps=new List<FleetJump>();
        GameObject placementGhost;
        public ShipClass? SelectedReinforcement { get; private set; }
        public int IncomingCount => jumps.Count;
        public int FleetCapacityUsed => FriendlyCount+jumps.Count(j=>j.Ship==null);
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
        float cameraDistance=103, spawnTimer, messageTimer;
        int enemiesRemaining, spawnIndex;
        bool bossQueued, gesture;
        Vector2 mouseStart;
        readonly Plane commandPlane=new Plane(Vector3.up,Vector3.zero);
        Transform world, shipsRoot, effectsRoot;
        AudioSource audioSource;
        AudioClip explosionClip, waveClip;
        readonly List<AudioSource> soundVoices=new List<AudioSource>();
        int nextVoice;
        bool muted;

        void Awake()
        {
            Application.targetFrameRate=60;
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
            var desktop=Screen.currentResolution;
            Screen.SetResolution(desktop.width,desktop.height,FullScreenMode.FullScreenWindow);
#endif
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
            RenderSettings.ambientLight=new Color(.23f,.3f,.43f);
            new GameObject("Orbital environment",typeof(FleetEnvironment)).GetComponent<FleetEnvironment>().Initialize(this,world,-light.transform.forward);
            var boundary=ShipVisuals.Ring(world,FleetRules.ArenaRadius,new Color(.06f,.2f,.32f),.08f,"Sector boundary");
            boundary.transform.position=Vector3.down;
            for(int i=-6;i<=6;i++)
            {
                GridLine(new Vector3(i*12,-1,-72),new Vector3(i*12,-1,72));
                GridLine(new Vector3(-72,-1,i*12),new Vector3(72,-1,i*12));
            }
            audioSource=gameObject.AddComponent<AudioSource>(); audioSource.spatialBlend=0; audioSource.volume=.2f;
            explosionClip=FleetSound.Get("Explosion");
            waveClip=Tone("Incoming fleet",.5f,330,500,.2f);
            for(int i=0;i<24;i++) { var voice=new GameObject("Battle sound voice "+i,typeof(AudioSource)).GetComponent<AudioSource>(); voice.transform.SetParent(transform); voice.playOnAwake=false; voice.spatialBlend=0; soundVoices.Add(voice); }
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
            CancelReinforcementPlacement(); foreach(var jump in jumps) jump.Cancel(); jumps.Clear();
            foreach(var s in Ships) if(s!=null) Destroy(s.gameObject);
            foreach(var b in Bolts) if(b!=null) Destroy(b.gameObject);
            Ships.Clear(); Bolts.Clear();
            formations.Clear();
            foreach(var voice in soundVoices) voice.Stop();
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
            Vector3 origin=Flagship!=null?Flagship.transform.position:Vector3.zero;
            Vector3 position=FindReinforcementPosition(origin,kind);
            return RecruitAt(kind,position);
        }
        public int CallInCount(ShipClass kind) => PlayerFaction==Faction.Republic && kind==ShipClass.Escort?2:1;
        public int CallInCost(ShipClass kind) => FleetRules.Stats(PlayerFaction,kind).Cost*CallInCount(kind);
        public string CallInName(ShipClass kind) => CallInCount(kind)==2?"Arquitens cruiser pair":FleetRules.Stats(PlayerFaction,kind).Name+(kind==ShipClass.Fighter || kind==ShipClass.Interceptor?" (6)":"");
        Vector3[] CallInPositions(ShipClass kind,Vector3 center)
        {
            center.y=0;
            float spacing=FleetRules.Stats(PlayerFaction,kind).Radius+1;
            return CallInCount(kind)==2?new[]{center-Vector3.right*spacing,center+Vector3.right*spacing}:new[]{center};
        }
        bool IsCallInClear(ShipClass kind,Vector3 center) => CallInPositions(kind,center).All(p=>IsArrivalClear(kind,p,null));
        public bool CanCallIn(ShipClass kind) => (Phase==BattlePhase.Preparation || Phase==BattlePhase.Combat) && !Paused && kind!=ShipClass.Flagship && FleetCapacityUsed+CallInCount(kind)<=FleetRules.FleetLimit && Salvage>=CallInCost(kind);
        public bool BeginReinforcementPlacement(ShipClass kind)
        {
            if(!CanCallIn(kind)) { Notify("Call-in unavailable: check salvage, fleet capacity, and pause."); return false; }
            CancelReinforcementPlacement(); SelectedReinforcement=kind;
            placementGhost=new GameObject("Reinforcement hologram"); AddCombatEffect(placementGhost);
            foreach(var offset in CallInPositions(kind,Vector3.zero))
            { var preview=new GameObject("Arrival preview").transform; preview.SetParent(placementGhost.transform,false); preview.localPosition=offset; ShipVisuals.Build(preview,PlayerFaction,kind); }
            foreach(var renderer in placementGhost.GetComponentsInChildren<MeshRenderer>())
            { var materials=renderer.sharedMaterials; for(int i=0;i<materials.Length;i++) materials[i]=Resources.Load<Material>("FleetHologram"); renderer.sharedMaterials=materials; }
            UpdateReinforcementPreview(FindReinforcementPosition(Flagship!=null?Flagship.transform.position:Vector3.zero,kind));
            Notify("CALL IN "+CallInName(kind)+": left-click a clear arrival point. Right-click / Esc cancels."); return true;
        }
        public void UpdateReinforcementPreview(Vector3 position)
        {
            if(placementGhost==null || !SelectedReinforcement.HasValue) return;
            position.y=0; placementGhost.transform.position=position;
            var material=Resources.Load<Material>("FleetHologram");
            material.color=IsCallInClear(SelectedReinforcement.Value,position)?new Color(.1f,.7f,1,.2f):new Color(1,.15f,.05f,.26f);
        }
        public void CancelReinforcementPlacement()
        {
            SelectedReinforcement=null; if(placementGhost!=null) Destroy(placementGhost); placementGhost=null;
            gesture=false; IsDragging=false;
        }
        public bool ConfirmReinforcement(Vector3 position)
        {
            if(!SelectedReinforcement.HasValue) return false;
            bool result=RecruitAt(SelectedReinforcement.Value,position);
            if(result) CancelReinforcementPlacement(); return result;
        }
        public bool RecruitAt(ShipClass kind,Vector3 position)
        {
            if(!CanCallIn(kind)) { Notify("Call-in unavailable: check salvage, fleet capacity, and pause."); return false; }
            position.y=0;
            if(!IsCallInClear(kind,position)) { Notify("Arrival blocked. Choose clear space inside the sector."); return false; }
            Salvage-=CallInCost(kind);
            foreach(var arrival in CallInPositions(kind,position)) jumps.Add(new FleetJump(this,kind,arrival));
            Notify("Hyperspace coordinates transmitted. Reinforcement inbound (3 seconds)."); return true;
        }
        bool IsArrivalClear(ShipClass kind,Vector3 position,FleetJump ignore)
        {
            float radius=FleetRules.Stats(PlayerFaction,kind).Radius;
            if(position.magnitude>FleetRules.ArenaRadius-radius-1) return false;
            if(Ships.Any(s=>s!=null && s.Alive && !s.IsArriving && Vector3.Distance(s.transform.position,position)<s.Stats.Radius+radius+1)) return false;
            return !jumps.Any(j=>j!=ignore && Vector3.Distance(j.ReservedPosition,position)<FleetRules.Stats(PlayerFaction,j.Kind).Radius+radius+1);
        }
        public bool ResolveArrivalPosition(ShipClass kind,Vector3 requested,FleetJump ignore,out Vector3 result)
        {
            if(IsArrivalClear(kind,requested,ignore)) { result=requested; return true; }
            for(int ring=1;ring<=4;ring++) for(int i=0;i<12;i++)
            {
                float angle=i*Mathf.PI/6; var point=requested+new Vector3(Mathf.Sin(angle),0,Mathf.Cos(angle))*ring*4;
                if(IsArrivalClear(kind,point,ignore)) { result=point; return true; }
            }
            result=requested; return false;
        }
        public void RefundCallIn(ShipClass kind)
        { Salvage+=FleetRules.Stats(PlayerFaction,kind).Cost; Notify("Arrival zone blocked. Reinforcement cancelled and salvage refunded."); }

        Vector3 FindReinforcementPosition(Vector3 origin,ShipClass kind)
        {
            float radius=FleetRules.Stats(PlayerFaction,kind).Radius;
            for(int ring=0;ring<6;ring++) for(int i=0;i<12;i++)
            {
                float a=i*Mathf.PI/6;
                Vector3 position=origin+new Vector3(Mathf.Sin(a),0,Mathf.Cos(a))*(12+ring*8);
                if(position.magnitude>FleetRules.ArenaRadius-radius) continue;
                if(IsCallInClear(kind,position)) return position;
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
            foreach(var s in Ships) if(s!=null && s.Friendly) { s.Hull=s.MaxHull; s.Shield=s.MaxShield; s.Destruction.Repair(); if(s.Squadron!=null) s.Squadron.Repair(); }
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
            messageTimer-=Time.unscaledDeltaTime;
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
            for(int i=jumps.Count-1;i>=0;i--) if(!jumps[i].Tick(dt)) jumps.RemoveAt(i);
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
            for(int i=formations.Count-1;i>=0;i--) if(!formations[i].Tick(dt)) formations.RemoveAt(i);
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
                if(ship==null || !ship.Alive || ship.IsArriving || ship.Friendly==from.Friendly) continue;
                float score=(ship.transform.position-from.transform.position).sqrMagnitude;
                // Enemy capitals prioritize the command ship when distances are comparable.
                if(!from.Friendly && ship.IsFlagship) score*=.72f;
                if(score<best) { best=score; result=ship; }
            }
            return result;
        }

        public void Fire(FleetShip from,FleetShip target,float damage)
        {
            from.Weapons.FireVolley(target,damage);
            bool fighter=from.Kind==ShipClass.Fighter || from.Kind==ShipClass.Interceptor;
            PlayBattleSound(FleetSound.Weapon(from.Faction,from.Kind),from.transform.position,fighter?.065f:.13f);
        }

        public void SpawnBolt(FleetShip from,FleetShip target,float damage,Vector3 muzzle,Color color)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube); go.name="Turbolaser bolt";
            Destroy(go.GetComponent<Collider>()); go.transform.SetParent(effectsRoot);
            go.transform.position=muzzle;
            go.transform.localScale=new Vector3(.14f,.14f,from.Kind==ShipClass.Fighter?1.3f:2.8f);
            go.GetComponent<Renderer>().sharedMaterial=ShipVisuals.Material(color,true);
            var core=GameObject.CreatePrimitive(PrimitiveType.Cube); Destroy(core.GetComponent<Collider>()); core.transform.SetParent(go.transform,false); core.transform.localScale=new Vector3(.4f,.4f,1.01f); core.GetComponent<Renderer>().sharedMaterial=ShipVisuals.Material(Color.white,true);
            var bolt=go.AddComponent<FleetBolt>(); bolt.Game=this; bolt.Target=target; bolt.Damage=damage; Bolts.Add(bolt);
        }
        public void PlayBattleSound(AudioClip clip,Vector3 position,float volume)
        {
            if(muted || clip==null) return;
            var voice=soundVoices[nextVoice++%soundVoices.Count]; voice.Stop(); voice.clip=clip; voice.volume=volume;
            voice.panStereo=Mathf.Clamp((ViewCamera.WorldToViewportPoint(position).x-.5f)*1.5f,-.8f,.8f); voice.pitch=UnityEngine.Random.Range(.96f,1.04f); voice.Play();
        }

        public void RemoveBolt(FleetBolt bolt) { Bolts.Remove(bolt); if(bolt!=null) Destroy(bolt.gameObject); }
        public void AddCombatEffect(GameObject effect) { effect.transform.SetParent(effectsRoot,true); }
        public void FighterLost(Vector3 position) { Burst(position,.8f); }

        public void ShipDestroyed(FleetShip ship)
        {
            ship.Destruction.BreakApart();
            Burst(ship.transform.position,ship.Stats.Radius);
            if(!ship.Friendly) { Salvage+=ship.Stats.Salvage; Kills++; }
            bool lostFlagship=ship.Friendly && ship.IsFlagship;
            Ships.Remove(ship); Destroy(ship.gameObject);
            if(lostFlagship)
            {
                CancelReinforcementPlacement(); foreach(var jump in jumps) jump.Cancel(); jumps.Clear();
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
            PlayBattleSound(explosionClip,position,.25f);
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
            foreach(var voice in soundVoices) { if(Paused) voice.Pause(); else voice.UnPause(); }
        }
        public void ToggleMute() { muted=!muted; audioSource.mute=muted; foreach(var voice in soundVoices) voice.mute=muted; HUD.SetMute(muted); }
        public void SelectAll() { foreach(var s in Ships) if(s!=null && s.Alive && s.Friendly && !s.IsArriving) s.Selected=true; }
        public void SelectFlagship()
        { foreach(var s in Ships) if(s!=null) s.Selected=s.Friendly && s.IsFlagship; if(Flagship!=null) { cameraFocus=Flagship.transform.position; PositionCamera(); } }

        void HandleInput()
        {
            if(Input.GetKeyDown(KeyCode.Escape) && SelectedReinforcement.HasValue) { CancelReinforcementPlacement(); return; }
            if(Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Space)) TogglePause();
            if(Phase==BattlePhase.Menu || Phase==BattlePhase.Defeat || Paused || SmokeMode) return;
            if(Input.GetKeyDown(KeyCode.Tab)) SelectAll();
            if(Input.GetKeyDown(KeyCode.Q)) SelectFlagship();
            if(Input.GetKeyDown(KeyCode.F)) { AttackMoveMode=!AttackMoveMode; Notify(AttackMoveMode?"Attack-move ready. Right-click a destination.":"Move orders ready."); }
            if(Input.GetKeyDown(KeyCode.Alpha1)) BeginReinforcementPlacement(ShipClass.Fighter);
            if(Input.GetKeyDown(KeyCode.Alpha2)) BeginReinforcementPlacement(ShipClass.Frigate);
            if(Input.GetKeyDown(KeyCode.Alpha3)) BeginReinforcementPlacement(ShipClass.Destroyer);
            if(Input.GetKeyDown(KeyCode.R)) RepairFleet();
            bool overUI=EventSystem.current!=null && EventSystem.current.IsPointerOverGameObject();
            if(SelectedReinforcement.HasValue)
            {
                if(Input.GetMouseButtonDown(1)) { CancelReinforcementPlacement(); return; }
                if(!overUI && commandPlane.Raycast(ViewCamera.ScreenPointToRay(Input.mousePosition),out float arrivalEnter))
                { var point=ViewCamera.ScreenPointToRay(Input.mousePosition).GetPoint(arrivalEnter); UpdateReinforcementPreview(point); if(Input.GetMouseButtonDown(0)) ConfirmReinforcement(point); }
                return;
            }
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
                    foreach(var s in Ships) if(s!=null && s.Selected) s.AttackTarget(target);
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
            var selected=Ships.Where(s=>s!=null && s.Alive && !s.IsArriving && s.Selected && s.Friendly).ToArray();
            if(selected.Length==0) { Notify("Select allied ships first, or press Tab to select the fleet."); return; }
            var formation=new FleetFormation(selected,destination,attackMove); formations.Add(formation);
            var marker=ShipVisuals.Ring(effectsRoot,2.2f,new Color(.2f,.9f,1),.16f,"Move destination"); marker.transform.position=formation.Goal; Destroy(marker.gameObject,1.5f);
            Notify(attackMove?"Fleet attack-move issued. Formation holds when enemies engage.":"Formation move issued. Escorts will keep pace with capital ships.");
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
            Debug.Log("FLEET_DISPLAY "+Screen.fullScreenMode+" "+Screen.width+"x"+Screen.height+" desktop "+Screen.currentResolution.width+"x"+Screen.currentResolution.height);
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
                    Check(Flagship.Destruction.DamagedSections==1,"Procedural hull damage "+faction,assertions);
                    Check(Flagship.GetComponentsInChildren<HullSection>(true).All(s=>s.gameObject.activeSelf),"Damaged hull stays intact "+faction,assertions);
                    Check(Flagship.GetComponentsInChildren<HullSection>().Any(s=>s.ArmorFragments!=null && s.ArmorFragments.Length>0),"Small armor fragment assets "+faction,assertions);
                    var block=new MaterialPropertyBlock(); bool burned=false;
                    foreach(var section in Flagship.GetComponentsInChildren<HullSection>())
                    { section.GetComponent<MeshRenderer>().GetPropertyBlock(block,0); burned|=!block.isEmpty && block.GetColor("baseColorFactor").r<.5f; }
                    Check(burned,"Hull scorch is applied to textured materials "+faction,assertions);
                    Check(RepairFleet() && Flagship.Hull==Flagship.MaxHull && Flagship.Destruction.DamagedSections==0,"Fleet repair restores sections "+faction,assertions);
                    bool clean=true;
                    foreach(var section in Flagship.GetComponentsInChildren<HullSection>())
                    { section.GetComponent<MeshRenderer>().GetPropertyBlock(block,0); clean&=block.isEmpty; }
                    Check(clean,"Repair clears hull scorches "+faction,assertions);
                    Check(!Recruit(ShipClass.Flagship),"No flagship purchase "+faction,assertions);
                    SelectAll(); OrderMove(new Vector3(5,0,5),false);
                    Check(Ships.All(s=>s.HasMoveOrder),"Formation orders "+faction,assertions);
                    foreach(var s in Ships) s.HasMoveOrder=false;
                    LaunchWave(); Check(!Recruit(ShipClass.Fighter),"Insufficient salvage blocks call-in "+faction,assertions);
                    TogglePause(); float time=BattleTime; Tick(.5f); Check(Paused && BattleTime==time,"Pause freezes simulation "+faction,assertions); TogglePause();
                    int steps=0;
                    while(Phase==BattlePhase.Combat && steps++<6000) Tick(.04f);
                    Check(Phase==BattlePhase.Preparation && Kills>0,"Complete survival wave "+faction,assertions);
                    LaunchWave(); var flagship=Flagship; flagship.Damage(flagship.Shield+flagship.Hull+1);
                    Check(Phase==BattlePhase.Defeat,"Flagship defeat "+faction,assertions);
                }
                CheckMovement(assertions);
                CheckCombatUpgrades(assertions);
                CheckSquadronsAndAudio(assertions);
                CheckLivingSector(assertions);
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
            Begin(Faction.Republic); SelectAll(); OrderMove(new Vector3(15,0,30),false);
            for(int i=0;i<550;i++) Tick(.04f);
            cameraFocus=new Vector3(15,0,20); cameraDistance=90; PositionCamera();
            yield return null;
            HUD.CapturePreview(Path.Combine(Application.persistentDataPath,"fleet-movement-preview.png"));
            Begin(Faction.Republic); Salvage=1000; LaunchWave();
            BeginReinforcementPlacement(ShipClass.Destroyer); UpdateReinforcementPreview(new Vector3(22,0,10));
            yield return null;
            HUD.CapturePreview(Path.Combine(Application.persistentDataPath,"fleet-callin-preview.png"));
            ConfirmReinforcement(new Vector3(22,0,10));
            for(int i=0;i<46;i++) Tick(.05f);
            cameraFocus=new Vector3(12,0,0); PositionCamera();
            yield return null;
            HUD.CapturePreview(Path.Combine(Application.persistentDataPath,"fleet-hyperspace-preview.png"));
            Begin(Faction.Republic); ClearBattle(); Salvage=1000;
            Spawn(Faction.Republic,ShipClass.Flagship,true,true,new Vector3(0,0,-16));
            var squad=Spawn(Faction.Republic,ShipClass.Fighter,true,false,new Vector3(-13,0,7)); squad.Selected=true;
            Spawn(Faction.Republic,ShipClass.Interceptor,true,false,new Vector3(13,0,7));
            RecruitAt(ShipClass.Escort,new Vector3(0,0,0));
            for(int i=0;i<60;i++) Tick(.05f);
            cameraFocus=new Vector3(0,0,0); cameraDistance=75; PositionCamera();
            Notify("Geonosis / drifting asteroids / six-fighter holding formations");
            yield return null;
            HUD.CapturePreview(Path.Combine(Application.persistentDataPath,"fleet-squadron-preview.png"));
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
            Notify("HULL DAMAGE: intact ship / scorched hull + armor fragments / total destruction");
            yield return new WaitForSecondsRealtime(.6f);
            HUD.CapturePreview(Path.Combine(Application.persistentDataPath,"fleet-destruction-preview.png"));
            yield return new WaitForSecondsRealtime(.3f);
            Application.Quit(0);
        }
        void CheckMovement(List<string> assertions)
        {
            Begin(Faction.Republic); ClearBattle();
            var capital=Spawn(Faction.Republic,ShipClass.Destroyer,true,false,new Vector3(0,0,-35));
            capital.MoveTo(new Vector3(0,0,25)); Vector3 initial=capital.transform.position;
            Tick(.1f);
            Check(capital.CurrentSpeed>0 && capital.CurrentSpeed<capital.Stats.Speed*.1f && Vector3.Distance(initial,capital.transform.position)<.1f,"Capital accelerates gradually",assertions);
            for(int i=0;i<100;i++) Tick(.04f);
            float cruising=capital.CurrentSpeed; var heading=capital.transform.rotation;
            capital.MoveTo(new Vector3(0,0,-45)); Tick(.1f);
            Check(Quaternion.Angle(heading,capital.transform.rotation)<=3.1f && capital.CurrentSpeed<cruising,"Capital brakes and turns without snapping",assertions);
            for(int i=0;i<1800 && capital.HasMoveOrder;i++) Tick(.04f);
            Check(!capital.HasMoveOrder && Vector3.Distance(capital.transform.position,capital.Destination)<1.1f && capital.CurrentSpeed<.3f,"Capital arrives and stops",assertions);
            var pausedPosition=capital.transform.position; float pausedSpeed=capital.CurrentSpeed;
            TogglePause(); Tick(1); Check(capital.transform.position==pausedPosition && capital.CurrentSpeed==pausedSpeed,"Pause freezes ship movement",assertions); TogglePause();
            ClearBattle();
            var fighter=Spawn(Faction.Republic,ShipClass.Fighter,true,false,new Vector3(-10,0,0));
            fighter.MoveTo(new Vector3(45,0,0)); heading=fighter.transform.rotation; Tick(.2f);
            Check(Quaternion.Angle(heading,fighter.transform.rotation)>20,"Fighters retain agile turning",assertions);
            Begin(Faction.Republic); SelectAll(); OrderMove(new Vector3(18,0,30),false);
            var members=Ships.ToArray(); var formation=Flagship.Formation;
            Check(members.All(s=>s.Formation==formation) && formation.Count==5,"Fleet shares a moving formation",assertions);
            bool pace=true,bounds=true;
            for(int i=0;i<2300 && members.Any(s=>s.HasMoveOrder);i++)
            {
                Tick(.04f);
                pace&=formation.Speed<=Flagship.Stats.Speed+.01f;
                bounds&=members.All(s=>s.transform.position.magnitude<=FleetRules.ArenaRadius);
            }
            Check(pace,"Formation respects slow capital speed",assertions);
            Check(bounds,"Formation stays inside battle sector",assertions);
            Check(members.All(s=>!s.HasMoveOrder && Vector3.Distance(s.transform.position,s.Destination)<1.2f),"Formation reaches separate slots together",assertions);
            SelectAll(); OrderMove(new Vector3(-20,0,15),false); var oldFormation=Flagship.Formation;
            // Choose an actual member of this fresh fleet to leave its previous group.
            var escort=Ships.First(s=>s.Kind==ShipClass.Fighter);
            foreach(var s in Ships) s.Selected=s==escort;
            OrderMove(new Vector3(35,0,0),false); Tick(.04f);
            Check(escort.Formation!=oldFormation && Flagship.Formation==oldFormation && oldFormation.Count==4,"New order detaches only selected ships",assertions);
            var enemy=Spawn(Faction.CIS,ShipClass.Frigate,false,false,new Vector3(32,0,10)); escort.AttackTarget(enemy); Tick(.04f);
            Check(escort.Formation==null && escort.ForcedTarget==enemy && !escort.HasMoveOrder,"Focus fire leaves formation cleanly",assertions);
        }
        void CheckCombatUpgrades(List<string> assertions)
        {
            foreach(var faction in new[]{Faction.Republic,Faction.CIS})
            {
                foreach(var kind in new[]{ShipClass.Flagship,ShipClass.Fighter})
                {
                    Begin(faction); ClearBattle();
                    var shooter=Spawn(faction,kind,true,false,Vector3.zero);
                    var target=Spawn(faction==Faction.Republic?Faction.CIS:Faction.Republic,ShipClass.Frigate,false,false,new Vector3(0,0,20));
                    Fire(shooter,target,shooter.Stats.Damage); shooter.Weapons.Tick(.5f);
                    Check(Bolts.Count==shooter.Weapons.BarrelCount && Bolts.Count>1,"Multiple weapon barrels "+faction+" "+kind,assertions);
                    Check(Mathf.Abs(Bolts.Sum(b=>b.Damage)-shooter.Stats.Damage)<.01f,"Salvo retains total damage "+faction+" "+kind,assertions);
                    Check(Bolts.Select(b=>b.transform.position).Distinct().Count()>1,"Distinct muzzle positions "+faction+" "+kind,assertions);
                    Color color=Bolts[0].GetComponent<Renderer>().sharedMaterial.color;
                    Check(faction==Faction.Republic?color.b>color.r:color.r>color.g && color.r>color.b,"Clone Wars laser color "+faction+" "+kind,assertions);
                }
            }
            Begin(Faction.Republic); Salvage=1000; LaunchWave(); int original=Salvage;
            Check(BeginReinforcementPlacement(ShipClass.Destroyer) && Salvage==original && IncomingCount==0,"Selecting call-in does not charge salvage",assertions);
            Check(!ConfirmReinforcement(Flagship.transform.position) && Salvage==original,"Blocked arrival does not charge salvage",assertions);
            Check(!ConfirmReinforcement(new Vector3(200,0,0)) && Salvage==original,"Arrival outside sector rejected",assertions);
            CancelReinforcementPlacement(); Check(!SelectedReinforcement.HasValue && IncomingCount==0,"Cancel call-in selection",assertions);
            BeginReinforcementPlacement(ShipClass.Destroyer);
            Check(ConfirmReinforcement(new Vector3(-35,0,-15)) && IncomingCount==1 && Salvage==original-400 && FriendlyCount==5,"Combat call-in reserves salvage and slot",assertions);
            float elapsed=jumps[0].Elapsed;
            TogglePause(); Tick(3); Check(jumps[0].Elapsed==elapsed && FriendlyCount==5,"Pause freezes hyperspace countdown",assertions); TogglePause();
            for(int i=0;i<46;i++) Tick(.05f);
            var inbound=jumps[0].Ship;
            Check(inbound!=null && inbound.IsArriving && !inbound.GetComponent<Collider>().enabled,"Ship exits hyperspace before becoming active",assertions);
            float hull=inbound.Hull; inbound.Damage(inbound.MaxHull*3);
            Check(inbound.Hull==hull,"Arriving ship cannot be damaged",assertions);
            for(int i=0;i<16;i++) Tick(.05f);
            Check(IncomingCount==0 && !inbound.IsArriving && inbound.GetComponent<Collider>().enabled && FriendlyCount==6,"Call-in finishes as an active ship",assertions);
            Begin(Faction.Republic); Salvage=2000;
            while(FriendlyCount<21) Spawn(Faction.Republic,ShipClass.Fighter,true,false,new Vector3(60,0,-50));
            Check(RecruitAt(ShipClass.Fighter,new Vector3(-50,0,-20)) && FleetCapacityUsed==22 && !RecruitAt(ShipClass.Fighter,new Vector3(-50,0,20)),"Pending call-ins respect fleet capacity",assertions);
            ReturnToMenu(); Check(IncomingCount==0 && !SelectedReinforcement.HasValue,"Menu clears pending reinforcements",assertions);
            var rep=FleetSound.Get("RepublicHeavy"); var cis=FleetSound.Get("CISHeavy");
            Check(rep!=cis && rep.frequency==44100 && FleetSound.Get("HyperspaceExit").length>1,"Separate original audio effects",assertions);
        }
        void CheckSquadronsAndAudio(List<string> assertions)
        {
            foreach(var faction in new[]{Faction.Republic,Faction.CIS}) foreach(var kind in new[]{ShipClass.Fighter,ShipClass.Interceptor})
            {
                Begin(faction); ClearBattle();
                var squad=Spawn(faction,kind,true,false,Vector3.zero);
                Check(squad.Squadron!=null && squad.Squadron.ActiveCount==6 && squad.VisualRoot.childCount==6,"Six separate fighter models "+faction+" "+kind,assertions);
                var fighter=squad.VisualRoot.GetChild(0); var renderer=fighter.GetComponentInChildren<MeshRenderer>();
                Check(fighter.localScale.x<.6f && renderer.bounds.size.magnitude<7,"Reduced fighter visual scale "+faction+" "+kind,assertions);
                squad.Shield=0; squad.Damage(squad.MaxHull/6+.01f,fighter.position);
                Check(squad.Squadron.ActiveCount==5 && !fighter.gameObject.activeSelf && squad.Alive,"Individual fighter loss leaves squadron alive "+faction+" "+kind,assertions);
                var target=Spawn(faction==Faction.Republic?Faction.CIS:Faction.Republic,ShipClass.Frigate,false,false,new Vector3(0,0,20));
                Fire(squad,target,squad.Stats.Damage*squad.Squadron.ActiveCount/6f); squad.Weapons.Tick(.5f);
                Check(Bolts.Count==10 && Mathf.Abs(Bolts.Sum(b=>b.Damage)-squad.Stats.Damage*5/6f)<.01f,"Surviving fighters fire and retain scaled damage "+faction+" "+kind,assertions);
                Salvage=1000; RepairFleet();
                Check(squad.Squadron.ActiveCount==6 && fighter.gameObject.activeSelf && squad.Hull==squad.MaxHull,"Repair restores squadron strength "+faction+" "+kind,assertions);
                var sound=FleetSound.Weapon(faction,kind);
                string expected=faction==Faction.CIS?"VultureCannon":kind==ShipClass.Fighter?"ARC170Cannon":"VWingCannon";
                Check(sound.name.StartsWith(expected) && sound.channels==1 && sound.frequency==44100,"Supplied fighter firing sound "+faction+" "+kind,assertions);
            }
            Begin(Faction.Republic); Salvage=1000;
            Check(CallInCount(ShipClass.Escort)==2 && CallInCost(ShipClass.Escort)==280,"Arquitens pair has two-cruiser cost",assertions);
            Check(RecruitAt(ShipClass.Escort,new Vector3(0,0,32)) && Salvage==720 && IncomingCount==2 && FleetCapacityUsed==7,"Arquitens call-in reserves two cruisers atomically",assertions);
            for(int i=0;i<60;i++) Tick(.05f);
            var cruisers=Ships.Where(s=>s.Kind==ShipClass.Escort && s.Friendly).ToArray();
            Check(cruisers.Length==2 && cruisers.All(s=>!s.IsArriving) && Vector3.Distance(cruisers[0].transform.position,cruisers[1].transform.position)>6,"Arquitens pair arrives at separated points",assertions);
            Begin(Faction.Republic); Salvage=1000;
            int original=Salvage;
            Spawn(Faction.Republic,ShipClass.Escort,true,false,new Vector3(3.6f,0,32));
            Check(!RecruitAt(ShipClass.Escort,new Vector3(0,0,32)) && Salvage==original && IncomingCount==0,"Partly blocked pair spends no salvage",assertions);
            while(FriendlyCount<21) Spawn(Faction.Republic,ShipClass.Frigate,true,false,new Vector3(55,0,-40));
            Check(!CanCallIn(ShipClass.Escort) && IncomingCount==0,"Arquitens pair needs two free fleet slots",assertions);
            Begin(Faction.CIS); Salvage=1000;
            Check(CallInCount(ShipClass.Escort)==1 && CallInCost(ShipClass.Escort)==125,"Munificent escort retains single-ship call-in",assertions);
            Check(FleetSound.Weapon(Faction.Republic,ShipClass.Flagship).name.StartsWith("VenatorCannon") && FleetSound.Weapon(Faction.CIS,ShipClass.Frigate).name.StartsWith("MunificentCannon"),"Supplied capital cannon sounds are mapped to hulls",assertions);
            Check(FleetSound.Get("HyperspaceCharge")==Resources.Load<AudioClip>("Audio/HyperspaceCharge") && Mathf.Abs(FleetSound.Get("HyperspaceCharge").length-2.2f)<.01f && FleetSound.Get("HyperspaceExit")==Resources.Load<AudioClip>("Audio/HyperspaceExit"),"Edited recording supplies separate hyperspace stages",assertions);
        }
        void CheckLivingSector(List<string> assertions)
        {
            Begin(Faction.Republic);ClearBattle();
            var squad=Spawn(Faction.Republic,ShipClass.Fighter,true,false,Vector3.zero);
            var craft=squad.VisualRoot.GetChild(0);Vector3 initial=craft.localPosition;
            for(int i=0;i<60;i++) Tick(.05f);
            Check(squad.transform.position==Vector3.zero && Vector3.Distance(initial,craft.localPosition)>.1f,"Idle fighters fly while squadron anchor holds",assertions);
            Vector3 held=craft.localPosition;Quaternion rotation=craft.localRotation;
            TogglePause();Tick(2);
            Check(craft.localPosition==held && craft.localRotation==rotation,"Tactical pause freezes fighter holding flight",assertions);TogglePause();
            squad.MoveTo(new Vector3(0,0,30));for(int i=0;i<40;i++) Tick(.05f);
            Check(Vector3.Distance(craft.localPosition,FleetSquadron.Offsets[0])<.01f && Quaternion.Angle(craft.localRotation,Quaternion.identity)<.1f && squad.transform.position.z>0,"Move orders restore squadron formation smoothly",assertions);
            squad.Shield=0;squad.Damage(squad.MaxHull/6+.1f,craft.position);held=craft.localPosition;
            for(int i=0;i<30;i++) Tick(.05f);
            Check(!craft.gameObject.activeSelf && craft.localPosition==held,"Lost fighters do not continue holding animation",assertions);
            var environment=FindObjectOfType<FleetEnvironment>();var asteroid=environment.transform.Find("Drifting asteroid 00");
            initial=asteroid.localPosition;rotation=asteroid.localRotation;environment.TickVisuals(1);
            Check(environment.AsteroidCount>=20 && asteroid.localPosition!=initial && asteroid.localRotation!=rotation,"Asteroids drift and tumble in the sector",assertions);
            held=asteroid.localPosition;TogglePause();environment.TickVisuals(2);
            Check(asteroid.localPosition==held,"Tactical pause freezes asteroids",assertions);TogglePause();
            Check(environment.transform.Find("Geonosis")!=null && environment.transform.Find("Geonosis").Find("Geonosis rocky ring")!=null && environment.GetComponentsInChildren<Collider>().Length==0,"Geonosis and its ring leave command rays clear",assertions);
            foreach(var faction in new[]{Faction.Republic,Faction.CIS}) foreach(ShipClass kind in Enum.GetValues(typeof(ShipClass)))
            {
                var clip=FleetSound.Weapon(faction,kind);var data=new float[clip.samples];bool loaded=clip.GetData(data,0);
                Check(loaded && clip.name.Contains("Cannon") && data.Any(v=>Mathf.Abs(v)>.02f),"Every hull has non-silent recording-based fire "+faction+" "+kind,assertions);
            }
        }
        static void Check(bool condition,string name,List<string> checks) { if(!condition) throw new Exception("Smoke test failed: "+name); checks.Add(name); }
        [Serializable] public sealed class SmokeReport { public bool passed; public string error; public string[] checks; }
    }
}
