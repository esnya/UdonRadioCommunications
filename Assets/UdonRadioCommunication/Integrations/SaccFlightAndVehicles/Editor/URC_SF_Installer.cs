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
        private static PilotSeat MovePilotSeat(PilotSeat src, GameObject dstGameObject)
        {
            var srcUdon = UdonSharpEditorUtility.GetBackingUdonBehaviour(src);

            var dstUdon = dstGameObject.AddComponent<UdonBehaviour>();
            Undo.RegisterCreatedObjectUndo(dstUdon, "URC Move PilotSeat");
            dstUdon.programSource = srcUdon.programSource;

            var dst = UdonSharpEditorUtility.GetProxyBehaviour(dstUdon) as PilotSeat;

            dst.EngineControl = src.EngineControl;
            dst.LeaveButton = src.LeaveButton;
            dst.Gun_pilot = src.Gun_pilot;
            dst.SeatAdjuster = src.SeatAdjuster;
            dst.ApplyProxyModifications();

            Undo.DestroyObjectImmediate(srcUdon);

            return dst;
        }

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

        private void Install(GameObject vehicleRoot, PilotSeat pilotSeat)
        {
            var urcSeatTemplate = pilotSeat.gameObject.AddComponent<URCPilotSeat>();
            var urcSeatUdon = UdonSharpEditorUtility.ConvertToUdonBehaviours(new[] { urcSeatTemplate }).First();
            Undo.RegisterCreatedObjectUndo(urcSeatUdon, "Install URC");
            var urcSeat = UdonSharpEditorUtility.GetProxyBehaviour(urcSeatUdon) as URCPilotSeat;

            var originalPilotSeatObj = new GameObject("PilotSeat");
            Undo.RegisterCreatedObjectUndo(originalPilotSeatObj, "Install URC");
            originalPilotSeatObj.transform.parent = urcSeat.transform;
            ClearLocalTransform(originalPilotSeatObj.transform);

            var originalPilotSeat = MovePilotSeat(pilotSeat, originalPilotSeatObj);

            var planeRadio = Instantiate(planeRadioPrefab);
            Undo.RegisterCreatedObjectUndo(planeRadio, "Install URC");
            planeRadio.name = planeRadioPrefab.name;

            planeRadio.transform.parent = vehicleRoot.transform;
            ClearLocalTransform(planeRadio.transform);

            urcSeat.originalPilotSeat = originalPilotSeat;
            urcSeat.transceiver = planeRadio.GetUdonSharpComponentInChildren<Transceiver>();
            urcSeat.ApplyProxyModifications();

            SetupURC();
        }

        private void Uninstall(PilotSeat pilotSeat, URCPilotSeat urcInstalledSeat)
        {
            MovePilotSeat(pilotSeat, urcInstalledSeat.gameObject);
            Undo.DestroyObjectImmediate(urcInstalledSeat.originalPilotSeat.gameObject);
            Undo.DestroyObjectImmediate(urcInstalledSeat.transceiver.gameObject);
            Undo.DestroyObjectImmediate(UdonSharpEditorUtility.GetBackingUdonBehaviour(urcInstalledSeat));
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
                    foreach (var engineController in engineControllers)
                    {
                        var vehicleRoot = engineController.GetComponentInParent<Rigidbody>()?.gameObject ?? engineController.gameObject;
                        var pilotSeat = vehicleRoot.GetUdonSharpComponentInChildren<PilotSeat>();
                        var urcInstalledSeat = vehicleRoot.GetUdonSharpComponentInChildren<URCPilotSeat>();
                        var installed = urcInstalledSeat != null;

                        using (new EditorGUILayout.HorizontalScope())
                        {
                            EditorGUILayout.ObjectField(vehicleRoot, typeof(GameObject), true);

                            using (new EditorGUI.DisabledGroupScope(installed)) if (GUILayout.Button("Install", EditorStyles.miniButtonLeft, miniButtonLayout)) Install(vehicleRoot, pilotSeat);
                            using (new EditorGUI.DisabledGroupScope(!installed)) if (GUILayout.Button("Uninstall", EditorStyles.miniButtonRight, miniButtonLayout)) Uninstall(pilotSeat, urcInstalledSeat);
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
