using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using Verse;
using VNPE;

namespace VNPEReimaginedProgression
{
    [StaticConstructorOnStartup]
    public class HarmonyPatches
    {
        private static readonly Type patchType;

        static HarmonyPatches()
        {
            patchType = typeof(HarmonyPatches);
            Harmony val = new Harmony("rimworld.mrhydralisk.VNPEReimaginedProgressionPatch");

            List<ThingDef> allDefsListForReading = DefDatabase<ThingDef>.AllDefsListForReading;
            List<ThingDef> VNPE_NPDs = allDefsListForReading.Where((ThingDef td) => td.defName.StartsWith("VNPE_NutrientPasteFeedingTube")).ToList();
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

            val.Patch(AccessTools.Method(typeof(Building_NutrientGrinder), "Tick"), transpiler: new HarmonyMethod(patchType, "BNG_Tick_Transpiler", (Type[])null));
            val.Patch(AccessTools.Method(typeof(Room), "Notify_RoomShapeChanged"), postfix: new HarmonyMethod(patchType, "Room_Notify_RoomShapeChanged_Postfix", (Type[])null));
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
                List<Thing> list1 = map.listerThings.AllThings.Where((Thing t) => t.def.defName.StartsWith("VNPE_NutrientPasteFeedingTube")).ToList();
                for (int k = 0; k < list1.Count; k++)
                {
                    list1[k].Notify_ColorChanged();
                }
            }
        }
    }
}
