using Verse;

namespace VNPEReimaginedProgression
{
    public class HediffCompProperties_HungerSuppressionSeverityPerDay : HediffCompProperties_SeverityPerDay
    {
        public int tickPerFeed = 125;
        public float Nutrition = 0.005f;
        public int amountOfFeed = 20;

        public HediffCompProperties_HungerSuppressionSeverityPerDay()
        {
            compClass = typeof(HediffComp_HungerSuppressionSeverityPerDay);
        }
    }
}
