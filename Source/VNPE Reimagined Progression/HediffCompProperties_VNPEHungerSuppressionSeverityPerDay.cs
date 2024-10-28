using Verse;

namespace VNPEReimaginedProgression
{
    public class HediffCompProperties_VNPEHungerSuppressionSeverityPerDay : HediffCompProperties_SeverityPerDay
    {
        public int tickPerFeed = 125;
        public float Nutrition = 0.005f;
        public int amountOfFeed = 20;

        public HediffCompProperties_VNPEHungerSuppressionSeverityPerDay()
        {
            compClass = typeof(HediffComp_VNPEHungerSuppressionSeverityPerDay);
        }
    }
}
