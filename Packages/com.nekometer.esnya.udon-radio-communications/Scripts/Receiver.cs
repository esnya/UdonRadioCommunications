using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace URC
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class Receiver : UdonSharpBehaviour
    {
        public bool limitRange = true;
        public float maxRange = 5.0f;
        public bool sync = true;
        public GameObject indicator;

        [NonSerialized] public UdonRadioCommunication urc;

        private bool _active;
        public bool Active
        {
            get => _active;
            set
            {
                if (!value) _DestroyAudioObject();
                if (indicator) indicator.SetActive(value);

                _active = value;

                if (sync && value != SyncedActive)
                {
                    _TakeOwnership();
                    SyncedActive = value;
                    RequestSerialization();
                }
            }
        }

        private float _frequency;
        public float Frequency
        {
            get => _frequency;
            set
            {
                _DestroyAudioObject();

                _frequency = value;

                if (sync && value != SyncedFrequency)
                {
                    _TakeOwnership();
                    SyncedFrequency = value;
                    RequestSerialization();
                }
            }
        }

        [UdonSynced][FieldChangeCallback(nameof(SyncedActive))] private bool _syncedActive;
        public bool SyncedActive
        {
            get => _syncedActive;
            private set
            {
                if (!sync) return;
                Active = _syncedActive = value;
            }
        }
        [UdonSynced][FieldChangeCallback(nameof(SyncedFrequency))] private float _syncedFrequency;
        public float SyncedFrequency
        {
            get => _syncedFrequency;
            private set
            {
                if (!sync) return;
                Frequency = _syncedFrequency = value;
            }
        }

        private GameObject audioObject;

        private void Start()
        {
            Active = false;
        }

        public void _Initialize(UdonRadioCommunication urc)
        {
            this.urc = urc;
            if (Networking.IsOwner(gameObject))
            {
                SyncedFrequency = Frequency = urc.defaultFrequency;
                RequestSerialization();
            }
        }

        public void _TakeOwnership()
        {
            if (!sync || Networking.IsOwner(gameObject)) return;
            Networking.SetOwner(Networking.LocalPlayer, gameObject);
        }
        public void _Activate() => Active = true;
        public void _Deactivate() => Active = false;
        public void _ToggleActive() => Active = !Active;

        public void _DestroyAudioObject()
        {
            if (audioObject) Destroy(audioObject);
        }
        public bool _IsPlayngAudio() => audioObject;
        public void _SpawnAudioObject(GameObject template)
        {
            _DestroyAudioObject();

            if (!template) return;

            audioObject = Instantiate(template);
            audioObject.transform.SetParent(transform, false);
            audioObject.transform.localPosition = Vector3.zero;
            audioObject.SetActive(true);
        }
    }
}
