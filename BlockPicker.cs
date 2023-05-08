using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace BuilderTools
{
    internal class BlockPicker : MonoBehaviour
    {
        internal static Type T_UIPaletteBlockSelect = typeof(UIPaletteBlockSelect);

        internal static readonly FieldInfo m_Grid = AccessTools.Field(T_UIPaletteBlockSelect, "m_Grid"),
            m_CategoryToggles = AccessTools.Field(T_UIPaletteBlockSelect, "m_CategoryToggles"),
            m_CorpToggles = AccessTools.Field(T_UIPaletteBlockSelect, "m_CorpToggles"),
            m_Controller = AccessTools.Field(typeof(UICorpToggles), "m_Controller");

        private void Update()
        {
            if (Input.GetMouseButtonDown(0) && Input.GetKey(Main.config.BlockPickerKey) && ManPlayer.inst.PaletteUnlocked)
            {
                UIPaletteBlockSelect palette = Singleton.Manager<ManHUD>.inst.GetHudElement(ManHUD.HUDElementType.BlockPalette) as UIPaletteBlockSelect;
                if (!palette.IsExpanded && Main.config.open_inventory)
                {
                    var blockMenuSelection = Singleton.Manager<ManHUD>.inst.GetHudElement(ManHUD.HUDElementType.BlockMenuSelection) as UIBlockMenuSelection;

                    blockMenuSelection.Hide(new UIBlockMenuSelection.Context()
                    {
                        targetMode = UIBlockMenuSelection.ModeMask.BlockPaletteAndTechs
                    });
                    blockMenuSelection.Show(new UIBlockMenuSelection.Context()
                    {
                        targetMode = UIBlockMenuSelection.ModeMask.BlockPalette
                    });

                    palette.Expand(new UIShopBlockSelect.ExpandContext()
                    {
                        expandReason = UIShopBlockSelect.ExpandReason.Button
                    });

                    try
                    {
                        Singleton.Manager<ManHUD>.inst.HideHudElement(ManHUD.HUDElementType.TechLoader);
                    }
                    catch
                    {
                    }
                }

                if (palette.IsExpanded)
                {
                    var temp_block = Singleton.Manager<ManPointer>.inst.targetVisible?.block;
                    if (temp_block)
                    {
                        var grid = m_Grid.GetValue(palette) as UIBlockSelectGrid;
                        grid.PreventSelection = false;

                        var catToggles = m_CategoryToggles.GetValue(palette) as UICategoryToggles;
                        var corpToggles = m_CorpToggles.GetValue(palette) as UICorpToggles;
                        var controller = m_Controller.GetValue(corpToggles) as UITogglesController;

                        if (Main.config.global_filters)
                        {
                            catToggles.ToggleAllOn();
                            corpToggles.ToggleAllOn();
                        }
                        else
                        {
                            catToggles.SetToggleSelected((int)temp_block.BlockCategory, true);
                            controller.SetToggleSelected((int)Singleton.Manager<ManSpawn>.inst.GetCorporation(temp_block.BlockType), true);
                        }

                        grid.Repopulate();


                        palette.TrySelectBlockType(temp_block.BlockType);
                    }
                }

                Singleton.Manager<ManPointer>.inst.ChangeBuildMode(ManPointer.BuildingMode.PaintBlock);
            }
        }
    }
}
