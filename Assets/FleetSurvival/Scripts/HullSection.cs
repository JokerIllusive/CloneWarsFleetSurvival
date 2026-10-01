using UnityEngine;

namespace FleetSurvival
{
    public sealed class HullSection : MonoBehaviour
    {
        public Mesh[] ArmorFragments;
        public Vector3[] FragmentCenters;
        public Mesh InnerHull;
        public Mesh[] BreachFragments;
        public Vector3[] BreachCenters,BreachNormals;
        public float[] BreachRadii;
    }
}
