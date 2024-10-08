using Verse;

namespace VNPEReimaginedProgression
{
    public static class VNPERPUtility
    {
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
