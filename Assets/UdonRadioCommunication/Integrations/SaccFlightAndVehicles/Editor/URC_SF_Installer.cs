using System.Linq;
using UdonSharp;
using UdonSharpEditor;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using VRC.Udon;

namespace UdonRadioCommunication
{
    public class URC_SF_Installer : EditorWindow
    {
        [MenuItem("UdonRadioCommunication/Installer for SaccFight")]
        private static void ShowWindow()
        {
            var window = GetWindow<URC_SF_Installer>();
            window.Show();
        }

        private static void ClearLocalTransform(Transform t)
        {
            t.localPosition = Vector3.zero;
            t.localRotation = Quaternion.identity;
            t.localScale = Vector3.one;
        }

        public GameObject planeRadioPrefab;

        private Vector2 scrollPosition;
        private readonly GUILayoutOption[] miniButtonLayout = {
            GUILayout.ExpandWidth(false),
            GUILayout.Width(100),
        };

        private void OnEnable()
        {
            titleContent = new GUIContent("URC Installer for SF");
            if (planeRadioPrefab == null) planeRadioPrefab = Resources.Load<GameObject>("PlaneRadio");
        }

#if !URC_SF
        private void OnGUI()
        {
            EditorGUILayout.Space();
            if (GUILayout.Button("Enable URC Integration for SaccFlight"))
            {
                var buildTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
                var syms = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTargetGroup);
                PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTargetGroup, $"URC_SF;{syms}");
            }
        }
#else
        private static UdonRadioCommunication GetOrCreateManager()
        {
            var found = SceneManager.GetActiveScene().GetRootGameObjects().Select(o => o.GetUdonSharpComponent<UdonRadioCommunication>()).FirstOrDefault(a => a != null);
            if (found != null) return found;

            var gameObject = new GameObject("UdonRadioCommunication");
            var created = gameObject.AddComponent<UdonRadioCommunication>();
            var udon = UdonSharpEditorUtility.ConvertToUdonBehaviours(new [] { created }).First();

            return UdonSharpEditorUtility.GetProxyBehaviour(udon) as UdonRadioCommunication;
        }

        private static void SetupURC()
        {
            var urc = GetOrCreateManager();
            urc.Setup();
        }

        private static void MoveBoxCollider(GameObject src, GameObject dst)
        {
            var srcCollider = src.GetComponent<BoxCollider>();
            var dstCollider = dst.AddComponent<BoxCollider>();

            Undo.RecordObject(srcCollider, "Move Collider");
            srcCollider.enabled = false;

            dstCollider.isTrigger = srcCollider.isTrigger;
            dstCollider.center = srcCollider.center;
            dstCollider.size = srcCollider.size;

            Undo.RegisterCreatedObjectUndo(dstCollider, "Move Collider");
        }

        private void Install(GameObject vehicleRoot, PilotSeat pilotSeat)
        {
            var planeRadio = Instantiate(planeRadioPrefab);
            Undo.RegisterCreatedObjectUndo(planeRadio, "Install URC");
            planeRadio.name = planeRadioPrefab.name;

            planeRadio.transform.parent = vehicleRoot.transform;
            ClearLocalTransform(planeRadio.transform);

            var relayObj = new GameObject("URC_Relay");
            relayObj.transform.parent = pilotSeat.transform;
            ClearLocalTransform(relayObj.transform);

            Undo.RegisterCreatedObjectUndo(relayObj, "Install URC");
            MoveBoxCollider(pilotSeat.gameObject, relayObj);

            var relay = relayObj.AddUdonSharpComponent<SFRelay>();
            relay.relayTarget = pilotSeat;
            relay.transceiver = planeRadio.GetUdonSharpComponentInChildren<Transceiver>();

            relay.ApplyProxyModifications();

            SetupURC();
        }

        private void Uninstall(PilotSeat pilotSeat, SFRelay relay)
        {
            Undo.DestroyObjectImmediate(relay.transceiver.gameObject);
            pilotSeat.GetComponent<BoxCollider>().enabled = true;
            Undo.DestroyObjectImmediate(relay.gameObject);
            SetupURC();
        }

        private void OnGUI()
        {
            var scene = SceneManager.GetActiveScene();
            var engineControllers = scene.GetRootGameObjects().SelectMany(o => o.GetUdonSharpComponentsInChildren<EngineController>());

            using (var scrollScope = new EditorGUILayout.ScrollViewScope(scrollPosition))
            {
                scrollPosition = scrollScope.scrollPosition;

                EditorGUILayout.Space();

                planeRadioPrefab = EditorGUILayout.ObjectField("Plane Radio Template", planeRadioPrefab, typeof(GameObject), true) as GameObject;

                EditorGUILayout.Space();

                using (new EditorGUI.DisabledGroupScope(planeRadioPrefab == null))
                {
                    foreach (var engineController in engineControllers)
                    {
                        var vehicleRoot = engineController.GetComponentInParent<Rigidbody>()?.gameObject ?? engineController.gameObject;
                        var pilotSeat = vehicleRoot.GetUdonSharpComponentInChildren<PilotSeat>();
                        var relay = vehicleRoot.GetUdonSharpComponentInChildren<SFRelay>();
                        var installed = relay != null;

                        using (new EditorGUILayout.HorizontalScope())
                        {
                            EditorGUILayout.ObjectField(vehicleRoot, typeof(GameObject), true);

                            using (new EditorGUI.DisabledGroupScope(installed)) if (GUILayout.Button("Install", EditorStyles.miniButtonLeft, miniButtonLayout)) Install(vehicleRoot, pilotSeat);
                            using (new EditorGUI.DisabledGroupScope(!installed)) if (GUILayout.Button("Uninstall", EditorStyles.miniButtonRight, miniButtonLayout)) Uninstall(pilotSeat, relay);
                        }
                    }
                }

                EditorGUILayout.Space();

                if (GUILayout.Button("Disable URC Integration for SaccFlight"))
                {
                    var buildTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
                    var syms = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTargetGroup);
                    PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTargetGroup, syms.Replace("URC_SF;", ""));
                }
            }
        }
#endif
    }
}
