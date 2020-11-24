using TUNING;
using UnityEngine;

namespace Infonite {
    public class InfoniteCondenserConfig : IBuildingConfig {
        public const string Id = nameof(STRINGS.BUILDINGS.PREFABS.INFONITECONDENSER);
        public const string Type = PlanStrings.Plumbing;

        public override BuildingDef CreateBuildingDef() {
            var buildingDef = BuildingTemplates.CreateBuildingDef(Id, 1, 2, "liquid_source_kanim", 100,
                120f, BUILDINGS.CONSTRUCTION_MASS_KG.TIER4, MATERIALS.ALL_METALS, 800f, BuildLocationRule.Anywhere,
                BUILDINGS.DECOR.PENALTY.TIER1, NOISE_POLLUTION.NOISY.TIER4);
            buildingDef.OutputConduitType = ConduitType.Liquid;
            buildingDef.Floodable = false;
            buildingDef.ViewMode = OverlayModes.LiquidConduits.ID;
            buildingDef.AudioCategory = "Metal";
            buildingDef.PermittedRotations = PermittedRotations.R360;
            buildingDef.UtilityOutputOffset = new CellOffset(0, 0);
            buildingDef.LogicInputPorts = LogicOperationalController.CreateSingleInputPortList(new CellOffset(0, 0));
            return buildingDef;
        }

        public override void ConfigureBuildingTemplate(GameObject go, Tag prefab_tag) {
            BuildingConfigManager.Instance.IgnoreDefaultKComponent(typeof(RequiresFoundation), prefab_tag);
            go.AddOrGet<Filterable>().filterElementState = Filterable.ElementState.Liquid;
            go.AddOrGet<InfoniteFlowControl>();
            go.AddOrGet<InfoniteSource>().Type = ConduitType.Liquid;
        }

        public override void DoPostConfigureComplete(GameObject go) {
            go.AddOrGet<LogicOperationalController>();
            go.AddOrGet<Operational>();
            Object.DestroyImmediate(go.GetComponent<RequireInputs>());
            Object.DestroyImmediate(go.GetComponent<ConduitConsumer>());
            Object.DestroyImmediate(go.GetComponent<ConduitDispenser>());
            go.AddOrGetDef<OperationalController.Def>();
            BuildingTemplates.DoPostConfigure(go);
        }
    }
}