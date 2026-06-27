using UnityEngine;

namespace NanoCamping
{
    /// <summary>
    /// 入口:Awake 时建平台 / 灯光 / 相机 / 网格辅助 / PlacementManager / HUD。
    /// 使用方式:在 Tuanjie 里新建空 GameObject "GameRoot",挂这个脚本,Play 即可。
    /// </summary>
    public class GameBootstrap : MonoBehaviour
    {
        public float platformSize = 8f;
        public Color platformColor = new Color(0.30f, 0.42f, 0.30f);

        void Awake()
        {
            BuildPlatform();
            BuildLights();
            BuildGrid();
            EnsureCamera();   // 必须在 BuildPlacement 之前 — 否则 pm.isoCam = null
            BuildPlacement();
            BuildHUD();
        }

        void BuildPlatform()
        {
            var platform = GameObject.CreatePrimitive(PrimitiveType.Cube);
            platform.name = "Platform";
            platform.transform.position = new Vector3(0, -0.15f, 0);
            platform.transform.localScale = new Vector3(platformSize, 0.3f, platformSize);
            var mr = platform.GetComponent<MeshRenderer>();
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            mat.color = platformColor;
            mr.sharedMaterial = mat;
            // 留 collider 给 raycast 命中
        }

        void BuildLights()
        {
            var sun = new GameObject("Sun");
            var light = sun.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.95f, 0.85f);
            light.intensity = 1.2f;
            sun.transform.rotation = Quaternion.Euler(50, -30, 0);

            // ambient
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.55f, 0.60f, 0.65f);
        }

        void BuildGrid()
        {
            // 简单的 LineRenderer grid
            var gridGo = new GameObject("GridLines");
            int n = Mathf.RoundToInt(platformSize * 2); // 每米 2 条线
            float step = platformSize / n;
            for (int i = 0; i <= n; i++)
            {
                float t = -platformSize / 2f + i * step;
                CreateGridLine(new Vector3(t, 0.01f, -platformSize / 2f),
                               new Vector3(t, 0.01f,  platformSize / 2f), gridGo.transform);
                CreateGridLine(new Vector3(-platformSize / 2f, 0.01f, t),
                               new Vector3( platformSize / 2f, 0.01f, t), gridGo.transform);
            }
        }

        void CreateGridLine(Vector3 a, Vector3 b, Transform parent)
        {
            var go = new GameObject("line");
            go.transform.SetParent(parent);
            var lr = go.AddComponent<LineRenderer>();
            lr.positionCount = 2;
            lr.SetPosition(0, a);
            lr.SetPosition(1, b);
            lr.startWidth = 0.02f;
            lr.endWidth = 0.02f;
            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.startColor = lr.endColor = new Color(0, 0, 0, 0.25f);
        }

        void BuildPlacement()
        {
            var go = new GameObject("PlacementManager");
            var pm = go.AddComponent<PlacementManager>();

            // 注入 SelectionIndicator(选中高亮)
            var indGo = new GameObject("SelectionIndicator");
            var ind = indGo.AddComponent<SelectionIndicator>();
            pm.indicator = ind;

            // 注入 GhostPreview(地面 ghost)
            var ghostGo = new GameObject("GhostPreview");
            var ghost = ghostGo.AddComponent<GhostPreview>();
            ghost.cam = Camera.main;
            pm.ghost = ghost;

            // 注入 IsoCamera(滚轮缩放 + Idle 左键旋转 + 右键平移)
            var iso = Camera.main.GetComponent<IsoCamera>();
            if (iso != null) iso.placement = pm;
            pm.isoCam = iso;
        }

        void EnsureCamera()
        {
            if (Camera.main == null)
            {
                var camGo = new GameObject("IsoCamera");
                var cam = camGo.AddComponent<Camera>();
                cam.tag = "MainCamera";
                cam.backgroundColor = new Color(0.10f, 0.18f, 0.27f);
                camGo.AddComponent<IsoCamera>();
            }
            else
            {
                var main = Camera.main;
                if (main.GetComponent<IsoCamera>() == null)
                    main.gameObject.AddComponent<IsoCamera>();
                main.backgroundColor = new Color(0.10f, 0.18f, 0.27f);
            }
        }

        void BuildHUD()
        {
            // 物件菜单(全屏 overlay)
            var pickerGo = new GameObject("ObjectPickerMenu");
            var picker = pickerGo.AddComponent<ObjectPickerMenu>();
            picker.placement = PlacementManager.Instance;
            // 让 PM 也能查菜单状态(相机滚轮缩放要避让)
            PlacementManager.Instance.picker = picker;

            // HUD(屏幕 UI)
            var go = new GameObject("HUD");
            var hud = go.AddComponent<HUD>();
            hud.placement = PlacementManager.Instance;
            hud.picker = picker;
        }
    }
}
