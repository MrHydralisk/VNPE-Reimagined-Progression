using RimWorld;
using Verse;

namespace VNPEReimaginedProgression
{
    public class HediffComp_HungerSuppressionSeverityPerDay : HediffComp_SeverityPerDay
    {
        public Building_NutrientPasteHungerSuppressor hungerSuppressor;
        public int TicksNextFeed;
        public int AmountOfFeed = 0;

        public HediffCompProperties_HungerSuppressionSeverityPerDay Props => (HediffCompProperties_HungerSuppressionSeverityPerDay)props;

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            if (Find.TickManager.TicksGame >= TicksNextFeed)
            {
                Pawn.needs.food.CurLevel += Props.Nutrition;
                Pawn.records.AddTo(RecordDefOf.NutritionEaten, Props.Nutrition);
                AmountOfFeed++;
                TicksNextFeed = Find.TickManager.TicksGame + Props.tickPerFeed;
                if (AmountOfFeed >= Props.amountOfFeed)
                {
                    Pawn.health.RemoveHediff(parent);
                }
            }
        }

        public override void CompPostPostRemoved()
        {
            hungerSuppressor?.RemoveCable(Pawn);
            base.CompPostPostRemoved();
        }
    }
}
