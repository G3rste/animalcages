using System.Collections.Generic;
using System.Text;
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
        public const string CAPTURED_ENTITY_GENERATION = "capturedEntityGeneration";
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

        public static int GetCapturedEntityGeneration(ItemStack stack, IWorldAccessor world)
        {
            if (stack.Attributes.HasAttribute(CAPTURED_ENTITY_GENERATION))
            {
                return stack.Attributes.GetInt(CAPTURED_ENTITY_GENERATION);
            }

            return EntityUtil.GetEntityGeneration(
                stack.Attributes.GetBytes(CAPTURED_ENTITY),
                stack.Attributes.GetString(CAPTURED_ENTITY_CLASS),
                world
            );
        }

        public override void OnAttackingWith(IWorldAccessor world, Entity byEntity, Entity attackedEntity, ItemSlot itemslot)
        {
            base.OnAttackingWith(world, byEntity, attackedEntity, itemslot);
            if (attackedEntity != null
                && attackedEntity.Alive
                && !itemslot.Itemstack.Attributes.HasAttribute(CAPTURED_ENTITY)
                && world is Vintagestory.API.Server.IServerWorldAccessor
                && isCatchable(byEntity, attackedEntity))
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
                int generation = GetCapturedEntityGeneration(inSlot.Itemstack, world);
                if (generation > 0)
                {
                    dsc.AppendLine(Lang.Get("Generation: {0}", generation));
                }
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
                stack.Attributes.SetInt(CAPTURED_ENTITY_GENERATION, GetCapturedEntityGeneration(stack, world));
                return stack;
            }
            return new ItemStack(world.BlockAccessor.GetBlock(CodeWithVariant("type", "opened")));
        }

        public void catchEntity(Entity entity, ItemStack stack)
        {
            stack.Attributes.SetBytes(CAPTURED_ENTITY, EntityUtil.EntityToBytes(entity));
            stack.Attributes.SetString(CAPTURED_ENTITY_CLASS, api.World.ClassRegistry.GetEntityClassName(entity.GetType()));
            stack.Attributes.SetString(CAPTURED_ENTITY_NAME, entity.Properties.Code.GetName());
            stack.Attributes.SetString(CAPTURED_ENTITY_SHAPE, entity.Properties.Client.Shape.Base.Clone().WithPathPrefix("shapes/").WithPathPrefix(entity.Properties.Client.Shape.Base.Domain + ":").WithPathAppendix(".json").Path);
            stack.Attributes.SetInt(CAPTURED_ENTITY_TEXTURE_ID, entity.WatchedAttributes.GetInt("textureIndex", 0));
            stack.Attributes.SetInt(CAPTURED_ENTITY_GENERATION, entity.WatchedAttributes.GetInt("generation", 0));
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
                        if (item.Client.FirstTexture.Alternates != null)
                        {
                            foreach (var val in item.Client.FirstTexture.Alternates)
                            {
                                count++;
                                val.Bake(api.Assets);
                                textureDict.AddTextureLocation(new AssetLocationAndSource(val.Baked.BakedName, "Item code ", item.Code));
                                tt.TextureSubIdsByCode[count] = textureDict[new AssetLocationAndSource(val.Baked.BakedName)];
                            }
                        }
                    }
                    EntitiyTextureIds(api)[item.Code.GetName()] = tt;
                }
            }
        }
        protected abstract bool isCatchable(Entity byEntity, Entity entity);
    }
}
