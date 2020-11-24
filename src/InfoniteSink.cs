using UnityEngine;

namespace Infonite {
    public class InfoniteSink : KMonoBehaviour {
        private readonly Operational.Flag incomingFlag =
            new Operational.Flag("incoming", Operational.Flag.Type.Requirement);

        private HandleVector<int>.Handle accumulator = HandleVector<int>.InvalidHandle;

        private int inputCell;
        [SerializeField] public ConduitType Type;

        protected override void OnPrefabInit() {
            base.OnPrefabInit();
            accumulator = Game.Instance.accumulators.Add("Sink", this);
        }

        protected override void OnSpawn() {
            base.OnSpawn();
            inputCell = GetComponent<Building>().GetUtilityInputCell();
            Conduit.GetFlowManager(Type).AddConduitUpdater(ConduitUpdate);
        }

        protected override void OnCleanUp() {
            Conduit.GetFlowManager(Type).RemoveConduitUpdater(ConduitUpdate);
            Game.Instance.accumulators.Remove(accumulator);
            base.OnCleanUp();
        }

        private void ConduitUpdate(float dt) {
            var flowManager = Conduit.GetFlowManager(Type);
            if (flowManager == null || !flowManager.HasConduit(inputCell)) {
                GetComponent<Operational>().SetFlag(incomingFlag, false);
            } else {
                var contents = flowManager.GetContents(inputCell);
                var operational = GetComponent<Operational>();
                operational.SetFlag(incomingFlag, contents.mass > 0.0);
                if (!operational.IsOperational) {
                    return;
                }

                flowManager.RemoveElement(inputCell, contents.mass);
                Game.Instance.accumulators.Accumulate(accumulator, contents.mass);
            }
        }
    }
}