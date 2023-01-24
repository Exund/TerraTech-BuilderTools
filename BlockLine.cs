using System;
using System.Collections.Generic;
using UnityEngine;

namespace BuilderTools
{
    public class BlockLine : Singleton.Manager<BlockLine>
    {
        private bool line_mode;
        internal bool self_call;

        private IntVector3Interval currentInterval = new IntVector3Interval();
        private Placement previous;
        private PreviewPool previewPool = new PreviewPool();

        public void Start()
        {
            ManGameMode.inst.ModeStartEvent.Subscribe(OnModeStart);
            ManTechBuilder.inst.PlacementReadyToAttachChangedEvent.Subscribe(OnPlacementReadyToAttachChanged);
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

            previewPool.Preview(currentInterval, tech, previous.rot);
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
            if (Input.GetKey(KeyCode.LeftAlt) && Input.GetKeyDown(KeyCode.L))
            {
                SetLineMode(!line_mode);
            }
        }

        public void SetLineMode(bool enabled)
        {
            line_mode = enabled;
            Main.logger.Info("BlockLine " + (line_mode ? "enabled" : "disabled"));
            ResetState(false);
        }

        public void OnPlacement(Tank tech, TankBlock block, IntVector3 pos, OrthoRotation rot)
        {
            if (self_call
                || !line_mode
                || block.filledCells.Length != 1
                || ManPointer.inst.BuildMode != ManPointer.BuildingMode.PaintBlock
            )
            {
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
                Main.logger.Trace("New line start at pos: " + pos);

                previous = p;
                previewPool.SetType(block.BlockType);
                currentInterval.Start = pos;
                return;
            }

            PlaceLine(p);
        }

        private static void IteratePositions(IntVector3 start, IntVector3 end, Action<IntVector3, int> action, bool skipFirst = true, bool includeEnd = false)
        {
            new IntVector3Interval(start, end).IteratePositions(action, skipFirst, includeEnd);
        }

        private void PlaceLine(Placement p)
        {
            var positions = new List<IntVector3>();

            IteratePositions(previous.pos, p.pos, (pos, i) =>
            {
                var blockat = previous.tech.blockman.GetBlockAtPosition(pos);

                if (blockat)
                {
                    Main.logger.Trace($"Block {blockat.BlockType} at {pos}, skipping placement");
                    return;
                }

                positions.Add(pos);
            });

            self_call = true;

            foreach (var position in positions)
            {
                var tb = ManSpawn.inst.SpawnBlock(previous.block, IntVector3.zero, Quaternion.identity);
                var res = ManLooseBlocks.inst.RequestAttachBlock(previous.tech, tb, position, previous.rot);

                Main.logger.Trace($"Adding block at {position}: {(res ? "Success" : "Failure")}");
            }

            self_call = false;

            ResetState(false);
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
