using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace FleetSurvival
{
    public sealed class FleetHUD : MonoBehaviour
    {
        FleetGame game;
        Canvas canvas;
        TMP_FontAsset font;
        RectTransform root, markersRoot, mapDots, marquee;
        GameObject menu, battle, paused, defeat, credits;
        TextMeshProUGUI resources, wave, contacts, message, selected, flagStatus, phase, best, endReport, muteLabel;
        UnityEngine.UI.Image hullBar, shieldBar;
        UnityEngine.UI.Button repair, refit, launch;
        readonly Dictionary<ShipClass,UnityEngine.UI.Button> buyButtons=new Dictionary<ShipClass,UnityEngine.UI.Button>();
        readonly Dictionary<FleetShip,Marker> markers=new Dictionary<FleetShip,Marker>();
        readonly Color ink=new Color(.87f,.93f,1), mutedText=new Color(.45f,.59f,.72f), cyan=new Color(.22f,.8f,1);
        readonly Color panelColor=new Color(.018f,.035f,.065f,.95f);
        float refresh;
        sealed class Marker { public RectTransform World, Dot; public UnityEngine.UI.Image Hull, Shield; public TextMeshProUGUI Name; }

        public void Initialize(FleetGame owner)
        {
            game=owner; font=Resources.Load<TMP_FontAsset>("CommanderFont");
            var go=new GameObject("Command interface",typeof(RectTransform),typeof(Canvas),typeof(UnityEngine.UI.CanvasScaler),typeof(UnityEngine.UI.GraphicRaycaster));
            root=go.GetComponent<RectTransform>(); canvas=go.GetComponent<Canvas>(); canvas.renderMode=RenderMode.ScreenSpaceOverlay;
            var scaler=go.GetComponent<UnityEngine.UI.CanvasScaler>(); scaler.uiScaleMode=UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution=new Vector2(1920,1080); scaler.matchWidthOrHeight=.5f;
            if(EventSystem.current==null) new GameObject("EventSystem",typeof(EventSystem),typeof(StandaloneInputModule));
            markersRoot=Stretch(root,"Ship readouts");
            BuildMenu(); BuildBattle(); BuildPause(); BuildDefeat(); BuildCredits();
            marquee=Panel(root,"Selection box",Vector2.zero,Vector2.zero,Vector2.zero,new Color(.1f,.7f,1,.13f),false);
            var border=marquee.gameObject.AddComponent<UnityEngine.UI.Outline>(); border.effectColor=cyan; border.effectDistance=new Vector2(1,-1);
            marquee.gameObject.SetActive(false); Rebuild();
        }
        RectTransform Stretch(Transform parent,string name)
        {
            var rect=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>(); rect.SetParent(parent,false);
            rect.anchorMin=Vector2.zero; rect.anchorMax=Vector2.one; rect.offsetMin=rect.offsetMax=Vector2.zero; return rect;
        }
        RectTransform Rect(Transform parent,string name,Vector2 anchor,Vector2 position,Vector2 size)
        {
            var rect=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>(); rect.SetParent(parent,false);
            rect.anchorMin=rect.anchorMax=anchor; rect.pivot=anchor; rect.anchoredPosition=position; rect.sizeDelta=size; return rect;
        }
        RectTransform Panel(Transform parent,string name,Vector2 anchor,Vector2 position,Vector2 size,Color color,bool block=true)
        { var rect=Rect(parent,name,anchor,position,size); var image=rect.gameObject.AddComponent<UnityEngine.UI.Image>(); image.color=color; image.raycastTarget=block; return rect; }
        TextMeshProUGUI Text(Transform parent,string value,Vector2 anchor,Vector2 position,Vector2 size,float points,Color color,TextAlignmentOptions alignment=TextAlignmentOptions.TopLeft)
        {
            var rect=Rect(parent,value,anchor,position,size); var text=rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.font=font; text.text=value; text.fontSize=points; text.color=color; text.alignment=alignment; text.raycastTarget=false;
            text.enableWordWrapping=true; text.overflowMode=TextOverflowModes.Ellipsis; return text;
        }
        UnityEngine.UI.Button Button(Transform parent,string title,Vector2 anchor,Vector2 pos,Vector2 size,Action callback,Color? accent=null)
        {
            Color tint=accent??cyan;
            var rect=Panel(parent,title,anchor,pos,size,new Color(.04f,.1f,.16f,.97f));
            var border=rect.gameObject.AddComponent<UnityEngine.UI.Outline>(); border.effectColor=new Color(tint.r,tint.g,tint.b,.45f); border.effectDistance=new Vector2(1,-1);
            var button=rect.gameObject.AddComponent<UnityEngine.UI.Button>(); var colors=button.colors;
            colors.normalColor=Color.white; colors.highlightedColor=new Color(.5f,.82f,1); colors.pressedColor=new Color(.35f,.65f,.85f); colors.disabledColor=new Color(.33f,.4f,.46f,.7f); button.colors=colors;
            button.onClick.AddListener(()=>callback()); Text(rect,title,new Vector2(.5f,.5f),Vector2.zero,size-new Vector2(16,8),21,ink,TextAlignmentOptions.Center); return button;
        }
        void BuildMenu()
        {
            var screen=Stretch(root,"Faction selection"); menu=screen.gameObject;
            var left=Panel(screen,"Mission panel",new Vector2(0,.5f),new Vector2(65,0),new Vector2(760,920),panelColor);
            Text(left,"STAR WARS  /  CLONE WARS",new Vector2(0,1),new Vector2(48,-50),new Vector2(660,50),24,cyan);
            Text(left,"FLEET\nSURVIVAL",new Vector2(0,1),new Vector2(44,-105),new Vector2(670,210),88,ink);
            Text(left,"Choose your allegiance. Command your fleet.\nHold the line against escalating enemy assaults.",new Vector2(0,1),new Vector2(48,-335),new Vector2(650,80),26,mutedText);
            Button(left,"GALACTIC REPUBLIC\nVenator flagship / durable cruisers",new Vector2(0,1),new Vector2(48,-450),new Vector2(660,110),()=>game.Begin(Faction.Republic));
            Button(left,"SEPARATIST ALLIANCE\nProvidence flagship / affordable droid fleets",new Vector2(0,1),new Vector2(48,-580),new Vector2(660,110),()=>game.Begin(Faction.CIS),new Color(1,.5f,.26f));
            best=Text(left,"BEST SURVIVAL: 0 WAVES",new Vector2(0,1),new Vector2(48,-725),new Vector2(650,45),22,mutedText);
            Button(left,"MODEL CREDITS",new Vector2(0,0),new Vector2(48,45),new Vector2(300,55),()=>credits.SetActive(true));
            Button(left,"QUIT",new Vector2(1,0),new Vector2(-48,45),new Vector2(180,55),()=>Application.Quit());
            Text(screen,"TACTICAL FLEET COMMAND",new Vector2(1,0),new Vector2(-72,76),new Vector2(800,55),28,ink,TextAlignmentOptions.BottomRight);
            Text(screen,"WINDOWS PROTOTYPE  /  SINGLE PLAYER",new Vector2(1,0),new Vector2(-72,40),new Vector2(800,40),18,mutedText,TextAlignmentOptions.BottomRight);
        }
        void BuildBattle()
        {
            var screen=Stretch(root,"Battle HUD"); battle=screen.gameObject;
            var top=Panel(screen,"Command bar",new Vector2(0,1),Vector2.zero,new Vector2(1920,92),panelColor); top.anchorMax=new Vector2(1,1); top.sizeDelta=new Vector2(0,92);
            Text(top,"FLEET / SURVIVAL",new Vector2(0,1),new Vector2(28,-20),new Vector2(330,45),28,ink);
            wave=Text(top,"WAVE 01",new Vector2(0,1),new Vector2(405,-25),new Vector2(180,40),25,cyan);
            resources=Text(top,"SALVAGE 220",new Vector2(0,1),new Vector2(620,-25),new Vector2(280,40),25,ink);
            contacts=Text(top,"CONTACTS 0",new Vector2(0,1),new Vector2(930,-25),new Vector2(640,40),22,ink);
            var mute=Button(top,"SOUND ON",new Vector2(1,1),new Vector2(-160,-20),new Vector2(145,50),()=>game.ToggleMute()); muteLabel=mute.GetComponentInChildren<TextMeshProUGUI>();
            Button(top,"PAUSE",new Vector2(1,1),new Vector2(-25,-20),new Vector2(120,50),()=>game.TogglePause());
            var command=Panel(screen,"Command ship status",new Vector2(0,1),new Vector2(24,-118),new Vector2(320,186),panelColor);
            Text(command,"COMMAND SHIP",new Vector2(0,1),new Vector2(20,-16),new Vector2(275,35),18,cyan);
            flagStatus=Text(command,"Venator flagship",new Vector2(0,1),new Vector2(20,-48),new Vector2(285,44),18,ink);
            hullBar=Bar(command,"Hull",new Vector2(20,-106),new Vector2(280,10),new Color(.35f,.86f,.65f));
            shieldBar=Bar(command,"Shields",new Vector2(20,-135),new Vector2(280,7),cyan);
            Text(command,"Hull / Shields     [Q] focus flagship",new Vector2(0,0),new Vector2(20,10),new Vector2(286,26),16,mutedText);
            var selection=Panel(screen,"Selection",new Vector2(0,1),new Vector2(24,-320),new Vector2(320,115),panelColor);
            selected=Text(selection,"1 ship selected",new Vector2(0,1),new Vector2(20,-14),new Vector2(280,52),21,ink);
            Button(selection,"SELECT FLEET [TAB]",new Vector2(0,0),new Vector2(20,14),new Vector2(280,35),()=>game.SelectAll());
            var map=Panel(screen,"Tactical map",new Vector2(1,1),new Vector2(-24,-118),new Vector2(244,220),panelColor);
            Text(map,"GEONOSIS SECTOR",new Vector2(0,1),new Vector2(15,-12),new Vector2(210,30),17,mutedText);
            mapDots=Rect(map,"Ship contacts",new Vector2(.5f,.5f),new Vector2(0,-12),new Vector2(210,158));
            var help=Panel(screen,"Controls",new Vector2(1,0),new Vector2(-24,285),new Vector2(300,265),panelColor);
            Text(help,"FLEET ORDERS",new Vector2(0,1),new Vector2(20,-18),new Vector2(265,32),18,cyan);
            Text(help,"Left click / drag: select ships\nShift: add to selection\nRight click: move / focus fire\nF then right click: attack-move\nWASD / middle drag: pan\nScroll: zoom     Q: flagship\nSpace / Esc: pause\n1 / 2 / 3: buy reinforcements",new Vector2(0,1),new Vector2(20,-59),new Vector2(265,192),18,mutedText);
            var bottom=Panel(screen,"Reinforcements",Vector2.zero,Vector2.zero,new Vector2(1920,265),panelColor); bottom.anchorMax=new Vector2(1,0); bottom.sizeDelta=new Vector2(0,265);
            phase=Text(bottom,"PREPARATION / REINFORCE YOUR FLEET",new Vector2(0,1),new Vector2(28,-15),new Vector2(1500,32),18,cyan);
            var classes=new[]{ShipClass.Fighter,ShipClass.Interceptor,ShipClass.Escort,ShipClass.Frigate,ShipClass.Destroyer,ShipClass.Carrier};
            for(int i=0;i<classes.Length;i++) { var kind=classes[i]; buyButtons[kind]=Button(bottom,kind.ToString(),new Vector2(0,1),new Vector2(28+(i%3)*320,-58-(i/3)*86),new Vector2(300,72),()=>game.BeginReinforcementPlacement(kind)); }
            repair=Button(bottom,"REPAIR FLEET [R]\n120 salvage",new Vector2(0,1),new Vector2(1010,-58),new Vector2(270,72),()=>game.RepairFleet());
            refit=Button(bottom,"WEAPON REFIT\n200 / +18% damage",new Vector2(0,1),new Vector2(1010,-144),new Vector2(270,72),()=>game.UpgradeWeapons());
            launch=Button(bottom,"LAUNCH WAVE",new Vector2(1,1),new Vector2(-28,-58),new Vector2(555,158),()=>game.LaunchWave());
            Text(bottom,"Call in: choose a ship, then left-click clear space. Right-click / Esc cancels. Repairs and refits are between waves.",new Vector2(0,0),new Vector2(28,12),new Vector2(1830,27),18,mutedText);
            message=Text(screen,"Fleet ready.",new Vector2(.5f,1),new Vector2(0,-111),new Vector2(1080,52),23,ink,TextAlignmentOptions.Center);
        }
        UnityEngine.UI.Image Bar(Transform parent,string name,Vector2 pos,Vector2 size,Color color)
        {
            var bg=Panel(parent,name,new Vector2(0,1),pos,size,new Color(.075f,.12f,.17f),false);
            var fg=Panel(bg,name+" fill",Vector2.zero,Vector2.zero,size,color,false); fg.anchorMax=Vector2.one; fg.offsetMin=fg.offsetMax=Vector2.zero; return fg.GetComponent<UnityEngine.UI.Image>();
        }
        void Fill(UnityEngine.UI.Image image,float value) { image.rectTransform.anchorMax=new Vector2(Mathf.Clamp01(value),1); image.rectTransform.offsetMin=image.rectTransform.offsetMax=Vector2.zero; }
        void BuildPause()
        {
            var screen=Stretch(root,"Pause overlay"); paused=screen.gameObject; screen.gameObject.AddComponent<UnityEngine.UI.Image>().color=new Color(0,.01f,.025f,.82f);
            var panel=Panel(screen,"Pause menu",new Vector2(.5f,.5f),Vector2.zero,new Vector2(580,480),panelColor);
            Text(panel,"TACTICAL PAUSE",new Vector2(.5f,1),new Vector2(0,-50),new Vector2(520,65),40,ink,TextAlignmentOptions.Center);
            Text(panel,"The battle is paused. Take a moment to plan.",new Vector2(.5f,1),new Vector2(0,-134),new Vector2(500,60),22,mutedText,TextAlignmentOptions.Center);
            Button(panel,"RESUME [SPACE / ESC]",new Vector2(.5f,1),new Vector2(0,-228),new Vector2(460,65),()=>game.TogglePause());
            Button(panel,"RETURN TO FACTION SELECT",new Vector2(.5f,1),new Vector2(0,-315),new Vector2(460,65),()=>game.ReturnToMenu());
        }
        void BuildDefeat()
        {
            var screen=Stretch(root,"Defeat overlay"); defeat=screen.gameObject; screen.gameObject.AddComponent<UnityEngine.UI.Image>().color=new Color(.02f,0,0,.82f);
            var panel=Panel(screen,"After action report",new Vector2(.5f,.5f),Vector2.zero,new Vector2(700,590),panelColor);
            Text(panel,"COMMAND SHIP LOST",new Vector2(.5f,1),new Vector2(0,-45),new Vector2(640,75),43,new Color(1,.5f,.35f),TextAlignmentOptions.Center);
            endReport=Text(panel,"Wave 1",new Vector2(.5f,1),new Vector2(0,-157),new Vector2(620,185),28,ink,TextAlignmentOptions.Center);
            Button(panel,"RETRY THIS FACTION",new Vector2(.5f,1),new Vector2(0,-375),new Vector2(540,70),()=>game.Begin(game.PlayerFaction));
            Button(panel,"CHOOSE ANOTHER FACTION",new Vector2(.5f,1),new Vector2(0,-470),new Vector2(540,70),()=>game.ReturnToMenu());
        }
        void BuildCredits()
        {
            var screen=Stretch(root,"Model credits"); credits=screen.gameObject; screen.gameObject.AddComponent<UnityEngine.UI.Image>().color=new Color(0,0,0,.92f);
            var panel=Panel(screen,"Credits panel",new Vector2(.5f,.5f),Vector2.zero,new Vector2(1100,880),panelColor);
            Text(panel,"SHIP MODEL CREDITS",new Vector2(0,1),new Vector2(45,-40),new Vector2(1010,60),40,ink);
            Text(panel,"Venator / Acclamator — Cpt.Kirk\nDetailed Venator — ForkyForklift\nARC-170 / Vulture droid — DrEgguin\nV-19 Torrent — AirStudios\nArquitens — A308 Digital\nLucrehulk — Neut2000\nMunificent / Providence / Recusant — lolqeeeeeeeeee\n\nProvidence and Recusant: CC BY-NC 4.0\nOther supplied models: CC BY 4.0\n\nModels supplied via Sketchfab; scale and orientation adjusted.\nFull source links and licenses: THIRD_PARTY_ASSETS.md.\nFree fan prototype. Star Wars belongs to its respective owners.",new Vector2(0,1),new Vector2(45,-135),new Vector2(1010,630),25,mutedText);
            Button(panel,"BACK",new Vector2(.5f,0),new Vector2(0,35),new Vector2(420,60),()=>credits.SetActive(false));
        }
        public void Rebuild()
        {
            if(menu==null) return; menu.SetActive(game.Phase==BattlePhase.Menu); battle.SetActive(game.Phase!=BattlePhase.Menu);
            markersRoot.gameObject.SetActive(game.Phase!=BattlePhase.Menu && game.Phase!=BattlePhase.Defeat);
            paused.SetActive(game.Paused); defeat.SetActive(game.Phase==BattlePhase.Defeat); credits.SetActive(false);
            best.text="BEST SURVIVAL: "+game.BestWave+" WAVES";
            endReport.text="Waves survived: "+Mathf.Max(0,game.Wave-1)+"\nEnemy ships destroyed: "+game.Kills+"\nBest survival: "+game.BestWave+" waves";
            foreach(var pair in buyButtons) pair.Value.GetComponentInChildren<TextMeshProUGUI>().text=game.CallInName(pair.Key)+"\n"+game.CallInCost(pair.Key)+" salvage";
            RefreshText();
        }
        public void SetMute(bool value) { muteLabel.text=value?"SOUND OFF":"SOUND ON"; }
        public void CapturePreview(string path)
        {
            RefreshText(); UpdateMarkers();
            var camera=game.ViewCamera; var previousMode=canvas.renderMode;
            var previousTarget=camera.targetTexture; var previousActive=RenderTexture.active;
            var target=new RenderTexture(1600,900,24);
            camera.targetTexture=target; canvas.renderMode=RenderMode.ScreenSpaceCamera; canvas.worldCamera=camera; canvas.planeDistance=1;
            Canvas.ForceUpdateCanvases(); camera.Render(); RenderTexture.active=target;
            var texture=new Texture2D(1600,900,TextureFormat.RGB24,false); texture.ReadPixels(new Rect(0,0,1600,900),0,0); texture.Apply();
            System.IO.File.WriteAllBytes(path,texture.EncodeToPNG());
            camera.targetTexture=previousTarget; RenderTexture.active=previousActive; canvas.renderMode=previousMode;
            target.Release(); Destroy(target); Destroy(texture);
        }
        void LateUpdate()
        {
            refresh-=Time.unscaledDeltaTime; if(refresh<=0) { RefreshText(); refresh=.15f; } UpdateMarkers();
            marquee.gameObject.SetActive(game.IsDragging);
            if(game.IsDragging) { var rect=FleetGame.ScreenRect(game.DragStart,game.DragCurrent); RectTransformUtility.ScreenPointToLocalPointInRectangle(root,new Vector2(rect.x,rect.y),null,out var local); marquee.pivot=Vector2.zero; marquee.anchorMin=marquee.anchorMax=new Vector2(.5f,.5f); marquee.anchoredPosition=local; marquee.sizeDelta=rect.size/canvas.scaleFactor; }
        }
        void RefreshText()
        {
            if(wave==null) return; wave.text="WAVE "+Mathf.Max(1,game.Wave).ToString("00"); resources.text="SALVAGE "+game.Salvage;
            contacts.text="FLEET "+game.FleetCapacityUsed+" / "+FleetRules.FleetLimit+"   HOSTILES "+game.EnemyCount+"   INBOUND "+game.IncomingCount; message.text=game.Message;
            phase.text=game.SelectedReinforcement.HasValue?"HYPERSPACE CALL-IN / LEFT-CLICK AN ARRIVAL POINT; RIGHT-CLICK TO CANCEL":game.Phase==BattlePhase.Preparation ? "PREPARATION / REPAIR, CALL IN, AND POSITION YOUR FLEET" : "SURVIVAL ASSAULT / CALL IN REINFORCEMENTS AND PROTECT YOUR COMMAND SHIP";
            var ship=game.Flagship;
            if(ship!=null) { flagStatus.text=ship.Stats.Name+"\n"+Mathf.CeilToInt(ship.Hull)+" hull / "+Mathf.CeilToInt(ship.Shield)+" shields"; Fill(hullBar,ship.Hull/ship.MaxHull); Fill(shieldBar,ship.Shield/ship.MaxShield); }
            else { Fill(hullBar,0); Fill(shieldBar,0); }
            var chosen=game.Ships.Where(s=>s!=null && s.Selected).ToArray(); selected.text=chosen.Length==1?chosen[0].Stats.Name+(chosen[0].Squadron!=null?"\n"+chosen[0].Squadron.ActiveCount+" / 6 fighters":""):chosen.Length+" units selected";
            foreach(var pair in buyButtons) pair.Value.interactable=game.CanCallIn(pair.Key);
            repair.interactable=game.Phase==BattlePhase.Preparation && !game.Paused && game.Salvage>=120 && game.Ships.Any(s=>s!=null && s.Friendly && s.Hull<s.MaxHull-.1f);
            refit.interactable=game.Phase==BattlePhase.Preparation && !game.Paused && game.Salvage>=200;
            launch.interactable=game.Phase==BattlePhase.Preparation && !game.Paused; launch.GetComponentInChildren<TextMeshProUGUI>().text=game.Phase==BattlePhase.Combat?"ASSAULT IN PROGRESS":"LAUNCH WAVE "+(game.Wave+1);
        }
        void UpdateMarkers()
        {
            foreach(var ship in markers.Keys.ToArray())
            {
                if(ship!=null && ship.Alive && game.Ships.Contains(ship)) continue;
                var old=markers[ship]; Destroy(old.World.gameObject); Destroy(old.Dot.gameObject); markers.Remove(ship);
            }
            if(game.Phase==BattlePhase.Menu || game.Phase==BattlePhase.Defeat) return;
            foreach(var ship in game.Ships)
            {
                if(ship==null || !ship.Alive) continue;
                if(!markers.TryGetValue(ship,out var marker))
                {
                    var holder=Rect(markersRoot,"Ship status",new Vector2(.5f,.5f),Vector2.zero,new Vector2(100,39)); marker=new Marker{World=holder};
                    marker.Name=Text(holder,"",new Vector2(.5f,1),Vector2.zero,new Vector2(220,24),15,ink,TextAlignmentOptions.Center);
                    marker.Hull=Bar(holder,"Hull",new Vector2(0,-27),new Vector2(100,4),ship.Friendly?new Color(.3f,.86f,.65f):new Color(1,.4f,.27f)); marker.Shield=Bar(holder,"Shields",new Vector2(0,-34),new Vector2(100,3),cyan);
                    marker.Dot=Panel(mapDots,"Ship contact",new Vector2(.5f,.5f),Vector2.zero,Vector2.one*(ship.IsFlagship?9:5),ship.Friendly?cyan:new Color(1,.35f,.2f),false); markers[ship]=marker;
                }
                Vector3 screen=game.ViewCamera.WorldToScreenPoint(ship.transform.position+Vector3.up*(ship.Stats.Radius*.45f+1));
                bool visible=screen.z>0 && screen.x>0 && screen.x<Screen.width && screen.y>0 && screen.y<Screen.height; marker.World.gameObject.SetActive(visible);
                if(visible) { RectTransformUtility.ScreenPointToLocalPointInRectangle(root,screen,null,out var position); marker.World.anchoredPosition=position; marker.Name.text=ship.IsFlagship?"COMMAND":ship.Selected?ship.Stats.Name:""; Fill(marker.Hull,ship.Hull/ship.MaxHull); Fill(marker.Shield,ship.Shield/ship.MaxShield); }
                marker.Dot.anchoredPosition=new Vector2(ship.transform.position.x/FleetRules.ArenaRadius*100,ship.transform.position.z/FleetRules.ArenaRadius*73);
            }
        }
    }
}
