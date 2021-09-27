using System;
using UdonSharp;
using UnityEngine;

namespace UdonRadioCommunication
{

    [
        UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync),
        DefaultExecutionOrder(100), // After EngineController
    ]
    public class SFComInjector : UdonSharpBehaviour
    {
        public Transform inVehicleOnly, seatedPlayerOnly;

#if URC_SF
        private void Start()
        {
            InjectInVehicleOnly();
            InjectSeatedPlayerOnly();
        }

        private void InjectInVehicleOnly()
        {
            if (inVehicleOnly == null) return;

            var engineController = GetEngineController();
            if (engineController == null) return;

            var hudController = engineController.HUDControl;
            if (hudController == null) return;

            inVehicleOnly.SetParent(hudController.transform, true);
        }

        private void InjectSeatedPlayerOnly()
        {
            var leaveButton = GetLeaveButton();
            if (leaveButton == null || seatedPlayerOnly == null) return;
            seatedPlayerOnly.SetParent(leaveButton.transform, true);
        }

        private EngineController GetEngineController()
        {
            var pilotSeat = GetComponentInParent<PilotSeat>();
            if (pilotSeat != null) return pilotSeat.EngineControl;

            var passengerSeat = GetComponentInParent<PassengerSeat>();
            if (passengerSeat != null) return passengerSeat.EngineControl;

            return null;
        }

        private GameObject GetLeaveButton()
        {
            var pilotSeat = GetComponentInParent<PilotSeat>();
            if (pilotSeat != null) return pilotSeat.LeaveButton;

            var passengerSeat = GetComponentInParent<PassengerSeat>();
            if (passengerSeat != null) return passengerSeat.LeaveButton;

            return null;
        }
#endif
    }
}
