using System;
using System.Collections.Generic;
using UnityEngine;

namespace FleetSurvival
{
    // Synthesized fallbacks and edited effects from the user's supplied recordings.
    public static class FleetSound
    {
        static readonly Dictionary<string,AudioClip> clips=new Dictionary<string,AudioClip>();
        public static bool UsesSuppliedCannon(Faction faction,ShipClass kind) => faction==Faction.Republic?(kind==ShipClass.Flagship || kind==ShipClass.Destroyer || kind==ShipClass.Carrier):(kind==ShipClass.Frigate || kind==ShipClass.Escort);
        public static AudioClip Weapon(Faction faction,ShipClass kind)
        {
            string prefix=null;
            if(UsesSuppliedCannon(faction,kind)) prefix=faction==Faction.Republic?"Venator":"Munificent";
            else if(kind==ShipClass.Fighter || kind==ShipClass.Interceptor) prefix=faction==Faction.CIS?"Vulture":kind==ShipClass.Fighter?"ARC170":"VWing";
            else if(faction==Faction.Republic) prefix=kind==ShipClass.Frigate?"Acclamator":"Arquitens";
            else prefix=kind==ShipClass.Flagship?"Providence":kind==ShipClass.Destroyer?"Recusant":"Lucrehulk";
            if(prefix!=null)
            {
                string name=prefix+"Cannon0"+UnityEngine.Random.Range(1,3);
                var supplied=Resources.Load<AudioClip>("Audio/"+name);
                if(supplied!=null) return supplied;
            }
            bool fighter=kind==ShipClass.Fighter || kind==ShipClass.Interceptor;
            return Get((faction==Faction.Republic?"Republic":"CIS")+(fighter?"Fighter":"Heavy"));
        }
        public static AudioClip Get(string name)
        {
            if(clips.TryGetValue(name,out var ready)) return ready;
            var supplied=Resources.Load<AudioClip>("Audio/"+name);
            if(supplied!=null) { clips[name]=supplied; return supplied; }
            bool heavy=name.Contains("Heavy"),cis=name.Contains("CIS"),jump=name.Contains("Hyperspace"),explosion=name=="Explosion";
            float duration=explosion?1.3f:jump?1.1f:heavy?.36f:.19f;
            int rate=44100,count=(int)(rate*duration); var samples=new float[count];
            var random=new System.Random(name.GetHashCode()); double phase=0,secondary=0; float filtered=0;
            for(int i=0;i<count;i++)
            {
                float t=i/(float)rate,u=t/duration;
                float frequency=jump?Mathf.Lerp(1800,70,Mathf.Pow(u,.45f)):explosion?Mathf.Lerp(120,22,u):Mathf.Lerp(heavy?(cis?540:660):(cis?1350:1650),heavy?55:130,Mathf.Pow(u,.32f));
                phase+=frequency/rate*2*Math.PI; secondary+=(frequency*1.51+35)/rate*2*Math.PI;
                float noise=(float)random.NextDouble()*2-1; filtered=Mathf.Lerp(filtered,noise,explosion?.14f:.35f);
                float envelope=Mathf.Min(1,t/.003f)*Mathf.Exp(-u*(jump?3:explosion?4:7))*Mathf.Clamp01((1-u)*15);
                float metallic=(float)(Math.Sin(phase)+.35*Math.Sin(secondary)+.15*Math.Sin(phase*3.7));
                float crack=i<rate*.022f?noise*Mathf.Exp(-t*100)*.6f:0;
                float sample=explosion?filtered*1.1f+metallic*.16f:jump?filtered*.7f+metallic*.24f:metallic*.45f+filtered*.28f+crack;
                samples[i]=(float)Math.Tanh(sample*1.4f)*envelope*.75f;
            }
            ready=AudioClip.Create(name,count,1,rate,false); ready.SetData(samples,0); clips[name]=ready; return ready;
        }
    }
}
