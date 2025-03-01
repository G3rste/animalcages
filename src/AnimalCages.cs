using System;
using System.Collections.Generic;
using System.Linq;
using ProtoBuf;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

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

            api.Network.RegisterChannel("animalcagesnetwork").RegisterMessageType<CageConfig>();
        }

        public override void StartClientSide(ICoreClientAPI api)
        {
            base.StartClientSide(api);

            api.Network.GetChannel("animalcagesnetwork")
                .SetMessageHandler<CageConfig>(packet => CageConfig.Current = packet);
        }
        public override void StartServerSide(ICoreServerAPI api)
        {
            base.StartServerSide(api);
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
                CageConfig.Current.mustBeBelowHpInPercent = Math.Clamp(CageConfig.Current.mustBeBelowHpInPercent, 0f, 1f);
                if (CageConfig.Current.smallCatchableEntities == null)
                    CageConfig.Current.smallCatchableEntities = new List<CageConfig.CatchableEntity>();
                if (CageConfig.Current.mediumCatchableEntities == null)
                    CageConfig.Current.mediumCatchableEntities = new List<CageConfig.CatchableEntity>();
                api.StoreModConfig(CageConfig.Current, "animalcagesconfig.json");
            }

            api.Event.PlayerJoin += (byPlayer) =>
                api.Network
                    .GetChannel("animalcagesnetwork")
                    .SendPacket<CageConfig>(CageConfig.Current, new IServerPlayer[] { byPlayer });
        }
    }

    [ProtoContract(ImplicitFields = ImplicitFields.None)]
    public class CageConfig
    {
        public static CageConfig Current { get; set; }

        [ProtoMember(1)]
        public List<CatchableEntity> smallCatchableEntities { get; set; }

        [ProtoMember(2)]
        public List<CatchableEntity> mediumCatchableEntities { get; set; }

        [ProtoMember(3)]
        public float mustBeBelowHpInPercent { get; set; } = 0.5f;

        public float getScale(string entity)
        {
            var find = GetSmallCatchableEntity(entity);
            if (find != null) { return find.scale; }

            find = GetMediumCatchableEntity(entity);
            if (find != null) { return find.scale; }

            return 1f;
        }

        public CatchableEntity GetSmallCatchableEntity(string entity)
        {
            return smallCatchableEntities.Find((x) => matches(x.name, entity));
        }

        public CatchableEntity GetMediumCatchableEntity(string entity)
        {
            return mediumCatchableEntities.Find((x) => matches(x.name, entity));
        }

        private bool matches(string match, string word)
        {
            var matches = true;
            var parts = match.Split('*');
            var currentIndex = 0;
            for (int i = 0; i < parts.Length; i++)
            {
                var part = parts[i];
                currentIndex = word.IndexOf(part, currentIndex) + part.Length;
                matches &= currentIndex != -1;
                matches &= i != 0 || currentIndex == part.Length;
                matches &= i != parts.Length - 1 || currentIndex == word.Length || part.Length == 0;
            }
            return matches;
        }

        [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
        public class CatchableEntity
        {
            public string name { get; set; }
            public float scale { get; set; }
        }
    }
}
