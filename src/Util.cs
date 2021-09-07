using System.Collections.Generic;
using System.IO;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;

namespace Animalcages
{
    public class CapturedEntityTextures
    {
        public Dictionary<int, int> TextureSubIdsByCode = new Dictionary<int, int>();
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

        public MeshData genMesh(float scale = 1f)
        {
            MeshData currentMesh = null;
            if (capi != null && entityShape != null)
            {
                Shape shape = capi.Assets.TryGet(new AssetLocation(entityShape)).ToObject<Shape>();
                capi.Tesselator.TesselateShapeWithJointIds("aimalcage", shape, out currentMesh, this, new Vec3f());
                ModelTransform transform = ModelTransform.NoTransform;
                scale *= CageConfig.Current.getScale(entityName);
                transform.Scale = scale;
                currentMesh.ModelTransform(transform);
                currentMesh.Translate(0f, 0.0625f - (1 - scale) / 2, 0f);
            }
            return currentMesh;
        }
    }
}
