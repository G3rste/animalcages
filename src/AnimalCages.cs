using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Common.Entities;
using Vintagestory.GameContent;
using System.IO;
using System.Text;
using Vintagestory.API.Datastructures;
using System;
using Vintagestory.API.Server;
using Vintagestory.API.Client;
using Vintagestory.API.Util;

namespace Animalcages
{
    public class CageMod : ModSystem
    {
        public override void Start(ICoreAPI api)
        {
            base.Start(api);
            api.RegisterBlockClass("BlockSmallAnimalCage", typeof(BlockCage));
            api.RegisterBlockEntityClass("BlockEntitySmallAnimalCage", typeof(BlockEntityAnimalCage));
        }
    }

    public class BlockCage : Block
    {
        public override void OnAttackingWith(IWorldAccessor world, Entity byEntity, Entity attackedEntity, ItemSlot itemslot)
        {
            base.OnAttackingWith(world, byEntity, attackedEntity, itemslot);
            if (attackedEntity != null && world is IServerWorldAccessor)
            {
                ItemStack newStack = new ItemStack(api.World.GetBlock(CodeWithVariant("type", "closed")));
                itemslot.TakeOutWhole();
                itemslot.Itemstack = newStack;
                catchEntity(attackedEntity, itemslot.Itemstack);
                attackedEntity.Die(EnumDespawnReason.PickedUp);
            }
        }
        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            ItemStack stack = new ItemStack(this);
            BlockEntityAnimalCage entity = api.World.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityAnimalCage;
            if (entity != null && entity.tmpCapturedEntityBytes != null && entity.tmpCapturedEntityClass != null)
            {
                stack.Attributes.SetBytes("capturedEntity", entity.tmpCapturedEntityBytes);
                stack.Attributes.SetString("capturedEntityClass", entity.tmpCapturedEntityClass);
            }
            if (byPlayer.InventoryManager.TryGiveItemstack(stack))
            {
                world.BlockAccessor.SetBlock(0, blockSel.Position);
                world.PlaySoundAt(new AssetLocation("sounds/block/planks"), blockSel.Position.X + 0.5, blockSel.Position.Y, blockSel.Position.Z + 0.5, byPlayer, false);

                return true;
            }

            return false;
        }

        public void catchEntity(Entity entity, ItemStack stack)
        {
            api.World.Logger.Debug("Catching Entity: " + entity.GetName());

            stack.Attributes.SetBytes("capturedEntity", EntityUtil.EntityToBytes(entity));
            stack.Attributes.SetString("capturedEntityClass", api.World.ClassRegistry.GetEntityClassName(entity.GetType()));
            stack.Attributes.SetString("capturedEntityShape", entity.Properties.Client.Shape.Base.Clone().WithPathPrefix("shapes/").WithPathAppendix(".json").Path);
            stack.Attributes.SetString("capturedEntityTexture", getEntityTextureString(entity));
        }
        private string getEntityTextureString(Entity entity)
        {
            int altTexNumber = entity.WatchedAttributes.GetInt("textureIndex", 0);
            if (altTexNumber == 0)
            {
                return entity.Properties.Client.FirstTexture.Base.Path;
            }
            return entity.Properties.Client.FirstTexture.Alternates[altTexNumber % entity.Properties.Client.FirstTexture.Alternates.Length].Base.Path;
        }

