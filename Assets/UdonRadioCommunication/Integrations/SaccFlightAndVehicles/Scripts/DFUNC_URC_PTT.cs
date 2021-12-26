using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace UdonRadioCommunication
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class DFUNC_URC_PTT : UdonSharpBehaviour
    {
        public GameObject Dial_Funcon;
        public KeyCode desktopKey = KeyCode.P;
        private Transceiver transceiver;
        private GameObject txIndicator;

#if URC_SF
        public void SFEXT_L_EntityStart()
        {
            var entity = GetComponentInParent<SaccEntity>();
            SaccVehicleSeat pilotSeat = null;
            foreach (var seat in entity.gameObject.GetComponentsInChildren<SaccVehicleSeat>(true)) // ToDo: Workarond on U# 0.x
            {
                if ((bool)seat.GetProgramVariable("IsPilotSeat"))
                {
                    pilotSeat = seat;
                    break;
                }
            }

            transceiver = pilotSeat.GetComponentInChildren<Transceiver>(true);
            var transmitter = transceiver.transmitter;
            if (transmitter.indicator) txIndicator = transmitter.indicator;
            else transmitter.indicator = Dial_Funcon;

            var enabledTrigger = pilotSeat.GetComponentInChildren<TransceiverEnabledTrigger>(true);
            if (enabledTrigger) enabledTrigger.defaultTransmit = false;

            foreach (var touchSwitch in entity.gameObject.GetComponentsInChildren<TouchSwitch>(true)) // ToDo: Workarond on U# 0.x
            {
                if (touchSwitch.eventTarget == transceiver && touchSwitch.eventName == nameof(Transceiver._ToggleTransmit))
                {
                    touchSwitch.enableDesktopKey = false;
                }
            }
        }

        private bool isLeftDial;
        public void DFUNC_LeftDial() => isLeftDial = true;
        public void DFUNC_RightDial() => isLeftDial = false;

        public void SFEXT_O_PilotEnter()
        {
            gameObject.SetActive(true);
            SetTransmit(false);
        }
        public void SFEXT_O_PilotExit() => gameObject.SetActive(false);

        private bool selected;
        public void DFUNC_Selected()
        {
            selected = true;
        }
        public void DFUNC_Deselected()
        {
            selected = false;
            if (transmit) SetTransmit(false);
        }

        private void Update()
        {
            if (Input.GetKeyDown(desktopKey)) SetTransmit(true);
            else if (Input.GetKeyUp(desktopKey)) SetTransmit(false);
            else if (selected)
            {
                var value = Input.GetAxisRaw(isLeftDial ? "Oculus_CrossPlatform_PrimaryIndexTrigger" : "Oculus_CrossPlatform_SecondaryIndexTrigger") > 0.75f;
                if (value != transmit) SetTransmit(value);
            }
            if (txIndicator && Dial_Funcon && txIndicator.activeSelf != Dial_Funcon.activeSelf) Dial_Funcon.SetActive(txIndicator.activeSelf);
        }

        private bool transmit;
        private void SetTransmit(bool value)
        {
            transmit = value;
            transceiver._SetTransmit(value);
        }
#endif
    }
}
