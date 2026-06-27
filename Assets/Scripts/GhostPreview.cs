using System.Collections.Generic;
using UnityEngine;

namespace NanoCamping
{
    /// <summary>
    /// 物件预览:在 hover 地面时显示:
    ///   1. 半透 mesh ghost(显示物件形状,URP/Lit transparent)
    ///   2. 底部 footprint 矩形 outline
    /// 跟随鼠标在地面 XZ 移动。
    ///
    /// v0.5:从"只 footprint"改为"mesh + footprint"。选中状态下不显示(避免覆盖选中框)。
    /// </summary>
    public class GhostPreview : MonoBehaviour
    {
        public Camera cam;
        public LayerMask groundMask = ~0;
        // 青色 — 与编辑态的黄色选中框做明确区分
        public Color footprintColor = new Color(0.2f, 0.85f, 1.0f, 0.85f);
        // mesh 半透 alpha 0.55 — 既能看清物件形状,又能透出底色
        public Color meshTint = new Color(0.3f, 0.95f, 1.0f, 0.55f);
        public float lineWidth = 0.04f;
        // ghost 比地面高多少(避免 z-fighting,原来 mesh y=0 跟平台 mesh 同 y 被遮挡)
        public float ghostLiftY = 0.05f;

        LineRenderer _rect;
        GameObject _meshGhost;       // 半透 mesh 副本
        Renderer[] _ghostRenderers;
        List<Material> _ghostMats;   // 持有引用便于销毁
        GameObject _target;          // 当前跟随的物件(prefab source)

        void Awake()
        {
            // footprint 矩形
            var go = new GameObject("footprint_rect");
            go.transform.SetParent(transform, false);
            _rect = go.AddComponent<LineRenderer>();
            _rect.positionCount = 5;
            _rect.startWidth = _rect.endWidth = lineWidth;
            _rect.loop = true;
            _rect.useWorldSpace = true;
            _rect.material = new Material(Shader.Find("Sprites/Default"));
            _rect.startColor = _rect.endColor = footprintColor;
            _rect.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _rect.enabled = false;
        }

        void OnDestroy()
        {
            if (_meshGhost != null) Destroy(_meshGhost);
        }

        /// <summary>
        /// 设置 ghost 跟随的物件(prefab 实例,带 ObjectItem)。
        /// null = 隐藏 ghost。
        /// </summary>
        public void Attach(GameObject target)
        {
            if (_target == target) return;
            _target = target;
            if (target == null)
            {
                Hide();
                return;
            }
            BuildMeshGhost(target);
        }

        void BuildMeshGhost(GameObject source)
        {
            // 销毁旧的 mesh ghost
            if (_meshGhost != null) Destroy(_meshGhost);

            // 用一个空 GameObject 装所有 Renderer(不要 Instantiate(source),
            // 因为我们要替换所有材质为 URP/Lit + 半透)
            _meshGhost = new GameObject("ghost_mesh");
            _meshGhost.transform.SetParent(transform, false);

            // 复制 mesh 结构(包括 root + 所有 children)
            CopyRenderers(source, _meshGhost.transform);

            _ghostRenderers = _meshGhost.GetComponentsInChildren<Renderer>(true);
            _ghostMats = new List<Material>();

            var urpLit = Shader.Find("Universal Render Pipeline/Lit");
            foreach (var r in _ghostRenderers)
            {
                // 关掉 shadow + 不参与光照计算,用 URP/Lit transparent
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                r.receiveShadows = false;
                var mats = r.materials;  // 实例化
                for (int i = 0; i < mats.Length; i++)
                {
                    mats[i].shader = urpLit;
                    // URP/Lit transparent 渲染模式 = Transparent
                    mats[i].SetFloat("_Surface", 1f);       // 0 Opaque, 1 Transparent
                    mats[i].SetFloat("_Blend", 0f);        // 0 Alpha
                    mats[i].SetFloat("_ZWrite", 0f);       // 不写深度
                    mats[i].SetFloat("_ZTest", 8f);        // Always — ghost 永远画在最上层,不被地面/物件遮挡
                    mats[i].SetOverrideTag("RenderType", "Transparent");
                    mats[i].renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent + 100; // 比一般 transparent 晚画
                    mats[i].EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                    mats[i].SetColor("_BaseColor", meshTint);
                    _ghostMats.Add(mats[i]);
                }
                r.materials = mats;
            }
            _meshGhost.SetActive(false);
        }

        void CopyRenderers(GameObject source, Transform dst)
        {
            // 0) 先复制 root 自己的 renderer(有些 prefab mesh 在根节点上,不在 children)
            var rootMf = source.GetComponent<MeshFilter>();
            var rootMr = source.GetComponent<MeshRenderer>();
            var rootSmr = source.GetComponent<SkinnedMeshRenderer>();
            if (rootMf != null && rootMf.sharedMesh != null && rootMr != null)
            {
                var newGo = new GameObject(source.name + "_root");
                newGo.transform.SetParent(dst, false);
                newGo.transform.localPosition = source.transform.localPosition;
                newGo.transform.localRotation = source.transform.localRotation;
                newGo.transform.localScale = source.transform.localScale;
                var newMf = newGo.AddComponent<MeshFilter>();
                newMf.sharedMesh = rootMf.sharedMesh;
                var newMr = newGo.AddComponent<MeshRenderer>();
                newMr.sharedMaterials = rootMr.sharedMaterials;
            }
            else if (rootSmr != null)
            {
                var newGo = new GameObject(source.name + "_root");
                newGo.transform.SetParent(dst, false);
                newGo.transform.localPosition = source.transform.localPosition;
                newGo.transform.localRotation = source.transform.localRotation;
                newGo.transform.localScale = source.transform.localScale;
                var newSmr = newGo.AddComponent<SkinnedMeshRenderer>();
                newSmr.sharedMesh = rootSmr.sharedMesh;
                newSmr.sharedMaterials = rootSmr.sharedMaterials;
                newSmr.bones = rootSmr.bones;
                newSmr.rootBone = rootSmr.rootBone;
            }

            // 1) 再递归所有 children
            CopyChildRenderers(source.transform, dst);
        }

