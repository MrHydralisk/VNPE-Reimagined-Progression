using RimWorld;
using System;
using Verse;

namespace VNPEReimaginedProgression
{
    public class HediffComp_VNPEHungerSuppressionSeverityPerDay : HediffComp_SeverityPerDay
    {
        public event Action<Pawn> OnRemoved;
        public int TicksNextFeed;
        public int AmountOfFeed = 0;

        public HediffCompProperties_VNPEHungerSuppressionSeverityPerDay Props => (HediffCompProperties_VNPEHungerSuppressionSeverityPerDay)props;

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
            OnRemoved(Pawn);
            base.CompPostPostRemoved();
        }
    }
}
