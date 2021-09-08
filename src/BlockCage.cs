using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace Animalcages
{
    public abstract class BlockCage : Block
    {
        public const string CAPTURED_ENTITY = "capturedEntity";
        public const string CAPTURED_ENTITY_CLASS = "capturedEntityClass";
        public const string CAPTURED_ENTITY_NAME = "capturedEntityName";
        public const string CAPTURED_ENTITY_SHAPE = "capturedEntityShape";
        public const string CAPTURED_ENTITY_TEXTURE_ID = "capturedEntityTextureId";
        public static Dictionary<string, CapturedEntityTextures> EntitiyTextureIds(ICoreAPI api)
        {
            Dictionary<string, CapturedEntityTextures> entityTextureSubIds;
            object obj;

            if (api.ObjectCache.TryGetValue("entityTextureSubIds", out obj))
            {
                entityTextureSubIds = obj as Dictionary<string, CapturedEntityTextures>;
            }
            else
            {
                api.ObjectCache["entityTextureSubIds"] = entityTextureSubIds = new Dictionary<string, CapturedEntityTextures>();
            }

            return entityTextureSubIds;
        }

        public override void OnAttackingWith(IWorldAccessor world, Entity byEntity, Entity attackedEntity, ItemSlot itemslot)
        {
            base.OnAttackingWith(world, byEntity, attackedEntity, itemslot);
            if (attackedEntity != null
                && attackedEntity.Alive
                && !itemslot.Itemstack.Attributes.HasAttribute(CAPTURED_ENTITY)
                && world is Vintagestory.API.Server.IServerWorldAccessor
                && isCatchable(attackedEntity))
            {
                ItemStack newStack = new ItemStack(api.World.GetBlock(CodeWithVariant("type", "closed")));
                itemslot.TakeOutWhole();
                itemslot.Itemstack = newStack;
                catchEntity(attackedEntity, itemslot.Itemstack);
                attackedEntity.Die(EnumDespawnReason.PickedUp);
            }
        }

        public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
        {
            base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
            string entityName = inSlot.Itemstack.Attributes.GetString(CAPTURED_ENTITY_NAME);
            if (inSlot.Itemstack.Attributes.HasAttribute(CAPTURED_ENTITY_NAME))
            {
                dsc.AppendLine("(" + Lang.Get("item-creature-" + entityName) + ")");
            }
        }

        public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
        {
            BlockEntityAnimalCage entity = api.World.BlockAccessor.GetBlockEntity(pos) as BlockEntityAnimalCage;
            if (entity != null && entity.tmpCapturedEntityBytes != null && entity.tmpCapturedEntityClass != null)
            {
                ItemStack stack = new ItemStack(this);
                stack.Attributes.SetBytes(CAPTURED_ENTITY, entity.tmpCapturedEntityBytes);
                stack.Attributes.SetString(CAPTURED_ENTITY_CLASS, entity.tmpCapturedEntityClass);
                stack.Attributes.SetString(CAPTURED_ENTITY_NAME, entity.tmpCapturedEntityName);
                stack.Attributes.SetString(CAPTURED_ENTITY_SHAPE, entity.tmpCapturedEntityShape);
                stack.Attributes.SetInt(CAPTURED_ENTITY_TEXTURE_ID, entity.tmpCapturedEntityTextureId);
                return stack;
            }
            return base.OnPickBlock(world, pos);
        }

        public void catchEntity(Entity entity, ItemStack stack)
        {
            stack.Attributes.SetBytes(CAPTURED_ENTITY, EntityUtil.EntityToBytes(entity));
            stack.Attributes.SetString(CAPTURED_ENTITY_CLASS, api.World.ClassRegistry.GetEntityClassName(entity.GetType()));
            stack.Attributes.SetString(CAPTURED_ENTITY_NAME, entity.Properties.Code.GetName());
            stack.Attributes.SetString(CAPTURED_ENTITY_SHAPE, entity.Properties.Client.Shape.Base.Clone().WithPathPrefix("shapes/").WithPathPrefix(entity.Properties.Client.Shape.Base.Domain + ":").WithPathAppendix(".json").Path);
            stack.Attributes.SetInt(CAPTURED_ENTITY_TEXTURE_ID, entity.WatchedAttributes.GetInt("textureIndex", 0));
        }
        public override void OnCollectTextures(ICoreAPI api, ITextureLocationDictionary textureDict)
        {
            base.OnCollectTextures(api, textureDict);
            lock (this)
            {
                for (int i = 0; i < api.World.EntityTypes.Count; i++)
                {
                    EntityProperties item = api.World.EntityTypes[i];

                    CapturedEntityTextures tt = new CapturedEntityTextures();

                    if (item.Client.FirstTexture != null)
                    {
                        int count = 0;
                        item.Client.FirstTexture.Bake(api.Assets);
                        textureDict.AddTextureLocation(new AssetLocationAndSource(item.Client.FirstTexture.Baked.BakedName, "Item code ", item.Code));
                        tt.TextureSubIdsByCode[count] = textureDict[new AssetLocationAndSource(item.Client.FirstTexture.Baked.BakedName)];
                        api.Logger.Debug("Load Entity Block Asset: " + item.Client.FirstTexture.Base.Path);
                        if (item.Client.FirstTexture.Alternates != null)
                        {
                            foreach (var val in item.Client.FirstTexture.Alternates)
                            {
                                count++;
                                val.Bake(api.Assets);
                                textureDict.AddTextureLocation(new AssetLocationAndSource(val.Baked.BakedName, "Item code ", item.Code));
                                tt.TextureSubIdsByCode[count] = textureDict[new AssetLocationAndSource(val.Baked.BakedName)];
                                api.Logger.Debug("Load Entity Block Asset: " + val.Base.Path);
                            }
                        }
                    }
                    EntitiyTextureIds(api)[item.Code.GetName()] = tt;
                }
            }
        }
        protected abstract bool isCatchable(Entity entity);
    }
}
