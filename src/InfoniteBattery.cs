using Harmony;
using UnityEngine;

namespace Infonite {
    [HarmonyPatch(typeof(GeneratedBuildings), "LoadGeneratedBuildings")]
    internal class InfoniteBattery {
        [HarmonyPatch(typeof(Battery), "ConsumeEnergy", typeof(float), typeof(bool))]
        internal class ConsumeEnergy {
            private static bool Prefix(Component __instance) {
                return __instance.gameObject.GetComponent<KPrefabID>().PrefabTag != InfoniteBatteryConfig.Id;
            }
        }

        [HarmonyPatch(typeof(Battery), "OnSpawn")]
        internal class OnSpawn {
            private static void Prefix(Component __instance) {
                if (__instance.gameObject.GetComponent<KPrefabID>().PrefabTag == InfoniteBatteryConfig.Id) {
                    AccessTools.Field(typeof(Battery), "joulesAvailable").SetValue(__instance, 40000f);
                }
            }
        }
    }
}