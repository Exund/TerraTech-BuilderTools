using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using HarmonyLib;

namespace BuilderTools
{
    internal static class Patches
    {
        private static class ManPointer_Patches
        {
            [HarmonyPatch(typeof(ManPointer), nameof(OnMouse))]
            private static class OnMouse
            {
                private static void Prefix()
                {
                    if (Input.GetMouseButton(0) && Input.GetKey(Main.config.BlockPickerKey) && ManPlayer.inst.PaletteUnlocked)
                    {
                        ManPointer.inst.ChangeBuildMode((ManPointer.BuildingMode)10);
                    }
                }
            }

            [HarmonyPatch(typeof(ManPointer), nameof(RemovePaintingBlock))]
            private static class RemovePaintingBlock
            {
                private static void Postfix()
                {
                    if (!ManPointer.inst.IsPaintingBlocked)
                    {
                        BlockLine.inst.ResetState();
                    }
                }
            }
        }

        private static class UIPaletteBlockSelect_Patches
        {
            [HarmonyPatch(typeof(UIPaletteBlockSelect), "BlockFilterFunction")]
            private static class BlockFilterFunction
            {
                private static void Postfix(ref BlockTypes blockType, ref bool __result)
                {
                    if (__result)
                    {
                        __result = PaletteTextFilter.BlockFilterFunction(blockType);
                    }
                }
            }

            [HarmonyPatch(typeof(UIPaletteBlockSelect), "OnPool")]
            private static class OnPool
            {
                private static void Postfix(ref UIPaletteBlockSelect __instance)
                {
                    PaletteTextFilter.InitUI(__instance);
                    BlockLine.InitUI(__instance);
                }
            }

            [HarmonyPatch(typeof(UIPaletteBlockSelect), "Collapse")]
            private static class Collapse
            {
                private static void Postfix(ref bool __result)
                {
                    PaletteTextFilter.OnPaletteCollapse(__result);
                }
            }

            [HarmonyPatch(typeof(UIPaletteBlockSelect), "Update")]
            private static class Update
            {
                private static readonly FieldInfo
                    m_Entries = AccessTools.Field(typeof(UITogglesController), "m_Entries"),
                    m_Toggle = AccessTools.Inner(typeof(UITogglesController), "ToggleEntry").GetField("m_Toggle");

                private static readonly int Alpha1 = (int)KeyCode.Alpha1;

                private static void Prefix(ref UIPaletteBlockSelect __instance)
                {
                    if (Main.config.kbdCategroryKeys
                        && __instance.IsExpanded
                        && PaletteTextFilter.PreventPause()
                        && Singleton.Manager<ManInput>.inst.GetCurrentUIInputMode() == UIInputMode.BlockBuilding)
                    {
                        var categoryToggles = (UICategoryToggles)BlockPicker.m_CategoryToggles.GetValue(__instance);

                        int selected = -1;

                        var max = Alpha1 + categoryToggles.NumToggles;
                        for (int i = Alpha1; i < max; i++)
                        {
                            if (Input.GetKeyDown((KeyCode)i))
                            {
                                selected = i - Alpha1;
                            }
                        }

                        if (selected >= 0)
                        {
                            var controller = BlockPicker.m_Controller.GetValue(categoryToggles);
                            var entries = (IList)m_Entries.GetValue(controller);
                            var toggle = (ToggleWrapper)m_Toggle.GetValue(entries[selected]);
                            categoryToggles.GetAllToggle().isOn = false;
                            categoryToggles.ToggleAllOff();

                            toggle.InvokeToggleHandler(true, false);
                        }
                    }
                }
            }

            [HarmonyPatch(typeof(UIPaletteBlockSelect), "OnSwitchToGrabButton")]
            private static class OnSwitchToGrabButton
            {
                private static void Postfix()
                {
                    BlockLine.inst.SetLineMode(false);
                }
            }
        }

        [HarmonyPatch(typeof(ManPauseGame), "TogglePauseMenu")]
        private static class ManPauseGame_TogglePauseMenu
        {
            private static bool Prefix()
            {
                return PaletteTextFilter.PreventPause();
            }
        }

        [HarmonyPatch(typeof(ManLooseBlocks), "RequestAttachBlock")]
        private static class ManLooseBlocks_RequestAttachBlock
        {
            private static void Postfix(Tank tech, TankBlock block, IntVector3 pos, OrthoRotation rot)
            {
                BlockLine.inst.OnPlacement(tech, block, pos, rot);
            }
        }
    }
}
