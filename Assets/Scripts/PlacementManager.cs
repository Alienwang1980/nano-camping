using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NanoCamping
{
    /// <summary>
    /// v0.6 物件管理 — 明确的三态机:
    ///
    ///   ┌────────────────────────────────────────────────────────────┐
    ///   │  状态        触发条件                退出条件                │
    ///   ├────────────────────────────────────────────────────────────┤
    ///   │  Idle         无                    点菜单 / 点物件         │
    ///   │  放置态       菜单选物件             放置完成(自动,一次性)   │
    ///   │  编辑态       点已放置物件           ✓ 确认 / 删除           │
    ///   └────────────────────────────────────────────────────────────┘
    ///
    /// 视觉区分:
    ///   - 放置态:cyan footprint + 地面 ghost 跟随鼠标
    ///   - 编辑态:yellow 半透 12-线 3D bbox 包围物件
    ///   - 状态名实时显示在顶栏(【空闲】/【放置态】/【编辑态】)
    ///
    /// v0.6 改动:
    ///   - 三态机显式命名为 Idle / Placing / Manipulating
    ///   - HUD 顶栏 + 底部 help 行显示当前状态名
    ///   - 放置态 ghost 改 cyan 与编辑态 yellow 区分
    ///   - 编辑态 bbox 改 alpha 0.45(原 0.95 太厚遮挡物件)
    ///   - 放置完成 currentIndex = -1,菜单不再保持高亮
    /// </summary>
    public class PlacementManager : MonoBehaviour
    {
        public static PlacementManager Instance { get; private set; }

        public GameObject platform;
        public LayerMask groundMask = ~0;

        public float dragThresholdPx = 5f;
        public float gridSnap = 0.5f;
        public float baseY = 0f;

        public List<GameObject> placed = new List<GameObject>();
        public int currentIndex = -1;  // 默认 Idle(没有选物件);>=0 进入放置态
        public GameObject currentSelected;

        public SelectionIndicator indicator;
        public GhostPreview ghost;
        public IsoCamera isoCam;
        public ObjectPickerMenu picker;  // 菜单状态查询用(IsMenuOpen)

        // 缩放动画
        public float growDuration = 0.3f;
        bool _isGrowing;

        Camera _cam;
        GameObject _dragging;
        Vector3 _dragOffset;
        Vector2 _mouseDownPos;
        bool _didDrag;

        void Awake()
        {
            if (Instance && Instance != this) { Destroy(this); return; }
            Instance = this;
            _cam = Camera.main;
        }

        public ObjectDatabase.Entry CurrentSpec => ObjectDatabase.Get(currentIndex);

        /// <summary>
        /// 菜单打开中?(用于禁用相机滚轮缩放等冲突输入)
        /// </summary>
        public bool IsMenuOpen => picker != null && picker.IsOpen;

        void Update()
        {
            HandleHotkeys();
            HandleMouse();
            UpdateGhost();
        }

        void HandleHotkeys()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (currentSelected != null)
                {
                    ExitManipulating();
                }
            }
        }

        /// <summary>
        /// HandleMouse:
        ///   右键被 IsoCamera 拦截(旋转)
        ///   滚轮被 IsoCamera 拦截(缩放)
        ///   左键 → 主交互:
        ///     Manipulating 态 + 点物件 → 换选/拖动
        ///     Manipulating 态 + 点空地 → 忽略
        ///     Idle 态 + 点物件 → 进入 Manipulating
        ///     Idle 态 + 点地面 → 放置(进入 Placing)
        ///     Idle 态 + 点地图外 → 忽略
        /// </summary>
        void HandleMouse()
        {
            if (_isGrowing) return;
            if (_cam == null) _cam = Camera.main;
            if (_cam == null) return;

            HandleMouseWithInput(
                TouchInput.GetPos(),
                TouchInput.GetDown(),
                TouchInput.GetHeld(),
                TouchInput.GetUp());
        }

        internal void HandleMouseWithInput(Vector2 mousePos, bool down, bool held, bool up)
        {
            // 右键按住 → 让 IsoCamera 旋转
            if (TouchInput.GetRightHeld()) return;

            if (down)
            {
                _mouseDownPos = mousePos;
                _didDrag = false;
                _dragging = null;

                if (TryRaycastObject(mousePos, out var hitObj, out var hitPoint))
                {
                    // 点中物件
                    if (currentSelected == null && currentIndex < 0)
                    {
                        // Idle → 进入 Manipulating(只有真正 Idle 才进编辑)
                        EnterManipulating(hitObj);
                    }
                    else if (currentSelected == null && currentIndex >= 0)
                    {
                        // 放置态点中已有物件 → 忽略,保持放置态
                        // (不要跟编辑态搞混)
                    }
                    else
                    {
                        // Manipulating → 换选 / 拖动
                        if (hitObj != currentSelected)
                        {
                            ExitManipulating();
                            EnterManipulating(hitObj);
                        }
                        else
                        {
                            // 同一个物件 → 开始拖动
                            _dragging = hitObj;
                            _dragOffset = hitPoint - hitObj.transform.position;
                            _dragOffset.y = 0;
                        }
                    }
                    RefreshSelection();
                }
                else
                {
                    // 没点中物件
                    bool hitGround = TryRaycastGround(mousePos, out var groundPoint);
                    if (currentSelected != null)
                    {
                        // Manipulating:点空地忽略(避免误触取消)
                    }
                    else if (hitGround && currentIndex >= 0)
                    {
                        // 放置态:点地面放置(放完即退,放完 currentIndex=-1 后不再触发)
                        PlaceCurrent(groundPoint);
                    }
                }
            }

            if (held && _dragging != null)
            {
                float dist = ((Vector2)mousePos - _mouseDownPos).sqrMagnitude;
                if (!_didDrag && dist > dragThresholdPx * dragThresholdPx) _didDrag = true;

                if (_didDrag && TryRaycastGround(mousePos, out var groundPoint))
                {
                    Vector3 newPos = groundPoint - _dragOffset;
                    newPos.y = _dragging.transform.position.y;
                    _dragging.transform.position = newPos;
                }
            }

            if (up && _dragging != null)
            {
                if (_didDrag) SettleOne(_dragging);
                _dragging = null;
            }
        }

        // ---------------------------------------------------------
        // 准备放置模式:鼠标 hover 地面 → ghost 跟随
        // ---------------------------------------------------------
        // 三态:Idle / Placing / Manipulating
        //   Idle         : currentIndex<0 && currentSelected==null
        //   Placing      : currentIndex>=0 && currentSelected==null  (一次性,放完即退)
        //   Manipulating : currentSelected!=null                       (只有 ✓ 确认可退)
        // ---------------------------------------------------------
        void UpdateGhost()
        {
            // 编辑态不显示 ghost(避免覆盖选中框)
            if (currentSelected != null)
            {
                if (ghost != null) ghost.Attach(null);
                return;
            }

            // Idle 态不放 ghost(放完/还没选)
            if (currentIndex < 0)
            {
                if (ghost != null) ghost.Attach(null);
                return;
            }

            // grow 动画中也不放 ghost
            if (_isGrowing)
            {
                if (ghost != null) ghost.Attach(null);
                return;
            }

            // ghost 跟随当前 currentIndex 对应的 prefab
            if (ghost != null)
            {
                var spec = CurrentSpec;
                var prefab = ObjectDatabase.LoadPrefab(spec.prefabPath);
                if (prefab == null) { ghost.Attach(null); return; }

                // 关键:currentIndex 变了就要重建 _ghostSource,
                // 否则 ghost 永远显示第一次选的那个物件(prefab 不会自动切)
                if (_ghostSource == null || _ghostSourceIndex != currentIndex)
                {
                    if (_ghostSource != null) Destroy(_ghostSource);
                    _ghostSource = Instantiate(prefab);
                    _ghostSource.name = "_ghost_source_" + spec.id;
                    _ghostSource.SetActive(false);
                    _ghostSourceIndex = currentIndex;
                }
                ghost.Attach(_ghostSource);
            }
        }

        GameObject _ghostSource;
        int _ghostSourceIndex = int.MinValue;  // -2 sentinel:从未创建

        // ---------------------------------------------------------
        // 放置:原地从小变大
        // ---------------------------------------------------------
        void PlaceCurrent(Vector3 groundPoint)
        {
            // 防御:Idle / 已退出放置态时不应再触发
            if (currentIndex < 0) return;
            var spec = CurrentSpec;
            var prefab = ObjectDatabase.LoadPrefab(spec.prefabPath);
            if (prefab == null) return;

            GameObject go = Instantiate(prefab);
            go.name = spec.id + "_" + Time.frameCount;

            var item = go.GetComponent<ObjectItem>() ?? go.AddComponent<ObjectItem>();
            item.kind = spec.kind;
            item.height = spec.height;
            item.stackable = spec.stackable;

            EnsureCollider(go);
            EnsureUrpMaterials(go);

            Vector3 xz = SnapToGrid(new Vector3(groundPoint.x, 0, groundPoint.z));
            float baseTop = ResolveBaseY(xz);
            // 起始位置:y 在 baseTop + pivotToBottomOffset 上(底面贴地),scale 0
            float finalY = baseTop + item.pivotToBottomOffset + 0.02f;
            go.transform.position = new Vector3(xz.x, finalY, xz.z);
            go.transform.localScale = Vector3.zero;

            placed.Add(go);
            StartCoroutine(GrowAnimation(go));
            // 一次性:放完就清选择,菜单不再高亮,guest 隐藏
            currentIndex = -1;
        }

        IEnumerator GrowAnimation(GameObject go)
        {
            _isGrowing = true;
            float t = 0f;
            Vector3 targetScale = Vector3.one;
            while (t < growDuration)
            {
                if (go == null) break;
                t += Time.deltaTime;
                float k = Mathf.SmoothStep(0f, 1f, t / growDuration);
                go.transform.localScale = targetScale * k;
                yield return null;
            }
            if (go != null) go.transform.localScale = targetScale;

            _isGrowing = false;
            // v0.5:放置完成不进入 Manipulating — 用户想"再放一个"就直接点地面。
            // 想编辑已放置的 → 点那个物件。
        }

        // ---------------------------------------------------------
        // 选中/退出 物件
        // ---------------------------------------------------------
        void EnterManipulating(GameObject obj)
        {
            currentSelected = obj;
            // 跨态:从放置态进入编辑态时,清掉放置选择
            currentIndex = -1;
            RefreshSelection();
            // 不再自动 FocusOn — 用户不要选中时相机自动聚焦
        }

        public void ExitManipulating()
        {
            currentSelected = null;
            _dragging = null;
            RefreshSelection();
            if (isoCam != null) isoCam.CancelFocus();
        }

        public void DeleteSelected()
        {
            if (currentSelected == null) return;
            Delete(currentSelected);
        }

        // 屏幕按钮调
        public void RotateSelected(float degrees)
        {
            if (currentSelected == null) return;
            currentSelected.transform.Rotate(0f, degrees, 0f, Space.World);
            SettleStack();
        }

        public void ConfirmSelection()
        {
            ExitManipulating();
        }

        // ---------------------------------------------------------
        // 删除 + 子物件自动沉降
        // ---------------------------------------------------------
        void Delete(GameObject obj)
        {
            if (obj == null) return;
            placed.Remove(obj);
            if (currentSelected == obj)
            {
                currentSelected = null;
                RefreshSelection();
                if (isoCam != null) isoCam.CancelFocus();
            }
            if (_dragging == obj) _dragging = null;
            Destroy(obj);
            SettleStack();
        }

        /// <summary>
        /// 全部物件重新计算 y(删完 / 拖完 / 旋转后调)。
        /// </summary>
        public void SettleStack()
        {
            placed.Sort((a, b) => a.transform.position.y.CompareTo(b.transform.position.y));
            foreach (var go in placed)
            {
                if (go != null) SettleOne(go);
            }
        }

        /// <summary>
        /// 单个物件重新计算 y。
        /// </summary>
        void SettleOne(GameObject go)
        {
            var item = go.GetComponent<ObjectItem>();
            if (item == null) return;

            Vector3 xz = go.transform.position;
            xz.y = 0;
            float baseTop = ResolveBaseY(xz, go);
            float finalY = baseTop + item.pivotToBottomOffset + 0.02f;
            go.transform.position = new Vector3(xz.x, finalY, xz.z);
        }

        float ResolveBaseY(Vector3 xz, GameObject exclude = null)
        {
            float bestTopY = baseY;
            foreach (var other in placed)
            {
                if (other == null || other == exclude) continue;
                var oi = other.GetComponent<ObjectItem>();
                if (oi == null || !oi.stackable) continue;
                Vector3 p = other.transform.position;
                if (Mathf.Abs(p.x - xz.x) < 0.5f && Mathf.Abs(p.z - xz.z) < 0.5f)
                {
                    var ob = oi.GetWorldBounds();
                    float top = ob.max.y;
                    if (top > bestTopY) bestTopY = top;
                }
            }
            return bestTopY;
        }

        Vector3 SnapToGrid(Vector3 v)
        {
            if (gridSnap <= 0) return v;
            return new Vector3(
                Mathf.Round(v.x / gridSnap) * gridSnap,
                v.y,
                Mathf.Round(v.z / gridSnap) * gridSnap);
        }

        void RefreshSelection()
        {
            if (indicator != null) indicator.Attach(currentSelected);
        }

        // ---------------------------------------------------------
        // 射线
        // ---------------------------------------------------------
        bool TryRaycastObject(Vector2 screenPos, out GameObject obj, out Vector3 point)
        {
            Ray ray = _cam.ScreenPointToRay(screenPos);
            if (Physics.Raycast(ray, out var hit, 1000f, groundMask))
            {
                if (placed.Contains(hit.collider.gameObject))
                {
                    obj = hit.collider.gameObject;
                    point = hit.point;
                    return true;
                }
                var t = hit.collider.transform;
                while (t != null)
                {
                    if (placed.Contains(t.gameObject))
                    {
                        obj = t.gameObject;
                        point = hit.point;
                        return true;
                    }
                    t = t.parent;
                }
            }
            obj = null; point = default;
            return false;
        }

        bool TryRaycastGround(Vector2 screenPos, out Vector3 point)
        {
            Ray ray = _cam.ScreenPointToRay(screenPos);
            if (Physics.Raycast(ray, out var hit, 1000f, groundMask))
            {
                point = hit.point;
                return true;
            }
            point = default;
            return false;
        }

        /// <summary>
        /// 给物件补一个 BoxCollider(如果它本身没 Collider)。
        /// </summary>
        void EnsureCollider(GameObject go)
        {
            if (go.GetComponentInChildren<Collider>(true) != null) return;

            var renderers = go.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) return;

            Bounds localB = new Bounds(Vector3.zero, Vector3.one);
            bool hasLocal = false;
            foreach (var r in renderers)
            {
                Bounds b;
                if (r is SkinnedMeshRenderer smr)
                {
                    b = smr.localBounds;
                }
                else if (r is MeshRenderer mr && mr.TryGetComponent<MeshFilter>(out var mf) && mf.sharedMesh != null)
                {
                    b = mf.sharedMesh.bounds;
                }
                else
                {
                    b = r.bounds;
                    b.center = go.transform.InverseTransformPoint(b.center);
                    b.size = Vector3.Scale(b.size, new Vector3(
                        1f / Mathf.Max(0.001f, go.transform.lossyScale.x),
                        1f / Mathf.Max(0.001f, go.transform.lossyScale.y),
                        1f / Mathf.Max(0.001f, go.transform.lossyScale.z)));
                }
                if (!hasLocal) { localB = b; hasLocal = true; }
                else localB.Encapsulate(b);
            }

            if (!hasLocal) return;

            var box = go.AddComponent<BoxCollider>();
            box.center = localB.center;
            box.size = localB.size;
        }

        /// <summary>
        /// 把物件所有 Renderer 的材质换成 URP/Lit。
        /// </summary>
        void EnsureUrpMaterials(GameObject go)
        {
            var urpLit = Shader.Find("Universal Render Pipeline/Lit");
            if (urpLit == null) return;

            var renderers = go.GetComponentsInChildren<Renderer>(true);
            foreach (var r in renderers)
            {
                var mats = r.materials;
                for (int i = 0; i < mats.Length; i++)
                {
                    if (mats[i] == null) continue;
                    var sh = mats[i].shader;
                    if (sh == null) continue;
                    var n = sh.name;
                    if (n.StartsWith("Universal Render Pipeline/")) continue;
                    Color baseColor = Color.white;
                    if (mats[i].HasProperty("_BaseColor")) baseColor = mats[i].GetColor("_BaseColor");
                    else if (mats[i].HasProperty("_Color")) baseColor = mats[i].GetColor("_Color");
                    mats[i].shader = urpLit;
                    mats[i].SetColor("_BaseColor", baseColor);
                }
                r.materials = mats;
            }
        }
    }
}