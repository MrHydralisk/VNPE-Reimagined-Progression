using PipeSystem;
using RimWorld;
using System;
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

        public CompResource ResourceCompTarget => resourceCompTarget ?? (resourceCompTarget = parent.GetComps<CompResource>().FirstOrDefault((CompResource cr) => cr.Props.pipeNet == Props.pipeNetTarget));
        private CompResource resourceCompTarget;

        private List<ThingDef> IngredientsList = new List<ThingDef>();
        protected List<Thing> ProducedThings = new List<Thing>();

        public BillRepeatModeDef repeatMode = BillRepeatModeDefOf.Forever;
        public int targetCount = 0;

        public int tickNextProduction;
        public int containedItems => ProducedThings.Sum((Thing t) => t.stackCount);
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
        public bool isEnabledUnloadingPipe = true;

        public override void PostPostMake()
        {
            base.PostPostMake();
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
            int containedAmount = containedItems;
            bool isContainedItems = containedAmount > 0;
            isRequireUnloading = false;
            if (containedAmount >= MaxCapacity)
            {
                isRequireUnloading = true;
                return;
            }
            if (repeatMode == BillRepeatModeDefOf.RepeatCount)
            {
                if (targetCount <= 0)
                {
                    isRequireUnloading = isContainedItems;
                    return;
                }
            }
            else if (repeatMode == BillRepeatModeDefOf.TargetCount)
            {
                if (CountProducts() >= targetCount)
                {
                    isRequireUnloading = isContainedItems;
                    return;
                }
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
            if (Props.pipeNetTarget != null && isEnabledUnloadingPipe && ResourceCompTarget?.PipeNet is PipeNet pipeNet && (pipeNet?.AvailableCapacity ?? -1f) >= (float)Props.ItemProducedAmount)
            {
                pipeNet.DistributeAmongStorage(Props.ItemProducedAmount, out var _);
            }
            else
            {
                Thing thingProduced = ThingMaker.MakeThing(Props.ItemProducedDef);
                thingProduced.stackCount = Props.ItemProducedAmount;
                CompIngredients compIngredients = thingProduced.TryGetComp<CompIngredients>();
                if (compIngredients != null)
                {
                    for (int j = 0; j < IngredientsList.Count; j++)
                    {
                        compIngredients.RegisterIngredient(IngredientsList[j]);
                    }
                }
                bool isAbsorb = false;
                for (int i = ProducedThings.Count - 1; i >= 0; i--)
                {
                    if (ProducedThings[i].TryAbsorbStack(thingProduced, false))
                    {
                        isAbsorb = true;
                        break;
                    }
                }
                if (!isAbsorb)
                {
                    ProducedThings.Add(thingProduced);
                }
            }
            if (repeatMode == BillRepeatModeDefOf.RepeatCount)
            {
                targetCount -= Props.ItemProducedAmount;
            }
            isProcessing = false;
        }

        public void ExtractProducedItems()
        {
            while (ProducedThings.Count > 0)
            {
                Thing thing = ProducedThings.PopFront();
                GenPlace.TryPlaceThing(thing, parent.Position, parent.Map, ThingPlaceMode.Near);
            }
        }

        public int CountProducts()
        {
            int amount = parent.Map.resourceCounter.GetCount(Props.ItemProducedDef) + containedItems;
            if (Props.pipeNetTarget != null && isEnabledUnloadingPipe && ResourceCompTarget?.PipeNet is PipeNet pipeNet)
            {
                amount += Mathf.RoundToInt(pipeNet.Stored);
            }
            return amount;
        }

        public string RepeatInfoText(int current, int total, BillRepeatModeDef billRepeatModeDef)
        {
            if (billRepeatModeDef == BillRepeatModeDefOf.Forever)
            {
                return "Forever".Translate();
            }
            if (billRepeatModeDef == BillRepeatModeDefOf.RepeatCount)
            {
                return total + "x";
            }
            if (billRepeatModeDef == BillRepeatModeDefOf.TargetCount)
            {
                return current + "/" + total;
            }
            return "---";
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
                    }, MaxCapacity));
                },
                defaultLabel = "VNPEReimaginedProgression.Processor.Gizmo.MaximumStored.Label".Translate(),
                defaultDesc = "VNPEReimaginedProgression.Processor.Gizmo.MaximumStored.Desc".Translate(),
                icon = ContentFinder<Texture2D>.Get("UI/Commands/SetTargetFuelLevel"),
                Order = 30
            };
            yield return new Command_Action
            {
                action = delegate
                {
                    Find.WindowStack.Add(new Dialog_NutrientPasteProcessorSlider("VNPEReimaginedProgression.Processor.Gizmo.ProductionBill.Slider".Translate(Props.ItemProducedDef.label), this));
                },
                defaultLabel = "VNPEReimaginedProgression.Processor.Gizmo.ProductionBill.Label".Translate(),
                defaultDesc = "VNPEReimaginedProgression.Processor.Gizmo.ProductionBill.Desc".Translate(RepeatInfoText(CountProducts(), targetCount, repeatMode)),
                icon = ContentFinder<Texture2D>.Get("UI/Commands/LaunchReport"),
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
            if (Props.pipeNetTarget != null && ResourceCompTarget?.PipeNet != null)
            {
                Command_Toggle command_TogglePipe = new Command_Toggle();
                command_TogglePipe.defaultLabel = "VNPEReimaginedProgression.Processor.Gizmo.UnloadingPipe.Label".Translate(Props.pipeNetTarget.resource.name);
                command_TogglePipe.defaultDesc = "VNPEReimaginedProgression.Processor.Gizmo.UnloadingPipe.Desc".Translate(Props.pipeNetTarget.resource.name);
                command_TogglePipe.isActive = () => isEnabledUnloadingPipe;
                command_TogglePipe.toggleAction = delegate
                {
                    isEnabledUnloadingPipe = !isEnabledUnloadingPipe;
                };
                command_TogglePipe.activateSound = SoundDefOf.Tick_Tiny;
                command_TogglePipe.icon = ContentFinder<Texture2D>.Get(Props.pipeNetTarget.resource.offTexPath);
                command_TogglePipe.hotKey = KeyBindingDefOf.Misc6;
                command_TogglePipe.Order = 30;
                yield return command_TogglePipe;
            }
            yield return new Command_Action
            {
                action = delegate
                {
                    ExtractProducedItems();
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
                ExtractProducedItems();
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
            inspectStrings.Add(RepeatInfoText(CountProducts(), targetCount, repeatMode));
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
            Scribe_Values.Look(ref isEnabledUnloadingPipe, "isEnabledUnloadingPipe");
            Scribe_Values.Look(ref targetCount, "targetCount", 0);
            Scribe_Defs.Look(ref repeatMode, "repeatMode");
            if (Scribe.mode == LoadSaveMode.PostLoadInit && !(new List<BillRepeatModeDef> { BillRepeatModeDefOf.Forever, BillRepeatModeDefOf.RepeatCount, BillRepeatModeDefOf.TargetCount }).Contains(repeatMode)) 
            {
                repeatMode = BillRepeatModeDefOf.Forever;
            }
        }
    }
}
