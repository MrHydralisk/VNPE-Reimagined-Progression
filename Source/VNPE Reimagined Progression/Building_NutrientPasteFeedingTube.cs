using PipeSystem;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using VNPE;

namespace VNPEReimaginedProgression
{
    public class Building_NutrientPasteFeedingTube : Building
    {
        public CompFacility facilityComp;

        public CompPowerTrader powerComp;

        public CompResource resourceComp;

        public override Color DrawColor => (base.Spawned && base.Position.IsInPrisonCell(base.Map)) ? Building_Bed.SheetColorForPrisoner : base.DrawColor;

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            facilityComp = GetComp<CompFacility>();
            resourceComp = GetComp<CompResource>();
            powerComp = GetComp<CompPowerTrader>();
        }

        public override void TickRare()
        {
            PipeNet pipeNet = resourceComp.PipeNet;
            if (!powerComp.PowerOn || pipeNet.Stored == 0f)
            {
                return;
            }
            IntVec3 position = base.Position;
            List<Thing> linkedBuildings = facilityComp.LinkedBuildings;
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
                    IntVec3 bottom = new IntVec3();
                    switch (thing.Rotation.AsInt)
                    {

                        case 2:
                            {
                                bottom = pawn.Position + IntVec3.South;
                                break;
                            }
                        case 0:
                            {
                                bottom = pawn.Position + IntVec3.North;
                                break;
                            }
                        case 3:
                            {
                                bottom = pawn.Position + IntVec3.West;
                                break;
                            }
                        case 1:
                            {
                                bottom = pawn.Position + IntVec3.East;
                                break;
                            }
                    };
                    if (!(pawn.Position == position || bottom == position) || !((double)pawn.needs.food.CurLevelPercentage <= 0.4))
                    {
                        continue;
                    }
                    pipeNet.DrawAmongStorage(1f, pipeNet.storages);
                    Thing thing2 = ThingMaker.MakeThing(ThingDefOf.MealNutrientPaste);
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
            base.TickRare();
        }
    }
}
