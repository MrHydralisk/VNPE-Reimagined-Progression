using HarmonyLib;
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

            val.Patch(AccessTools.Method(typeof(Building_NutrientGrinder), "Tick"), transpiler: new HarmonyMethod(patchType, "BNG_Tick_Transpiler", (Type[])null));
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
            Log.Message($"VNPERP {VNPERP == null} {VNPERP?.AdditionalGrind}");
            if (VNPERP != null && VNPERP.AdditionalGrind > 0)
            {
                for (int i = 0; i < VNPERP.AdditionalGrind; i++)
                {
                    Log.Message((bool)AccessTools.Method(typeof(Building_NutrientGrinder), "TryProducePaste").Invoke(__instance, Array.Empty<object>()));
                }
            }
        }
    }
}
