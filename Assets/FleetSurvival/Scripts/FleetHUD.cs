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
        RectTransform cardArea;
        UnityEngine.UI.GridLayoutGroup cardGrid;
        readonly RectTransform[] viewEdges=new RectTransform[4];
        GameObject controls;
        GameObject hangarPanel;
        TextMeshProUGUI hangarReadout;
        readonly UnityEngine.UI.Button[] hangarLaunch=new UnityEngine.UI.Button[3],hangarBays=new UnityEngine.UI.Button[3];
        UnityEngine.UI.Button recoverSquad,relaunchSquad;
        FleetHangar displayedHangar;
        public string HangarDetails => hangarReadout.text;
        public bool ControlsVisible => controls.activeSelf;
        GameObject menu, battle, paused, defeat, credits;
        TextMeshProUGUI resources, wave, contacts, message, selected, flagStatus, phase, best, endReport, muteLabel, unitDetails, unitRefit, tallyWave, tallyRun;
        public string SelectedDetails => unitDetails.text+"\n"+unitRefit.text;
        UnityEngine.UI.Image hullBar, shieldBar;
        UnityEngine.UI.Button repair, refit, launch;
        readonly Dictionary<ShipClass,UnityEngine.UI.Button> buyButtons=new Dictionary<ShipClass,UnityEngine.UI.Button>();
        readonly Dictionary<FleetShip,Marker> markers=new Dictionary<FleetShip,Marker>();
        readonly Dictionary<FleetWreck,TextMeshProUGUI> hazards=new Dictionary<FleetWreck,TextMeshProUGUI>();
        readonly Color ink=new Color(.87f,.93f,1), mutedText=new Color(.45f,.59f,.72f), cyan=new Color(.22f,.8f,1);
        readonly Color panelColor=new Color(.018f,.035f,.065f,.88f);
        float refresh;
        sealed class Marker { public RectTransform World, Dot; public UnityEngine.UI.Image Hull, Shield; public TextMeshProUGUI Name; }

        public void Initialize(FleetGame owner)
        {
            game=owner; font=Resources.Load<TMP_FontAsset>("CommanderFont");
            var go=new GameObject("Command interface",typeof(RectTransform),typeof(Canvas),typeof(UnityEngine.UI.CanvasScaler),typeof(UnityEngine.UI.GraphicRaycaster));
            root=go.GetComponent<RectTransform>(); canvas=go.GetComponent<Canvas>(); canvas.renderMode=RenderMode.ScreenSpaceOverlay;
            var scaler=go.GetComponent<UnityEngine.UI.CanvasScaler>(); scaler.uiScaleMode=UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution=new Vector2(1920,1080); scaler.matchWidthOrHeight=1;
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
            var top=Panel(screen,"Command bar",new Vector2(0,1),Vector2.zero,new Vector2(1920,64),panelColor);top.anchorMax=new Vector2(1,1);top.sizeDelta=new Vector2(0,64);
            Text(top,"FLEET / SURVIVAL",new Vector2(0,1),new Vector2(24,-17),new Vector2(285,32),24,ink);
            wave=Text(top,"WAVE 01",new Vector2(0,1),new Vector2(320,-21),new Vector2(140,30),20,cyan);
            resources=Text(top,"SALVAGE 220",new Vector2(0,1),new Vector2(475,-21),new Vector2(235,30),20,ink);
            contacts=Text(top,"CONTACTS 0",new Vector2(0,1),new Vector2(750,-21),new Vector2(700,30),18,ink);
            var mute=Button(top,"SOUND ON",new Vector2(1,1),new Vector2(-172,-12),new Vector2(112,40),()=>game.ToggleMute());muteLabel=mute.GetComponentInChildren<TextMeshProUGUI>();muteLabel.fontSize=16;
            Button(top,"PAUSE",new Vector2(1,1),new Vector2(-64,-12),new Vector2(96,40),()=>game.TogglePause()).GetComponentInChildren<TextMeshProUGUI>().fontSize=17;
            Button(top,"?",new Vector2(1,1),new Vector2(-16,-12),new Vector2(36,40),ToggleControls);
            var command=Panel(screen,"Command ship status",new Vector2(0,1),new Vector2(18,-88),new Vector2(280,140),panelColor);
            Text(command,"COMMAND SHIP",new Vector2(0,1),new Vector2(16,-12),new Vector2(248,25),16,cyan);
            flagStatus=Text(command,"Venator flagship",new Vector2(0,1),new Vector2(16,-39),new Vector2(248,42),17,ink);
            hullBar=Bar(command,"Hull",new Vector2(16,-87),new Vector2(248,6),new Color(.35f,.86f,.65f));
            shieldBar=Bar(command,"Shields",new Vector2(16,-100),new Vector2(248,4),cyan);
            Text(command,"[Q] focus command ship",new Vector2(0,0),new Vector2(16,9),new Vector2(248,20),15,mutedText);
            var selection=Panel(screen,"Selection",new Vector2(0,1),new Vector2(18,-240),new Vector2(280,264),panelColor);
            selected=Text(selection,"1 ship selected",new Vector2(0,1),new Vector2(16,-12),new Vector2(248,44),20,ink);
            unitDetails=Text(selection,"Select a friendly unit to inspect it.",new Vector2(0,1),new Vector2(16,-63),new Vector2(248,88),17,ink);
            unitRefit=Text(selection,"",new Vector2(0,1),new Vector2(16,-156),new Vector2(248,60),14,cyan);
            Button(selection,"SELECT FLEET [TAB]",new Vector2(0,0),new Vector2(16,10),new Vector2(248,30),()=>game.SelectAll()).GetComponentInChildren<TextMeshProUGUI>().fontSize=16;
            var tally=Panel(screen,"Battle tally",new Vector2(0,1),new Vector2(18,-516),new Vector2(280,144),panelColor);
            Text(tally,"BATTLE TALLY",new Vector2(0,1),new Vector2(16,-12),new Vector2(145,24),16,cyan);
            Text(tally,"WAVE    RUN",new Vector2(1,1),new Vector2(-14,-14),new Vector2(108,20),14,mutedText,TextAlignmentOptions.TopRight);
            Text(tally,"Enemy capitals\nEnemy fighters\nFriendly capitals lost\nFriendly fighters lost",new Vector2(0,1),new Vector2(16,-43),new Vector2(183,90),16,ink);
            tallyWave=Text(tally,"0\n0\n0\n0",new Vector2(1,1),new Vector2(-67,-43),new Vector2(35,90),16,ink,TextAlignmentOptions.TopRight);
            tallyRun=Text(tally,"0\n0\n0\n0",new Vector2(1,1),new Vector2(-16,-43),new Vector2(40,90),16,cyan,TextAlignmentOptions.TopRight);
            var map=Panel(screen,"Tactical map",new Vector2(1,1),new Vector2(-18,-88),new Vector2(238,232),panelColor);
            Text(map,"GEONOSIS / ORBIT",new Vector2(0,1),new Vector2(14,-12),new Vector2(210,25),16,cyan);
            mapDots=Panel(map,"Ship contacts",new Vector2(.5f,.5f),new Vector2(0,-4),new Vector2(210,158),new Color(.01f,.025f,.04f,.9f));
            mapDots.gameObject.AddComponent<UnityEngine.UI.RectMask2D>();mapDots.gameObject.AddComponent<FleetMinimap>().Initialize(this);
            BuildMapGrid();
            Text(map,"CLICK / DRAG TO PAN",new Vector2(.5f,0),new Vector2(0,8),new Vector2(210,20),13,mutedText,TextAlignmentOptions.Center);
            var help=Panel(screen,"Controls",new Vector2(1,1),new Vector2(-18,-334),new Vector2(300,259),panelColor);controls=help.gameObject;
            Text(help,"FLEET ORDERS [H / ?]",new Vector2(0,1),new Vector2(18,-15),new Vector2(264,28),17,cyan);
            Text(help,"Left click / drag: select ships\nShift: add to selection\nRight click: move / focus fire\nF then right click: attack-move\nWASD / middle drag: pan\nScroll: zoom     Q: flagship\nSpace / Esc: pause\n1 / 2 / 3: call reinforcements",new Vector2(0,1),new Vector2(18,-55),new Vector2(264,192),17,ink);controls.SetActive(false);
            BuildHangarPanel(screen);
            var bottom=Panel(screen,"Reinforcements",Vector2.zero,Vector2.zero,new Vector2(1920,190),panelColor);bottom.anchorMax=new Vector2(1,0);bottom.sizeDelta=new Vector2(0,190);
            phase=Text(bottom,"PREPARATION / REINFORCE YOUR FLEET",new Vector2(0,1),new Vector2(20,-12),new Vector2(1490,24),16,cyan);
            cardArea=Rect(bottom,"Fleet requisitions",Vector2.zero,new Vector2(20,37),new Vector2(1492,115));cardArea.anchorMax=new Vector2(1,0);cardArea.sizeDelta=new Vector2(-408,115);
            cardGrid=cardArea.gameObject.AddComponent<UnityEngine.UI.GridLayoutGroup>();cardGrid.constraint=UnityEngine.UI.GridLayoutGroup.Constraint.FixedColumnCount;cardGrid.constraintCount=6;cardGrid.spacing=new Vector2(10,0);cardGrid.cellSize=new Vector2(235,115);
            var classes=new[]{ShipClass.Fighter,ShipClass.Interceptor,ShipClass.Escort,ShipClass.Frigate,ShipClass.Destroyer,ShipClass.Carrier};
            for(int i=0;i<classes.Length;i++) {var kind=classes[i];var button=Button(cardArea,kind.ToString(),Vector2.zero,Vector2.zero,new Vector2(235,115),()=>game.BeginReinforcementPlacement(kind));buyButtons[kind]=button;var label=button.GetComponentInChildren<TextMeshProUGUI>();label.fontSize=16;label.rectTransform.anchorMin=Vector2.zero;label.rectTransform.anchorMax=Vector2.one;label.rectTransform.offsetMin=new Vector2(6,5);label.rectTransform.offsetMax=new Vector2(-6,-5);}
            repair=Button(bottom,"REPAIR [R]\n120 salvage",new Vector2(1,0),new Vector2(-206,91),new Vector2(162,61),()=>game.RepairFleet());repair.GetComponentInChildren<TextMeshProUGUI>().fontSize=16;
            refit=Button(bottom,"WEAPON REFIT\n200 / +10%\nMAX +100%",new Vector2(1,0),new Vector2(-20,91),new Vector2(176,61),()=>game.UpgradeWeapons());refit.GetComponentInChildren<TextMeshProUGUI>().fontSize=15;
            launch=Button(bottom,"LAUNCH WAVE",new Vector2(1,0),new Vector2(-20,37),new Vector2(348,44),()=>game.LaunchWave());launch.GetComponentInChildren<TextMeshProUGUI>().fontSize=19;
            Text(bottom,"Choose a ship, then click an arrival point. Right-click / Esc cancels.   [H / ?] controls",new Vector2(0,0),new Vector2(20,9),new Vector2(1700,20),15,mutedText);
            message=Text(screen,"Fleet ready.",new Vector2(.5f,1),new Vector2(0,-78),new Vector2(1050,46),19,ink,TextAlignmentOptions.Center);
        }
        void BuildHangarPanel(Transform screen)
        {
            var panel=Panel(screen,"Hangar operations",new Vector2(1,1),new Vector2(-18,-334),new Vector2(300,367),panelColor);hangarPanel=panel.gameObject;
            Text(panel,"HANGAR OPERATIONS",new Vector2(0,1),new Vector2(16,-12),new Vector2(268,24),16,cyan);
            hangarReadout=Text(panel,"",new Vector2(0,1),new Vector2(16,-44),new Vector2(268,62),15,ink);
            foreach(SquadronRole role in Enum.GetValues(typeof(SquadronRole)))
            {
                var type=role;int index=(int)role;
                string label=role==SquadronRole.Interceptor?"INTERCEPT":role.ToString().ToUpper();
                hangarLaunch[index]=Button(panel,label+"\n"+FleetRules.LaunchCost(role)+" salvage",new Vector2(0,1),new Vector2(16+index*92,-113),new Vector2(84,55),()=>game.LaunchSelectedSquadron(type));var launchLabel=hangarLaunch[index].GetComponentInChildren<TextMeshProUGUI>();launchLabel.fontSize=13;launchLabel.rectTransform.sizeDelta=new Vector2(78,47);
            }
            recoverSquad=Button(panel,"RECOVER [BACKSPACE]",new Vector2(0,1),new Vector2(16,-180),new Vector2(268,34),()=>game.RecoverSelectedSquadron());recoverSquad.GetComponentInChildren<TextMeshProUGUI>().fontSize=14;
            relaunchSquad=Button(panel,"RELAUNCH SQUADRON",new Vector2(0,1),new Vector2(16,-180),new Vector2(268,34),()=>game.RelaunchSelectedSquadron());relaunchSquad.GetComponentInChildren<TextMeshProUGUI>().fontSize=14;
            for(int i=0;i<3;i++) {int bay=i;hangarBays[i]=Button(panel,"BAY "+(i+1),new Vector2(0,1),new Vector2(16,-224-i*32),new Vector2(268,27),()=>game.SelectHangarBay(displayedHangar,bay));hangarBays[i].GetComponentInChildren<TextMeshProUGUI>().fontSize=13;}
            Text(panel,"1 capacity / squad, including docked.\nLost craft: 8 salvage each / 6 seconds.",new Vector2(0,0),new Vector2(16,9),new Vector2(268,38),13,mutedText);
        }
        void RefreshHangar(FleetShip unit,bool single)
        {
            displayedHangar=single && unit!=null?(unit.Hangar??(unit.Squadron!=null?game.RecoveryHangar(unit):null)):null;
            hangarPanel.SetActive(displayedHangar!=null && !ControlsVisible);
            if(displayedHangar==null) return;
            var hangar=displayedHangar;
            bool carrierSelected=unit.Hangar==hangar;
            hangarPanel.GetComponent<RectTransform>().sizeDelta=new Vector2(300,carrierSelected?330:300);
            string deck=hangar.Cooldown>0?"Deck ready in "+hangar.Cooldown.ToString("0.0")+"s":"Launch deck ready";
            hangarReadout.text=hangar.Owner.Stats.Name+"\nBays "+hangar.UsedBays+" / "+hangar.Bays+" | "+deck+"\n"+(unit.Docked?"Docked / rearming "+Mathf.Max(0,6-unit.DockTimer).ToString("0.0")+"s":unit.ReturningToHangar?"Returning / vulnerable until docked":"Select a bay below to inspect its squad.");
            foreach(SquadronRole role in Enum.GetValues(typeof(SquadronRole))) {hangarLaunch[(int)role].gameObject.SetActive(carrierSelected);hangarLaunch[(int)role].interactable=carrierSelected && hangar.CanLaunch(role);}
            recoverSquad.gameObject.SetActive(!carrierSelected && !unit.Docked);relaunchSquad.gameObject.SetActive(!carrierSelected && unit.Docked);
            recoverSquad.GetComponent<RectTransform>().anchoredPosition=new Vector2(16,-113);relaunchSquad.GetComponent<RectTransform>().anchoredPosition=new Vector2(16,-113);
            recoverSquad.interactable=unit.Squadron!=null && unit.Targetable && !unit.ReturningToHangar && hangar.Available;
            relaunchSquad.interactable=unit.Docked && hangar.Available && hangar.Cooldown<=0 && unit.DockTimer>=6;
            for(int i=0;i<3;i++)
            {
                hangarBays[i].gameObject.SetActive(i<hangar.Bays);
                hangarBays[i].GetComponent<RectTransform>().anchoredPosition=new Vector2(16,-(carrierSelected?180:153)-i*32);
                var squad=i<hangar.Squadrons.Count?hangar.Squadrons[i]:null;hangarBays[i].interactable=squad!=null && squad.Alive;
                hangarBays[i].GetComponentInChildren<TextMeshProUGUI>().text="BAY "+(i+1)+" / "+(squad==null?"EMPTY":squad.Role.ToString().ToUpper()+" "+squad.Squadron.ActiveCount+"/6 • "+(squad.Docked?"DOCKED":squad.ReturningToHangar?"RETURNING":squad.IsArriving?"LAUNCHING":"DEPLOYED"));
            }
        }
        public void ToggleControls() { controls.SetActive(!controls.activeSelf); }
        public void PanMap(Vector2 screenPosition,Camera eventCamera)
        {
            if(game.Phase==BattlePhase.Menu || game.Phase==BattlePhase.Defeat) return;
            if(RectTransformUtility.ScreenPointToLocalPointInRectangle(mapDots,screenPosition,eventCamera,out var point))
                game.FocusSector(new Vector3(point.x/100*FleetRules.ArenaRadius,0,point.y/73*FleetRules.ArenaRadius));
        }
        RectTransform MapLine(string name,Color color)
        { return Panel(mapDots,name,new Vector2(.5f,.5f),Vector2.zero,new Vector2(1,1),color,false); }
        static void LineBetween(RectTransform line,Vector2 a,Vector2 b,float width)
        {line.anchoredPosition=(a+b)*.5f;line.sizeDelta=new Vector2(Vector2.Distance(a,b),width);line.localRotation=Quaternion.Euler(0,0,Mathf.Atan2(b.y-a.y,b.x-a.x)*Mathf.Rad2Deg);}
        void BuildMapGrid()
        {
            var dim=new Color(.08f,.18f,.23f,.75f);
            for(int i=-2;i<=2;i++) {LineBetween(MapLine("Sector grid",dim),new Vector2(i*40,-73),new Vector2(i*40,73),1);LineBetween(MapLine("Sector grid",dim),new Vector2(-100,i*29.2f),new Vector2(100,i*29.2f),1);}
            for(int i=0;i<40;i++) {float a=i*Mathf.PI*2/40,b=(i+1)*Mathf.PI*2/40;LineBetween(MapLine("Sector perimeter",new Color(.2f,.39f,.45f)),new Vector2(Mathf.Cos(a)*100,Mathf.Sin(a)*73),new Vector2(Mathf.Cos(b)*100,Mathf.Sin(b)*73),1);}
            for(int i=0;i<4;i++) viewEdges[i]=MapLine("Camera footprint",new Color(.68f,.76f,.8f,.7f));
        }
        void UpdateMapView()
        {
            var plane=new Plane(Vector3.up,Vector3.zero);var corners=new[]{Vector2.zero,Vector2.right,Vector2.one,Vector2.up};var points=new Vector2[4];
            for(int i=0;i<4;i++) {var ray=game.ViewCamera.ViewportPointToRay(corners[i]);if(plane.Raycast(ray,out float distance)) {var p=ray.GetPoint(distance);points[i]=new Vector2(p.x/FleetRules.ArenaRadius*100,p.z/FleetRules.ArenaRadius*73);}}
            for(int i=0;i<4;i++) LineBetween(viewEdges[i],points[i],points[(i+1)%4],1.4f);
        }
        UnityEngine.UI.Image Bar(Transform parent,string name,Vector2 pos,Vector2 size,Color color)
        {
            var bg=Panel(parent,name,new Vector2(0,1),pos,size,new Color(.075f,.12f,.17f),false);
            var fg=Panel(bg,name+" fill",Vector2.zero,Vector2.zero,size,color,false); fg.anchorMax=Vector2.one; fg.offsetMin=fg.offsetMax=Vector2.zero; return fg.GetComponent<UnityEngine.UI.Image>();
        }
        void Fill(UnityEngine.UI.Image image,float value) { image.rectTransform.anchorMax=new Vector2(Mathf.Clamp01(value),1); image.rectTransform.offsetMin=image.rectTransform.offsetMax=Vector2.zero; }
        void BuildPause()
        {
            var screen=Stretch(root,"Pause overlay");paused=screen.gameObject;
            var dim=screen.gameObject.AddComponent<UnityEngine.UI.Image>();dim.color=new Color(0,.01f,.025f,.45f);dim.raycastTarget=false;
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
            Text(panel,"Venator / Acclamator — Cpt.Kirk\nDetailed Venator — ForkyForklift\nARC-170 / V-Wing / Vulture droid — DrEgguin\nV-19 Torrent — AirStudios\nDroid Tri-fighter — Steven\nArquitens — A308 Digital\nLucrehulk — Neut2000\nMunificent / Providence / Recusant — lolqeeeeeeeeee\n\nProvidence / Recusant / Tri-fighter: CC BY-NC 4.0\nOther supplied models: CC BY 4.0\n\nModels supplied via Sketchfab; scale and orientation adjusted.\nFull source links and licenses: THIRD_PARTY_ASSETS.md.\nFree fan prototype. Star Wars belongs to its respective owners.",new Vector2(0,1),new Vector2(45,-135),new Vector2(1010,630),25,mutedText);
            Button(panel,"BACK",new Vector2(.5f,0),new Vector2(0,35),new Vector2(420,60),()=>credits.SetActive(false));
        }
        public void Rebuild()
        {
            if(menu==null) return; menu.SetActive(game.Phase==BattlePhase.Menu); battle.SetActive(game.Phase!=BattlePhase.Menu);
            markersRoot.gameObject.SetActive(game.Phase!=BattlePhase.Menu && game.Phase!=BattlePhase.Defeat);
            paused.SetActive(game.Paused); defeat.SetActive(game.Phase==BattlePhase.Defeat); credits.SetActive(false);
            best.text="BEST SURVIVAL: "+game.BestWave+" WAVES";
            endReport.text="Waves survived: "+Mathf.Max(0,game.Wave-1)+"\nEnemy capitals / fighters: "+game.RunTally.EnemyCapitals+" / "+game.RunTally.EnemyFighters+"\nFriendly capitals / fighters lost: "+game.RunTally.FriendlyCapitals+" / "+game.RunTally.FriendlyFighters+"\nBest survival: "+game.BestWave+" waves";
            foreach(var pair in buyButtons) pair.Value.GetComponentInChildren<TextMeshProUGUI>().text=ReinforcementLabel(pair.Key);
            RefreshText();
        }
        public void SetMute(bool value) { muteLabel.text=value?"SOUND OFF":"SOUND ON"; }
        public void CapturePreview(string path,int width=1600,int height=900)
        {
            RefreshText(); UpdateMarkers();
            var camera=game.ViewCamera; var previousMode=canvas.renderMode;
            var previousTarget=camera.targetTexture; var previousActive=RenderTexture.active;
            var target=new RenderTexture(width,height,24);
            camera.targetTexture=target; canvas.renderMode=RenderMode.ScreenSpaceCamera; canvas.worldCamera=camera; canvas.planeDistance=1;
            Canvas.ForceUpdateCanvases();RefreshText();UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(cardArea);UpdateMarkers();Canvas.ForceUpdateCanvases();camera.Render(); RenderTexture.active=target;
            var texture=new Texture2D(width,height,TextureFormat.RGB24,false); texture.ReadPixels(new Rect(0,0,width,height),0,0); texture.Apply();
            System.IO.File.WriteAllBytes(path,texture.EncodeToPNG());
            camera.targetTexture=previousTarget; RenderTexture.active=previousActive; canvas.renderMode=previousMode;
            target.Release(); Destroy(target); Destroy(texture);
        }
        void LateUpdate()
        {
            if(game.Phase!=BattlePhase.Menu && Input.GetKeyDown(KeyCode.H)) ToggleControls();
            refresh-=Time.unscaledDeltaTime; if(refresh<=0) { RefreshText(); refresh=.15f; } UpdateMarkers();
            marquee.gameObject.SetActive(game.IsDragging);
            if(game.IsDragging) { var rect=FleetGame.ScreenRect(game.DragStart,game.DragCurrent); RectTransformUtility.ScreenPointToLocalPointInRectangle(root,new Vector2(rect.x,rect.y),null,out var local); marquee.pivot=Vector2.zero; marquee.anchorMin=marquee.anchorMax=new Vector2(.5f,.5f); marquee.anchoredPosition=local; marquee.sizeDelta=rect.size/canvas.scaleFactor; }
        }
        public string ReinforcementLabel(ShipClass kind)
        {
            var stats=FleetRules.Stats(game.PlayerFaction,kind);bool pair=game.CallInCount(kind)>1;
            string name=pair?"Arquitens cruiser pair (2)":game.CallInName(kind);
            return name+"\n"+game.CallInCost(kind)+" salvage | "+game.CallInCapacity(kind)+" capacity\n"+(stats.Damage/stats.Interval).ToString("0.#")+" DPS"+(pair?" each":"")+"\nHull "+stats.Hull+" | Shields "+stats.Shield+(pair?" each":"");
        }
        public void RefreshText()
        {
            if(wave==null) return; wave.text="WAVE "+Mathf.Max(1,game.Wave).ToString("00"); resources.text="SALVAGE "+game.Salvage;
            cardGrid.cellSize=new Vector2(Mathf.Max(160,(cardArea.rect.width-50)/6),115);
            bool compact=root.rect.width<1800;
            resources.rectTransform.anchoredPosition=new Vector2(compact?430:475,-21);
            float contactX=compact?640:750;contacts.rectTransform.anchoredPosition=new Vector2(contactX,-21);
            contacts.rectTransform.sizeDelta=new Vector2(Mathf.Max(300,root.rect.width-contactX-304),30);
            contacts.enableAutoSizing=true;contacts.fontSizeMin=14;contacts.fontSizeMax=18;
            contacts.text="CAPACITY "+game.FleetCapacityUsed+" / "+FleetRules.FleetLimit+"   HOSTILES "+game.EnemyCount+"   INBOUND "+game.IncomingCount; message.text=game.Message;
            phase.text=game.SelectedReinforcement.HasValue?"HYPERSPACE / CLICK AN ARRIVAL POINT   •   RIGHT-CLICK TO CANCEL":game.Phase==BattlePhase.Preparation ? "PREPARATION / NEXT: "+game.NextWaveBriefing : "ASSAULT / SCREEN YOUR CAPITALS; INTERCEPT ENEMY STRIKE SQUADRONS";
            var ship=game.Flagship;
            if(ship!=null) { flagStatus.text=ship.Stats.Name+"\n"+Mathf.CeilToInt(ship.Hull)+" hull / "+Mathf.CeilToInt(ship.Shield)+" shields"; Fill(hullBar,ship.Hull/ship.MaxHull); Fill(shieldBar,ship.Shield/ship.MaxShield); }
            else { Fill(hullBar,0); Fill(shieldBar,0); }
            var chosen=game.Ships.Where(s=>s!=null && s.Alive && s.Selected && s.Friendly).ToArray(); selected.text=chosen.Length==1?chosen[0].Stats.Name:chosen.Length+" units selected";
            if(chosen.Length==1)
            {
                var unit=chosen[0];
                string status=unit.Docked?"DOCKED":unit.IsArriving?"LAUNCHING":unit.ReturningToHangar?"RETURNING":unit.Destruction.Status;
                unitDetails.text="Hull "+Mathf.CeilToInt(unit.Hull)+" / "+Mathf.CeilToInt(unit.MaxHull)+"\nShields "+Mathf.CeilToInt(unit.Shield)+" / "+Mathf.CeilToInt(unit.MaxShield)+"\n"+unit.EffectiveDPS.ToString("0.#")+" DPS"+(unit.Docked?" (docked)":" | Range "+unit.Stats.Range)+"\nSpeed "+(unit.Docked || unit.IsArriving?0:unit.Stats.Speed*unit.Destruction.Mobility).ToString("0.#")+" | "+status;
                unitRefit.text=FleetTactics.RoleText(unit)+"\n"+(unit.WeaponRefits>0?"Refits "+unit.WeaponRefits+" | +"+(unit.RefitBonus*100).ToString("0")+"% weapons":"No weapon refit")+"\n"+(unit.Squadron!=null?unit.Squadron.ActiveCount+" / 6 fighters | ":"")+(unit.Docked?"Servicing":unit.Destruction.Firepower<1?"Hull damage: -15% firepower":"Weapons operational");
            }
            else if(chosen.Length>1)
            {
                unitDetails.text="Hull "+Mathf.CeilToInt(chosen.Sum(s=>s.Hull))+" / "+Mathf.CeilToInt(chosen.Sum(s=>s.MaxHull))+"\nShields "+Mathf.CeilToInt(chosen.Sum(s=>s.Shield))+" / "+Mathf.CeilToInt(chosen.Sum(s=>s.MaxShield))+"\nCombined "+chosen.Sum(s=>s.EffectiveDPS).ToString("0.#")+" DPS\nSelect one unit for range and speed.";
                unitRefit.text="Refitted units: "+chosen.Count(s=>s.WeaponRefits>0)+" / "+chosen.Length+"\nNew reinforcements arrive without refits.";
            }
            else { unitDetails.text="Select a friendly unit to inspect hull, shields, weapons and handling.";unitRefit.text="Refits affect the fleet currently in battle."; }
            RefreshHangar(chosen.Length==1?chosen[0]:null,chosen.Length==1);
            tallyWave.text=TallyText(game.WaveTally);tallyRun.text=TallyText(game.RunTally);
            if(game.Phase==BattlePhase.Defeat) endReport.text="Waves survived: "+Mathf.Max(0,game.Wave-1)+"\nEnemy capitals / fighters: "+game.RunTally.EnemyCapitals+" / "+game.RunTally.EnemyFighters+"\nFriendly capitals / fighters lost: "+game.RunTally.FriendlyCapitals+" / "+game.RunTally.FriendlyFighters+"\nBest survival: "+game.BestWave+" waves";
            foreach(var pair in buyButtons) pair.Value.interactable=game.CanCallIn(pair.Key);
            repair.interactable=game.Phase==BattlePhase.Preparation && !game.Paused && game.Salvage>=120 && game.Ships.Any(s=>s!=null && s.Friendly && s.Hull<s.MaxHull-.1f);
            refit.interactable=game.CanUpgradeWeapons;
            refit.GetComponentInChildren<TextMeshProUGUI>().text=!game.HasUpgradeableShips?"WEAPON REFIT\nMAX +100% REACHED":"WEAPON REFIT\n200 / +10%\nMAX +100%";
            launch.interactable=game.Phase==BattlePhase.Preparation && !game.Paused; launch.GetComponentInChildren<TextMeshProUGUI>().text=game.Phase==BattlePhase.Combat?"ASSAULT IN PROGRESS":"LAUNCH WAVE "+(game.Wave+1);
        }
        static string TallyText(FleetBattleTally tally) => tally.EnemyCapitals+"\n"+tally.EnemyFighters+"\n"+tally.FriendlyCapitals+"\n"+tally.FriendlyFighters;
        void UpdateMarkers()
        {
            foreach(var wreck in hazards.Keys.ToArray()) if(wreck==null || wreck.Complete || !game.ActiveWrecks.Contains(wreck)) { Destroy(hazards[wreck].gameObject);hazards.Remove(wreck); }
            foreach(var ship in markers.Keys.ToArray())
            {
                if(ship!=null && ship.Alive && game.Ships.Contains(ship)) continue;
                var old=markers[ship]; Destroy(old.World.gameObject); Destroy(old.Dot.gameObject); markers.Remove(ship);
            }
            if(game.Phase==BattlePhase.Menu || game.Phase==BattlePhase.Defeat) return;
            UpdateMapView();
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
                bool readout=ship.Selected || ship.IsFlagship || ship.Hull<ship.MaxHull-.1f || ship.Shield<ship.MaxShield-.1f;
                bool visible=!ship.Docked && readout && screen.z>0 && screen.x>0 && screen.x<game.ViewCamera.pixelWidth && screen.y>0 && screen.y<game.ViewCamera.pixelHeight; marker.World.gameObject.SetActive(visible);
                if(visible) { RectTransformUtility.ScreenPointToLocalPointInRectangle(root,screen,canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera,out var position); marker.World.anchoredPosition=position; marker.Name.text=ship.IsFlagship?"COMMAND":ship.Selected?ship.Stats.Name:""; Fill(marker.Hull,ship.Hull/ship.MaxHull); Fill(marker.Shield,ship.Shield/ship.MaxShield); }
                marker.Dot.anchoredPosition=new Vector2(ship.transform.position.x/FleetRules.ArenaRadius*100,ship.transform.position.z/FleetRules.ArenaRadius*73);
                marker.Dot.gameObject.SetActive(!ship.Docked);
                marker.Dot.GetComponent<UnityEngine.UI.Image>().color=ship.Selected?Color.white:ship.Friendly?cyan:new Color(1,.35f,.2f);
            }
            foreach(var wreck in game.ActiveWrecks)
            {
                if(wreck==null || !wreck.Meltdown || wreck.Complete) continue;
                if(!hazards.TryGetValue(wreck,out var warning)) { warning=Text(markersRoot,"REACTOR FAILURE",new Vector2(.5f,.5f),Vector2.zero,new Vector2(260,56),20,new Color(1,.65f,.28f),TextAlignmentOptions.Center);hazards[wreck]=warning; }
                var screen=game.ViewCamera.WorldToScreenPoint(wreck.transform.position+Vector3.up*(wreck.Radius+2));
                warning.gameObject.SetActive(screen.z>0);warning.text="REACTOR FAILURE\n"+wreck.Countdown.ToString("0.0")+"s";
                RectTransformUtility.ScreenPointToLocalPointInRectangle(root,screen,canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera,out var position);warning.rectTransform.anchoredPosition=position;
            }
        }
    }
}
