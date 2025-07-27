using System.Collections.Generic;
using Verse;

namespace VNPEReimaginedProgression
{
    public static class VNPERPUtility
    {
        public static List<ThingDef> Defs_Dripper_FeedingTube = new List<ThingDef>();
        public static List<ThingDef> Defs_Feeder = new List<ThingDef>();
        public static List<ThingDef> Defs_Tap = new List<ThingDef>();

        public static float DrawAmongStorage(Building building, float amount = 1f)
        {
            VNPERPDefModExtension ext = building.def.GetModExtension<VNPERPDefModExtension>();
            if (ext != null && ext.storageCost != 1f)
            {
                return ext.storageCost;
            }
            return amount;
        }

        public static ThingDef MealNutrientPasteDef(Building building, ThingDef meal)
        {
            VNPERPDefModExtension ext = building.def.GetModExtension<VNPERPDefModExtension>();
            if (ext != null && ext.customMeal != null)
            {
                return ext.customMeal;
            }
            return meal;
        }
    }
}
