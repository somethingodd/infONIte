using System;
using System.Runtime.Serialization;
using KSerialization;
using STRINGS;
using UnityEngine;

namespace Infonite {
    [SerializationConfig(MemberSerialization.OptIn)]
    public class InfoniteSource : KMonoBehaviour, ISingleSliderControl {
        public const int MinAllowedTemperature = 1;
        public const int MaxAllowedTemperature = 7500;
        private static StatusItem filterStatusItem;

        private readonly Operational.Flag
            filterFlag = new Operational.Flag("filter", Operational.Flag.Type.Requirement);

        private HandleVector<int>.Handle accumulator = HandleVector<int>.InvalidHandle;
        private Filterable filterable;

        [Serialize] public Tag FilteredTag;

        [Serialize] public float Flow = 10000f;

        private bool inUpdate;
        private int outputCell = -1;

        [Serialize] public float Temp = 300f;

        [SerializeField] public ConduitType Type;

        public SimHashes FilteredElement { get; private set; } = SimHashes.Void;

        private bool IsValidFilter => FilteredTag != null && FilteredElement != SimHashes.Void &&
                                      FilteredElement != SimHashes.Vacuum;

        public string SliderTitleKey {
            get {
                switch (Type) {
                    case ConduitType.Gas:
                        return nameof(STRINGS.UI.UISIDESCREENS.INFONITEVAPORATOR.TITLE);
                    case ConduitType.Liquid:
                        return nameof(STRINGS.UI.UISIDESCREENS.INFONITECONDENSER.TITLE);
                    default:
                        return "STRINGS.UI.UISIDESCREENS.INVALIDCONDUITTYPE.TITLE";
                }
            }
        }

        public string SliderUnits => UI.UNITSUFFIXES.TEMPERATURE.KELVIN;

        public int SliderDecimalPlaces(int index) {
            return 1;
        }

        public float GetSliderMin(int index) {
            var element = ElementLoader.GetElement(FilteredTag);
            return element == null ? 0.0f : Math.Max(element.lowTemp, 1f);
        }

        public float GetSliderMax(int index) {
            var element = ElementLoader.GetElement(FilteredTag);
            return element == null ? 100f : Math.Min(element.highTemp, 7500f);
        }

        public float GetSliderValue(int index) {
            return Temp;
        }

        public void SetSliderValue(float percent, int index) {
            Temp = percent;
        }

        public string GetSliderTooltipKey(int index) {
            switch (Type) {
                case ConduitType.Gas:
                    return nameof(STRINGS.UI.UISIDESCREENS.INFONITEVAPORATOR.TOOLTIP);
                case ConduitType.Liquid:
                    return nameof(STRINGS.UI.UISIDESCREENS.INFONITECONDENSER.TOOLTIP);
                default:
                    throw new Exception("Invalid ConduitType provided to InfiniteSource: " + Type);
            }
        }

        public string GetSliderTooltip() {
            switch (Type) {
                case ConduitType.Gas:
                    return STRINGS.UI.UISIDESCREENS.INFONITEVAPORATOR.TOOLTIP;
                case ConduitType.Liquid:
                    return STRINGS.UI.UISIDESCREENS.INFONITECONDENSER.TOOLTIP;
                default:
                    throw new Exception("Invalid ConduitType provided to InfiniteSource: " + Type);
            }
        }

        protected override void OnPrefabInit() {
            base.OnPrefabInit();
            filterable = GetComponent<Filterable>();
            accumulator = Game.Instance.accumulators.Add("Source", this);
            InitializeStatusItems();
        }

        protected override void OnSpawn() {
            base.OnSpawn();
            outputCell = GetComponent<Building>().GetUtilityOutputCell();
            Conduit.GetFlowManager(Type).AddConduitUpdater(ConduitUpdate);
            OnFilterChanged(ElementLoader.FindElementByHash(FilteredElement).tag);
            filterable.onFilterChanged += OnFilterChanged;
            GetComponent<KSelectable>().SetStatusItem(Db.Get().StatusItemCategories.Main, filterStatusItem, this);
        }

        protected override void OnCleanUp() {
            Conduit.GetFlowManager(Type).RemoveConduitUpdater(ConduitUpdate);
            Game.Instance.accumulators.Remove(accumulator);
            base.OnCleanUp();
        }

        private void OnFilterChanged(Tag tag) {
            FilteredTag = tag;
            var element = ElementLoader.GetElement(FilteredTag);
            if (element != null) {
                FilteredElement = element.id;
            }

            GetComponent<KSelectable>()
                .ToggleStatusItem(Db.Get().BuildingStatusItems.NoFilterElementSelected, !IsValidFilter);
            GetComponent<Operational>().SetFlag(filterFlag, IsValidFilter);
            Temp = Math.Max(Temp, element.lowTemp);
            Temp = Math.Min(Temp, element.highTemp);
            Temp = Math.Max(Temp, 1f);
            Temp = Math.Min(Temp, 7500f);
            SetSliderValue(Temp, -1);
            if (DetailsScreen.Instance == null || inUpdate) {
                return;
            }

            inUpdate = true;
            try {
                DetailsScreen.Instance.Refresh(gameObject);
            } catch (Exception ex) {
                // ignored
            }

            inUpdate = false;
        }

        [OnDeserialized]
        private void OnDeserialized() {
            if (ElementLoader.GetElement(FilteredTag) == null) {
                return;
            }

            filterable.SelectedTag = FilteredTag;
            OnFilterChanged(FilteredTag);
        }

        private void InitializeStatusItems() {
            if (filterStatusItem != null) {
                return;
            }

            filterStatusItem = new StatusItem("Filter", "BUILDING", "",
                StatusItem.IconType.Info, NotificationType.Neutral, false, OverlayModes.LiquidConduits.ID);
            filterStatusItem.resolveStringCallback = (str, data) => {
                var infiniteSource = (InfoniteSource) data;
                if (infiniteSource.FilteredElement == SimHashes.Void) {
                    str = string.Format(BUILDINGS.PREFABS.GASFILTER.STATUS_ITEM,
                        BUILDINGS.PREFABS.GASFILTER.ELEMENT_NOT_SPECIFIED);
                } else {
                    var elementByHash = ElementLoader.FindElementByHash(infiniteSource.FilteredElement);
                    str = string.Format(BUILDINGS.PREFABS.GASFILTER.STATUS_ITEM, elementByHash.name);
                }

                return str;
            };
            filterStatusItem.conditionalOverlayCallback = ShowInUtilityOverlay;
        }

        private bool ShowInUtilityOverlay(HashedString mode, object data) {
            var flag = false;
            switch (Type) {
                case ConduitType.Gas:
                    flag = mode == OverlayModes.GasConduits.ID;
                    break;
                case ConduitType.Liquid:
                    flag = mode == OverlayModes.LiquidConduits.ID;
                    break;
            }

            return flag;
        }

        private void ConduitUpdate(float dt) {
            var flowManager = Conduit.GetFlowManager(Type);
            if (flowManager == null || !flowManager.HasConduit(outputCell) ||
                !GetComponent<Operational>().IsOperational) {
                return;
            }

            Game.Instance.accumulators.Accumulate(accumulator,
                flowManager.AddElement(outputCell, FilteredElement, Flow / 1000f, Temp, 0, 0));
        }
    }
}