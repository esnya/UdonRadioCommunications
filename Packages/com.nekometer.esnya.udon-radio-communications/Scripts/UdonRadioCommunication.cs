using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using TMPro;

#if !COMPILER_UDONSHARP && UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine.SceneManagement;
using UdonSharpEditor;
using UnityEditorInternal;
using VRC.SDKBase.Editor.BuildPipeline;
#endif

namespace URC
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [DefaultExecutionOrder(1100)]
    public class UdonRadioCommunication : UdonSharpBehaviour
    {
        public const float MaxDistance = 1000000.0f;
        public const float MaxVolumetricRadius = 1000.0f;

        [Range(0, 24)] public float defaultVoiceGain = 15;
        [Range(0, 1000000)] public float defaultVoiceDistanceNear = 0;
        [Range(0, 1000000)] public float defaultVoiceDistanceFar = 25;
        [Range(0, 1000)] public float defaultVoiceVolumetricRadius = 0;
        public float distanceAttenuation = 10.0f;
        public bool disableLowpassFilter = true;

        [Space]
        public float defaultFrequency = 118.0f;
        public float minFrequency = 118.0f;
        public float maxFrequency = 136.975f;
        public float frequencyStep = 0.025f;
        public float fastFrequencyStep = 1.0f;
        public float frequencyGap = 0.01f;

        [Space]
        public GameObject[] audioObjectTemplates = { };
        public float[] audioObjectFrequencies = { };

        [Space]
        public TextMeshPro debugText;
        public TextMeshProUGUI debugTextUi;

        [Space]
        public Transmitter[] transmitters;
        public Receiver[] receivers;
        public Transceiver[] transceivers;

        private bool playerListDirty = true;
        private VRCPlayerApi[] players = { };
        private Transmitter[] playerTransmitters = { };
        private bool[] playerPrevIsDefaultVoice = { };
        private GameObject[] transmitterParents = null;
        private GameObject[] receiverParents = null;

        private void Start()
        {
            SendCustomEventDelayedSeconds(nameof(_LateStart), 3);
        }

        public void _LateStart()
        {
            foreach (var receiver in receivers)
            {
                if (receiver) receiver._Initialize(this);
            }

            foreach (var transmitter in transmitters)
            {
                if (transmitter) transmitter._Initialize(this);
            }

            foreach (var transceiver in transceivers)
            {
                if (transceiver) transceiver._Initialize(this);
            }
        }

        private int GetPlayerIndex(VRCPlayerApi player)
        {
            if (Utilities.IsValid(player))
            {
                for (int i = 0; i < players.Length; i++)
                {
                    if (Utilities.IsValid(players[i]) && player.playerId == players[i].playerId)
                    {
                        return i;
                    }
                }
            }

            return -1;
        }

        private void UpdatePlayerVoice(VRCPlayerApi player, float gain, float near, float far, float volumetric, bool lowpassEnabled)
        {
            // Debug.Log($"[{gameObject.name}] Update player ({player.playerId}:{player.displayName}) voice {gain}/{near}-{far}/{volumetric}/{lowpassEnabled}");
            player.SetVoiceGain(gain);
            player.SetVoiceDistanceNear(Mathf.Clamp(near, 0.0f, MaxDistance));
            player.SetVoiceDistanceFar(Mathf.Clamp(far, 0.0f, MaxDistance));
            player.SetVoiceVolumetricRadius(Mathf.Clamp(volumetric, 0.0f, MaxVolumetricRadius));
            if (disableLowpassFilter) player.SetVoiceLowpass(lowpassEnabled);
        }

        private bool FrequencyEqual(float a, float b)
        {
            return Mathf.Abs(a - b) < frequencyGap;
        }

        private Receiver GetReceiver(float frequency)
        {
            var localPosition = Networking.LocalPlayer.GetPosition();
            float minDistance = float.MaxValue;
            Receiver result = null;
            foreach (var r in receivers)
            {
                if (r == null || !r.Active || !FrequencyEqual(r.Frequency, frequency)) continue;

                var distance = Vector3.SqrMagnitude(r.transform.position - localPosition);
                if ((!r.limitRange || distance <= Mathf.Pow(r.maxRange, 2.0f)) && distance < minDistance) result = r;
            }
            return result;
        }

        private GameObject GetAudioTemplate(float frequency)
        {
            for (var i = 0; i < audioObjectFrequencies.Length; i++)
            {
                if (FrequencyEqual(frequency, audioObjectFrequencies[i]) && i < audioObjectTemplates.Length)
                {
                    return audioObjectTemplates[i];
                }
            }
            return null;
        }

        private void Update()
        {
            var localPlayer = Networking.LocalPlayer;
            if (!Utilities.IsValid(localPlayer)) return;

            if (playerListDirty)
            {
                Debug.Log($"Updating player list");
                playerListDirty = false;
                players = new VRCPlayerApi[VRCPlayerApi.GetPlayerCount()];
                VRCPlayerApi.GetPlayers(players);
                playerTransmitters = new Transmitter[players.Length];
                playerPrevIsDefaultVoice = new bool[players.Length];
            }

            for (int i = 0; i < playerTransmitters.Length; i++)
            {
                playerTransmitters[i] = null;
            }

            var localPlayerPosition = localPlayer.GetPosition();
            foreach (var transmitter in transmitters)
            {
                if (
                    transmitter == null
                    || !transmitter.Active
                    || (transmitter.transform.position - localPlayerPosition).sqrMagnitude < Mathf.Pow(transmitter.minDistance, 2)
                ) continue;

                var owner = Networking.GetOwner(transmitter.gameObject);
                var index = GetPlayerIndex(owner);
                if (index < 0) continue;

                playerTransmitters[index] = transmitter;
            }

            for (int i = 0; i < players.Length; i++)
            {
                var remotePlayer = players[i];
                if (remotePlayer.isLocal) continue;

                var transmitter = playerTransmitters[i];
                Receiver receiver = transmitter == null ? null : GetReceiver(transmitter.Frequency);
                var isDefaultVoice = receiver == null;

                if (isDefaultVoice)
                {
                    if (!playerPrevIsDefaultVoice[i]) UpdatePlayerVoice(remotePlayer, defaultVoiceGain, defaultVoiceDistanceNear, defaultVoiceDistanceFar, defaultVoiceVolumetricRadius, true);
                }
                else
                {
                    if (Utilities.IsValid(receiver) && Utilities.IsValid(remotePlayer))
                    {
                        var receiverPosition = receiver.transform.position;
                        var remotePlayerPosition = remotePlayer.GetPosition();
                        var transmitterPosition = transmitter.transform.position;

                        var distanceOverRadio = (Vector3.Distance(remotePlayerPosition, transmitterPosition) + Vector3.Distance(localPlayerPosition, receiverPosition)) * 0;
                        var realDistance = Vector3.Distance(localPlayerPosition, remotePlayerPosition);

                        var near = Mathf.Max(realDistance - distanceOverRadio, 0);
                        var far = near + defaultVoiceDistanceFar - defaultVoiceDistanceNear;
                        var gain = defaultVoiceGain / Mathf.Max(1.0f + Mathf.Pow(distanceOverRadio * distanceAttenuation, 2.0f), 1);
                        UpdatePlayerVoice(remotePlayer, gain, near, far, far, false);
                    }
                }

                playerPrevIsDefaultVoice[i] = isDefaultVoice;
            }

            foreach (var receiver in receivers)
            {
                if (!receiver || !receiver.Active) continue;

                if (!receiver._IsPlayngAudio())
                {
                    var template = GetAudioTemplate(receiver.Frequency);
                    if (template) receiver._SpawnAudioObject(template);
                }
            }

            if (debugText != null && debugText.gameObject.activeInHierarchy || debugTextUi != null && ((Component)debugTextUi).gameObject.activeInHierarchy)
            {
                if (transmitterParents == null)
                {
                    transmitterParents = new GameObject[transmitters.Length];
                    for (var i = 0; i < transmitters.Length; i++)
                    {
                        var rigidbody = transmitters[i].GetComponentInParent<Rigidbody>();
                        transmitterParents[i] = rigidbody ? rigidbody.gameObject : null;
                    }
                }

                if (receiverParents == null)
                {
                    receiverParents = new GameObject[receivers.Length];
                    for (var i = 0; i < receivers.Length; i++)
                    {
                        var rigidbody = receivers[i].GetComponentInParent<Rigidbody>();
                        receiverParents[i] = rigidbody ? rigidbody.gameObject : null;
                    }
                }

                var text = "<color=red>FOR DEBUG ONLY: This screen will worsen performance</color>\n\nTransmitters:\n";
                var closeText = "<color=red>Too Close (Active)</color>";
                var activeText = "<color=green>Active</color>";
                var nonActiveText = "<color=blue>Disabled</color>";

                for (int i = 0; i < transmitters.Length; i++)
                {
                    var transmitter = transmitters[i];
                    if (transmitter == null) continue;
                    var owner = Networking.GetOwner(transmitter.gameObject);
                    var tooClose = (transmitter.transform.position - localPlayerPosition).sqrMagnitude < Mathf.Pow(transmitter.minDistance, 2);
                    text += $"\t{i:000}:{GetUniqueName(transmitterParents[i])}/{GetUniqueName(transmitter)}\t{(transmitter.Active ? (tooClose ? closeText : activeText) : nonActiveText)}\t{transmitter.Frequency:#0.00}\t{GetDebugPlayerString(owner)}\n";
                }

                text += "\nReceivers:\n";
                for (int i = 0; i < receivers.Length; i++)
                {
                    var receiver = receivers[i];
                    if (receiver == null) continue;
                    var owner = Networking.GetOwner(receiver.gameObject);
                    text += $"\t{i:000}:{GetUniqueName(receiverParents[i])}/{GetUniqueName(receiver)}\t{(receiver.Active ? activeText : nonActiveText)}\t{receiver.Frequency:#0.00}\t{(receiver.sync ? "Sync" : "Local")}\t{GetDebugPlayerString(owner)}\n";
                }

                text += "\nPlayers:\n";
                var talkingText = "<color=red>Talking</color>";
                var defaultVoiceText = "<color=green>Default</color>";
                for (int i = 0; i < players.Length; i++)
                {
                    var player = players[i];
                    if (!Utilities.IsValid(player)) continue;

                    var transmitter = playerTransmitters[i];
                    var receiver = transmitter == null ? (Receiver)null : GetReceiver(transmitter.Frequency);

                    text += $"\t{i:000}:{GetDebugPlayerString(player)}\t{GetUniqueName(transmitter)}\t{GetUniqueName(receiver)}\t{(player.isLocal ? "<color=blue>Local</color>" : playerPrevIsDefaultVoice[i] ? defaultVoiceText : talkingText)}\n";
                }

                if (debugText != null) debugText.text = text;
                if (debugTextUi != null) debugTextUi.text = text;
            }
        }

        private string GetUniqueName(Object o)
        {
            if (o == null) return "-";
            return $"{o.GetInstanceID():x8}@{o}";
        }

        private string GetDebugPlayerString(VRCPlayerApi player)
        {
            return $"({player.playerId:000}){player.displayName}";
        }

        public override void OnPlayerJoined(VRCPlayerApi player)
        {
            playerListDirty = true;
        }
        public override void OnPlayerLeft(VRCPlayerApi player)
        {
            playerListDirty = true;
        }

#if !COMPILER_UDONSHARP && UNITY_EDITOR
        private void Awake()
        {
            Setup();
        }

        private static IEnumerable<T> GetComponentsInScene<T>(bool includeInActive) where T : UdonSharpBehaviour
        {
            return SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(o => o.GetComponentsInChildren<T>(includeInActive));
        }

        public void Setup()
        {
            transmitters = GetComponentsInScene<Transmitter>(true).ToArray();
            receivers = GetComponentsInScene<Receiver>(true).ToArray();
            transceivers = GetComponentsInScene<Transceiver>(true).ToArray();
            Debug.Log($"{this} Setup done: ({transmitters.Length}, {receivers.Length}, {transceivers.Length})", this);
            EditorUtility.SetDirty(this);
        }

        static private void SetupAll()
        {
            var targets = GetComponentsInScene<UdonRadioCommunication>(true);
            foreach (var urc in targets)
            {
                urc.Setup();
            }
        }
#endif
    }

