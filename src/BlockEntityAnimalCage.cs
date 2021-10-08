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
                tree.SetBytes(BlockCage.CAPTURED_ENTITY, tmpCapturedEntityBytes);
                tree.SetString(BlockCage.CAPTURED_ENTITY_CLASS, tmpCapturedEntityClass);
                tree.SetString(BlockCage.CAPTURED_ENTITY_SHAPE, tmpCapturedEntityShape);
                tree.SetString(BlockCage.CAPTURED_ENTITY_NAME, tmpCapturedEntityName);
                tree.SetInt(BlockCage.CAPTURED_ENTITY_TEXTURE_ID, tmpCapturedEntityTextureId);
            }
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
        {
            base.FromTreeAttributes(tree, worldAccessForResolve);
            tmpCapturedEntityBytes = tree.GetBytes(BlockCage.CAPTURED_ENTITY);
            tmpCapturedEntityClass = tree.GetString(BlockCage.CAPTURED_ENTITY_CLASS);
            tmpCapturedEntityShape = tree.GetString(BlockCage.CAPTURED_ENTITY_SHAPE);
            tmpCapturedEntityName = tree.GetString(BlockCage.CAPTURED_ENTITY_NAME);
            tmpCapturedEntityTextureId = tree.GetInt(BlockCage.CAPTURED_ENTITY_TEXTURE_ID);
        }

        public override void OnBlockPlaced(ItemStack byItemStack = null)
        {
            base.OnBlockPlaced(byItemStack);
            if (byItemStack != null)
            {
                tmpCapturedEntityBytes = byItemStack.Attributes.GetBytes(BlockCage.CAPTURED_ENTITY);
                tmpCapturedEntityClass = byItemStack.Attributes.GetString(BlockCage.CAPTURED_ENTITY_CLASS);
                tmpCapturedEntityShape = byItemStack.Attributes.GetString(BlockCage.CAPTURED_ENTITY_SHAPE);
                tmpCapturedEntityName = byItemStack.Attributes.GetString(BlockCage.CAPTURED_ENTITY_NAME);
                tmpCapturedEntityTextureId = byItemStack.Attributes.GetInt(BlockCage.CAPTURED_ENTITY_TEXTURE_ID);
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
            currentMesh = new CagedEntityRenderer(Api as ICoreClientAPI, tmpCapturedEntityName, tmpCapturedEntityTextureId, tmpCapturedEntityShape).genMesh();
            string variant = Block.CodeEndWithoutParts(0);
            if (currentMesh != null)
            {
                ModelTransform transform = new ModelTransform();
                transform.EnsureDefaultValues();
                if (variant.Contains("east"))
                {
                }
                if (variant.Contains("south"))
                {
                    transform.Rotation.Y = 270;
                }
                if (variant.Contains("west"))
                {
                    transform.Rotation.Y = 180;
                }
                if (variant.Contains("north"))
                {
                    transform.Rotation.Y = 90;
                }
                currentMesh.ModelTransform(transform);
            }
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
