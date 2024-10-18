using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;

namespace VNPEReimaginedProgression
{
    public class WorkGiver_TakeOutNutrientPasteProcessor : WorkGiver_Scanner
    {
        public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
        {
            return pawn.Map.listerBuildings.allBuildingsColonist.Where((Building b) => b.HasComp<CompNutrientPasteProcessor>());
        }

        public override PathEndMode PathEndMode => PathEndMode.Touch;

        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            if (!(t is Building building) || t.IsBurning())
            {
                return false;
            }
            CompNutrientPasteProcessor compNutrientPasteProcessor = building.TryGetComp<CompNutrientPasteProcessor>();
            if (compNutrientPasteProcessor == null || !compNutrientPasteProcessor.IsRequireUnloading || compNutrientPasteProcessor.containedItems <= 0)
            {
                return false;
            }
            if (t.IsForbidden(pawn) || !pawn.CanReserve(t, 1, -1, null, forced))
            {
                return false;
            }
            return true;
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            return JobMaker.MakeJob(JobDefOfLocal.TakeOutNutrientPasteProcessor, t);
        }
    }
}