#if !COMPILER_UDONSHARP && UNITY_EDITOR

    [CustomEditor(typeof(UdonRadioCommunication))]
    public class UdonRadioCommunicationEditor : Editor
    {
        private static IEnumerable<T> GetComponentsInScene<T>() where T : UdonSharpBehaviour
        {
            return FindObjectsOfType<UdonBehaviour>()
                .Where(UdonSharpEditorUtility.IsUdonSharpBehaviour)
                .Select(UdonSharpEditorUtility.GetProxyBehaviour)
                .Select(u => u as T)
                .Where(u => u != null);
        }

        private ReorderableList audioObjectTemplatesList;

        private void OnEnable()
        {
            var audioObjectTemplatesProperty = serializedObject.FindProperty(nameof(UdonRadioCommunication.audioObjectTemplates));
            var audioObjectFrequenciesProperty = serializedObject.FindProperty(nameof(UdonRadioCommunication.audioObjectFrequencies));
            audioObjectTemplatesList = new ReorderableList(serializedObject, audioObjectTemplatesProperty)
            {
                drawHeaderCallback = (rect) =>
                {
                    var itemRect = rect;
                    itemRect.width /= 2;
                    EditorGUI.LabelField(itemRect, "Template");
                    itemRect.x += itemRect.width;
                    EditorGUI.LabelField(itemRect, "Frequency");
                },
                drawElementCallback = (rect, index, isActive, isFocused) =>
                {
                    var itemRect = rect;
                    itemRect.width /= 2;
                    EditorGUI.PropertyField(itemRect, audioObjectTemplatesProperty.GetArrayElementAtIndex(index), GUIContent.none);
                    itemRect.x += itemRect.width;
                    EditorGUI.PropertyField(itemRect, audioObjectFrequenciesProperty.GetArrayElementAtIndex(index), GUIContent.none);
                },
                onAddCallback = (list) =>
                {
                    audioObjectTemplatesProperty.arraySize += 1;
                    audioObjectFrequenciesProperty.arraySize = audioObjectTemplatesProperty.arraySize;
                },
                onRemoveCallback = (list) =>
                {
                    audioObjectTemplatesProperty.arraySize -= 1;
                    audioObjectFrequenciesProperty.arraySize = audioObjectTemplatesProperty.arraySize;
                },
                onCanRemoveCallback = (list) => audioObjectTemplatesProperty.arraySize >= 1,
                onReorderCallbackWithDetails = (list, oldIndex, newIndex) =>
                {
                    audioObjectTemplatesProperty.MoveArrayElement(oldIndex, newIndex);
                    audioObjectFrequenciesProperty.MoveArrayElement(oldIndex, newIndex);
                },
            };

            serializedObject.Update();
            audioObjectFrequenciesProperty.arraySize = audioObjectTemplatesProperty.arraySize;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        public override void OnInspectorGUI()
        {
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target)) return;

            serializedObject.Update();

            var property = serializedObject.GetIterator();
            property.NextVisible(true);
            do
            {
                switch (property.name)
                {
                    case nameof(UdonRadioCommunication.audioObjectTemplates):
                        EditorGUILayout.Space();
                        EditorGUILayout.LabelField("Audio Objects");
                        audioObjectTemplatesList.DoLayoutList();
                        break;
                    case nameof(UdonRadioCommunication.audioObjectFrequencies):
                        break;
                    default:
                        EditorGUILayout.PropertyField(property, true);
                        break;
                }
            } while (property.NextVisible(false));

            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space();
        }

        private static void SetupAll()
        {
            var urcs = GetComponentsInScene<UdonRadioCommunication>();
            foreach (var urc in urcs)
            {
                Debug.Log($"[{urc.gameObject.name}] Auto setup");
                urc.Setup();
            }
        }

        public class BuildCallback : Editor, IVRCSDKBuildRequestedCallback
        {
            public int callbackOrder => 10;

            public bool OnBuildRequested(VRCSDKRequestedBuildType requestedBuildType)
            {
                SetupAll();
                return true;
            }
        }
    }
#endif
}
