using HarmonyLib;
using PipeSystem;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using UnityEngine;
using Verse;
using Verse.Sound;
using VNPE;

namespace VNPEReimaginedProgression
{
    [StaticConstructorOnStartup]
    public class HarmonyPatches
    {
        private static readonly Type patchType;

        private static AccessTools.FieldRef<object, int> maxHeldThingStackSizeRef;

        static HarmonyPatches()
        {
            patchType = typeof(HarmonyPatches);
            Harmony val = new Harmony("rimworld.mrhydralisk.VNPEReimaginedProgressionPatch");

            List<ThingDef> allDefsListForReading = DefDatabase<ThingDef>.AllDefsListForReading;
            List<ThingDef> VNPE_NPDs = allDefsListForReading.Where((ThingDef td) => td.defName.StartsWith("VNPERP_NutrientPasteFeedingTube") || td.defName.StartsWith("VNPERP_NutrientPasteDripper")).ToList();
            foreach (ThingDef item in allDefsListForReading)
            {
                if (item.building != null && item.building.buildingTags.Contains("Bed") && !item.defName.Contains("Spot"))
                {
                    CompProperties_AffectedByFacilities compProperties = item.GetCompProperties<CompProperties_AffectedByFacilities>();
                    foreach (ThingDef VNPE_NPD in VNPE_NPDs)
                    {
                        if (compProperties != null && !compProperties.linkableFacilities.Contains(VNPE_NPD))
                        {
                            compProperties.linkableFacilities.Add(VNPE_NPD);
                        }
                    }
                }
            }
            foreach (ThingDef VNPE_NPD in VNPE_NPDs)
            {
                VNPE_NPD.GetCompProperties<CompProperties_Facility>().ResolveReferences(VNPE_NPD);
            }

            maxHeldThingStackSizeRef = AccessTools.FieldRefAccess<int>(typeof(CompConvertToThing), "maxHeldThingStackSize");

            val.Patch(AccessTools.Method(typeof(Building_NutrientGrinder), "Tick"), transpiler: new HarmonyMethod(patchType, "BNG_Tick_Transpiler"));

            val.Patch(AccessTools.Method(typeof(Room), "Notify_RoomShapeChanged"), postfix: new HarmonyMethod(patchType, "Room_Notify_RoomShapeChanged_Postfix"));

            val.Patch(AccessTools.Method(typeof(ThingListGroupHelper), "Includes"), postfix: new HarmonyMethod(patchType, "TLGH_Includes_Postfix"));
            val.Patch(AccessTools.Property(typeof(Building_NutrientPasteTap), "CanDispenseNowOverride").GetGetMethod(), prefix: new HarmonyMethod(patchType, "BNPT_CanDispenseNowOverride_Prefix"));
            val.Patch(AccessTools.Method(typeof(Building_NutrientPasteTap), "HasEnoughFeedstockInHoppers"), prefix: new HarmonyMethod(patchType, "BNPT_HasEnoughFeedstockInHoppers_Prefix"));
            val.Patch(AccessTools.Method(typeof(Building_NutrientPasteTap), "TryDispenseFoodOverride"), prefix: new HarmonyMethod(patchType, "BNPT_TryDispenseFoodOverride_Prefix"));
            val.Patch(AccessTools.Method(typeof(Building_NutrientPasteTap), "TryDropFood"), prefix: new HarmonyMethod(patchType, "BNPT_TryDropFood_Prefix"));
            val.Patch(AccessTools.Method(typeof(FoodUtility).GetNestedTypes(AccessTools.all).First((Type t) => t.Name.Contains("c__DisplayClass16_0")), "<BestFoodSourceOnMap>b__0"), transpiler: new HarmonyMethod(patchType, "FU_BestFoodSourceOnMap_foodValidator_Transpiler"));
            val.Patch(AccessTools.Property(typeof(Building_NutrientPasteDispenser), "DispensableDef").GetGetMethod(), postfix: new HarmonyMethod(patchType, "BNPD_DispensableDef_Postfix"));

            val.Patch(AccessTools.Method(typeof(Building_Dripper), "TickRare"), prefix: new HarmonyMethod(patchType, "BD_TickRare_Prefix", (Type[])null));

            val.Patch(AccessTools.Method(typeof(PipeNet), "DistributeAmongConverters", (Type[])null, (Type[])null), transpiler: new HarmonyMethod(patchType, "PN_DistributeAmongConverters_Transpiler"));
        }

