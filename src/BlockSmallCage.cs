using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;

namespace Animalcages
{
    class BlockSmallCage : BlockCage
    {
        public static Dictionary<string, MeshRef> CachedMeshRefs(ICoreAPI api)
        {
            Dictionary<string, MeshRef> toolTextureSubIds;
            object obj;

            if (api.ObjectCache.TryGetValue("cachedSmallEntityMeshRefs", out obj))
            {
                toolTextureSubIds = obj as Dictionary<string, MeshRef>;
            }
            else
            {
                api.ObjectCache["cachedSmallEntityMeshRefs"] = toolTextureSubIds = new Dictionary<string, MeshRef>();
            }

            return toolTextureSubIds;
        }

        public override void OnBeforeRender(ICoreClientAPI capi, ItemStack itemstack, EnumItemRenderTarget target, ref ItemRenderInfo renderinfo)
        {
            string entity = itemstack.Attributes.GetString(CAPTURED_ENTITY_NAME);
            if (!string.IsNullOrEmpty(entity))
            {
                if (!CachedMeshRefs(capi).ContainsKey(entity + "_" + itemstack.Attributes.GetInt(CAPTURED_ENTITY_TEXTURE_ID)))
                {
                    MeshData cageMesh;
                    capi.Tesselator.TesselateBlock(this, out cageMesh);
                    MeshData entityMesh = new CagedEntityRenderer(capi,
                                entity,
                                itemstack.Attributes.GetInt(CAPTURED_ENTITY_TEXTURE_ID),
                                itemstack.Attributes.GetString(CAPTURED_ENTITY_SHAPE))
                            .genMesh();
                    cageMesh.AddMeshData(entityMesh);
                    CachedMeshRefs(capi)[entity + "_" + itemstack.Attributes.GetInt(CAPTURED_ENTITY_TEXTURE_ID)] = capi.Render
                        .UploadMesh(cageMesh);
                }
                renderinfo.ModelRef = CachedMeshRefs(capi)[entity + "_" + itemstack.Attributes.GetInt(CAPTURED_ENTITY_TEXTURE_ID)];
            }
            else { base.OnBeforeRender(capi, itemstack, target, ref renderinfo); }
        }
        protected override bool isCatchable(Entity entity)
        {
            return CageConfig.Current.smallCatchableEntities.Exists(x => x.name == entity.Properties.Code.GetName());
        }
    }
}
