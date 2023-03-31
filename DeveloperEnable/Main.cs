using EasyFeedback.APIs;
using HarmonyLib;
using Rewired;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityModManagerNet;
using Visor;

namespace DeveloperEnable
{
#if DEBUG
	[EnableReloading]
#endif
    public static class Main
    {
        static Harmony harmony;

        public static bool Load(UnityModManager.ModEntry entry)
        {
            harmony = new Harmony(entry.Info.Id);

            entry.OnToggle = OnToggle;
#if DEBUG
			entry.OnUnload = OnUnload;
#endif

            return true;
        }

        static bool OnToggle(UnityModManager.ModEntry entry, bool active)
        {
            if (active)
            {
                harmony.PatchAll(Assembly.GetExecutingAssembly());
            }
            else
            {
                harmony.UnpatchAll(entry.Info.Id);
            }

            return true;
        }

#if DEBUG
		static bool OnUnload(UnityModManager.ModEntry entry) {
			return true;
		}
#endif

        // Console
        [HarmonyPatch(typeof(WorldController), "ShouldOpenConsole")]
        class ConsolePatch1
        {
            public static bool Prefix(Player ____input, ref bool __result)
            {

                __result = ____input.GetButtonDown("CONSOLE_Open");
                return false;
            }
        }

        [HarmonyPatch(typeof(WorldController), "ShouldCloseConsole")]
        class ConsolePatch2
        {
            public static bool Prefix(Player ____input, ref bool __result)
            {

                __result = ____input.GetButtonDown("CONSOLE_Close");
                return false;
            }
        }

        // Developer Nodes
        [HarmonyPatch(typeof(ComponentLibraryControllerAgent), "ValidateComponents")]
        class DeveloperPatch1
        {
            public static bool Prefix(System.Collections.Generic.List<int> ____validIds, AgentProperty ____componentIdsProperty, ref bool __result)
            {

                ____validIds.Clear();
                float[] array = ____componentIdsProperty.GetValueNumberArray().array;
                for (int i = 0; i < array.Length; i++)
                {
                    int num = Mathf.FloorToInt(array[i]);
                    AgentGestalt agentGestalt;
                    if (!Holder.componentGestalts.TryGetValue((AgentGestaltEnum)num, out agentGestalt) || agentGestalt.componentHidden || agentGestalt.developersOnly)
                    {
                        __result = false;
                        return false;
                    }
                    ____validIds.Add(num);
                }

                __result = true;
                return false;
            }
        }
        [HarmonyPatch(typeof(ComponentLibrary), "Populate")]
        class DeveloperPatch2
        {
            public static bool Prefix(ComponentLibrary __instance, ComponentCategoryButton ____selectedCategoryButton)
            {

                if (__instance.searchField.text.Length != 1)
                {
                    Controllers.visorUIController.DeactivateAllComponentLibraryItems();
                    string partialKey = (__instance.searchField.text.Length > 1) ? __instance.searchField.text.ToUpperInvariant() : "";
                    List<List<AgentGestalt>> list = Holder.sortedComponentGestaltsByKeyword.PartialMatch(partialKey);
                    List<AgentGestalt> list2 = new List<AgentGestalt>();
                    foreach (List<AgentGestalt> second in list)
                    {
                        list2 = list2.Union(second).ToList<AgentGestalt>();
                    }
                    list2.Sort((AgentGestalt a, AgentGestalt b) => a.displayName.CompareTo(b.displayName));
                    int num = 0;
                    foreach (AgentGestalt agentGestalt in list2)
                    {
                        if ((agentGestalt.componentCategory == ____selectedCategoryButton.category || ____selectedCategoryButton.category == AgentGestalt.ComponentCategories.All) && agentGestalt.componentPrefab != null && !agentGestalt.componentHidden && Controllers.worldController.CanPlayerSpawnComponent((int)agentGestalt.id))
                        {
                            Controllers.visorUIController.ActivateComponentLibraryItem(agentGestalt.id, __instance.content).componentLibrary = __instance;
                            num++;
                        }
                    }
                    __instance.scrollRect.verticalNormalizedPosition = 1f;
                    __instance.message.SetActive(num == 0);
                }
                return false;
            }
        }
        [HarmonyPatch(typeof(ProcessorUINodeLibrary), "ShouldShowNode")]
        class DeveloperPatch3
        {
            public static bool Prefix(ref bool __result)
            {
                __result = true;
                return false;
            }
        }
    }
}
