/// Original Code Created by https://github.com/Sacchan-VRC
/// Edited by https://github.com/esnya

using UnityEngine;
using UdonSharp;
using VRC.SDKBase;

namespace UdonRadioCommunication
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync), DefaultExecutionOrder(100)]
    public class URCPilotSeat : UdonSharpBehaviour
    {
        public Transceiver transceiver;
        public PilotSeat originalPilotSeat;
        public UdonSharpBehaviour[] onEnterEventTargets = {};
        public string[] onEnterEventNames = {};
        public UdonSharpBehaviour[] onLeaveEventTargets = {};
        public string[] onLeaveEventNames = {};


        private EngineController EngineControl;
        private GameObject LeaveButton;
        private GameObject Gun_pilot;
        private GameObject SeatAdjuster;
        private LeaveVehicleButton LeaveButtonControl;
        private new Collider collider;

        private void Start()
        {
            EngineControl = originalPilotSeat.EngineControl;
            LeaveButton = originalPilotSeat.LeaveButton;
            Gun_pilot = originalPilotSeat.Gun_pilot;
            SeatAdjuster = originalPilotSeat.SeatAdjuster;
            LeaveButtonControl = LeaveButton.GetComponent<LeaveVehicleButton>();
            collider = GetComponent<Collider>();
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

        public override void Interact()
        {
            EngineControl.PilotEnterPlaneLocal();
            originalPilotSeat.EngineControl.localPlayer.UseAttachedStation();
            if (LeaveButton != null) { LeaveButton.SetActive(true); }
            if (Gun_pilot != null) { Gun_pilot.SetActive(true); }
            if (SeatAdjuster != null) { SeatAdjuster.SetActive(true); }
        }

        public override void OnStationEntered(VRCPlayerApi player)
        {
            collider.enabled = false;

            if (player.isLocal)
            {
                transceiver.exclusive = false;
                transceiver._StartReceive();
                transceiver._StartTransmit();
                SendEvents(onEnterEventTargets, onEnterEventNames);
            }

            EngineControl.PilotEnterPlaneGlobal(player);
            //voice range change to allow talking inside cockpit (after VRC patch 1008)
            LeaveButtonControl.SeatedPlayer = player.playerId;
            if (player.isLocal)
            {
                foreach (LeaveVehicleButton crew in EngineControl.LeaveButtons)
                {//get get a fresh VRCPlayerAPI every time to prevent players who   left leaving a broken one behind and causing crashes
                    VRCPlayerApi guy = VRCPlayerApi.GetPlayerById(crew.SeatedPlayer);
                    if (guy != null)
                    {
                        SetVoiceInside(guy);
                    }
                }
            }
            else if (EngineControl.Piloting || EngineControl.Passenger)
            {
                SetVoiceInside(player);
            }
        }

        public override void OnStationExited(VRCPlayerApi player)
        {
            collider.enabled = true;

            if (player.isLocal)
            {
                transceiver._StopTransmit();
                transceiver._StopReceive();
                SendEvents(onLeaveEventTargets, onLeaveEventNames);
            }
            originalPilotSeat.PlayerExitPlane(player);
        }

        private void SetVoiceInside(VRCPlayerApi Player)
        {
            Player.SetVoiceDistanceNear(999999);
            Player.SetVoiceDistanceFar(1000000);
            Player.SetVoiceGain(.6f);
        }
        private void SetVoiceOutside(VRCPlayerApi Player)
        {
            Player.SetVoiceDistanceNear(0);
            Player.SetVoiceDistanceFar(25);
            Player.SetVoiceGain(15);
        }
    }
}
