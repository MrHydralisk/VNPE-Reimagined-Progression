using Verse;

namespace VNPEReimaginedProgression
{
    public class CompProperties_NutrientPasteProcessor : CompProperties
    {
        public int ticksPerProduction = 500;
        public float NutrientPasteCost = 1f;
        public int ItemProducedAmount = 1;
        public ThingDef ItemProducedDef;
        public int MaxCapacity = 10;

        public CompProperties_NutrientPasteProcessor()
        {
            compClass = typeof(CompNutrientPasteProcessor);
        }
    }
}
