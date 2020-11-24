using System;
using System.IO;
using System.Linq;
using Database;
using Harmony;
using KSerialization;
using PeterHan.PLib;
using PeterHan.PLib.Options;
using UnityEngine;

namespace Infonite {
    public class Infonite {
        [HarmonyPatch(typeof(Localization), "Initialize")]
        public class L10nPatch {
            public static void Postfix() {
                L10n.Translate(typeof(STRINGS));
            }
        }

        [HarmonyPatch(typeof(GeneratedBuildings), "LoadGeneratedBuildings")]
        public class BuildingsPatch {
            private static void Prefix() {
                ModUtil.AddBuildingToPlanScreen(InfoniteBatteryConfig.Type, InfoniteBatteryConfig.Id);
                ModUtil.AddBuildingToPlanScreen(InfoniteCompressorConfig.Type, InfoniteCompressorConfig.Id);
                ModUtil.AddBuildingToPlanScreen(InfoniteCondenserConfig.Type, InfoniteCondenserConfig.Id);
                ModUtil.AddBuildingToPlanScreen(InfoniteDrainConfig.Type, InfoniteDrainConfig.Id);
                ModUtil.AddBuildingToPlanScreen(InfoniteFridgeConfig.Type, InfoniteFridgeConfig.Id);
                ModUtil.AddBuildingToPlanScreen(InfoniteVaporatorConfig.Type, InfoniteVaporatorConfig.Id);
                ModUtil.AddBuildingToPlanScreen(InfoniteVentConfig.Type, InfoniteVentConfig.Id);
            }
        }

        [HarmonyPatch(typeof(FilterSideScreen), "IsValidForTarget")]
        public class FlowPatch {
            private static bool Prefix(GameObject target, FilterSideScreen __instance, ref bool __result) {
                if (target.GetComponent<InfoniteFlowControl>() == null) {
                    return true;
                }

                __result = !__instance.isLogicFilter;
                return false;
            }
        }

        [HarmonyPatch(typeof(Game), "")]
        public class GamePath {
            public void nothingtest() {
            }

        }

        [HarmonyPatch(typeof(Db), "Initialize")]
        public class ResearchPatch {
            public static void OnLoad() {
                PUtil.InitLibrary();
                POptions.RegisterOptions(typeof(InfoniteConfig));
            }

            private static void Prefix() {
                var gasPipingTechs = Techs.TECH_GROUPING["GasPiping"]
                    .AddToArray(InfoniteVaporatorConfig.Id)
                    .AddToArray(InfoniteVentConfig.Id);
                Techs.TECH_GROUPING["GasPiping"] = gasPipingTechs;
                var liquidPipingTechs = Techs.TECH_GROUPING["LiquidPiping"]
                    .AddToArray(InfoniteCondenserConfig.Id)
                    .AddToArray(InfoniteDrainConfig.Id);
                Techs.TECH_GROUPING["LiquidPiping"] = liquidPipingTechs;
            }
        }

        [HarmonyPatch(typeof(Manager), "GetType", typeof(string))]
        public static class InfinitePipesSerializationPatch {
            public static void Postfix(string type_name, ref Type __result) {
                if (type_name == "InfoniteSink") {
                    __result = typeof(InfoniteSink);
                } else {
                    if (type_name != "InfoniteSource") {
                        return;
                    }

                    __result = typeof(InfoniteSource);
                }
            }
        }
    }
}