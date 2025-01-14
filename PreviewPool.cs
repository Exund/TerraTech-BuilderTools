﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Animations;

namespace BuilderTools
{
    public class PreviewPool
    {
        private static readonly MethodInfo RemoveAllBlocks = HarmonyLib.AccessTools.Method(typeof(BlockManager), nameof(RemoveAllBlocks));

        private static readonly ManSpawn.TankSpawnParams dummySpawnParams;

        private static Tank dummy;

        private static BlockManager dummyTable;

        private static ParentConstraint dummyParent;

        private static readonly List<BlockPlacementPreviewHandler.APConnection> m_BlockAPConnections = new List<BlockPlacementPreviewHandler.APConnection>();

        static PreviewPool()
        {
            var dummyData = new TechData
            {
                m_BlockSpecs = new List<TankPreset.BlockSpec>()
                {
                    new TankPreset.BlockSpec()
                    {
                        m_BlockType = BlockTypes.GSOCockpit_111,
                        position = IntVector3.zero,
                        orthoRotation = OrthoRotation.identity,
                    }
                }
            };
            dummyData.CalculateBlockBounds();

            dummySpawnParams = new ManSpawn.TankSpawnParams()
            {
                teamID = -2,
                forceSpawn = true,
                hideMarker = true,
                isInvulnerable = true,
                position = Vector3.down * 500f,
                ignoreSceneryOnSpawnProjection = true,
                grounded = false,
                techData = dummyData
            };

            RegenDummy();
        }

        public static void RegenDummy()
        {
            Main.logger.Trace($"[PreviewPool] Regenerating dummy");
            if (dummy)
            {
                GameObject.DestroyImmediate(dummy);
            }

            dummy = ManSpawn.inst.SpawnUnmanagedTank(dummySpawnParams);
            dummy.visible.EnablePhysics(false);
            dummy.rbody.isKinematic = true;
            dummyTable = dummy.blockman;

            dummyParent = dummy.gameObject.AddComponent<ParentConstraint>();
            dummyParent.constraintActive = true;
            dummyParent.AddSource(new ConstraintSource());
            dummyParent.SetTranslationOffset(0, Vector3.zero);
            dummyParent.SetRotationOffset(0, Vector3.zero);
        }

        private static void PrepareBlockAPConnections(IEnumerable<BlockManager.BlockAttachment> attachments, Vector3 blockLocalPos, Quaternion blockLocalRot, TankBlock overrideOther = null)
        {
            m_BlockAPConnections.Clear();
            foreach (BlockManager.BlockAttachment blockAttachment in attachments)
            {
                m_BlockAPConnections.Add(new BlockPlacementPreviewHandler.APConnection
                {
                    blockAP = BlockPlacementPreviewHandler.TechPlacementToBlockAP(blockAttachment.apPosLocal, blockLocalPos, blockLocalRot),
                    otherBlock = (overrideOther ?? blockAttachment.other)
                });
            }
        }

        private readonly List<BlockPreview> previews = new List<BlockPreview>();

        private BlockTypes type;

        private HashSet<IBlockPlacementPreview> m_LastPlacementPreviews = new HashSet<IBlockPlacementPreview>();

        private HashSet<IBlockPlacementPreview> m_CurrentPlacementPreviews = new HashSet<IBlockPlacementPreview>();

        private static readonly List<BlockManager.BlockAttachment> m_APConnections = new List<BlockManager.BlockAttachment>();

        private static readonly List<BlockManager.BlockAttachment> m_DummyAPConnections = new List<BlockManager.BlockAttachment>();

        private void HandlePlacementPreviews(BlockPreview preview, Tank tech, IntVector3 pos, OrthoRotation rot)
        {
            tech.blockman.TryGetBlockAttachments(preview.block, pos, rot, m_APConnections);
            dummyTable.TryGetBlockAttachments(preview.block, pos, rot, m_DummyAPConnections);

            dummyTable.AddBlockToTech(preview.block, pos, rot);
            preview.block.SetCachedLocalPositionData(pos, rot);

            var connections = m_APConnections.Union(m_DummyAPConnections, new BlockAttachmentComparer()).ToList();

            PrepareBlockAPConnections(connections, pos, rot);

            preview.Show(m_BlockAPConnections);

            foreach (IGrouping<TankBlock, BlockManager.BlockAttachment> grouping in from a in connections group a by a.other)
            {
                TankBlock key = grouping.Key;
                IBlockPlacementPreview component = key.GetComponent<IBlockPlacementPreview>();
                if (component != null)
                {
                    Main.logger.Debug($"[PreviewPool] Reverse PlacementPreview at {key.cachedLocalPosition}");
                    PrepareBlockAPConnections(grouping, key.cachedLocalPosition, key.cachedLocalRotation, preview.block);

                    var otherPreview = previews.Find(bp => bp.block == key);
                    if (otherPreview != null)
                    {
                        Main.logger.Trace($"[PreviewPool] Reverse PlacementPreview is BlockPreview");
                        otherPreview.AddAP(m_BlockAPConnections);
                    }
                    else
                    {
                        component.TryPreviewAttachments(m_BlockAPConnections);
                    }

                    m_CurrentPlacementPreviews.Add(component);
                    m_LastPlacementPreviews.Remove(component);
                }
            }
        }

        private static void ResetDummy(IntVector3 position)
        {
            Main.logger.Debug("[PreviewPool] Recentering dummy at: " + position);
            RemoveAllBlocks.Invoke(dummyTable, new object[] { BlockManager.RemoveAllAction.HandOff });
        }

