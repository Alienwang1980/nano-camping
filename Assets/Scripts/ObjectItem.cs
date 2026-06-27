using UnityEngine;

namespace NanoCamping
{
    /// <summary>
    /// 物件 metadata:kind + 真实包围盒 + 可堆叠标记。
    /// 包围盒运行时通过 GetWorldBounds() 动态算(读 Renderer.bounds,无需烘焙)。
    /// </summary>
    public class ObjectItem : MonoBehaviour
    {
        public enum Kind { Tent, Fire, Chair, Tree, Person, Table, Flower, Box, Lantern, Tool, Medical, Stove, Prop }

        public Kind kind;

        [Tooltip("可堆叠:其他物件可以放它顶上(桌子/箱子/桶等)")]
        public bool stackable = false;

        [Tooltip("占地估算高度(m)。仅用于菜单/列表显示,实际落位用真实包围盒。")]
        public float height = 0.5f;

        /// <summary>
        /// pivot 离模型底面的偏移(m,常量,跟位置无关)。
        /// Awake 时基于 Renderer.bounds 算一次,SettleOne 直接用,
        /// 避免每次都重复算 bounds。
        /// </summary>
        public float pivotToBottomOffset = -1f;

        void Awake()
        {
            // 首次拿 bounds,缓存 pivot 到底面的偏移
            if (pivotToBottomOffset < 0f)
            {
                var b = GetWorldBounds();
                pivotToBottomOffset = transform.position.y - b.min.y;
            }
        }

        /// <summary>
        /// 物件在世界空间的真实包围盒(包含子级 Renderer)。
        /// Renderer.bounds 是缓存的,Encapsulate 是值类型,不昂贵。
        /// </summary>
        public Bounds GetWorldBounds()
        {
            var renderers = GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
                return new Bounds(transform.position, Vector3.one * 0.5f);

            var b = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                b.Encapsulate(renderers[i].bounds);
            return b;
        }

        /// <summary>
        /// 仅 XZ 平面包围盒(用于 footprint / 占用判定)。
        /// </summary>
        public Bounds GetWorldBoundsXZ()
        {
            var b = GetWorldBounds();
            return new Bounds(new Vector3(b.center.x, b.min.y, b.center.z),
                              new Vector3(b.size.x, 0.01f, b.size.z));
        }

        void OnDrawGizmosSelected()
        {
            // Editor 选中时画绿框(仅 Scene 视图)
            Gizmos.color = Color.green;
            var b = GetWorldBounds();
            Gizmos.DrawWireCube(b.center, b.size);
        }
    }
}
