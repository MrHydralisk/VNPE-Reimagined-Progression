using PipeSystem;
using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace VNPEReimaginedProgression
{
    public class Building_NutrientPasteHungerSuppressor : Building
    {
        private readonly List<Cable> cables = new List<Cable>();

        private const int UpdateHediffInterval = 50;

        private static readonly float CableRotRange1 = 0f - Rand.Range(15f, 20f);

        private static readonly float CableRotRange2 = Rand.Range(10f, 15f);

        private Texture CableTexture => cableTexture ?? (cableTexture = ContentFinder<Texture2D>.Get("Things/Building/HungerSuppressor/NutrientPasteHungerSuppressorCable"));
        private Texture cableTexture;

        private CompPowerTrader PowerTraderComp => powerTraderComp ?? (powerTraderComp = GetComp<CompPowerTrader>());
        private CompPowerTrader powerTraderComp;

        private CompNoiseSource NoiseSourceComp => noiseSourceComp ?? (noiseSourceComp = GetComp<CompNoiseSource>());
        private CompNoiseSource noiseSourceComp;

        private CompResource ResourceComp => resourceComp ?? (resourceComp = GetComp<CompResource>());
        private CompResource resourceComp;

        private CompAttachPoints AttachPointsComp => attachPointsComp ?? (attachPointsComp = GetComp<CompAttachPoints>());
        private CompAttachPoints attachPointsComp;

        private CompCableConnection CableConnectionComp => cableConnectionComp ?? (cableConnectionComp = GetComp<CompCableConnection>());
        private CompCableConnection cableConnectionComp;

        private VNPERPDefModExtension ExtVNPERP => extVNPERP ?? (extVNPERP = def.GetModExtension<VNPERPDefModExtension>());
        private VNPERPDefModExtension extVNPERP;

        public override void Tick()
        {
            base.Tick();
            if (Spawned)
            {
                if (this.IsHashIntervalTick(UpdateHediffInterval) && (PowerTraderComp?.PowerOn ?? true))
                {
                    List<Pawn> allHumanlikeSpawned = base.Map.mapPawns.AllHumanlikeSpawned;
                    for (int i = 0; i < allHumanlikeSpawned.Count && cables.Count < ExtVNPERP.maxTargets; i++)
                    {
                        Pawn pawn = allHumanlikeSpawned[i];
                        if (pawn.RaceProps.Humanlike && base.Position.InHorDistOf(pawn.PositionHeld, NoiseSourceComp.Props.radius) && (double)pawn.needs.food.CurLevelPercentage <= 0.15 && !pawn.health.hediffSet.TryGetHediff(HediffDefOfLocal.Hediff_VNPERP_HungerSuppression, out Hediff __) && TryAddCable(pawn))
                        {
                            HediffComp_HungerSuppressionSeverityPerDay compHungerSuppression = pawn.health.GetOrAddHediff(HediffDefOfLocal.Hediff_VNPERP_HungerSuppression, pawn.health.hediffSet.GetBodyPartRecord(BodyPartDefOf.Torso)).TryGetComp<HediffComp_HungerSuppressionSeverityPerDay>();
                            compHungerSuppression.OnRemoved += RemoveCable;
                        }
                    }
                }
                for (int i = cables.Count - 1; i >= 0; i--)
                {
                    Cable cable = cables[i];
                    if (base.Position.InHorDistOf(cable.to.PositionHeld, NoiseSourceComp.Props.radius))
                    {
                        cable.Tick();
                    }
                    else
                    {
                        cables.Remove(cable);
                    }
                }
            }
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            base.DrawAt(drawLoc, flip);
            foreach (Cable cable in cables)
            {
                cable.Draw();
            }
        }

        public bool TryAddCable(Pawn pawn)
        {
            PipeNet pipeNet = ResourceComp.PipeNet;
            if ((PowerTraderComp?.PowerOn ?? true) && pipeNet.Stored >= ExtVNPERP.storageCost)
            {
                pipeNet.DrawAmongStorage(ExtVNPERP.storageCost, pipeNet.storages);
                AddCable(pawn);
                return true;
            }
            return false;
        }

        public void AddCable(Pawn pawn)
        {
            Vector3 worldPos = AttachPointsComp.points.GetWorldPos(AttachPointType.CableConnection0);
            MaterialRequest req = default(MaterialRequest);
            req.mainTex = CableTexture;
            req.shader = ShaderDatabase.Transparent;
            req.color = CableConnectionComp.Props.color;
            Material mat = MaterialPool.MatFrom(req);
            Cable cable = new Cable
            {
                mat = mat,
                from = worldPos,
                to = pawn,
                quat = Quaternion.identity
            };
            cable.Tick();
            cables.Add(cable);
        }

        public void RemoveCable(Pawn pawn)
        {
            cables.RemoveWhere((Cable c) => c.to == pawn);
        }

        private class Cable
        {
            public Mesh mesh;

            public Vector3 from;

            public Vector3 prevTo;

            public Thing to;

            public Vector3 pos;

            public Quaternion quat;

            public Material mat;

            public void Draw()
            {
                Graphics.DrawMesh(mesh, pos, quat, mat, 100);
            }

            public void Tick()
            {
                if (prevTo != to.DrawPos)
                {
                    prevTo = to.DrawPos;
                    Vector3 vector = from - prevTo;
                    Vector2 vector2 = new Vector2(vector.x, vector.z);
                    Vector2 vector3 = vector2 * 0.25f;
                    pos = prevTo;
                    Vector2 vector4 = Vector2.Perpendicular(vector2 * 0.2f);
                    bool num = Vector2.Dot(vector4.normalized, Vector2.up) < 0f;
                    if (num)
                    {
                        vector4 = -vector4;
                    }
                    float degrees = (num ? CableRotRange1 : CableRotRange2);
                    float degrees2 = (num ? CableRotRange2 : CableRotRange1);
                    Vector2[] array = LineMeshGenerator.CalculateEvenlySpacedPoints(new List<Vector2>
                    {
                        new Vector2(0f, 0f),
                        vector3 + vector4.RotatedBy(degrees),
                        vector3 + vector4.RotatedBy(degrees2),
                        vector2
                    }, 0.2f);
                    mesh = LineMeshGenerator.Generate(array, 0.05f);
                }
            }
        }
    }
}
