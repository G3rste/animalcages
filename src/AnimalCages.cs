using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Common.Entities;
using Vintagestory.GameContent;
using System.IO;
using System.Text;
using Vintagestory.API.Datastructures;
using System;
using Vintagestory.API.Server;

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

    public class BlockCage : BlockContainer
    {
        public override void OnAttackingWith(IWorldAccessor world, Entity byEntity, Entity attackedEntity, ItemSlot itemslot)
        {
            base.OnAttackingWith(world, byEntity, attackedEntity, itemslot);
            if (attackedEntity != null && world is IServerWorldAccessor)
            {
                catchEntity(attackedEntity, itemslot.Itemstack);
                attackedEntity.Die(EnumDespawnReason.PickedUp);
            }
        }

        public void catchEntity(Entity entity, ItemStack stack)
        {
            api.World.Logger.Debug("Catching Entity: " + entity.GetName());

            stack.Attributes.SetBytes("capturedEntity", EntityUtil.EntityToBytes(entity));
            stack.Attributes.SetString("capturedEntityClass", api.World.ClassRegistry.GetEntityClassName(entity.GetType()));
        }
        public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
        {
            ItemStack stack = base.OnPickBlock(world, pos);
            BlockEntityAnimalCage entity = api.World.BlockAccessor.GetBlockEntity(pos) as BlockEntityAnimalCage;
            if (entity != null && entity.tmpCapturedEntityBytes != null && entity.tmpCapturedEntityClass != null)
            {
                api.World.Logger.Debug("Hier ist noch was drin");
                stack.Attributes.SetBytes("capturedEntity", entity.tmpCapturedEntityBytes);
                stack.Attributes.SetString("capturedEntityClass", entity.tmpCapturedEntityClass);
            }

            return stack;
        }
    }

    public class BlockEntityAnimalCage : BlockEntity
    {
        public byte[] tmpCapturedEntityBytes;
        public string tmpCapturedEntityClass;

        public override void OnBlockBroken()
        {
            base.OnBlockBroken();
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
                if (tmpCapturedEntityClass != null && tmpCapturedEntityBytes != null)
                {
                    Api.World.Logger.Debug("Placed with Entity:" + tmpCapturedEntityClass);
                }
                byItemStack.Attributes.RemoveAttribute("capturedEntity");
                byItemStack.Attributes.RemoveAttribute("capturedEntityClass");
            }
            Api.World.Logger.Debug("Placed");
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