        public void SetType(BlockTypes type)
        {
            if (this.type != type)
            {
                Main.logger.Debug($"[PreviewPool] Changing type from {this.type} to {type}");
                this.type = type;

                Cleanup();
            }
        }

        public void Cleanup()
        {
            RemoveAllBlocks.Invoke(dummyTable, new object[] { BlockManager.RemoveAllAction.HandOff });

            foreach (var preview in previews)
            {
                ManLooseBlocks.inst.RequestDespawnBlock(preview.block, DespawnReason.PaintingBlock);
            }

            previews.Clear();
            Hide();
        }

        public void Fit(int size)
        {
            Main.logger.Debug($"[PreviewPool] Fit to size: {previews.Count}->{size}");
            while (previews.Count < size)
            {
                var pblock = ManLooseBlocks.inst.RequestSpawnPaintingBlock(type, Vector3.zero, Quaternion.identity);
                pblock.visible.EnablePhysics(false);
                pblock.SetCustomMaterialOverride(ManTechMaterialSwap.MatType.Alpha);
                pblock.transform.SetParent(dummy.transform);
                pblock.rbody.isKinematic = true;
                pblock.rbody.detectCollisions = false;
                pblock.gameObject.SetActive(false);
                previews.Add(new BlockPreview(pblock));
            }
        }

        public void Preview(IntVector3Interval interval, Tank tech, OrthoRotation rot)
        {
            var size = interval.Size;
            Main.logger.Trace($"[PreviewPool] Interval start-end: {interval.Start}-{interval.End}");
            Main.logger.Debug("[PreviewPool] Interval size: " + size);

            if (size < 0)
            {
                return;
            }

            Fit(size);

            dummy.gameObject.SetActive(true);

            dummyParent.SetSource(0, new ConstraintSource()
            {
                sourceTransform = tech.trans,
                weight = 1
            });

            ResetDummy(interval.Start);

            int i = 0;
            foreach (var pos in interval)
            {
                Main.logger.Debug($"[PreviewPool] Preview #{i} at {pos}");
                var preview = previews[i];

                HandlePlacementPreviews(preview, tech, pos, rot);

                var blockat = tech.blockman.GetBlockAtPosition(pos);
                if (blockat)
                {
                    Main.logger.Debug($"[PreviewPool] Block {blockat.BlockType} at {pos}, hiding preview");
                    preview.block.gameObject.SetActive(false);
                }
                else
                {
                    preview.block.gameObject.SetActive(true);
                }

                i++;
            }

            size = i;
            if (previews.Count > size)
            {
                var excess = previews.GetRange(size, previews.Count - size);
                Main.logger.Trace("[PreviewPool] Excess size: " + excess.Count);
                foreach (var preview in excess)
                {
                    preview.Hide();
                }
            }

            /*if (interval.Type == IntVector3Interval.IterationType.MaxAxis)
            {
                var preview = previews[i];
                var diff = (interval.End - preview.block.cachedLocalPosition).normalized;

                HandlePlacementPreviews(preview, tech, interval.End, rot);

                var trueline = Math.Abs(diff.x + diff.y + diff.z).Approximately(1);
                preview.block.gameObject.SetActive(trueline);

                if (!trueline)
                {
                    Main.logger.Trace($"[PreviewPool] Hiding last preview at {interval.End}");
                }
            }*/

            ClearLastPlacements();
            HashSet<IBlockPlacementPreview> lastPlacementPreviews = m_LastPlacementPreviews;
            m_LastPlacementPreviews = m_CurrentPlacementPreviews;
            m_CurrentPlacementPreviews = lastPlacementPreviews;
        }

        public void ClearLastPlacements()
        {
            m_BlockAPConnections.Clear();
            m_APConnections.Clear();
            m_DummyAPConnections.Clear();
            foreach (IBlockPlacementPreview blockPlacementPreview in m_LastPlacementPreviews)
            {
                blockPlacementPreview.TryPreviewAttachments(null);
            }

            m_LastPlacementPreviews.Clear();
        }

        public void Hide()
        {
            if (dummyParent.sourceCount > 0)
            {
                dummyParent.SetSource(0, new ConstraintSource());
            }

            ClearLastPlacements();
            foreach (IBlockPlacementPreview blockPlacementPreview in m_CurrentPlacementPreviews)
            {
                blockPlacementPreview.TryPreviewAttachments(null);
            }

            m_CurrentPlacementPreviews.Clear();

            dummy.gameObject.SetActive(false);
        }

        public class BlockPreview
        {
            public TankBlock block;
            public IBlockPlacementPreview placementPreview;

            private List<BlockPlacementPreviewHandler.APConnection> tempConnections = new List<BlockPlacementPreviewHandler.APConnection>();

            public BlockPreview(TankBlock block)
            {
                this.block = block;
                placementPreview = block.GetComponent<IBlockPlacementPreview>();
            }

            public void Hide()
            {
                block.gameObject.SetActive(false);
                placementPreview?.TryPreviewAttachments(null);
                tempConnections.Clear();
            }

            public void Show(IEnumerable<BlockPlacementPreviewHandler.APConnection> previewAPs)
            {
                block.gameObject.SetActive(true);
                tempConnections = previewAPs.ToList();
                placementPreview?.TryPreviewAttachments(tempConnections);
            }

            internal void AddAP(IEnumerable<BlockPlacementPreviewHandler.APConnection> previewAPs)
            {
                tempConnections = tempConnections.Union(previewAPs, new APConnectionComparer()).ToList();
                placementPreview?.TryPreviewAttachments(tempConnections);
            }
        }
    }
}
