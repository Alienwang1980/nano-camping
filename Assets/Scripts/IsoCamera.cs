using UnityEngine;

namespace NanoCamping
{
    /// <summary>
    /// 45° 等距相机 + 交互控制(v0.9 重写):
    ///   滚轮 / 键盘 +/- :缩放
    ///   Idle 态左键拖动:水平旋转(只改 azimuth)
    ///   右键拖动:平移焦点(pan)
    ///   选中物件不再自动聚焦 — 相机保持当前视角
    ///
    /// 关键:orthographicSize 是 IsoCamera 的字段,但实际渲染用 Camera.orthographicSize。
    /// 必须在每帧 ApplyTransform 同步,否则 Camera 的 view frustum 不变,transform.position
    /// 又因为 dist = orthoSize * sqrt(3) 移动,会出现"画面被切掉"的诡异效果。
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class IsoCamera : MonoBehaviour
    {
        public float orthographicSize = 5.5f;  // 默认拉近一些
        public float minSize = 2.5f;
        public float maxSize = 25f;
        public float zoomSpeed = 1.4f;        // 滚轮一格缩放倍率
        public float rotateSpeed = 2.5f;      // Idle 左键拖动 1px = 2.5°(用户要幅度大)
        public float panSpeed = 0.18f;        // 右键拖动 1px = 0.18m * size
        public float focusLerpSpeed = 6f;
        [Tooltip("相机到 focus 的固定距离(orthographic 下不依赖 orthoSize)")]
        public float cameraDistance = 14f;

        [Tooltip("拖动判定阈值(像素),避免按下时立即触发旋转/平移")]
        public float dragThresholdPx = 3f;

        Camera _cam;
        Vector3 _focusPoint = Vector3.zero;
        float _azimuth = 45f;
        float _elevation = 35.26f;

        Vector3 _defaultFocusPoint = Vector3.zero;
        float _defaultOrthoSize = 5.5f;

        // 聚焦过渡(已弃用:不再自动 FocusOn,但保留 CancelFocus 给将来用)
        bool _focusing;
        Vector3 _focusTarget;
        float _focusSizeTarget;
        float _focusSizeStart;
        Vector3 _focusPointStart;

        // 拖动状态
        bool _leftDragging;
        bool _rightDragging;
        Vector2 _leftDownPos;
        Vector2 _rightDownPos;
        // 本次 left down 是否用于旋转(由 PM 在 down 时通过 pmConsumedLeftDown 决定)
        // true  = 空白处按下,相机接管左键用于旋转
        // false = PM 接管(选中/拖动物件/放置),相机不响应
        bool _camRotateActive;

        public PlacementManager placement;  // 注入:用于判断是否在 Idle 态

        void Awake()
        {
            _cam = GetComponent<Camera>();
            _cam.orthographic = true;
            orthographicSize = 5.5f;
            _cam.orthographicSize = orthographicSize;
            _defaultFocusPoint = _focusPoint;
            _defaultOrthoSize = orthographicSize;
            ApplyTransform();
        }

        void Update()
        {
            HandleZoom();
            HandleRotateAndPan();
            UpdateFocus();
            ApplyTransform();
        }

        // -------------------------------------------------
        // 缩放:滚轮 + 键盘 +/- 多通道
        // 菜单打开时禁用(避免和菜单 scroll 冲突)
        // -------------------------------------------------
        void HandleZoom()
        {
            // 菜单打开时滚轮只滚动菜单,相机不响应
            if (placement != null && placement.IsMenuOpen) return;

            float delta = 0f;

            // 通道 1:鼠标滚轮
            float scroll = Input.mouseScrollDelta.y;
            if (Mathf.Abs(scroll) > 0.001f) delta += scroll;

            // 通道 2:键盘 +/- 和 [/]
            if (Input.GetKey(KeyCode.Equals) || Input.GetKey(KeyCode.Plus)
                || Input.GetKey(KeyCode.RightBracket))   // ] 拉近
                delta += 1f;
            if (Input.GetKey(KeyCode.Minus) || Input.GetKey(KeyCode.Underscore)
                || Input.GetKey(KeyCode.LeftBracket))    // [ 拉远
                delta -= 1f;

            if (Mathf.Abs(delta) < 0.001f) return;

            float factor = delta > 0 ? 1f / zoomSpeed : zoomSpeed;
            orthographicSize = Mathf.Clamp(orthographicSize * factor, minSize, maxSize);

            // 缩放打断聚焦
            if (_focusing) _focusing = false;
        }

        // -------------------------------------------------
        // 鼠标交互:Idle 态左键旋转 + 右键平移
        // 左键 Idle 拖动:水平旋转(只改 azimuth,保持 elevation)
        // 右键拖动:平移焦点(任何模式都生效)
        // -------------------------------------------------
        void HandleRotateAndPan()
        {
            bool idle = IsIdle();
            bool leftHeld = Input.GetMouseButton(0);
            bool rightHeld = Input.GetMouseButton(1);

            // 左键 down — 询问 PM 是否要占用左键
            if (Input.GetMouseButtonDown(0))
            {
                _leftDragging = true;
                _leftDownPos = Input.mousePosition;
                // PM 命中物件(选中/拖动)或 放置态点地面 → PM 占用,相机不响应
                bool pmWants = placement != null && placement.pmConsumedLeftDown;
                _camRotateActive = !pmWants;
            }
            if (Input.GetMouseButtonUp(0))
            {
                _leftDragging = false;
                _camRotateActive = false;
            }

            // 右键 down
            if (Input.GetMouseButtonDown(1))
            {
                _rightDragging = true;
                _rightDownPos = Input.mousePosition;
            }
            if (Input.GetMouseButtonUp(1))
            {
                _rightDragging = false;
            }

            // 左键拖动 → 旋转(只要 PM 没占用)
            if (_camRotateActive && _leftDragging && leftHeld)
            {
                float mx = Input.GetAxisRaw("Mouse X");
                if (Mathf.Abs(mx) > 0.001f)
                    _azimuth -= mx * rotateSpeed;
                // 不改 elevation — 保持当前高度
            }

            // 右键拖动 → 平移(任何模式都允许)
            if (_rightDragging && rightHeld)
            {
                float mx = Input.GetAxisRaw("Mouse X");
                float my = Input.GetAxisRaw("Mouse Y");
                if (Mathf.Abs(mx) > 0.001f || Mathf.Abs(my) > 0.001f)
                {
                    var right = transform.right;
                    var up = transform.up;
                    float scale = panSpeed * orthographicSize * 0.1f;
                    _focusPoint -= right * mx * scale;
                    _focusPoint -= up * my * scale;
                }
            }
        }

        bool IsIdle()
        {
            return placement == null
                || (placement.currentSelected == null && placement.currentIndex < 0);
        }

        // -------------------------------------------------
        // 聚焦过渡(已弃用 — 不再被 EnterManipulating 调用,保留以备将来)
        // -------------------------------------------------
        public void FocusOn(Vector3 worldPoint, float targetSize)
        {
            _focusing = true;
            _focusTarget = worldPoint;
            _focusSizeTarget = Mathf.Clamp(targetSize, minSize, maxSize);
            _focusSizeStart = orthographicSize;
            _focusPointStart = _focusPoint;
        }

        public void CancelFocus()
        {
            if (Mathf.Approximately(orthographicSize, _defaultOrthoSize)
                && (_focusPoint - _defaultFocusPoint).sqrMagnitude < 0.0001f)
            {
                _focusing = false;
                return;
            }
            _focusing = true;
            _focusTarget = _defaultFocusPoint;
            _focusSizeTarget = _defaultOrthoSize;
            _focusSizeStart = orthographicSize;
            _focusPointStart = _focusPoint;
        }

        void UpdateFocus()
        {
            if (!_focusing) return;
            float t = 1f - Mathf.Exp(-focusLerpSpeed * Time.deltaTime);
            _focusPoint = Vector3.Lerp(_focusPoint, _focusTarget, t);
            orthographicSize = Mathf.Lerp(orthographicSize, _focusSizeTarget, t);
            if ((_focusPoint - _focusTarget).sqrMagnitude < 0.0001f
                && Mathf.Abs(orthographicSize - _focusSizeTarget) < 0.01f)
            {
                _focusPoint = _focusTarget;
                orthographicSize = _focusSizeTarget;
                _focusing = false;
            }
        }

        // -------------------------------------------------
        // 应用 _azimuth / _elevation / _focusPoint → transform + camera
        // 关键改动(orthographic):
        //   - camera 到 focus 的距离 = cameraDistance(固定),不依赖 orthoSize
        //     (原来 dist = orthoSize * sqrt(3) 会让相机随 zoom 拉近/拉远,导致拉近到
        //      一定距离后画面出现 clipping / 缺损)
        //   - orthoSize 只控制 Camera 的 view frustum 大小
        // -------------------------------------------------
        void ApplyTransform()
        {
            if (_cam != null) _cam.orthographicSize = orthographicSize;

            float az = _azimuth * Mathf.Deg2Rad;
            float el = _elevation * Mathf.Deg2Rad;
            float dist = cameraDistance;  // 固定距离,不再随 orthoSize 变化
            Vector3 dir = new Vector3(
                Mathf.Cos(el) * Mathf.Sin(az),
                Mathf.Sin(el),
                Mathf.Cos(el) * Mathf.Cos(az)
            );
            transform.position = _focusPoint + dir * dist;
            transform.LookAt(_focusPoint, Vector3.up);
        }

        public Vector3 FocusPoint => _focusPoint;
        public float OrthographicSize => orthographicSize;
    }
}