using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace VNPEReimaginedProgression
{
    public class JobDriver_TakeOutNutrientPasteProcessor : JobDriver
    {
        private const TargetIndex NutrientPasteProcessorInd = TargetIndex.A;

        private const TargetIndex ProcessedToHaulInd = TargetIndex.B;

        private const int Duration = 200;

        protected Building NutrientPasteProcessor => (Building)job.GetTarget(NutrientPasteProcessorInd).Thing;

        protected Thing ProcessedThing => job.GetTarget(ProcessedToHaulInd).Thing;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(NutrientPasteProcessor, job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            CompNutrientPasteProcessor compNutrientPasteProcessor = NutrientPasteProcessor.GetComp<CompNutrientPasteProcessor>();
            this.FailOnDespawnedNullOrForbidden(NutrientPasteProcessorInd);
            this.FailOnBurningImmobile(NutrientPasteProcessorInd);
            yield return Toils_Goto.GotoThing(NutrientPasteProcessorInd, PathEndMode.Touch);
            yield return Toils_General.Wait(Duration).FailOnDestroyedNullOrForbidden(NutrientPasteProcessorInd).FailOnCannotTouch(NutrientPasteProcessorInd, PathEndMode.Touch)
                .FailOn(() => compNutrientPasteProcessor.containedItems < 1f)
                .WithProgressBarToilDelay(NutrientPasteProcessorInd);
            Toil toil = ToilMaker.MakeToil("MakeNewToils");
            toil.initAction = delegate
            {
                compNutrientPasteProcessor.ExtractProducedItems();
            };
            toil.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return toil;
        }
    }
}