        private int getEntityTextureSubId(Entity entity)
        {
            JsonObject json = entity.Properties.Attributes;
            if (json != null)
            {
                string skinBaseTextureKey = json["skinBaseTextureKey"].AsString();
                if (skinBaseTextureKey != null)
                {
                    return entity.Properties.Client.Textures[skinBaseTextureKey].Baked.TextureSubId;
                }
            }
            return entity
                .Properties
                .Client
                .Texture
                .Baked
                .TextureSubId;
        }
    }

    public class BlockEntityAnimalCage : BlockEntity, ITexPositionSource
    {
        public byte[] tmpCapturedEntityBytes;
        public string tmpCapturedEntityClass;
        public string tmpCapturedEntityShape;
        public string tmpCapturedEntityTexture;
        public int tmpCapturedEntityTextureId;
        MeshData currentMesh;

        public Size2i AtlasSize
        {
            get
            {
                ICoreClientAPI capi = Api as ICoreClientAPI;
                if (capi != null)
                {
                    return capi.EntityTextureAtlas.Size;
                }
                return null;
            }
        }
        public TextureAtlasPosition this[string textureCode]
        {
            get
            {
                ICoreClientAPI capi = Api as ICoreClientAPI;
                if (capi != null)
                {
                    return capi.EntityTextureAtlas[new AssetLocation(tmpCapturedEntityTexture)];
                }

                return null;
            }
        }
        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
        }
        public override void OnBlockBroken()
        {
            Entity entity = getCapturedEntity();
            if (entity != null)
            {
                entity.Pos.SetPos(Pos);
                entity.ServerPos.SetPos(Pos);
                Api.World.SpawnEntity(entity);
                deleteCapturedEntity();
            }
        }
        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            if (tmpCapturedEntityBytes != null && tmpCapturedEntityClass != null)
            {
                tree.SetBytes("capturedEntity", tmpCapturedEntityBytes);
                tree.SetString("capturedEntityClass", tmpCapturedEntityClass);
            }
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
        {
            base.FromTreeAttributes(tree, worldAccessForResolve);
            tmpCapturedEntityBytes = tree.GetBytes("capturedEntity");
            tmpCapturedEntityClass = tree.GetString("capturedEntityClass");
        }

        public override void OnBlockPlaced(ItemStack byItemStack = null)
        {
            base.OnBlockPlaced(byItemStack);
            if (byItemStack != null)
            {
                tmpCapturedEntityBytes = byItemStack.Attributes.GetBytes("capturedEntity", null);
                tmpCapturedEntityClass = byItemStack.Attributes.GetString("capturedEntityClass", null);
                tmpCapturedEntityShape = byItemStack.Attributes.GetString("capturedEntityShape", null);
                tmpCapturedEntityTexture = byItemStack.Attributes.GetString("capturedEntityTexture", null);
                tmpCapturedEntityTextureId = byItemStack.Attributes.GetInt("capturedEntityTextureId", 0);
                Api.World.Logger.Debug("Placed with Entity:" + tmpCapturedEntityClass);
                Api.World.Logger.Debug("Placed with Shape:" + tmpCapturedEntityShape);
                Api.World.Logger.Debug("Placed with Texture:" + tmpCapturedEntityTexture);
                Api.World.Logger.Debug("Placed with TextureId:" + tmpCapturedEntityTextureId);
                byItemStack.Attributes.RemoveAttribute("capturedEntity");
                byItemStack.Attributes.RemoveAttribute("capturedEntityClass");
            }
            ICoreClientAPI capi = Api as ICoreClientAPI;
            if (capi != null)
            {
                Api.World.Logger.Debug("Should be rendered");
                Shape shape = capi.Assets.TryGet(new AssetLocation(tmpCapturedEntityShape)).ToObject<Shape>();
                AssetLocation texture = new AssetLocation(tmpCapturedEntityTexture);
                tmpCapturedEntityTextureId = capi.EntityTextureAtlas[texture].atlasNumber;
                capi.Tesselator.TesselateShape("aimalcage", shape, out currentMesh, this);
            }
            Api.World.Logger.Debug("Placed");
        }
        public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator)
        {
            mesher.AddMeshData(currentMesh);
            return false;
        }

        BlockEntityAnimationUtil animUtil
        {
            get { return GetBehavior<BEBehaviorAnimatable>().animUtil; }
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
        private void deleteCapturedEntity()
        {
            tmpCapturedEntityBytes = null;
            tmpCapturedEntityClass = null;
        }
    }

    public class EntityUtil
    {
        public static byte[] EntityToBytes(Entity entity)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(ms, Encoding.UTF8))
                {
                    entity.ToBytes(writer, false);
                    writer.Flush();
                    return ms.ToArray();
                }
            }
        }

        public static Entity BytesToEntity(byte[] enityBytes, string enitiyClass, IWorldAccessor world)
        {
            if (enitiyClass != null && enityBytes != null)
            {
                using (MemoryStream ms = new MemoryStream(enityBytes))
                {
                    using (BinaryReader reader = new BinaryReader(ms, Encoding.UTF8))
                    {
                        Entity capturedEntity = world.ClassRegistry.CreateEntity(enitiyClass);
                        capturedEntity.FromBytes(reader, false);
                        return capturedEntity;
                    }
                }
            }
            else return null;
        }
    }
}