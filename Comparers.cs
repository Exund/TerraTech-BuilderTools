using System.Collections.Generic;

namespace BuilderTools
{
    public class APConnectionComparer : EqualityComparer<BlockPlacementPreviewHandler.APConnection>
    {
        public override bool Equals(BlockPlacementPreviewHandler.APConnection x, BlockPlacementPreviewHandler.APConnection y)
        {
            return x.blockAP == y.blockAP;
        }

        public override int GetHashCode(BlockPlacementPreviewHandler.APConnection obj)
        {
            return obj.blockAP.GetHashCode();
        }
    }

    public class BlockAttachmentComparer : EqualityComparer<BlockManager.BlockAttachment>
    {
        public override bool Equals(BlockManager.BlockAttachment x, BlockManager.BlockAttachment y)
        {
            return x.apPosLocal == y.apPosLocal;
        }

        public override int GetHashCode(BlockManager.BlockAttachment obj)
        {
            return obj.apPosLocal.GetHashCode();
        }
    }
}
