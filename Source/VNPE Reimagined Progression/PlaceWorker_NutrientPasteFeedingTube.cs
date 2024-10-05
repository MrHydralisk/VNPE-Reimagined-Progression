using RimWorld;
using System.Linq;
using Verse;

namespace VNPEReimaginedProgression
{
    public class PlaceWorker_NutrientPasteFeedingTube : PlaceWorker
    {
        public override AcceptanceReport AllowsPlacing(BuildableDef checkingDef, IntVec3 loc, Rot4 rot, Map map, Thing thingToIgnore = null, Thing thing = null)
        {
            Thing bed = map.thingGrid.ThingsListAtFast(loc).FirstOrDefault((Thing t) => t?.def?.IsBed ?? false);
            if (bed == null)
            {
                return "VNPEReimaginedProgression.FeedingTube.PlaceWorker.MustBeOnBed".Translate();
            }
            return true;
        }
    }
}
