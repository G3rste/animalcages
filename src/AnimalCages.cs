using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Common.Entities;
using System.IO;
using System.Text;
using Vintagestory.API.Datastructures;
using System;
using Vintagestory.API.Server;
using Vintagestory.API.Client;
using System.Collections.Generic;
using Vintagestory.API.Config;

namespace Animalcages
{
    public class CageMod : ModSystem
    {
        public override void Start(ICoreAPI api)
        {
            base.Start(api);
            api.RegisterBlockClass("blocksmallanimalcage", typeof(BlockCage));
            api.RegisterBlockEntityClass("blockentitysmallanimalcage", typeof(BlockEntityAnimalCage));
            try
            {
                var Config = api.LoadModConfig<CageConfig>("animalcagesconfig.json");
                if (Config != null && CageConfig.Current != null)
                {
                    api.Logger.Notification("Mod Config successfully loaded.");
                    CageConfig.Current = Config;
                }
                else
                {
                    api.Logger.Notification("No Mod Config specified. Falling back to default settings");
                    CageConfig.Current = CageConfig.getDefault();
                }
            }
            catch
            {
                CageConfig.Current = CageConfig.getDefault();
                api.Logger.Error("Failed to load custom mod configuration. Falling back to default settings!");
            }
            finally
            {
                api.StoreModConfig(CageConfig.Current, "animalcagesconfig.json");
            }
        }
    }
    public class CapturedEntityTextures
    {
        public Dictionary<int, int> TextureSubIdsByCode = new Dictionary<int, int>();
    }
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
                && world is IServerWorldAccessor)
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

    public class BlockEntityAnimalCage : BlockEntity
    {
        public byte[] tmpCapturedEntityBytes;
        public string tmpCapturedEntityClass;
        public string tmpCapturedEntityShape;
        public int tmpCapturedEntityTextureId;
        public string tmpCapturedEntityName;
        public CagedEntityRenderer renderer;
        MeshData currentMesh;

        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            renderer = new CagedEntityRenderer(Api as ICoreClientAPI, tmpCapturedEntityName, tmpCapturedEntityTextureId, tmpCapturedEntityShape);
            currentMesh = renderer.genMesh();
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
            renderer = new CagedEntityRenderer(Api as ICoreClientAPI, tmpCapturedEntityName, tmpCapturedEntityTextureId, tmpCapturedEntityShape);
            currentMesh = renderer.genMesh();
            MarkDirty(true);
        }
        public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator)
        {
            mesher.AddMeshData(currentMesh);
            return false;
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

    public class CagedEntityRenderer : ITexPositionSource
    {
        private ICoreClientAPI capi;
        private string entityName;
        private int entityTextureId;
        private string entityShape;

        public CagedEntityRenderer(ICoreClientAPI capi, string entityName, int entityTextureId, string entityShape)
        {
            this.capi = capi;
            this.entityName = entityName;
            this.entityTextureId = entityTextureId;
            this.entityShape = entityShape;
        }

        public Size2i AtlasSize
        {
            get
            {
                if (capi != null)
                {
                    return capi.BlockTextureAtlas.Size;
                }
                return null;
            }
        }
        public TextureAtlasPosition this[string textureCode]
        {
            get
            {
                if (capi != null)
                {
                    CapturedEntityTextures textures;
                    BlockCage.EntitiyTextureIds(capi).TryGetValue(entityName, out textures);
                    int position = textures.TextureSubIdsByCode[entityTextureId];
                    return capi.BlockTextureAtlas.Positions[position];
                }

                return null;
            }
        }

        public MeshData genMesh()
        {
            MeshData currentMesh = null;
            if (capi != null && entityShape != null)
            {
                Shape shape = capi.Assets.TryGet(new AssetLocation(entityShape)).ToObject<Shape>();
                capi.Tesselator.TesselateShapeWithJointIds("aimalcage", shape, out currentMesh, this, new Vec3f());
                ModelTransform transform = ModelTransform.NoTransform;
                float scale = CageConfig.Current.getScale(entityName);
                transform.Scale = scale;
                currentMesh.ModelTransform(transform);
                currentMesh.Translate(0f, 0.0625f - (1 - scale) / 2, 0f);
            }
            return currentMesh;
        }
    }

    public class CageConfig
    {
        public static CageConfig Current { get; set; }

        public List<CatchableEntity> smallCatchableEntities;

        public static CageConfig getDefault()
        {
            CageConfig defaultConfig = new CageConfig();
            CatchableEntity[] smallEntities = { new CatchableEntity("sheep-bighorn-lamb", 0.85f), new CatchableEntity("deer-fawn", 0.85f), new CatchableEntity("chicken-baby", 1f), new CatchableEntity("chicken-hen", 1f), new CatchableEntity("chicken-henpoult", 1f), new CatchableEntity("chicken-rooster", 1f), new CatchableEntity("chicken-roosterpoult", 1f), new CatchableEntity("hare-baby", 1f), new CatchableEntity("pig-wild-piglet", 1f), new CatchableEntity("wolf-pup", 1f), new CatchableEntity("raccoon-male", 1f), new CatchableEntity("raccoon-female", 1f), new CatchableEntity("raccoon-pub", 1f), new CatchableEntity("hyena-pup", 1f),
                              new CatchableEntity("hare-female-arctic", 1f), new CatchableEntity("hare-female-ashgrey", 1f), new CatchableEntity("hare-female-darkbrown", 1f), new CatchableEntity("hare-female-desert", 1f), new CatchableEntity("hare-female-gold", 1f), new CatchableEntity("hare-female-lightbrown", 1f), new CatchableEntity("hare-female-lightgrey", 1f), new CatchableEntity("hare-female-silver", 1f), new CatchableEntity("hare-female-smokegrey", 1f),
                              new CatchableEntity("hare-male-arctic", 1f), new CatchableEntity("hare-male-ashgrey", 1f), new CatchableEntity("hare-male-darkbrown", 1f), new CatchableEntity("hare-male-desert", 1f), new CatchableEntity("hare-male-gold", 1f), new CatchableEntity("hare-male-lightbrown", 1f), new CatchableEntity("hare-male-lightgrey", 1f), new CatchableEntity("hare-male-silver", 1f), new CatchableEntity("hare-male-smokegrey", 1f)};

            defaultConfig.smallCatchableEntities = new List<CatchableEntity>(smallEntities);
            return defaultConfig;
        }

        public float getScale(string entity)
        {
            var find = smallCatchableEntities.Find(x => x.name == entity);
            if (find != null) { return find.scale; }
            return 1f;

        }
        public class CatchableEntity
        {
            public string name;
            public float scale;

            public CatchableEntity(string name, float scale)
            {
                this.name = name;
                this.scale = scale;
            }
        }
    }
}
