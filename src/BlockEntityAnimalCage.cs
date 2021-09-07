using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Client;

namespace Animalcages
{
    public class BlockEntityAnimalCage : BlockEntity
    {
        public byte[] tmpCapturedEntityBytes;
        public string tmpCapturedEntityClass;
        public string tmpCapturedEntityShape;
        public int tmpCapturedEntityTextureId;
        public string tmpCapturedEntityName;
        public CagedEntityRenderer renderer;
        protected MeshData currentMesh;

        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            MarkDirty(true);
        }
        public override void OnBlockBroken()
        {
            Entity entity = getCapturedEntity();
            if (entity != null)
            {
                entity.Pos.SetPos(Pos);
                entity.ServerPos.SetPos(Pos);
                Api.World.SpawnEntity(entity);
            }
        }
        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            if (tmpCapturedEntityBytes != null && tmpCapturedEntityClass != null)
            {
                tree.SetBytes("capturedEntity", tmpCapturedEntityBytes);
                tree.SetString("capturedEntityClass", tmpCapturedEntityClass);
                tree.SetString("capturedEntityShape", tmpCapturedEntityShape);
                tree.SetString("capturedEntityName", tmpCapturedEntityName);
                tree.SetInt("capturedEntityTextureId", tmpCapturedEntityTextureId);
            }
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
        {
            base.FromTreeAttributes(tree, worldAccessForResolve);
            tmpCapturedEntityBytes = tree.GetBytes("capturedEntity");
            tmpCapturedEntityClass = tree.GetString("capturedEntityClass");
            tmpCapturedEntityShape = tree.GetString("capturedEntityShape");
            tmpCapturedEntityName = tree.GetString("capturedEntityName");
            tmpCapturedEntityTextureId = tree.GetInt("capturedEntityTextureId");
        }

        public override void OnBlockPlaced(ItemStack byItemStack = null)
        {
            base.OnBlockPlaced(byItemStack);
            if (byItemStack != null)
            {
                tmpCapturedEntityBytes = byItemStack.Attributes.GetBytes("capturedEntity", null);
                tmpCapturedEntityClass = byItemStack.Attributes.GetString("capturedEntityClass", null);
                tmpCapturedEntityShape = byItemStack.Attributes.GetString("capturedEntityShape", null);
                tmpCapturedEntityTextureId = byItemStack.Attributes.GetInt("capturedEntityTextureId", 0);
                tmpCapturedEntityName = byItemStack.Attributes.GetString("capturedEntityName", null);
            }
            MarkDirty(true);
        }
        public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator)
        {
            tryGenMesh();
            mesher.AddMeshData(currentMesh);
            return false;
        }
        protected virtual void tryGenMesh()
        {
            renderer = new CagedEntityRenderer(Api as ICoreClientAPI, tmpCapturedEntityName, tmpCapturedEntityTextureId, tmpCapturedEntityShape);
            currentMesh = renderer.genMesh();
        }
        private Entity getCapturedEntity()
        {
            if (tmpCapturedEntityBytes != null && tmpCapturedEntityClass != null)
            {
                return EntityUtil.BytesToEntity(tmpCapturedEntityBytes, tmpCapturedEntityClass, Api.World);
            }
            else
            {
                return null;
            }
        }
    }
}
