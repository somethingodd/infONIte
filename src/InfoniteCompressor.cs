using System.Collections.Generic;
using System.Linq;
using KSerialization;
using UnityEngine;

namespace Infonite {
    public class InfoniteCompressor : KMonoBehaviour, ISim1000ms, IUserControlledCapacity {
        private static readonly List<Tag> SingleUnitTags = new List<Tag>();

        private static readonly List<Tag> FastMassTags = new List<Tag>();
        [Serialize] public string lockerName = string.Empty;
        protected FilteredStorage filteredStorage;
        [MyCmpGet] private Storage storage;

        [MyCmpReq] private TreeFilterable treeFilterable;

        [Serialize] private float userMaxCapacity = float.PositiveInfinity;

        static InfoniteCompressor() {
            SingleUnitTags.Add(GameTags.Artifact);
            SingleUnitTags.Add(GameTags.Clothes);
            SingleUnitTags.AddRange(GameTags.AllClothesTags);
            SingleUnitTags.Add(GameTags.Egg);
            SingleUnitTags.Add(GameTags.IncubatableEgg);
            SingleUnitTags.Add(GameTags.Medicine);
            SingleUnitTags.Add(GameTags.Seed);
            SingleUnitTags.Add(GameTags.CropSeed);
            SingleUnitTags.Add(GameTags.DecorSeed);
            SingleUnitTags.Add(GameTags.WaterSeed);
            SingleUnitTags.Add(GameTags.Suit);
            SingleUnitTags.AddRange(GameTags.AllSuitTags);
        }

        public void Sim1000ms(float dt) {
            var toRemove = storage.items.Where(item => !treeFilterable.AcceptedTags.Any(item.HasTag));
            foreach (var removable in toRemove) {
                storage.Remove(removable);
            }

            if (AmountStored < UserMaxCapacity || AmountStored - UserMaxCapacity > 20) {
                var tags = treeFilterable.AcceptedTags;
                var num = UserMaxCapacity / tags.Count;
                foreach (var tagToAdd in tags.Where(tagToAdd => storage.GetAmountAvailable(tagToAdd) == 0)) {
                    AddTagPrefab(tagToAdd);
                }

                foreach (var aTag in tags) {
                    if (TagIsSingular(aTag)) {
                        var available = storage.GetAmountAvailable(aTag);
                        if (available < num) {
                            AddTagPrefab(aTag);
                        }
                    } else {
                        foreach (var item in storage.items.Where(item => item.HasTag(aTag))) {
                            var element = item.GetComponent<PrimaryElement>();
                            if (element != null && element.Mass < num) {
                                element.Mass = num;
                            }
                        }
                    }
                }
            }
        }

        public virtual float UserMaxCapacity {
            get => Mathf.Min(userMaxCapacity, GetComponent<Storage>().capacityKg);
            set {
                userMaxCapacity = value;
                filteredStorage.FilterChanged();
            }
        }

        public float AmountStored => GetComponent<Storage>().MassStored();

        public float MinCapacity => 0.0f;

        public float MaxCapacity => GetComponent<Storage>().capacityKg;

        public bool WholeValues => false;

        public LocString CapacityUnits => GameUtil.GetCurrentMassUnit();

        private static bool TagIsSingular(Tag t) {
            var p = t.Prefab();
            foreach (var singleUnitTag in SingleUnitTags.Where(singleUnitTag => p.HasTag(singleUnitTag))) {
                Debug.Log("Found a singular (measured by units, not kg) tag: " + singleUnitTag);
                return true;
            }

            return false;
        }

        private void AddTagPrefab(Tag tag) {
            var go = Instantiate(tag.Prefab());
            go.SetActive(true);
            storage.Store(go, do_disease_transfer: false, is_deserializing: true);
        }

        protected override void OnSpawn() {
            if (InfoniteConfig.Instance.Discover) {
                foreach (var unitCategory in GameTags.UnitCategories) {
                    foreach (var go in Assets.GetPrefabsWithTag(unitCategory)) {
                        var goTag = unitCategory != GameTags.Compostable
                            ? (Tag) go.name
                            : (Tag) go.tag;
                        if (unitCategory == GameTags.Compostable) {
                            if (!go.CompareTag("Untagged")) {
                                WorldInventory.Instance.Discover(goTag, GameTags.Seed);
                            }
                        } else {
                            WorldInventory.Instance.Discover(goTag, unitCategory);
                        }
                    }
                }
            }

            filteredStorage.FilterChanged();
            if (lockerName.IsNullOrWhiteSpace()) {
                return;
            }

            SetName(lockerName);
        }

        protected override void OnPrefabInit() {
            Initialize(false);
        }

        protected void Initialize(bool use_logic_meter) {
            base.OnPrefabInit();
            filteredStorage = new FilteredStorage(this, null, null,
                this, use_logic_meter, Db.Get().ChoreTypes.StorageFetch);
        }

        protected override void OnCleanUp() {
            filteredStorage.CleanUp();
        }

        private void OnCopySettings(object data) {
            if (data == null) {
                return;
            }

            var component = ((GameObject) data).GetComponent<InfoniteCompressor>();
            if (component == null) {
                return;
            }

            UserMaxCapacity = component.UserMaxCapacity;
        }

        public void SetName(string name) {
            var component = GetComponent<KSelectable>();
            this.name = name;
            lockerName = name;
            if (component != null) {
                component.SetName(name);
            }

            gameObject.name = name;
            NameDisplayScreen.Instance.UpdateName(gameObject);
        }
    }
}