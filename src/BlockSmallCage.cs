using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;

namespace Animalcages
{
    class BlockSmallCage : BlockCage
    {
        public static Dictionary<string, MultiTextureMeshRef> CachedMeshRefs(ICoreAPI api)
        {
            Dictionary<string, MultiTextureMeshRef> toolTextureSubIds;
            object obj;

            if (api.ObjectCache.TryGetValue("cachedSmallEntityMeshRefs", out obj))
            {
                toolTextureSubIds = obj as Dictionary<string, MultiTextureMeshRef>;
            }
            else
            {
                api.ObjectCache["cachedSmallEntityMeshRefs"] = toolTextureSubIds = new Dictionary<string, MultiTextureMeshRef>();
            }

            return toolTextureSubIds;
        }

        public override void OnBeforeRender(ICoreClientAPI capi, ItemStack itemstack, EnumItemRenderTarget target, ref ItemRenderInfo renderinfo)
        {
            string entity = itemstack.Attributes.GetString(CAPTURED_ENTITY_NAME);
            if (!string.IsNullOrEmpty(entity))
            {
                try
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
                            .UploadMultiTextureMesh(cageMesh);
                    }
                    renderinfo.ModelRef = CachedMeshRefs(capi)[entity + "_" + itemstack.Attributes.GetInt(CAPTURED_ENTITY_TEXTURE_ID)];
                }
                catch (Exception)
                {
                    base.OnBeforeRender(capi, itemstack, target, ref renderinfo);
                }
            }
            else { base.OnBeforeRender(capi, itemstack, target, ref renderinfo); }
        }
        protected override bool isCatchable(Entity byEntity, Entity entity)
        {
            return CageConfig.Current.GetSmallCatchableEntity(entity.Properties.Code.GetName()) != null;
        }
    }
}
