using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace Animalcages
{
    class BlockMediumCage : BlockCage
    {
        public static Dictionary<string, MeshRef> CachedMeshRefs(ICoreAPI api)
        {
            Dictionary<string, MeshRef> toolTextureSubIds;
            object obj;

            if (api.ObjectCache.TryGetValue("cachedMediumEntityMeshRefs", out obj))
            {
                toolTextureSubIds = obj as Dictionary<string, MeshRef>;
            }
            else
            {
                api.ObjectCache["cachedMediumEntityMeshRefs"] = toolTextureSubIds = new Dictionary<string, MeshRef>();
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
                    Shape guiShape = capi.Assets.Get(new AssetLocation("animalcages:shapes/mediumanimalcage-closed-gui.json")).ToObject<Shape>();
                    capi.Tesselator.TesselateShape(this, guiShape, out cageMesh);
                    MeshData entityMesh = new CagedEntityRenderer(capi,
                                entity,
                                itemstack.Attributes.GetInt(CAPTURED_ENTITY_TEXTURE_ID),
                                itemstack.Attributes.GetString(CAPTURED_ENTITY_SHAPE))
                            .genMesh();
                    ModelTransform transform = new ModelTransform();
                    transform.EnsureDefaultValues();
                    transform.Translation.X -= 0.4f;
                    entityMesh.ModelTransform(transform);
                    cageMesh.AddMeshData(entityMesh);
                    CachedMeshRefs(capi)[entity + "_" + itemstack.Attributes.GetInt(CAPTURED_ENTITY_TEXTURE_ID)] = capi.Render
                        .UploadMesh(cageMesh);
                }
                renderinfo.ModelRef = CachedMeshRefs(capi)[entity + "_" + itemstack.Attributes.GetInt(CAPTURED_ENTITY_TEXTURE_ID)];
            }
            else { base.OnBeforeRender(capi, itemstack, target, ref renderinfo); }
        }
        protected override bool isCatchable(Entity attackedEntity)
        {
            bool mediumEntity = CageConfig.Current.mediumCatchableEntities.Exists(x => x.name == attackedEntity.Properties.Code.GetName());
            bool smallEntity = CageConfig.Current.smallCatchableEntities.Exists(x => x.name == attackedEntity.Properties.Code.GetName());
            int generation = attackedEntity.WatchedAttributes.GetInt("generation", 0);
            var bh = attackedEntity.GetBehavior<EntityBehaviorHealth>();
            bool wounded = bh.MaxHealth >= bh.Health * CageConfig.Current.woundedMultiplicator;

            return smallEntity || mediumEntity && (generation > 0 || wounded);
        }

        public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref string failureCode)
        {
            if (!world.Claims.TryAccess(byPlayer, blockSel.Position, EnumBlockAccessFlags.BuildOrBreak))
            {
                byPlayer.InventoryManager.ActiveHotbarSlot.MarkDirty();
                return false;
            }

            if (CanPlaceBlock(world, byPlayer, blockSel, ref failureCode))
            {
                BlockFacing[] horVer = SuggestedHVOrientation(byPlayer, blockSel);

                BlockPos secondPos = blockSel.Position.AddCopy(BlockFacing.UP);

                BlockSelection secondBlockSel = new BlockSelection() { Position = secondPos, Face = BlockFacing.UP };
                if (!CanPlaceBlock(world, byPlayer, secondBlockSel, ref failureCode)) return false;

                BlockPos thirdPos = blockSel.Position.AddCopy(horVer[0]);

                BlockSelection thirdBlockSel = new BlockSelection() { Position = thirdPos, Face = BlockFacing.UP };
                if (!CanPlaceBlock(world, byPlayer, thirdBlockSel, ref failureCode)) return false;

                BlockPos fourthPos = thirdBlockSel.Position.AddCopy(BlockFacing.UP);

                BlockSelection fourthBlockSel = new BlockSelection() { Position = fourthPos, Face = BlockFacing.UP };
                if (!CanPlaceBlock(world, byPlayer, fourthBlockSel, ref failureCode)) return false;

                string front = horVer[0].Code;
                string back = horVer[0].Opposite.Code;

                string type = itemstack.Attributes.HasAttribute(CAPTURED_ENTITY) ? "closed" : "opened";

                Block orientedBlock = world.BlockAccessor.GetBlock(CodeWithParts("floor", "closed", back));
                orientedBlock.DoPlaceBlock(world, byPlayer, blockSel, itemstack);

                orientedBlock = world.BlockAccessor.GetBlock(CodeWithParts("ceiling", "closed", back));
                orientedBlock.DoPlaceBlock(world, byPlayer, secondBlockSel, itemstack);

                orientedBlock = world.BlockAccessor.GetBlock(CodeWithParts("floor", type, front));
                orientedBlock.DoPlaceBlock(world, byPlayer, thirdBlockSel, itemstack);

                orientedBlock = world.BlockAccessor.GetBlock(CodeWithParts("ceiling", type, front));
                orientedBlock.DoPlaceBlock(world, byPlayer, fourthBlockSel, itemstack);

                return true;
            }

            return false;
        }

        public override void OnBlockRemoved(IWorldAccessor world, BlockPos pos)
        {
            BlockFacing hfacing = BlockFacing.FromCode(LastCodePart()).Opposite;

            BlockFacing vFacing = CodeEndWithoutParts(0).Contains("floor") ? BlockFacing.UP : BlockFacing.DOWN;

            Block secondBlock = world.BlockAccessor.GetBlock(pos.AddCopy(hfacing));
            Block thirdBlock = world.BlockAccessor.GetBlock(pos.AddCopy(vFacing));
            Block fourthBlock = world.BlockAccessor.GetBlock(pos.AddCopy(vFacing).AddCopy(hfacing));

            if (secondBlock is BlockMediumCage)
            {
                world.BlockAccessor.SetBlock(0, pos.AddCopy(hfacing));
            }
            if (thirdBlock is BlockMediumCage)
            {
                world.BlockAccessor.SetBlock(0, pos.AddCopy(vFacing));
            }
            if (fourthBlock is BlockMediumCage)
            {
                world.BlockAccessor.SetBlock(0, pos.AddCopy(vFacing).AddCopy(hfacing));
            }

            base.OnBlockRemoved(world, pos);
        }
    }
}
