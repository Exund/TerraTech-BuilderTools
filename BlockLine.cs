using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace BuilderTools
{
    public class BlockLine : Singleton.Manager<BlockLine>
    {
        private class AxisOrder
        {
            public IntVector3Interval.Axis a;
            public IntVector3Interval.Axis b;
            public IntVector3Interval.Axis c;

            public override string ToString()
            {
                return string.Join("", a, b, c);
            }
        }

        private static readonly AxisOrder[] axisConfigs =
        {
            new AxisOrder()
            {
                a = IntVector3Interval.Axis.X,
                b = IntVector3Interval.Axis.Y,
                c = IntVector3Interval.Axis.Z,
            },
            new AxisOrder()
            {
                a = IntVector3Interval.Axis.X,
                b = IntVector3Interval.Axis.Z,
                c = IntVector3Interval.Axis.Y,
            },
            new AxisOrder()
            {
                a = IntVector3Interval.Axis.Y,
                b = IntVector3Interval.Axis.X,
                c = IntVector3Interval.Axis.Z,
            },
            new AxisOrder()
            {
                a = IntVector3Interval.Axis.Y,
                b = IntVector3Interval.Axis.Z,
                c = IntVector3Interval.Axis.X,
            },
            new AxisOrder()
            {
                a = IntVector3Interval.Axis.Z,
                b = IntVector3Interval.Axis.X,
                c = IntVector3Interval.Axis.Y,
            },
            new AxisOrder()
            {
                a = IntVector3Interval.Axis.Z,
                b = IntVector3Interval.Axis.Y,
                c = IntVector3Interval.Axis.X,
            },
        };

        private static readonly string[] axisConfigsString = axisConfigs.Select(ac => ac.ToString()).ToArray();
        private static int selectedAxisConfig = 0;

        private static readonly string[] IterationTypes = Enum.GetNames(typeof(IntVector3Interval.IterationType));

        private static readonly int ID = GUIUtility.GetControlID(FocusType.Passive);

        private bool line_mode;
        internal bool self_call;

        private readonly IntVector3Interval currentInterval = new IntVector3Interval();
        private readonly PreviewPool previewPool = new PreviewPool();
        private Placement previous;

        private UIPaletteBlockSelect blockPalette;
        private Button lineButton;

        private static readonly MethodInfo SetGrabModeActive = AccessTools.Method(BlockPicker.T_UIPaletteBlockSelect, nameof(SetGrabModeActive));

        public void Start()
        {
            ManGameMode.inst.ModeStartEvent.Subscribe(OnModeStart);
            ManTechBuilder.inst.PlacementReadyToAttachChangedEvent.Subscribe(OnPlacementReadyToAttachChanged);
            useGUILayout = false;
        }

        private void OnPlacementReadyToAttachChanged(TankBlock block, Tank tech, BlockPlacementCollector.Placement placement)
        {
            if (!line_mode || previous == null || ManPointer.inst.BuildMode != ManPointer.BuildingMode.PaintBlock)
            {
                return;
            }

            if (placement == null)
            {
                previewPool.Hide();
                return;
            }

            currentInterval.End = placement.localPos;

            try
            {
                previewPool.Preview(currentInterval, tech, previous.rot);
            }
            catch (Exception e)
            {
                Main.logger.Error("Error in preview");
                Main.logger.Error(e);
            }
        }

        public void ResetState(bool removePreviews = true)
        {
            previous = null;

            if (removePreviews)
            {
                previewPool.Cleanup();
            }
            else
            {
                previewPool.Hide();
            }
        }

        private void OnModeStart(Mode obj)
        {
            ResetState();
            PreviewPool.RegenDummy();
        }

        public void Update()
        {
            if (Event.current.alt && Input.GetKeyDown(KeyCode.L))
            {
                SetLineMode(!line_mode);
            }

            if (Event.current.alt && Input.GetKeyDown(KeyCode.Minus))
            {
                useGUILayout = !useGUILayout;
            }
        }

        public void OnGUI()
        {
            if (!useGUILayout)
            {
                return;
            }

            var height = currentInterval.Type == IntVector3Interval.IterationType.Lines3D ? 250 : 75;

            GUI.Window(ID, new Rect(Screen.width - 100, (Screen.height - height) * 0.5f, 100, height), DoWindow, "Block Lines");
        }

        private void DoWindow(int id)
        {
            GUILayout.FlexibleSpace();
            var type = (IntVector3Interval.IterationType)GUILayout.SelectionGrid((int)currentInterval.Type, IterationTypes, 1);
            if (type != currentInterval.Type)
            {
                currentInterval.Type = type;
                Main.logger.Info("Interval type set to " + type);
            }

            if (currentInterval.Type == IntVector3Interval.IterationType.Lines3D)
            {
                GUILayout.Label("Axis Order");

                var prev = selectedAxisConfig;
                selectedAxisConfig = GUILayout.SelectionGrid(selectedAxisConfig, axisConfigsString, 1);
                if (prev != selectedAxisConfig)
                {
                    var config = axisConfigs[selectedAxisConfig];

                    currentInterval.SetAxisOrder(config.a, config.b, config.c);
                }
            }

            GUILayout.FlexibleSpace();
        }

        public void SetLineMode(bool enabled)
        {
            line_mode = enabled;
            Main.logger.Info("BlockLine " + (line_mode ? "enabled" : "disabled"));
            ResetState(false);

            if (!self_call)
            {
                self_call = true;
                SetGrabModeActive.Invoke(blockPalette, new object[] { !enabled });
                self_call = false;
            }

            lineButton.interactable = !enabled;
        }

        public void OnPlacement(Tank tech, TankBlock block, IntVector3 pos, OrthoRotation rot)
        {
            if (self_call
                || !line_mode
                || block.filledCells.Length != 1
                || block.attachPoints.Length != 6
                || ManPointer.inst.BuildMode != ManPointer.BuildingMode.PaintBlock
            )
            {
                if (line_mode && (block.filledCells.Length != 1 || block.attachPoints.Length != 6))
                {
                    Main.logger.Info("BlockLine limited to single \"cube\" blocks (1x1x1 with APs on every face)");
                }

                return;
            }

            var p = new Placement
            {
                tech = tech,
                block = block.BlockType,
                pos = pos,
                rot = rot
            };

            if (previous == null || !previous.Compatible(p))
            {
                Main.logger.Trace("Line start at pos: " + pos);

                previous = p;
                previewPool.SetType(block.BlockType);
                currentInterval.Start = pos;
                return;
            }

            Main.logger.Trace("Line end at pos: " + pos);

            PlaceLine(p);
        }

        private void PlaceLine(Placement p)
        {
            var positions = new List<IntVector3>();

            currentInterval.Start = previous.pos;
            currentInterval.End = p.pos;

            foreach (var pos in currentInterval)
            {
                var blockat = previous.tech.blockman.GetBlockAtPosition(pos);

                if (blockat)
                {
                    Main.logger.Trace($"Block {blockat.BlockType} at {pos}, skipping placement");
                    continue;
                }

                positions.Add(pos);
            }

            self_call = true;

            foreach (var position in positions)
            {
                var tb = ManSpawn.inst.SpawnBlock(previous.block, IntVector3.zero, Quaternion.identity);
                var res = ManLooseBlocks.inst.RequestAttachBlock(previous.tech, tb, position, previous.rot);

                Main.logger.Trace($"Adding block at {position}: {(res ? "Success" : "Failure")}");
                if (!res)
                {
                    tb.visible.RemoveFromGame();
                }
            }

            self_call = false;

            ResetState(false);
        }

        // HUD_BlockPainting_Base/UIPaletteBlockSelect Integration
        public static void InitUI(UIPaletteBlockSelect palette)
        {
            BlockLine.inst.blockPalette = palette;

            var actionMenu = palette.transform.Find("HUD_BlockPainting_BG/ActionMenu");

            var lineButton = DefaultControls.CreateButton(new DefaultControls.Resources()
            {
                standard = Main.BuilderToolsContainer.Contents.FindAsset("ICON_ACTION_LINE") as Sprite
            });

            lineButton.name = "LineButton";
            lineButton.transform.SetParent(actionMenu, false);

            GameObject.DestroyImmediate(lineButton.transform.GetChild(0));
            lineButton.transform.DetachChildren();

            var lbr = lineButton.GetComponent<RectTransform>();
            lbr.anchoredPosition3D = new Vector3(10, -72, 0);
            lbr.pivot = lbr.anchorMin = lbr.anchorMax = Vector2.up;
            lbr.sizeDelta = new Vector2(32, 32);

            var lbi = lineButton.GetComponent<Image>();
            lbi.color = Color.white;
            lbi.type = Image.Type.Simple;

            var lbb = lineButton.GetComponent<Button>();
            lbb.transition = Selectable.Transition.ColorTint;
            var colors = lbb.colors;
            colors.normalColor = new Color(0.278f, 0.278f, 0.278f, 1);
            colors.disabledColor = new Color(0.718f, 0.553f, 0.149f, 1);
            lbb.colors = colors;

            lbb.onClick.AddListener(() =>
            {
                BlockLine.inst.SetLineMode(true);
            });

            var tooltip = lineButton.AddComponent<TooltipComponent>();
            tooltip.SetMode(UITooltipOptions.Default);
            tooltip.SetText("Line Mode");

            BlockLine.inst.lineButton = lbb;
        }

        public class Placement
        {
            public Tank tech;
            public BlockTypes block;
            public IntVector3 pos;
            public OrthoRotation rot;

            public bool Compatible(Placement p)
            {
                return tech == p.tech && block == p.block && rot == p.rot;
            }
        }
    }
}
