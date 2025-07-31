using System.Collections.Generic;
using System.Text;
using PipeSystem;
using RimWorld;
using UnityEngine;
using Verse;

namespace VNPEReimaginedProgression
{
    [StaticConstructorOnStartup]
    public class CompRegisterIngredients : CompIngredients
    {
        private static readonly Texture2D transferIcon = ContentFinder<Texture2D>.Get("UI/TransferStorageContent");

        private CompResourceStorage compResourceStorage;

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (Gizmo item in base.CompGetGizmosExtra())
            {
                yield return item;
            }
            yield return new Command_Action
            {
                action = delegate
                {
                    ingredients.Clear();
                    compResourceStorage.Empty();
                },
                defaultLabel = "VNPE_Drain".Translate(),
                defaultDesc = "VNPE_DrainDesc".Translate(),
                icon = (Texture)(object)transferIcon
            };
        }

        public override string CompInspectStringExtra()
        {
            StringBuilder stringBuilder = new StringBuilder();
            if (ingredients.Count > 0)
            {
                stringBuilder.Append("Ingredients".Translate() + ": ");
                stringBuilder.Append(GetIngredientsString(includeMergeCompatibility: false, out var _));
            }
            return stringBuilder.ToString();
        }

        public override void CompTickRare()
        {
            base.CompTickRare();
            if (parent.Spawned && (compResourceStorage?.AmountStored ?? 1) == 0f)
            {
                ingredients.Clear();
            }
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            compResourceStorage = parent.GetComp<CompResourceStorage>();
        }
    }
}
