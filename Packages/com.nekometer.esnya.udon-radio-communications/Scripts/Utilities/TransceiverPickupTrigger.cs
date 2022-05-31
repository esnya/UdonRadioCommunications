using UdonSharp;
using UnityEngine;
using VRC.SDK3.Components;

namespace URC
{
    [RequireComponent(typeof(VRCPickup))]
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class TransceiverPickupTrigger : UdonSharpBehaviour
    {
        public Transceiver trasnceiver;
        public GameObject pickupOnly;
        public bool keepPosition;

        private Vector3 initialPosition;

        private void Start()
        {
            initialPosition = transform.localPosition;
            OnDrop();
        }

        public override void OnPickupUseDown()
        {
            if (trasnceiver) trasnceiver._StartTransmit();
        }
        public override void OnPickupUseUp()
        {
            if (trasnceiver) trasnceiver._StopTransmit();
        }

        public override void OnPickup()
        {
            if (pickupOnly) pickupOnly.SetActive(true);
        }
        public override void OnDrop()
        {
            if (pickupOnly) pickupOnly.SetActive(false);
            if (keepPosition) transform.localPosition = initialPosition;
        }
    }
}
