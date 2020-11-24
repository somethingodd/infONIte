using KSerialization;
using TUNING;
using UnityEngine;

namespace Infonite {
    [SerializationConfig(MemberSerialization.OptIn)]
    public class InfoniteCompressorConfig : IBuildingConfig {
        public const string Id = nameof(STRINGS.BUILDINGS.PREFABS.INFONITECOMPRESSOR);
        public const string Type = PlanStrings.Base;

        public override BuildingDef CreateBuildingDef() {
            var buildingDef = BuildingTemplates.CreateBuildingDef(Id, 1, 2, "storagelocker_kanim", 1000, 3f,
                BUILDINGS.CONSTRUCTION_MASS_KG.TIER0, MATERIALS.RAW_MINERALS, 800f, BuildLocationRule.Anywhere,
                BUILDINGS.DECOR.BONUS.TIER5, NOISE_POLLUTION.NONE);
            buildingDef.Floodable = false;
            buildingDef.AudioCategory = "Metal";
            buildingDef.Overheatable = false;
            buildingDef.PermittedRotations = PermittedRotations.R360;
            return buildingDef;
        }

        public override void ConfigureBuildingTemplate(GameObject go, Tag prefab_tag) {
            SoundEventVolumeCache.instance.AddVolume("storagelocker_kanim", "StorageLocker_Hit_metallic_low",
                NOISE_POLLUTION.NOISY.TIER1);
            Prioritizable.AddRef(go);
            var storage = go.AddOrGet<Storage>();
            storage.showInUI = true;
            storage.allowItemRemoval = true;
            storage.showDescriptor = true;
            storage.storageFilters = STORAGEFILTERS.NOT_EDIBLE_SOLIDS;
            storage.storageFullMargin = STORAGE.STORAGE_LOCKER_FILLED_MARGIN;
            storage.fetchCategory = Storage.FetchCategory.GeneralStorage;
            go.AddOrGet<CopyBuildingSettings>().copyGroupTag = GameTags.StorageLocker;
            go.AddOrGet<InfoniteCompressor>();
        }

        public override void DoPostConfigureComplete(GameObject go) {
            go.AddOrGetDef<StorageController.Def>();
        }
    }
}