using PipeSystem;
using Verse;

namespace VNPEReimaginedProgression
{
    public class CompProperties_NutrientPasteFilter : CompProperties
    {
        public float ticksPerNutrient = 2.5f;
        public float minPercentFilter = 0.1f;

        public DestroyOption destroyOption;

        [NoTranslate]
        public string startCommandTexPath;

        public CompProperties_NutrientPasteFilter()
        {
            compClass = typeof(CompNutrientPasteFilter);
        }
    }
}
