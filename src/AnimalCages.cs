using System.Collections.Generic;
using Vintagestory.API.Common;

namespace Animalcages
{
    public class Animalcages : ModSystem
    {
        public override void Start(ICoreAPI api)
        {
            base.Start(api);
            api.RegisterBlockClass("blocksmallanimalcage", typeof(BlockSmallCage));
            api.RegisterBlockClass("blockmediumanimalcage", typeof(BlockMediumCage));
            api.RegisterBlockEntityClass("blockentitysmallanimalcage", typeof(BlockEntityAnimalCage));
            api.RegisterBlockEntityClass("blockentitymediumanimalcage", typeof(BlockEntityMediumAnimalCage));
            try
            {
                var Config = api.LoadModConfig<CageConfig>("animalcagesconfig.json");
                if (Config != null)
                {
                    api.Logger.Notification("Mod Config successfully loaded.");
                    CageConfig.Current = Config;
                }
                else
                {
                    api.Logger.Notification("No Mod Config specified. Falling back to default settings");
                    CageConfig.Current = api.Assets.Get<CageConfig>(new AssetLocation("animalcages", "config/defaultconfig.json"));
                }
            }
            catch
            {
                CageConfig.Current = api.Assets.Get<CageConfig>(new AssetLocation("animalcages", "config/defaultconfig.json"));
                api.Logger.Error("Failed to load custom mod configuration. Falling back to default settings!");
            }
            finally
            {
                if (CageConfig.Current.woundedMultiplicator <= 0)
                    CageConfig.Current.woundedMultiplicator = 3;
                if (CageConfig.Current.smallCatchableEntities == null)
                    CageConfig.Current.smallCatchableEntities = new List<CageConfig.CatchableEntity>();
                if (CageConfig.Current.mediumCatchableEntities == null)
                    CageConfig.Current.mediumCatchableEntities = new List<CageConfig.CatchableEntity>();
                api.StoreModConfig(CageConfig.Current, "animalcagesconfig.json");
            }
        }
    }

    public class CageConfig
    {
        public static CageConfig Current { get; set; }

        public List<CatchableEntity> smallCatchableEntities { get; set; }

        public List<CatchableEntity> mediumCatchableEntities { get; set; }

        public int woundedMultiplicator { get; set; }

        public float getScale(string entity)
        {
            var find = smallCatchableEntities.Find(x => x.name == entity);
            if (find != null) { return find.scale; }

            find = mediumCatchableEntities.Find(x => x.name == entity);
            if (find != null) { return find.scale; }

            return 1f;

        }
        public class CatchableEntity
        {
            public string name { get; set; }
            public float scale { get; set; }

            public CatchableEntity(string name, float scale)
            {
                this.name = name;
                this.scale = scale;
            }
        }
    }
}
