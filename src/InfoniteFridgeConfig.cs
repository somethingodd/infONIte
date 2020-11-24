using KSerialization;
using TUNING;
using UnityEngine;
using BUILDINGS = TUNING.BUILDINGS;

namespace Infonite {
    [SerializationConfig(MemberSerialization.OptIn)]
    public class InfoniteFridgeConfig : IBuildingConfig {
        public const string Id = nameof(STRINGS.BUILDINGS.PREFABS.INFONITEFRIDGE);
        public static readonly string Type = PlanStrings.Food;

        public override BuildingDef CreateBuildingDef() {
            const int width = 1;
            const int height = 2;
            const string anim = "fridge_kanim";
            const int hitpoints = 1000;
            const float constructionTime = 3f;
            var constructionMass = BUILDINGS.CONSTRUCTION_MASS_KG.TIER0;
            var rawMinerals = MATERIALS.RAW_MINERALS;
            const float meltingPoint = 800f;
            const BuildLocationRule buildLocationRule = BuildLocationRule.Anywhere;
            var noise = NOISE_POLLUTION.NONE;
            var buildingDef = BuildingTemplates.CreateBuildingDef(Id, width, height, anim, hitpoints, constructionTime,
                constructionMass, rawMinerals, meltingPoint, buildLocationRule,
                BUILDINGS.DECOR.BONUS.TIER5, noise);
            buildingDef.Floodable = false;
            buildingDef.AudioCategory = "Metal";
            buildingDef.Overheatable = false;
            buildingDef.PermittedRotations = PermittedRotations.R360;
            SoundEventVolumeCache.instance.AddVolume("fridge_kanim", "Refrigerator_open", NOISE_POLLUTION.NOISY.TIER1);
            SoundEventVolumeCache.instance.AddVolume("fridge_kanim", "Refrigerator_close", NOISE_POLLUTION.NOISY.TIER1);
            return buildingDef;
        }

        public override void ConfigureBuildingTemplate(GameObject go, Tag prefab_tag) {
            go.AddOrGet<InfoniteFridge>();
        }

        public override void DoPostConfigureComplete(GameObject go) {
            var storage = go.AddOrGet<Storage>();
            storage.showInUI = true;
            storage.showDescriptor = true;
            storage.storageFilters = STORAGEFILTERS.FOOD;
            storage.allowItemRemoval = true;
            storage.capacityKg = 100f;
            storage.storageFullMargin = STORAGE.STORAGE_LOCKER_FILLED_MARGIN;
            storage.fetchCategory = Storage.FetchCategory.GeneralStorage;
            Prioritizable.AddRef(go);
            go.AddOrGet<TreeFilterable>();
            go.AddOrGet<StorageLocker>();
            go.AddOrGet<InfoniteFridge>();
            go.AddOrGet<DropAllWorkable>();
            go.AddOrGetDef<StorageController.Def>();
        }
    }
}