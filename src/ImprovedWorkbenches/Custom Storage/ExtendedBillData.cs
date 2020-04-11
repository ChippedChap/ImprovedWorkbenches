using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using HarmonyLib;

namespace ImprovedWorkbenches
{
    public class ExtendedBillData : IExposable
    {
        public bool CountAway;
        public string Name;
        public ThingFilter ProductAdditionalFilter;
        public bool ForbidIncompleteStacks;

        // Def Names -> Stack Reference
        private Dictionary<string, Thing> IncompleteStacks = new Dictionary<string, Thing>();
        private List<string> stackDefNames = new List<string>();
        private List<Thing> stacks = new List<Thing>();

        public ExtendedBillData()
        {
        }

        public void CloneFrom(ExtendedBillData other, bool cloneName)
        {
            CountAway = other.CountAway;
            ProductAdditionalFilter = new ThingFilter();
            ForbidIncompleteStacks = other.ForbidIncompleteStacks;
            if (other.ProductAdditionalFilter != null)
                ProductAdditionalFilter.CopyAllowancesFrom(other.ProductAdditionalFilter);

            if (cloneName)
                Name = other.Name;
        }

        public void HandleForbid(Thing t, Bill_Production p)
        {
            if (t.def.stackLimit == 1) return;
            if (IncompleteStacks.ContainsKey(t.def.defName))
            {
                if (t != IncompleteStacks[t.def.defName])
                {
                    IncompleteStacks[t.def.defName].SetForbidden(false);
                    IncompleteStacks[t.def.defName] = t;
                }
            }
            else
            {
                IncompleteStacks.Add(t.def.defName, t);
            }
            t.SetForbidden(t.stackCount < t.def.stackLimit && (p.repeatMode != BillRepeatModeDefOf.RepeatCount || p.repeatCount != 0));
        }

        public void UnforbidAndClearStacks()
        {
            foreach(Thing stack in IncompleteStacks.Values)
                if (stack != null) stack.SetForbidden(false);
            IncompleteStacks.Clear();
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref CountAway, "countAway", false);
            Scribe_Values.Look(ref Name, "name", null);
            Scribe_Values.Look(ref ForbidIncompleteStacks, "autoForbid");
            Scribe_Deep.Look(ref ProductAdditionalFilter, "productFilter");
            Scribe_Collections.Look(ref IncompleteStacks, "incompleteStacks", LookMode.Value, LookMode.Reference, ref stackDefNames, ref stacks);
        }
    }


    [HarmonyPatch(typeof(Bill_Production), nameof(Bill_Production.ExposeData))]
    public static class ExtendedBillData_ExposeData
    {
        public static void Postfix(Bill_Production __instance)
        {
            var storage = HugsLib.Utils.UtilityWorldObjectManager.GetUtilityWorldObject<ExtendedBillDataStorage>();
            storage.GetOrCreateExtendedDataFor(__instance).ExposeData();
        }
    }


    [HarmonyPatch(typeof(Bill_Production), nameof(Bill_Production.Clone))]
    public static class ExtendedBillData_Clone
    {
        public static void Postfix(Bill_Production __instance, Bill __result)
        {
            if (__result is Bill_Production billProduction)
            {
                var storage = Main.Instance.GetExtendedBillDataStorage();
                var sourceExtendedData = storage.GetExtendedDataFor(__instance);
                var destinationExtendedData = storage.GetOrCreateExtendedDataFor(billProduction);

                destinationExtendedData?.CloneFrom(sourceExtendedData, true);
            }
        }
    }

}