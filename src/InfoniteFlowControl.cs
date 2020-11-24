using Harmony;
using STRINGS;

namespace Infonite {
    public class InfoniteFlowControl : KMonoBehaviour, IIntSliderControl {
        public string SliderTitleKey => nameof(STRINGS.UI.UISIDESCREENS.INFONITEFLOW.FLOW.TITLE);

        public string SliderUnits => UI.UNITSUFFIXES.MASS.GRAM + "/" + UI.UNITSUFFIXES.SECOND;

        public float GetSliderMax(int index) {
            return Traverse.Create(Conduit.GetFlowManager(GetComponent<InfoniteSource>().Type)).Field("MaxMass")
                .GetValue<float>() * 1000f;
        }

        public float GetSliderMin(int index) {
            return 0;
        }

        public string GetSliderTooltipKey(int index) {
            return nameof(STRINGS.UI.UISIDESCREENS.INFONITEFLOW.FLOW.TOOLTIP);
        }

        public string GetSliderTooltip() {
            return STRINGS.UI.UISIDESCREENS.INFONITEFLOW.FLOW.TOOLTIP;
        }

        public float GetSliderValue(int index) {
            return GetComponent<InfoniteSource>().Flow;
        }

        public void SetSliderValue(float percent, int index) {
            GetComponent<InfoniteSource>().Flow = percent;
        }

        public int SliderDecimalPlaces(int index) {
            return 0;
        }

        protected override void OnSpawn() {
            if (InfoniteConfig.Instance.Discover) {
                foreach (var unitCategory in GameTags.LiquidElements) {
                    foreach (var go in Assets.GetPrefabsWithTag(unitCategory)) {
                        WorldInventory.Instance.Discover(go.tag, unitCategory);
                    }
                }

                foreach (var unitCategory in GameTags.GasElements) {
                    foreach (var go in Assets.GetPrefabsWithTag(unitCategory)) {
                        WorldInventory.Instance.Discover(go.tag, unitCategory);
                    }
                }
            }
        }
    }
}