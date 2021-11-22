using System;
using UdonSharp;
using UnityEngine;

namespace UdonRadioCommunication
{

    [
        UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync),
        DefaultExecutionOrder(100), // After SaccEntity
    ]
    public class SFComInjector : UdonSharpBehaviour
    {
        public Transform inVehicleOnly, seatedPlayerOnly;

#if URC_SF
        private void Start()
        {
            SendCustomEventDelayedSeconds(nameof(_Inject), 10);
        }

        public void _Inject()
        {
            InjectInVehicleOnly();
            InjectSeatedPlayerOnly();
        }

        private void InjectInVehicleOnly()
        {
            if (inVehicleOnly == null) return;

            var saccEntity = GetSaccEntity();
            if (saccEntity == null) return;

            var target = saccEntity.InVehicleOnly;
            if (target == null) return;

            inVehicleOnly.SetParent(target.transform, true);
        }

        private void InjectSeatedPlayerOnly()
        {
            var seatOnly = GetSeatOnly();
            if (seatOnly == null || seatedPlayerOnly == null) return;
            seatedPlayerOnly.SetParent(seatOnly.transform, true);
        }

        private SaccEntity GetSaccEntity()
        {
            return GetComponentInParent<SaccEntity>();
        }

        private GameObject GetSeatOnly()
        {
            var seat = GetComponentInParent<SaccVehicleSeat>();
            if (seat != null) return (GameObject)seat.GetProgramVariable("ThisSeatOnly");
            return null;
        }
#endif
    }
}
