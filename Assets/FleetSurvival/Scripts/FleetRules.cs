using UnityEngine;

namespace FleetSurvival
{
    public enum Faction { Republic, CIS }
    public enum ShipClass { Flagship, Frigate, Fighter, Destroyer, Interceptor, Escort, Carrier }
    public enum BattlePhase { Menu, Preparation, Combat, Defeat }

    public struct ShipStats
    {
        public string Name;
        public float Hull, Shield, Speed, Range, Damage, Interval, Radius;
        public int Cost, Salvage;
    }

    public struct FlightHandling
    {
        public float Acceleration, Braking, TurnRate;
        public FlightHandling(float acceleration,float braking,float turnRate)
        { Acceleration=acceleration; Braking=braking; TurnRate=turnRate; }
    }

    public static class FleetRules
    {
        public const int FleetLimit = 22;
        public const float ArenaRadius = 76;
        public const int StartingSalvage = 220;

        public static FlightHandling Handling(ShipClass kind)
        {
            switch(kind)
            {
                case ShipClass.Flagship: return new FlightHandling(.85f,1.6f,22);
                case ShipClass.Carrier: return new FlightHandling(.75f,1.4f,18);
                case ShipClass.Destroyer: return new FlightHandling(1.4f,2.2f,30);
                case ShipClass.Frigate: return new FlightHandling(2.2f,3.2f,45);
                case ShipClass.Escort: return new FlightHandling(3.5f,4.5f,65);
                case ShipClass.Interceptor: return new FlightHandling(12,16,200);
                default: return new FlightHandling(8,12,150);
            }
        }

        public static ShipStats Stats(Faction faction, ShipClass kind)
        {
            bool republic = faction == Faction.Republic;
            if (kind == ShipClass.Flagship)
                return new ShipStats { Name = republic ? "Venator flagship" : "Providence flagship", Hull = republic ? 1600 : 1400,
                    Shield = republic ? 850 : 1050, Speed = 4.2f, Range = 36, Damage = 55, Interval = 1.5f, Radius = 5.5f, Cost = 650, Salvage = 260 };
            if (kind == ShipClass.Frigate)
                return new ShipStats { Name = republic ? "Acclamator cruiser" : "Munificent frigate", Hull = republic ? 520 : 440,
                    Shield = republic ? 300 : 240, Speed = republic ? 8 : 9, Range = 29, Damage = republic ? 29 : 27,
                    Interval = 1.15f, Radius = 3.2f, Cost = republic ? 180 : 155, Salvage = 80 };
            if (kind == ShipClass.Destroyer)
                return new ShipStats { Name = republic ? "Venator destroyer" : "Recusant destroyer", Hull = republic ? 1000 : 850,
                    Shield = republic ? 500 : 420, Speed = 6, Range = 34, Damage = republic ? 43 : 46,
                    Interval = 1.3f, Radius = 4.5f, Cost = republic ? 400 : 340, Salvage = 150 };
            if (kind == ShipClass.Escort)
                return new ShipStats { Name = republic ? "Arquitens light cruiser" : "Munificent escort", Hull = republic ? 340 : 300,
                    Shield = 180, Speed = 11, Range = 25, Damage = 20, Interval = 1.0f, Radius = 2.6f, Cost = republic ? 140 : 125, Salvage = 60 };
            if (kind == ShipClass.Carrier)
                return new ShipStats { Name = republic ? "Venator carrier" : "Lucrehulk carrier", Hull = republic ? 1450 : 1800,
                    Shield = republic ? 700 : 900, Speed = 4, Range = 32, Damage = 38, Interval = 1.5f, Radius = 6.2f, Cost = republic ? 650 : 600, Salvage = 220 };
            if (kind == ShipClass.Interceptor)
                return new ShipStats { Name = republic ? "V-19 Torrent squadron" : "Vulture interceptor wing", Hull = republic ? 100 : 85,
                    Shield = 45, Speed = republic ? 24 : 27, Range = 15, Damage = 9, Interval = .48f, Radius = 1.3f, Cost = republic ? 65 : 50, Salvage = 25 };
            return new ShipStats { Name = republic ? "ARC-170 squadron" : "Vulture squadron", Hull = republic ? 145 : 115,
                Shield = republic ? 80 : 50, Speed = republic ? 18 : 23, Range = 17, Damage = republic ? 12 : 10,
                Interval = republic ? .65f : .48f, Radius = 1.5f, Cost = republic ? 85 : 65, Salvage = 30 };
        }

        public static int WaveBudget(int wave) => 2 + wave * 2 + wave / 3;
        public static int WaveReward(int wave) => 105 + wave * 25;
        public static float EnemyMultiplier(int wave) => 0.65f + Mathf.Min(wave - 1, 25) * .055f;
        public static Color Color(Faction faction) => faction == Faction.Republic
            ? new Color(.25f, .8f, 1f) : new Color(1f, .43f, .2f);
        public static string FleetName(Faction faction) => faction == Faction.Republic ? "GALACTIC REPUBLIC" : "SEPARATIST ALLIANCE";
    }
}
