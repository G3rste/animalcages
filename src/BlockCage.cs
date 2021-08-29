using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace Animalcages
{
    public class BlockCage : Block
    {
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

        public static Dictionary<string, MeshRef> CachedMeshRefs(ICoreAPI api)
        {
            Dictionary<string, MeshRef> toolTextureSubIds;
            object obj;

            if (api.ObjectCache.TryGetValue("cachedEntityMeshRefs", out obj))
            {
                toolTextureSubIds = obj as Dictionary<string, MeshRef>;
            }
            else
            {
                api.ObjectCache["cachedEntityMeshRefs"] = toolTextureSubIds = new Dictionary<string, MeshRef>();
            }

            return toolTextureSubIds;
        }
        public override void OnBeforeRender(ICoreClientAPI capi, ItemStack itemstack, EnumItemRenderTarget target, ref ItemRenderInfo renderinfo)
        {
            string entity = itemstack.Attributes.GetString("capturedEntityName");
            if (!string.IsNullOrEmpty(entity))
            {
                if (!CachedMeshRefs(capi).ContainsKey(entity + "_" + itemstack.Attributes.GetInt("capturedEntityTextureId")))
                {
                    MeshData cageMesh;
                    capi.Tesselator.TesselateBlock(this, out cageMesh);
                    cageMesh.AddMeshData(new CagedEntityRenderer(capi,
                                entity,
                                itemstack.Attributes.GetInt("capturedEntityTextureId"),
                                itemstack.Attributes.GetString("capturedEntityShape"))
                            .genMesh());
                    CachedMeshRefs(capi)[entity + "_" + itemstack.Attributes.GetInt("capturedEntityTextureId")] = capi.Render
                        .UploadMesh(cageMesh);
                }
                renderinfo.ModelRef = CachedMeshRefs(capi)[entity + "_" + itemstack.Attributes.GetInt("capturedEntityTextureId")];
            }
            else { base.OnBeforeRender(capi, itemstack, target, ref renderinfo); }
        }

        public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
        {
            base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
            string entityName = inSlot.Itemstack.Attributes.GetString("capturedEntityName", null);
            if (inSlot.Itemstack.Attributes.HasAttribute("capturedEntityName"))
            {
                dsc.AppendLine("(" + Lang.Get("item-creature-" + entityName) + ")");
            }
        }
        public override void OnAttackingWith(IWorldAccessor world, Entity byEntity, Entity attackedEntity, ItemSlot itemslot)
        {
            base.OnAttackingWith(world, byEntity, attackedEntity, itemslot);
            if (attackedEntity != null
                && attackedEntity.Alive
                && !itemslot.Itemstack.Attributes.HasAttribute("capturedEntity")
                && CageConfig.Current.smallCatchableEntities.Exists(x => x.name == attackedEntity.Properties.Code.GetName())
                && world is Vintagestory.API.Server.IServerWorldAccessor)
            {
                ItemStack newStack = new ItemStack(api.World.GetBlock(CodeWithVariant("type", "closed")));
                itemslot.TakeOutWhole();
                itemslot.Itemstack = newStack;
                catchEntity(attackedEntity, itemslot.Itemstack);
                attackedEntity.Die(EnumDespawnReason.PickedUp);
            }
        }

        public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
        {
            BlockEntityAnimalCage entity = api.World.BlockAccessor.GetBlockEntity(pos) as BlockEntityAnimalCage;
            if (entity != null && entity.tmpCapturedEntityBytes != null && entity.tmpCapturedEntityClass != null)
            {
                ItemStack stack = new ItemStack(this);
                stack.Attributes.SetBytes("capturedEntity", entity.tmpCapturedEntityBytes);
                stack.Attributes.SetString("capturedEntityClass", entity.tmpCapturedEntityClass);
                stack.Attributes.SetString("capturedEntityName", entity.tmpCapturedEntityName);
                stack.Attributes.SetString("capturedEntityShape", entity.tmpCapturedEntityShape);
                stack.Attributes.SetInt("capturedEntityTextureId", entity.tmpCapturedEntityTextureId);
                return stack;
            }
            return base.OnPickBlock(world, pos);
        }

        public void catchEntity(Entity entity, ItemStack stack)
        {
            stack.Attributes.SetBytes("capturedEntity", EntityUtil.EntityToBytes(entity));
            stack.Attributes.SetString("capturedEntityClass", api.World.ClassRegistry.GetEntityClassName(entity.GetType()));
            stack.Attributes.SetString("capturedEntityShape", entity.Properties.Client.Shape.Base.Clone().WithPathPrefix("shapes/").WithPathPrefix(entity.Properties.Client.Shape.Base.Domain + ":").WithPathAppendix(".json").Path);
            stack.Attributes.SetInt("capturedEntityTextureId", getEntityTextureId(entity));
            stack.Attributes.SetString("capturedEntityName", entity.Properties.Code.GetName());
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

        private int getEntityTextureId(Entity entity)
        {
            return entity.WatchedAttributes.GetInt("textureIndex", 0);
        }
    }
}
