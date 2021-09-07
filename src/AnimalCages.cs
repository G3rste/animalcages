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

    public class CageConfig
    {
        public static CageConfig Current { get; set; }

        public List<CatchableEntity> smallCatchableEntities { get; set; }

        public List<CatchableEntity> mediumCatchableEntities { get; set; }

        public int woundedMultiplicator { get; set; }

        public static CageConfig getDefault()
        {
            CageConfig defaultConfig = new CageConfig();
            CatchableEntity[] smallEntities = { new CatchableEntity("sheep-bighorn-lamb", 0.85f), new CatchableEntity("deer-fawn", 0.85f), new CatchableEntity("chicken-baby", 1f), new CatchableEntity("chicken-hen", 1f), new CatchableEntity("chicken-henpoult", 1f), new CatchableEntity("chicken-rooster", 1f), new CatchableEntity("chicken-roosterpoult", 1f), new CatchableEntity("hare-baby", 1f), new CatchableEntity("pig-wild-piglet", 1f), new CatchableEntity("wolf-pup", 1f), new CatchableEntity("fox-pup-forest", 1f), new CatchableEntity("fox-pup-arctic", 1f), new CatchableEntity("aurochs-lamb", 1f), new CatchableEntity("raccoon-male", 1f), new CatchableEntity("raccoon-female", 1f), new CatchableEntity("raccoon-pub", 1f), new CatchableEntity("hyena-pup", 1f),
                              new CatchableEntity("hare-female-arctic", 1f), new CatchableEntity("hare-female-ashgrey", 1f), new CatchableEntity("hare-female-darkbrown", 1f), new CatchableEntity("hare-female-desert", 1f), new CatchableEntity("hare-female-gold", 1f), new CatchableEntity("hare-female-lightbrown", 1f), new CatchableEntity("hare-female-lightgrey", 1f), new CatchableEntity("hare-female-silver", 1f), new CatchableEntity("hare-female-smokegrey", 1f),
                              new CatchableEntity("hare-male-arctic", 1f), new CatchableEntity("hare-male-ashgrey", 1f), new CatchableEntity("hare-male-darkbrown", 1f), new CatchableEntity("hare-male-desert", 1f), new CatchableEntity("hare-male-gold", 1f), new CatchableEntity("hare-male-lightbrown", 1f), new CatchableEntity("hare-male-lightgrey", 1f), new CatchableEntity("hare-male-silver", 1f), new CatchableEntity("hare-male-smokegrey", 1f)};

            CatchableEntity[] mediumEntities = { new CatchableEntity("wolf-male", 0.9f), new CatchableEntity("wolf-female", 0.9f), new CatchableEntity("pig-wild-male", 1f), new CatchableEntity("pig-wild-female", 1f), new CatchableEntity("sheep-bighorn-male", 0.9f), new CatchableEntity("sheep-bighorn-female", 1f), new CatchableEntity("hyena-male", 0.9f), new CatchableEntity("hyena-female", 0.9f), new CatchableEntity("fox-male", 1f), new CatchableEntity("fox-female", 1f), new CatchableEntity("fox-arctic-male", 1f), new CatchableEntity("fox-arctic-female", 1f)
            , new CatchableEntity("aurochs-female", 0.75f), new CatchableEntity("aurochs-male", 0.75f), new CatchableEntity("deer-female", 0.9f), new CatchableEntity("deer-male", 0.8f), new CatchableEntity("white-hart", 0.8f)};

            defaultConfig.smallCatchableEntities = new List<CatchableEntity>(smallEntities);
            defaultConfig.mediumCatchableEntities = new List<CatchableEntity>(mediumEntities);
            defaultConfig.woundedMultiplicator = 3;
            return defaultConfig;
        }

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
