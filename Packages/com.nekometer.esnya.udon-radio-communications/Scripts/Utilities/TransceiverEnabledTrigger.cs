using UdonSharp;
using UnityEngine;

namespace URC
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [DefaultExecutionOrder(1100)] // After Transceiver
    public class TransceiverEnabledTrigger : UdonSharpBehaviour
    {
        public Transceiver transceiver;
        public bool defaultReceive = true, defaultTransmit = true;

        private void OnEnable()
        {
            transceiver._SetReceive(defaultReceive);
            transceiver._SetTransmit(defaultTransmit);
        }

        private void OnDisable()
        {
            transceiver._Deactivate();
        }
    }
}
