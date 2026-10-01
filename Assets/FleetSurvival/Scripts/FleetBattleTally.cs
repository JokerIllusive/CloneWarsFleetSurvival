namespace FleetSurvival
{
    public sealed class FleetBattleTally
    {
        public int EnemyCapitals, EnemyFighters, FriendlyCapitals, FriendlyFighters;
        public void Record(bool friendly,bool fighter)
        {
            if(friendly) { if(fighter) FriendlyFighters++; else FriendlyCapitals++; }
            else { if(fighter) EnemyFighters++; else EnemyCapitals++; }
        }
        public void Reset() { EnemyCapitals=EnemyFighters=FriendlyCapitals=FriendlyFighters=0; }
    }
}
