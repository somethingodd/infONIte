using TUNING;
using UnityEngine;

namespace Infonite {
    public class InfoniteBatteryConfig : BaseBatteryConfig {
        public const string Id = nameof(STRINGS.BUILDINGS.PREFABS.INFONITEBATTERY);
        public const string Type = PlanStrings.Power;

        public override BuildingDef CreateBuildingDef() {
            var result = base.CreateBuildingDef(Id, 1, 2, 30, "batterysm_kanim",
                BUILDINGS.CONSTRUCTION_TIME_SECONDS.TIER0, BUILDINGS.CONSTRUCTION_MASS_KG.TIER0, MATERIALS.ALL_METALS,
                BUILDINGS.MELTING_POINT_KELVIN.TIER4, 0f, 0f, BUILDINGS.DECOR.PENALTY.TIER2,
                NOISE_POLLUTION.NOISY.TIER0);
            SoundEventVolumeCache.instance.AddVolume("batterysm_kanim", "Battery_sm_rattle",
                NOISE_POLLUTION.NOISY.TIER2);
            result.Floodable = false;
            result.Entombable = false;
            result.ContinuouslyCheckFoundation = false;
            result.PermittedRotations = PermittedRotations.R360;
            result.ObjectLayer = ObjectLayer.Wire;
            result.BuildLocationRule = BuildLocationRule.Conduit;
            return result;
        }


        public override void DoPostConfigureComplete(GameObject go) {
            var battery = go.AddOrGet<Battery>();
            battery.capacity = 40000f;
            battery.joulesLostPerSecond = battery.capacity * 0.05f / 600f;
            base.DoPostConfigureComplete(go);
        }
    }
}