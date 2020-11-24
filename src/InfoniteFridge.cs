using System.Collections.Generic;
using System.Linq;
using KSerialization;
using PeterHan.PLib;
using TUNING;
using UnityEngine;

namespace Infonite {
    public class InfoniteFridge : KMonoBehaviour, IUserControlledCapacity, IGameObjectEffectDescriptor, ISim1000ms,
        ISim4000ms {
        [SerializeField] private const float simulatedInternalTemperature = 274.26f;

        [SerializeField] private const float simulatedInternalHeatCapacity = 4000f;

        [SerializeField] private const float simulatedThermalConductivity = 10000f;

        [Serialize] private float _userMaxCapacity = float.PositiveInfinity;
        [MyCmpGet] internal KBatchedAnimController controller;
        private FilteredStorage filteredStorage;
        [MyCmpGet] private Operational operational;
        [MyCmpGet] private LogicPorts ports;
        [MyCmpGet] public Refrigerator refs;
        [MyCmpGet] private Storage storage;

        private SimulatedTemperatureAdjuster temperatureAdjuster;

        [MyCmpReq] private TreeFilterable treeFilterable;

        private bool IsOperational => GetComponent<Operational>().IsOperational;


        public List<Descriptor> GetDescriptors(GameObject go) {
            return SimulatedTemperatureAdjuster.GetDescriptors(simulatedInternalTemperature);
        }

        public void Sim4000ms(float dt) {
            if (InfoniteConfig.Instance.Refresherator) {
                foreach (var rot in storage.items.Select(item => item.GetSMI<Rottable.Instance>())) {
                    if (rot != null) {
                        rot.RotValue = rot.def.spoilTime;
                    }
                }
            }
        }

        public void Sim1000ms(float dt) {
            if (!IsOperational || AmountStored >= UserMaxCapacity) {
                return;
            }

            var acceptedTags = treeFilterable.AcceptedTags;
            var num = UserMaxCapacity / acceptedTags.Count;

            foreach (var acceptedTag in acceptedTags) {
                var alreadyStored = false;
                foreach (var item in storage.items.Where(item => item.HasTag(acceptedTag))) {
                    item.GetComponent<PrimaryElement>().Mass = num;
                    alreadyStored = true;
                    break;
                }

                if (!alreadyStored) {
                    var go = Instantiate(acceptedTag.Prefab());
                    go.SetActive(true);
                    var element = go.GetComponent<PrimaryElement>();
                    if (element != null) {
                        element.Mass = num;
                    }

                    storage.Store(go, do_disease_transfer: false, is_deserializing: true);
                }
            }
        }

        public float UserMaxCapacity {
            get => Mathf.Min(_userMaxCapacity, storage.capacityKg);
            set {
                _userMaxCapacity = value;
                filteredStorage.FilterChanged();
            }
        }

        public float AmountStored => storage.MassStored();

        public float MinCapacity => 0.0f;

        public float MaxCapacity => storage.capacityKg;

        public bool WholeValues => false;

        public LocString CapacityUnits => GameUtil.GetCurrentMassUnit();

        protected override void OnPrefabInit() {
            filteredStorage = new FilteredStorage(this, null, new[] {
                GameTags.Compostable
            }, this, true, Db.Get().ChoreTypes.FoodFetch);
        }

        protected override void OnSpawn() {
            if (InfoniteConfig.Instance.Discover) {
                foreach (var foodTypes in FOOD.FOOD_TYPES_LIST) {
                    var tag = foodTypes.Id.ToTag();
                    if (foodTypes.CaloriesPerUnit > 0.0) {
                        WorldInventory.Instance.Discover(tag, GameTags.Edible);
                    }

                    if (foodTypes.CaloriesPerUnit == 0.0) {
                        WorldInventory.Instance.Discover(tag, GameTags.CookingIngredient);
                    }
                }


                foreach (var tag in Assets.GetPrefabsWithTag(GameTags.Medicine).Select(go => go.PrefabID())
                    .Where(tag => tag != (Tag) "Untagged")) {
                    WorldInventory.Instance.Discover(tag, GameTags.Medicine);
                }
            }

            operational.SetActive(operational.IsOperational);
            GetComponent<KAnimControllerBase>().Play((HashedString) "off");
            filteredStorage.FilterChanged();
            temperatureAdjuster = new SimulatedTemperatureAdjuster(simulatedInternalTemperature,
                simulatedInternalHeatCapacity, simulatedThermalConductivity, GetComponent<Storage>());
        }

        private void OnOperationalChanged(object data) {
            operational.SetActive(operational.IsOperational);
        }

        protected override void OnCleanUp() {
            filteredStorage.CleanUp();
            temperatureAdjuster.CleanUp();
        }

        public bool IsActive() {
            return operational.IsActive;
        }

        private void OnCopySettings(object data) {
            if (data == null) {
                return;
            }

            var component = ((GameObject) data).GetComponent<InfoniteFridge>();
            if (component == null) {
                return;
            }

            UserMaxCapacity = component.UserMaxCapacity;
        }
    }
}