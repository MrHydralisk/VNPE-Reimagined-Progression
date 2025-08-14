using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace VNPEReimaginedProgression
{
    public class Dialog_NutrientPasteProcessorSlider : Window
    {
        public string textGetter;
        public BillRepeatModeDef repeatMode = BillRepeatModeDefOf.Forever;
        public int repeatCount;
        CompNutrientPasteProcessor compNutrientPasteProcessor;

        public override Vector2 InitialSize => new Vector2(300f, 106f);

        protected override float Margin => 10f;

        public Dialog_NutrientPasteProcessorSlider(string textGetter, CompNutrientPasteProcessor compNutrientPasteProcessor)
        {
            this.textGetter = textGetter;
            this.compNutrientPasteProcessor = compNutrientPasteProcessor;
            this.repeatCount = compNutrientPasteProcessor.targetCount;
            this.repeatMode = compNutrientPasteProcessor.repeatMode;
            forcePause = true;
            closeOnClickedOutside = true;
        }

        public override void DoWindowContents(Rect inRect)
        {
            Widgets.BeginGroup(inRect);
            TextAnchor textAnchor = Text.Anchor;
            Text.Anchor = TextAnchor.MiddleCenter;
            Rect baseRec = inRect.ContractedBy(2);
            Widgets.Label(new Rect(baseRec.x, baseRec.y, baseRec.width, 26f), textGetter);
            baseRec.y += 30f;
            WidgetRow widgetRowA = new WidgetRow(baseRec.xMax, baseRec.y, UIDirection.LeftThenUp);
            if (widgetRowA.ButtonIcon(TexButton.Plus))
            {
                if (repeatMode == BillRepeatModeDefOf.Forever)
                {
                    repeatMode = BillRepeatModeDefOf.RepeatCount;
                }
                else if (repeatMode == BillRepeatModeDefOf.TargetCount || repeatMode == BillRepeatModeDefOf.RepeatCount)
                {
                    int num = compNutrientPasteProcessor.Props.ItemProducedAmount * GenUI.CurrentAdjustmentMultiplier();
                    repeatCount = Mathf.Max(0, repeatCount + num);
                }
                SoundDefOf.DragSlider.PlayOneShotOnCamera();
            }
            Color color = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, 0.65f);
            widgetRowA.Label(compNutrientPasteProcessor.RepeatInfoText(compNutrientPasteProcessor.CountProducts(), repeatCount, repeatMode), 230);
            GUI.color = color;
            if (widgetRowA.ButtonIcon(TexButton.Minus))
            {
                if (repeatMode == BillRepeatModeDefOf.Forever)
                {
                    repeatMode = BillRepeatModeDefOf.RepeatCount;
                }
                else if (repeatMode == BillRepeatModeDefOf.TargetCount || repeatMode == BillRepeatModeDefOf.RepeatCount)
                {
                    int num = compNutrientPasteProcessor.Props.ItemProducedAmount * GenUI.CurrentAdjustmentMultiplier();
                    repeatCount = Mathf.Max(0, repeatCount - num);
                }
                SoundDefOf.DragSlider.PlayOneShotOnCamera();
            }
            baseRec.y += 30f;
            WidgetRow widgetRowB = new WidgetRow(baseRec.xMax, baseRec.y, UIDirection.LeftThenUp);
            if (widgetRowB.ButtonText("OK".Translate()))
            {
                Close();
                compNutrientPasteProcessor.targetCount = repeatCount;
                compNutrientPasteProcessor.repeatMode = repeatMode;
            }
            if (widgetRowB.ButtonText(repeatMode.LabelCap.Resolve().PadRight(20)))
            {
                List<FloatMenuOption> list = new List<FloatMenuOption>();
                list.Add(new FloatMenuOption(BillRepeatModeDefOf.RepeatCount.LabelCap, delegate
                {
                    repeatMode = BillRepeatModeDefOf.RepeatCount;
                }));
                list.Add(new FloatMenuOption(BillRepeatModeDefOf.TargetCount.LabelCap, delegate
                {
                    repeatMode = BillRepeatModeDefOf.TargetCount;
                }));
                list.Add(new FloatMenuOption(BillRepeatModeDefOf.Forever.LabelCap, delegate
                {
                    repeatMode = BillRepeatModeDefOf.Forever;
                }));
                Find.WindowStack.Add(new FloatMenu(list));
            }
            if (widgetRowB.ButtonText("CancelButton".Translate()))
            {
                Close();
            }
            Widgets.EndGroup();
            Text.Anchor = textAnchor;
        }
    }
}