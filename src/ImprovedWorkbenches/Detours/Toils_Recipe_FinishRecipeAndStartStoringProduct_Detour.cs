using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Verse;
using Verse.AI;

namespace ImprovedWorkbenches.Detours
{
    [HarmonyPatch]
    public class Toils_Recipe_FinishRecipeAndStartStoringProduct_Detour
    {
        private static Type innerClass;

        public static MethodBase TargetMethod()
        {
            // Intended to point to the delegate toils.initAction is set to in FinishRecipeAndStartStoringProduct.
            innerClass = AccessTools.Inner(typeof(Toils_Recipe), "<>c__DisplayClass3_0");
            return AccessTools.Method(innerClass, "<FinishRecipeAndStartStoringProduct>b__0");
        }

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> ins)
        {
            Type[] tryPlaceThingArgs = { typeof(Thing), typeof(IntVec3), typeof(Map), 
                typeof(ThingPlaceMode), typeof(Action<Thing, int>), typeof(Predicate<IntVec3>), typeof(Rot4)};
            int patchCount = 0;
            foreach(CodeInstruction i in ins)
            {
                if(patchCount < 2 && i.Calls(typeof(GenPlace).GetMethod("TryPlaceThing", tryPlaceThingArgs)))
                {
                    patchCount++;
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Ldfld, innerClass.GetField("toil"));
                    yield return new CodeInstruction(OpCodes.Call, typeof(Toils_Recipe_FinishRecipeAndStartStoringProduct_Detour).GetMethod("HandleThingForbid"));
                }
                else
                {
                    yield return i;
                }
            }
        }

        public static bool HandleThingForbid(Thing t, IntVec3 v, Map m, 
            ThingPlaceMode mode, Action<Thing, int> act, Predicate<IntVec3> cond, Rot4 rot, Toil toil)
        {
            bool result = GenPlace.TryPlaceThing(t, v, m, mode, out Thing lastThing, act, cond, rot);
            if (toil.actor.jobs.curJob.bill is Bill_Production production) 
            {
                var extendedBillData = Main.Instance.GetExtendedBillDataStorage().GetOrCreateExtendedDataFor(production);
                if(extendedBillData.ForbidIncompleteStacks) extendedBillData.HandleForbid(lastThing, production);
            }
            return result;
        }
    }
}
