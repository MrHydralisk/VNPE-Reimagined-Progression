using PipeSystem;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Sound;
using VNPE;

namespace VNPEReimaginedProgression
{
    public class CompNutrientPasteFilter : ThingComp
    {
        public CompProperties_NutrientPasteFilter Props => (CompProperties_NutrientPasteFilter)props;

        public CompPowerTrader PowerTraderComp => powerTraderComp ?? (powerTraderComp = parent.GetComp<CompPowerTrader>());
        private CompPowerTrader powerTraderComp;

        public CompResource ResourceComp => resourceComp ?? (resourceComp = parent.GetComp<CompResource>());
        private CompResource resourceComp;

        private ThingDef IngredientDef;
        private float amountFiltered;

        private int ticksTotalFilter;
        private int ticksTillFiltered;
        private float amountLeft => amountFiltered * ticksTillFiltered / ticksTotalFilter;
        private float filterCost => Mathf.Max(1f / Mathf.Max(IngredientsList.Count(), 1), Props.minPercentFilter);

        private bool isFiltering => ticksTillFiltered > 0;

        public List<ThingDef> IngredientsList
        {
            get
            {
                if (Find.TickManager.TicksGame > tickLastIngerdientCheck || IngredientsListCached.NullOrEmpty())
                {
                    IngredientsListCached = new List<ThingDef>();
                    PipeNet pipeNet = ResourceComp?.PipeNet;
                    if (pipeNet != null)
                    {
                        for (int i = 0; i < pipeNet.storages.Count; i++)
                        {
                            CompRegisterIngredients compRegisterIngredients = pipeNet.storages[i].parent.TryGetComp<CompRegisterIngredients>();
                            if (compRegisterIngredients != null)
                            {
                                IngredientsListCached.AddRange(compRegisterIngredients.ingredients.Where((ThingDef td1) => !IngredientsListCached.Any((ThingDef td2) => td2 == td1)));
                            }
                        }
                    }
                    tickLastIngerdientCheck = Find.TickManager.TicksGame;
                }
                return IngredientsListCached;
            }
        }
        public List<ThingDef> IngredientsListCached;
        private int tickLastIngerdientCheck;

        public override void CompTick()
        {
            base.CompTick();
            if (isFiltering)
            {
                ticksTillFiltered--;
                if (ticksTillFiltered <= 0)
                {
                    FinishFilter();
                }
            }
        }

        public void StartFilter(ThingDef ingredientDef)
        {
            PipeNet pipeNet;
            if (!PowerTraderComp.PowerOn || ResourceComp == null || (pipeNet = ResourceComp.PipeNet) == null)
            {
                return;
            }
            IngredientDef = ingredientDef;
            float percentTaken = filterCost;
            for (int i = 0; i < pipeNet.storages.Count; i++)
            {
                CompRegisterIngredients compRegisterIngredients = pipeNet.storages[i].parent.TryGetComp<CompRegisterIngredients>();
                if (compRegisterIngredients != null)
                {
                    compRegisterIngredients.ingredients.RemoveAll((ThingDef td) => td == IngredientDef);
                }
            }
            amountFiltered = pipeNet.Stored * percentTaken;
            pipeNet.DrawAmongStorage(amountFiltered, pipeNet.storages);
            ticksTotalFilter = Mathf.FloorToInt(Props.ticksPerNutrient * amountFiltered);
            ticksTillFiltered = ticksTotalFilter;
        }

        public void FinishFilter()
        {
            amountFiltered = 0;
            ticksTillFiltered = 0;
        }

        public void StopFilter()
        {
            PipeNet pipeNet;
            if (!PowerTraderComp.PowerOn || ResourceComp == null || (pipeNet = ResourceComp.PipeNet) == null)
            {
                return;
            }
            for (int i = 0; i < pipeNet.storages.Count; i++)
            {
                CompRegisterIngredients compRegisterIngredients = pipeNet.storages[i].parent.TryGetComp<CompRegisterIngredients>();
                if (compRegisterIngredients != null)
                {
                    compRegisterIngredients.RegisterIngredient(IngredientDef);
                }
            }
            pipeNet.DistributeAmongStorage(amountLeft, out _);
            amountFiltered = 0;
            ticksTillFiltered = 0;
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (Gizmo gizmo in base.CompGetGizmosExtra())
            {
                yield return gizmo;
            }
            if (isFiltering)
            {
                yield return new Command_Action
                {
                    action = delegate
                    {
                        StopFilter();
                    },
                    defaultLabel = "Cancel",
                    defaultDesc = $"Will cancel filtering process and will add left over nutrient paste back to storage."
                };
            }
            else
            {
                yield return new Command_Action
                {
                    action = delegate
                    {
                        List<FloatMenuOption> floatMenuOptions = new List<FloatMenuOption>();
                        Log.Message($"IngredientsList {IngredientsList.Count()}");
                        foreach (ThingDef ingredientDef in IngredientsList)
                        {
                            floatMenuOptions.Add(new FloatMenuOption(ingredientDef.LabelCap, delegate
                            {
                                StartFilter(ingredientDef);
                            }));
                        }
                        Find.WindowStack.Add(new FloatMenu(floatMenuOptions));
                    },
                    defaultLabel = "Filter",
                    defaultDesc = $"Initiate filter process to remove ingredient. Will consume {filterCost.ToStringPercent()} of stored nutrient paste."
                };
            }
        }

        public override void PostDestroy(DestroyMode mode, Map previousMap)
        {
            if (isFiltering && Props.destroyOption != null)
            {
                IntVec3 position = parent.Position;
                int num = (int)(amountLeft / (float)Props.destroyOption.ratio);
                for (int i = 0; i < num; i++)
                {
                    FilthMaker.TryMakeFilth(CellFinder.StandableCellNear(position, previousMap, Props.destroyOption.maxRadius), previousMap, Props.destroyOption.filth);
                }
            }
            base.PostDestroy(mode, previousMap);
        }

        public override string CompInspectStringExtra()
        {
            List<string> inspectStrings = new List<string>();
            if (isFiltering)
            {
                inspectStrings.Add($"Amount of {IngredientDef.label} filtered: {amountLeft}/{amountFiltered}");
                inspectStrings.Add($"Finish in {ticksTillFiltered.ToStringTicksToPeriod()}");
            }
            else
            {
                inspectStrings.Add($"Filter will cost {filterCost.ToStringPercent()}");
            }
            return string.Join("\n", inspectStrings);
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Defs.Look(ref IngredientDef, "IngredientDef");
            Scribe_Values.Look(ref amountFiltered, "amountFiltered");
            Scribe_Values.Look(ref ticksTotalFilter, "ticksTotalFilter");
            Scribe_Values.Look(ref ticksTillFiltered, "ticksTillFiltered");
        }
    }
}
