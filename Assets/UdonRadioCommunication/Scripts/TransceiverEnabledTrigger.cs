#pragma warning disable IDE0051
using UdonSharp;
using UnityEngine;

namespace UdonRadioCommunication
{
    [
        UdonBehaviourSyncMode(/*BehaviourSyncMode.None*/ BehaviourSyncMode.NoVariableSync),
        DefaultExecutionOrder(1100), // After Transceiver
    ]
    public class TransceiverEnabledTrigger : UdonSharpBehaviour
    {
        public Transceiver transceiver;

        private void OnEnable()
        {
            transceiver._Activate();
        }

        private void OnDisable()
        {
            transceiver._Deactivate();
        }
    }
}
