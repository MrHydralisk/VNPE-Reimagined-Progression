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
    public class CompNutrientPasteProcessor : ThingComp
    {
        public CompProperties_NutrientPasteProcessor Props => (CompProperties_NutrientPasteProcessor)props;

        public CompPowerTrader PowerTraderComp => powerTraderComp ?? (powerTraderComp = parent.GetComp<CompPowerTrader>());
        private CompPowerTrader powerTraderComp;

        public CompResource ResourceComp => resourceComp ?? (resourceComp = parent.GetComp<CompResource>());
        private CompResource resourceComp;

        private List<ThingDef> IngredientsList = new List<ThingDef>();
        protected List<Thing> ProducedThings = new List<Thing>();

        public int tickNextProduction;
        public int containedItems => ProducedThings.Count();
        protected int maxCapacity;
        public int MaxCapacity
        {
            get
            {
                return maxCapacity;
            }
            set
            {
                maxCapacity = Mathf.Clamp(value, 1, Props.MaxCapacity);
            }
        }

        public bool isProcessing;
        public bool IsRequireUnloading => isEnabledUnloading && isRequireUnloading;
        public bool isRequireUnloading;
        public bool isEnabledUnloading = true;

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            MaxCapacity = Props.MaxCapacity;
        }

        public override void CompTick()
        {
            base.CompTick();
            if (isProcessing)
            {
                if (Find.TickManager.TicksGame >= tickNextProduction)
                {
                    Produce();
                    TryStartProduction();
                }
            }
            else if (parent.IsHashIntervalTick(250))
            {
                TryStartProduction();
            }
        }

        public void TryStartProduction()
        {
            PipeNet pipeNet;
            if (!PowerTraderComp.PowerOn || ResourceComp == null || (pipeNet = ResourceComp.PipeNet) == null || pipeNet.Stored < Props.NutrientPasteCost)
            {
                return;
            }
            isRequireUnloading = false;
            if (containedItems >= MaxCapacity)
            {
                isRequireUnloading = true;
                return;
            }
            pipeNet.DrawAmongStorage(Props.NutrientPasteCost, pipeNet.storages);
            tickNextProduction = Find.TickManager.TicksGame + Props.ticksPerProduction;
            IngredientsList = new List<ThingDef>();
            for (int i = 0; i < pipeNet.storages.Count; i++)
            {
                CompRegisterIngredients compRegisterIngredients = pipeNet.storages[i].parent.TryGetComp<CompRegisterIngredients>();
                if (compRegisterIngredients != null)
                {
                    IngredientsList.AddRange(compRegisterIngredients.ingredients.Where((ThingDef td1) => !IngredientsList.Any((ThingDef td2) => td2 == td1)));
                }
            }
            isProcessing = true;
        }

        public void Produce()
        {
            parent.def.building.soundDispense.PlayOneShot(new TargetInfo(parent.Position, parent.Map));
            for (int i = 0; i < Props.ItemProducedAmount; i++)
            {
                Thing thingProduced = ThingMaker.MakeThing(Props.ItemProducedDef);
                CompIngredients compIngredients = thingProduced.TryGetComp<CompIngredients>();
                if (compIngredients != null)
                {
                    for (int j = 0; j < IngredientsList.Count; j++)
                    {
                        compIngredients.RegisterIngredient(IngredientsList[j]);
                    }
                }
                ProducedThings.Add(thingProduced);
            }
            isProcessing = false;
        }

        public void ExtractProducedItems(int amount)
        {
            int takeAmount = Mathf.Clamp(amount, 0, containedItems);
            while (takeAmount > 0)
            {
                Thing thing = ProducedThings.PopFront();
                GenPlace.TryPlaceThing(thing, parent.Position, parent.Map, ThingPlaceMode.Near);
                takeAmount--;
            }
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (Gizmo gizmo in base.CompGetGizmosExtra())
            {
                yield return gizmo;
            }
            yield return new Command_Action
            {
                action = delegate
                {
                    Find.WindowStack.Add(new Dialog_Slider("VNPEReimaginedProgression.Processor.Gizmo.MaximumStored.Slider".Translate(), 1, Props.MaxCapacity, delegate (int x)
                    {
                        MaxCapacity = x;
                    }));
                },
                defaultLabel = "VNPEReimaginedProgression.Processor.Gizmo.MaximumStored.Label".Translate(),
                defaultDesc = "VNPEReimaginedProgression.Processor.Gizmo.MaximumStored.Desc".Translate(),
                icon = ContentFinder<Texture2D>.Get("UI/Commands/SetTargetFuelLevel"),
                Order = 30
            };
            Command_Toggle command_Toggle = new Command_Toggle();
            command_Toggle.defaultLabel = "VNPEReimaginedProgression.Processor.Gizmo.Unloading.Label".Translate();
            command_Toggle.defaultDesc = "VNPEReimaginedProgression.Processor.Gizmo.Unloading.Desc".Translate();
            command_Toggle.isActive = () => isEnabledUnloading;
            command_Toggle.toggleAction = delegate
            {
                isEnabledUnloading = !isEnabledUnloading;
            };
            command_Toggle.activateSound = SoundDefOf.Tick_Tiny;
            command_Toggle.icon = ContentFinder<Texture2D>.Get("UI/Commands/UnloadingProcessed");
            command_Toggle.hotKey = KeyBindingDefOf.Misc6;
            command_Toggle.Order = 30;
            yield return command_Toggle;
            yield return new Command_Action
            {
                action = delegate
                {
                    ExtractProducedItems(containedItems);
                },
                defaultLabel = "VNPEReimaginedProgression.Processor.Gizmo.Eject.Label".Translate(),
                defaultDesc = "VNPEReimaginedProgression.Processor.Gizmo.Eject.Desc".Translate(),
                icon = ContentFinder<Texture2D>.Get("UI/Commands/EjectProcessed"),
                Disabled = containedItems <= 0,
                disabledReason = "VNPEReimaginedProgression.Processor.Gizmo.Eject.Empty".Translate(),
                activateSound = SoundDefOf.Tick_Tiny,
                hotKey = KeyBindingDefOf.Misc5,
                Order = 30
            };
        }

        public override void PostDestroy(DestroyMode mode, Map previousMap)
        {
            if (mode != DestroyMode.Vanish)
            {
                ExtractProducedItems(containedItems);
            }
            base.PostDestroy(mode, previousMap);
        }

        public override string CompInspectStringExtra()
        {
            List<string> inspectStrings = new List<string>();
            string str = base.CompInspectStringExtra();
            if (!str.NullOrEmpty())
            {
                inspectStrings.Add(str);
            }
            inspectStrings.Add("VNPEReimaginedProgression.Processor.InspectString.Stored".Translate(Props.ItemProducedDef.LabelCap, containedItems, Props.MaxCapacity));
            if (isProcessing)
            {
                inspectStrings.Add("VNPEReimaginedProgression.Processor.InspectString.Processing".Translate(tickNextProduction - Find.TickManager.TicksGame));
            }
            else
            {
                if (isRequireUnloading)
                {
                    inspectStrings.Add("VNPEReimaginedProgression.Processor.InspectString.Full".Translate());
                }
                else
                {
                    inspectStrings.Add("VNPEReimaginedProgression.Processor.InspectString.Require".Translate(Props.NutrientPasteCost));
                }
            }
            return string.Join("\n", inspectStrings);
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Collections.Look(ref IngredientsList, "IngredientsList", LookMode.Def);
            if (Scribe.mode == LoadSaveMode.PostLoadInit && IngredientsList == null)
            {
                IngredientsList = new List<ThingDef>();
            }
            Scribe_Collections.Look(ref ProducedThings, "ProducedThings", LookMode.Deep);
            Scribe_Values.Look(ref maxCapacity, "maxCapacity", Props.MaxCapacity);
            Scribe_Values.Look(ref tickNextProduction, "tickNextProduction");
            Scribe_Values.Look(ref isProcessing, "isProcessing");
            Scribe_Values.Look(ref isRequireUnloading, "isRequireUnloading");
            Scribe_Values.Look(ref isEnabledUnloading, "isEnabledUnloading");
        }
    }
}
