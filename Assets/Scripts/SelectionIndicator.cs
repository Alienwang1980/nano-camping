using UnityEngine;

namespace NanoCamping
{
    /// <summary>
    /// 选中状态图形化:用 12 条 LineRenderer 画包围盒的底/顶矩形 + 4 立柱。
    /// 绑到 currentSelected,LateUpdate 中按 Renderer.bounds 动态计算 8 角点。
    /// 取消选中时 Hide。
    /// </summary>
    public class SelectionIndicator : MonoBehaviour
    {
        // 半透明,不要遮挡物件内容(原 0.95 太厚)
        public Color color = new Color(1f, 0.85f, 0.1f, 0.45f);
        public float lineWidth = 0.035f;

        GameObject _target;
        LineRenderer[] _lines;  // 12 条:底 4 边 + 顶 4 边 + 4 立柱

        void Awake()
        {
            _lines = new LineRenderer[12];
            for (int i = 0; i < 12; i++)
            {
                var go = new GameObject("sel_line_" + i);
                go.transform.SetParent(transform, false);
                var lr = go.AddComponent<LineRenderer>();
                lr.positionCount = 2;
                lr.startWidth = lr.endWidth = lineWidth;
                lr.material = new Material(Shader.Find("Sprites/Default"));
                lr.startColor = lr.endColor = color;
                lr.useWorldSpace = true;
                lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                lr.receiveShadows = false;
                _lines[i] = lr;
            }
            gameObject.SetActive(false);
        }

        /// <summary>
        /// 绑定目标物件。null = 隐藏。
        /// </summary>
        public void Attach(GameObject target)
        {
            _target = target;
            gameObject.SetActive(target != null);
        }

        void LateUpdate()
        {
            if (_target == null) { gameObject.SetActive(false); return; }

            var renderers = _target.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) { gameObject.SetActive(false); return; }

            var b = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                b.Encapsulate(renderers[i].bounds);

            // 留一点 padding,免得线穿模
            var padded = new Bounds(b.center, b.size + Vector3.one * 0.02f);

            Vector3 c = padded.center, e = padded.extents;
            Vector3[] corners = {
                new Vector3(c.x - e.x, c.y - e.y, c.z - e.z), // 0 下 -x -z
                new Vector3(c.x + e.x, c.y - e.y, c.z - e.z), // 1 下 +x -z
                new Vector3(c.x + e.x, c.y - e.y, c.z + e.z), // 2 下 +x +z
                new Vector3(c.x - e.x, c.y - e.y, c.z + e.z), // 3 下 -x +z
                new Vector3(c.x - e.x, c.y + e.y, c.z - e.z), // 4 上 -x -z
                new Vector3(c.x + e.x, c.y + e.y, c.z - e.z), // 5 上 +x -z
                new Vector3(c.x + e.x, c.y + e.y, c.z + e.z), // 6 上 +x +z
                new Vector3(c.x - e.x, c.y + e.y, c.z + e.z), // 7 上 -x +z
            };

            // 底 4 边:0-1, 1-2, 2-3, 3-0
            Set(0, corners[0], corners[1]);
            Set(1, corners[1], corners[2]);
            Set(2, corners[2], corners[3]);
            Set(3, corners[3], corners[0]);
            // 顶 4 边:4-5, 5-6, 6-7, 7-4
            Set(4, corners[4], corners[5]);
            Set(5, corners[5], corners[6]);
            Set(6, corners[6], corners[7]);
            Set(7, corners[7], corners[4]);
            // 立柱 4:0-4, 1-5, 2-6, 3-7
            Set(8, corners[0], corners[4]);
            Set(9, corners[1], corners[5]);
            Set(10, corners[2], corners[6]);
            Set(11, corners[3], corners[7]);
        }

        void Set(int idx, Vector3 a, Vector3 b)
        {
            _lines[idx].SetPosition(0, a);
            _lines[idx].SetPosition(1, b);
        }
    }
}