        void CopyChildRenderers(Transform src, Transform dst)
        {
            for (int i = 0; i < src.childCount; i++)
            {
                var c = src.GetChild(i);
                var mf = c.GetComponent<MeshFilter>();
                var mr = c.GetComponent<MeshRenderer>();
                var smr = c.GetComponent<SkinnedMeshRenderer>();
                if (mf != null && mf.sharedMesh != null && mr != null)
                {
                    var newGo = new GameObject(c.name);
                    newGo.transform.SetParent(dst, false);
                    newGo.transform.localPosition = c.localPosition;
                    newGo.transform.localRotation = c.localRotation;
                    newGo.transform.localScale = c.localScale;
                    var newMf = newGo.AddComponent<MeshFilter>();
                    newMf.sharedMesh = mf.sharedMesh;
                    var newMr = newGo.AddComponent<MeshRenderer>();
                    newMr.sharedMaterials = mr.sharedMaterials;
                }
                else if (smr != null)
                {
                    // SkinnedMeshRenderer:复制 bones + mesh
                    var newGo = new GameObject(c.name);
                    newGo.transform.SetParent(dst, false);
                    newGo.transform.localPosition = c.localPosition;
                    newGo.transform.localRotation = c.localRotation;
                    newGo.transform.localScale = c.localScale;
                    var newSmr = newGo.AddComponent<SkinnedMeshRenderer>();
                    newSmr.sharedMesh = smr.sharedMesh;
                    newSmr.sharedMaterials = smr.sharedMaterials;
                    newSmr.bones = smr.bones;
                    newSmr.rootBone = smr.rootBone;
                }
                // 跳过只有 MeshFilter 没有 MeshRenderer 的物件(纯 collider)
                CopyChildRenderers(c, dst);
            }
        }

        void Hide()
        {
            _rect.enabled = false;
            if (_meshGhost != null) _meshGhost.SetActive(false);
        }

        void Update()
        {
            if (_target == null || cam == null) { Hide(); return; }
            if (_meshGhost == null) return;

            Vector2 screenPos = TouchInput.GetPos();
            Ray ray = cam.ScreenPointToRay(screenPos);
            if (!Physics.Raycast(ray, out var hit, 1000f, groundMask))
            {
                Hide();
                return;
            }

            // 地面点
            Vector3 ground = hit.point;
            // 物件 footprint 包围盒
            var b = _target.GetComponent<ObjectItem>()?.GetWorldBoundsXZ()
                    ?? new Bounds(_target.transform.position, new Vector3(1f, 0.01f, 1f));
            float extX = Mathf.Max(0.2f, b.extents.x);
            float extZ = Mathf.Max(0.2f, b.extents.z);

            // footprint
            Vector3 c = ground;
            c.y = Mathf.Max(0.02f, hit.point.y + 0.02f);
            _rect.SetPosition(0, c + new Vector3(-extX, 0, -extZ));
            _rect.SetPosition(1, c + new Vector3( extX, 0, -extZ));
            _rect.SetPosition(2, c + new Vector3( extX, 0,  extZ));
            _rect.SetPosition(3, c + new Vector3(-extX, 0,  extZ));
            _rect.SetPosition(4, c + new Vector3(-extX, 0, -extZ));
            _rect.enabled = true;

            // mesh ghost 抬到 ground+liftY,避免与平台 mesh 同 y 被遮挡/深度冲突
            Vector3 ghostPos = ground;
            ghostPos.y += ghostLiftY;
            _meshGhost.transform.position = ghostPos;
            _meshGhost.transform.rotation = _target.transform.rotation;
            _meshGhost.SetActive(true);
        }
    }

    /// <summary>
    /// 统一鼠标 / 触摸输入:优先 touch,iPad 上 Tuanjie Editor 也工作。
    /// </summary>
    public static class TouchInput
    {
        public static Vector2 GetPos()
        {
            if (Input.touchCount > 0) return Input.GetTouch(0).position;
            return Input.mousePosition;
        }

        public static bool GetDown()
        {
            if (Input.touchCount > 0) return Input.GetTouch(0).phase == TouchPhase.Began;
            return Input.GetMouseButtonDown(0);
        }

        public static bool GetUp()
        {
            if (Input.touchCount > 0)
            {
                var t = Input.GetTouch(0);
                return t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled;
            }
            return Input.GetMouseButtonUp(0);
        }

        public static bool GetHeld()
        {
            if (Input.touchCount > 0) return true;
            return Input.GetMouseButton(0);
        }

        public static bool GetRightHeld()
        {
            return Input.GetMouseButton(1);
        }
    }
}