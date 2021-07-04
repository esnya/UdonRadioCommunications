using UdonSharp;
using UnityEngine;
using VRC.Udon;
using VRC.SDKBase;

namespace UdonRadioCommunication
{

    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class SFRelay : UdonSharpBehaviour
    {

#if URC_SF
        public Transceiver transceiver;
        public PilotSeat relayTarget;
        public UdonSharpBehaviour[] onEnterEventTargets = {};
        public string[] onEnterEventNames = {};
        public UdonSharpBehaviour[] onLeaveEventTargets = {};
        public string[] onLeaveEventNames = {};

        private EngineController engineController;
        private bool seated;

        private void Start()
        {
            engineController = relayTarget.EngineControl;
        }

        private void SendEvents(UdonSharpBehaviour[] targets, string[] names)
        {
            for (int i = 0; i < targets.Length; i++)
            {
                var target = targets[i];
                if (target == null) continue;
                target.SendCustomEvent(names[i]);
            }
        }

        private void StationExited()
        {
            seated = false;
            transceiver.StopTalking();
            transceiver.Deactivate();
            SendEvents(onLeaveEventTargets, onLeaveEventNames);
        }

        private void Update()
        {
            if (!seated) return;
            if (!engineController.Piloting) StationExited();
        }

        public override void Interact()
        {
            if (engineController.Piloting) return;

            seated = true;

            transceiver.exclusive = false;
            transceiver.receiver.sync = false;
            transceiver.Activate();
            transceiver.StartTalking();
            SendEvents(onEnterEventTargets, onEnterEventNames);

            relayTarget.SendCustomEvent("_interact");
        }
#endif
    }
}