        public static IEnumerable<CodeInstruction> BNG_Tick_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            int targetIndex = -1;
            List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
            targetIndex = codes.FindIndex(ins => ins.Calls(AccessTools.Method(typeof(Building_NutrientGrinder), "TryProducePaste")));
            if (targetIndex > -1)
            {
                List<CodeInstruction> instructionsToInsert = new List<CodeInstruction>();
                instructionsToInsert.Add(new CodeInstruction(OpCodes.Ldarg_0));
                instructionsToInsert.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(HarmonyPatches), "BNG_Tick_AdditionalPaste")));
                codes.InsertRange(targetIndex + 2, instructionsToInsert);
            }
            return codes.AsEnumerable();
        }

        public static void BNG_Tick_AdditionalPaste(Building_NutrientGrinder __instance)
        {
            VNPERPDefModExtension VNPERP = __instance.def.GetModExtension<VNPERPDefModExtension>();
            if (VNPERP != null && VNPERP.AdditionalGrind > 0)
            {
                for (int i = 0; i < VNPERP.AdditionalGrind; i++)
                {
                    AccessTools.Method(typeof(Building_NutrientGrinder), "TryProducePaste").Invoke(__instance, Array.Empty<object>());
                }
            }
        }

        public static void Room_Notify_RoomShapeChanged_Postfix(Room __instance)
        {
            if (!__instance.Dereferenced)
            {
                Map map = __instance.Map;
                List<Thing> list1 = map.listerThings.AllThings.Where((Thing t) => t != null && (t.def.defName.StartsWith("VNPERP_NutrientPasteFeedingTube") || t.def.defName.StartsWith("VNPERP_NutrientPasteTap") || t.def.defName.StartsWith("VNPERP_NutrientPasteFeeder") || t.def.defName.StartsWith("VNPERP_NutrientPasteDripper"))).ToList();
                for (int k = 0; k < list1.Count; k++)
                {
                    list1[k].Notify_ColorChanged();
                }
            }
        }

        public static void TLGH_Includes_Postfix(ThingDef def, ThingRequestGroup group, ref bool __result)
        {
            if ((group == ThingRequestGroup.FoodSourceNotPlantOrTree || group == ThingRequestGroup.FoodSource) && def.defName.StartsWith("VNPERP_NutrientPasteTap"))
            {
                __result = true;
            }
        }

        public static bool BNPT_CanDispenseNowOverride_Prefix(ref bool __result, Building_NutrientPasteTap __instance)
        {
            VNPERPDefModExtension VNPERP = __instance.def.GetModExtension<VNPERPDefModExtension>();
            if (VNPERP != null)
            {
                if (__instance.powerComp.PowerOn)
                {
                    PipeNet pipeNet = __instance.resourceComp.PipeNet;
                    if (pipeNet != null)
                    {
                        __result = (pipeNet.Stored >= VNPERP.storageCost);
                        return false;
                    }
                }
                __result = false;
                return false;
            }
            return true;
        }

        public static bool BNPT_HasEnoughFeedstockInHoppers_Prefix(ref bool __result, Building_NutrientPasteTap __instance)
        {
            VNPERPDefModExtension VNPERP = __instance.def.GetModExtension<VNPERPDefModExtension>();
            if (VNPERP != null)
            {
                PipeNet pipeNet = __instance.resourceComp.PipeNet;
                __result = pipeNet != null && pipeNet.Stored >= VNPERP.storageCost;
                return false;
            }
            return true;
        }

        public static bool BNPT_TryDispenseFoodOverride_Prefix(Building_NutrientPasteTap __instance, ref Thing __result)
        {
            VNPERPDefModExtension VNPERP = __instance.def.GetModExtension<VNPERPDefModExtension>();
            if (VNPERP != null)
            {
                PipeNet pipeNet = __instance.resourceComp.PipeNet;
                __instance.def.building.soundDispense.PlayOneShot(new TargetInfo(__instance.Position, __instance.Map));
                pipeNet.DrawAmongStorage(VNPERP.storageCost, pipeNet.storages);
                Thing thing = ThingMaker.MakeThing(VNPERP.customMeal);
                CompIngredients compIngredients = thing.TryGetComp<CompIngredients>();
                if (compIngredients != null)
                {
                    for (int i = 0; i < pipeNet.storages.Count; i++)
                    {
                        ThingWithComps parent = pipeNet.storages[i].parent;
                        CompRegisterIngredients compRegisterIngredients = parent.TryGetComp<CompRegisterIngredients>();
                        if (compRegisterIngredients != null)
                        {
                            for (int j = 0; j < compRegisterIngredients.ingredients.Count; j++)
                            {
                                compIngredients.RegisterIngredient(compRegisterIngredients.ingredients[j]);
                            }
                        }
                    }
                }
                __result = thing;
                return false;
            }
            return true;
        }

        public static bool BNPT_TryDropFood_Prefix(int amount, Building_NutrientPasteTap __instance)
        {
            VNPERPDefModExtension VNPERP = __instance.def.GetModExtension<VNPERPDefModExtension>();
            if (VNPERP != null)
            {
                if (!__instance.powerComp.PowerOn || amount <= 0 || Find.TickManager.Paused)
                {
                    return false;
                }
                PipeNet pipeNet = __instance.resourceComp.PipeNet;
                float stored = pipeNet.Stored;
                if (stored < VNPERP.storageCost)
                {
                    return false;
                }
                Map map = __instance.Map;
                IntVec3 interactionCell = __instance.InteractionCell;
                int available = (int)(stored / VNPERP.storageCost);
                int num = Mathf.Clamp(amount, 0, available);
                float storageCost = (float)num * VNPERP.storageCost;
                if (storageCost > stored)
                {
                    storageCost = stored;
                }
                pipeNet.DrawAmongStorage(storageCost, pipeNet.storages);
                __instance.def.building.soundDispense.PlayOneShot(new TargetInfo(interactionCell, map));
                List<CompRegisterIngredients> list = new List<CompRegisterIngredients>();
                for (int i = 0; i < pipeNet.storages.Count; i++)
                {
                    CompRegisterIngredients compRegisterIngredients = pipeNet.storages[i].parent.TryGetComp<CompRegisterIngredients>();
                    if (compRegisterIngredients != null)
                    {
                        list.Add(compRegisterIngredients);
                    }
                }
                while (num > 0)
                {
                    Thing thing = ThingMaker.MakeThing(VNPERP.customMeal);
                    CompIngredients compIngredients = thing.TryGetComp<CompIngredients>();
                    if (compIngredients != null)
                    {
                        for (int j = 0; j < list.Count; j++)
                        {
                            CompRegisterIngredients compRegisterIngredients2 = list[j];
                            for (int k = 0; k < compRegisterIngredients2.ingredients.Count; k++)
                            {
                                compIngredients.RegisterIngredient(compRegisterIngredients2.ingredients[k]);
                            }
                        }
                    }
                    num -= (thing.stackCount = ((num > thing.def.stackLimit) ? thing.def.stackLimit : num));
                    GenPlace.TryPlaceThing(thing, interactionCell, map, ThingPlaceMode.Near);
                }
                return false;
            }
            return true;
        }

        public static IEnumerable<CodeInstruction> FU_BestFoodSourceOnMap_foodValidator_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
            for (int i = 0; i < codes.Count(); i++)
            {
                if (codes[i].ToString().Contains("MealNutrientPaste"))
                {
                    codes.Insert(i + 1, new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(VNPERPUtility), "MealNutrientPasteDef")));
                    codes.Insert(i, new CodeInstruction(OpCodes.Ldloc_1));
                    i += 2;
                }
            }
            return codes.AsEnumerable();
        }

        public static void BNPD_DispensableDef_Postfix(Building_NutrientPasteDispenser __instance, ref ThingDef __result)
        {
            __result = VNPERPUtility.MealNutrientPasteDef(__instance, __result);
        }

        public static bool BD_TickRare_Prefix(Building_Dripper __instance)
        {
            VNPERPDefModExtension VNPERP = __instance.def.GetModExtension<VNPERPDefModExtension>();
            if (VNPERP != null)
            {
                PipeNet pipeNet = __instance.resourceComp.PipeNet;
                if (!__instance.powerComp.PowerOn || pipeNet.Stored < VNPERP.storageCost)
                {
                    return false;
                }
                IntVec3 position = __instance.Position;
                List<Thing> linkedBuildings = __instance.facilityComp.LinkedBuildings;
                for (int i = 0; i < linkedBuildings.Count; i++)
                {
                    Thing thing = linkedBuildings[i];
                    if (!(thing is Building_Bed building_Bed))
                    {
                        continue;
                    }
                    List<Pawn> list = building_Bed.CurOccupants.ToList();
                    for (int j = 0; j < list.Count; j++)
                    {
                        Pawn pawn = list[j];
                        if (!pawn.Position.AdjacentToCardinal(position) || !((double)pawn.needs.food.CurLevelPercentage <= 0.4))
                        {
                            continue;
                        }
                        pipeNet.DrawAmongStorage(VNPERP.storageCost, pipeNet.storages);
                        Thing thing2 = ThingMaker.MakeThing(VNPERP.customMeal);
                        CompIngredients compIngredients = thing2.TryGetComp<CompIngredients>();
                        if (compIngredients != null)
                        {
                            for (int k = 0; k < pipeNet.storages.Count; k++)
                            {
                                ThingWithComps parent = pipeNet.storages[k].parent;
                                CompRegisterIngredients compRegisterIngredients = parent.TryGetComp<CompRegisterIngredients>();
                                if (compRegisterIngredients != null)
                                {
                                    for (int l = 0; l < compRegisterIngredients.ingredients.Count; l++)
                                    {
                                        compIngredients.RegisterIngredient(compRegisterIngredients.ingredients[l]);
                                    }
                                }
                            }
                        }
                        float num = thing2.Ingested(pawn, pawn.needs.food.NutritionWanted);
                        pawn.needs.food.CurLevel += num;
                        pawn.records.AddTo(RecordDefOf.NutritionEaten, num);
                    }
                }
                if (__instance.AllComps != null)
                {
                    int i = 0;
                    for (int count = __instance.AllComps.Count; i < count; i++)
                    {
                        __instance.AllComps[i].CompTickRare();
                    }
                }
                return false;
            }
            return true;
        }
        public static IEnumerable<CodeInstruction> PN_DistributeAmongConverters_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            int startIndex = -1;
            int endIndex = -1;
            List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
            for (int i = 0; i < codes.Count - 1; i++)
            {
                if (codes[i].IsStloc() && codes[i].ToString().Contains("10 (PipeSystem.CompConvertToThing)") && codes[i + 1].IsLdloc() && codes[i + 1].ToString().Contains("10 (PipeSystem.CompConvertToThing)"))
                {
                    startIndex = i;
                    for (int j = startIndex + 1; j < codes.Count; j++)
                    {
                        if (codes[j].opcode == OpCodes.Nop && codes[j + 1].IsLdloc() && codes[j + 1].ToString().Contains("9 (System.Int32"))
                        {
                            endIndex = j;
                            break;
                        }
                    }
                    if (endIndex > -1)
                    {
                        break;
                    }
                }
            }
            if (startIndex > -1 && endIndex > -1)
            {
                Label labelSkip = il.DefineLabel();
                codes[endIndex].labels.Add(labelSkip);
                List<CodeInstruction> instructionsToInsert = new List<CodeInstruction>();
                instructionsToInsert.Add(new CodeInstruction(OpCodes.Ldloc_S, 10));
                instructionsToInsert.Add(new CodeInstruction(OpCodes.Ldarga_S, 1));
                instructionsToInsert.Add(new CodeInstruction(OpCodes.Ldloca_S, 0));
                instructionsToInsert.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(HarmonyPatches), "DistributeAmongConvertersFloat")));
                instructionsToInsert.Add(new CodeInstruction(OpCodes.Brtrue_S, labelSkip));
                codes.InsertRange(startIndex + 1, instructionsToInsert);
            }
            return codes.AsEnumerable();
        }

        public static bool DistributeAmongConvertersFloat(CompConvertToThing compConvertToThing, ref float available, ref float num)
        {
            VNPERPDefModExtension VNPERP = compConvertToThing.parent.def.GetModExtension<VNPERPDefModExtension>();
            if (VNPERP == null)
            {
                return false;
            }
            if (compConvertToThing.CanOutputNow)
            {
                int amount = (int)(available / VNPERP.storageCost);
                int num3 = Mathf.Min(amount, compConvertToThing.MaxCanOutput);
                if (num3 <= 0)
                {
                    return false;
                }
                compConvertToThing.OutputResource(num3);
                float storageCost = (float)num3 * VNPERP.storageCost;
                num += storageCost;
                available -= storageCost;
            }
            return true;
        }
    }
}
