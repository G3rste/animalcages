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
                var Config = api.LoadModConfig<CatchableEntities>("animalcagesconfig.json");
                if (Config != null)
                {
                    CageConfig.Current = Config;
                }
            }
            catch
            {
                api.Logger.Error("Failed to load custom mod configuration, falling back to default settings!");
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
        public static Dictionary<string, CapturedEntityTextures> ToolTextureSubIds(ICoreAPI api)
        {
            Dictionary<string, CapturedEntityTextures> toolTextureSubIds;
            object obj;

            if (api.ObjectCache.TryGetValue("entityTextureSubIds", out obj))
            {
                toolTextureSubIds = obj as Dictionary<string, CapturedEntityTextures>;
            }
            else
            {
                api.ObjectCache["entityTextureSubIds"] = toolTextureSubIds = new Dictionary<string, CapturedEntityTextures>();
            }

            return toolTextureSubIds;
        }
        public override void OnBeforeRender(ICoreClientAPI capi, ItemStack itemstack, EnumItemRenderTarget target, ref ItemRenderInfo renderinfo)
        {
            base.OnBeforeRender(capi, itemstack, target, ref renderinfo);
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
                && CageConfig.Current.catchableEntities.Contains(attackedEntity.Properties.Code.GetName())
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



                    ToolTextureSubIds(api)[item.Code.GetName()] = tt;
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
                    BlockCage.ToolTextureSubIds(capi).TryGetValue(entityName, out textures);
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
        public static CatchableEntities Current = CatchableEntities.getDefault();
    }

    public class CatchableEntities
    {
        public List<string> catchableEntities;
        public List<float> catchableEntityScales;

        public static CatchableEntities getDefault()
        {
            CatchableEntities defaultConfig = new CatchableEntities();
            string[] entities = { "sheep-bighorn-lamb", "deer-fawn", "chicken-baby", "chicken-hen", "chicken-henpoult", "chicken-rooster", "chicken-roosterpoult", "hare-baby", "pig-wild-piglet", "wolf-pup", "raccoon-male", "raccoon-female", "raccoon-pub", "hyena-pup",
                              "hare-female-arctic", "hare-female-ashgrey", "hare-female-darkbrown", "hare-female-desert", "hare-female-gold", "hare-female-lightbrown", "hare-female-lightgrey", "hare-female-silver", "hare-female-smokegrey",
                              "hare-male-arctic", "hare-male-ashgrey", "hare-male-darkbrown", "hare-male-desert", "hare-male-gold", "hare-male-lightbrown", "hare-male-lightgrey", "hare-male-silver", "hare-male-smokegrey"};
            float[] scales = { 0.85f, 0.85f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f,
                              1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f,
                              1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f};
            defaultConfig.catchableEntities = new List<string>(entities);
            defaultConfig.catchableEntityScales = new List<float>(scales);
            return defaultConfig;
        }

        public float getScale(string entity)
        {
            if (catchableEntities.IndexOf(entity) != -1)
            {
                return catchableEntityScales[catchableEntities.IndexOf(entity)];
            }
            return 1f;
        }
    }
}
