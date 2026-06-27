using System.Collections.Generic;
using UnityEngine;

namespace NanoCamping
{
    /// <summary>
    /// v0.4 全屏物件选择菜单 — 用 prefab 缩略图代替纯文字。
    ///
    /// 缩略图烘焙:Assets/Thumbnails/{id}.png(由 Editor 工具生成)。
    /// 菜单打开时按需加载(LoadAssetAtPath,Editor 模式)
    /// 或 Resources(运行时)。这里优先 Editor path,缺失则回退到文字。
    ///
    /// 按主题分组:帐篷 / 火 / 桌椅 / 人物 / 自然 / 道具。
    /// 点 item → 关闭菜单 + 设 placement.currentIndex + 清 currentSelected。
    /// Esc 关闭菜单。
    /// </summary>
    public class ObjectPickerMenu : MonoBehaviour
    {
        public PlacementManager placement;
        public bool IsOpen { get; private set; }

        static readonly (string title, ObjectItem.Kind[] kinds, Color tint)[] Groups = new[]
        {
            ("帐篷",  new[] { ObjectItem.Kind.Tent }, new Color(0.9f, 0.7f, 0.5f)),
            ("火堆",  new[] { ObjectItem.Kind.Fire, ObjectItem.Kind.Stove }, new Color(1.0f, 0.55f, 0.2f)),
            ("桌椅",  new[] { ObjectItem.Kind.Table, ObjectItem.Kind.Chair }, new Color(0.7f, 0.7f, 0.5f)),
            ("人物",  new[] { ObjectItem.Kind.Person }, new Color(0.55f, 0.75f, 1.0f)),
            ("自然",  new[] { ObjectItem.Kind.Tree, ObjectItem.Kind.Flower }, new Color(0.55f, 0.85f, 0.55f)),
            ("道具",  new[] { ObjectItem.Kind.Box, ObjectItem.Kind.Lantern, ObjectItem.Kind.Tool, ObjectItem.Kind.Medical, ObjectItem.Kind.Prop },
                                          new Color(0.7f, 0.65f, 0.85f)),
        };

        GUIStyle _overlay, _title, _groupLabel, _itemBg, _itemBgActive, _closeBtn, _itemText;

        // 缩略图缓存:id → Texture2D(首次访问时懒加载)
        readonly Dictionary<string, Texture2D> _thumbs = new Dictionary<string, Texture2D>();

        // 缩略图缺失时的占位背景(共享 1x1)
        Texture2D _placeholderTex;

        void EnsureStyles()
        {
            if (_overlay != null) return;

            // 半透明黑 overlay(全屏)
            var overlayTex = new Texture2D(1, 1);
            overlayTex.SetPixel(0, 0, new Color(0, 0, 0, 0.72f));
            overlayTex.Apply();
            overlayTex.hideFlags = HideFlags.HideAndDontSave;
            _overlay = new GUIStyle();
            _overlay.normal.background = overlayTex;

            _title = new GUIStyle(GUI.skin.box);
            _title.normal.textColor = Color.white;
            _title.fontSize = 22;
            _title.padding = new RectOffset(20, 20, 12, 12);
            _title.alignment = TextAnchor.MiddleCenter;

            _groupLabel = new GUIStyle(GUI.skin.label);
            _groupLabel.normal.textColor = new Color(1, 1, 1, 0.85f);
            _groupLabel.fontSize = 16;
            _groupLabel.padding = new RectOffset(6, 6, 6, 6);

            // 缩略图格子背景(深蓝灰)
            var bgTex = new Texture2D(1, 1);
            bgTex.SetPixel(0, 0, new Color(0.12f, 0.15f, 0.20f, 0.95f));
            bgTex.Apply();
            bgTex.hideFlags = HideFlags.HideAndDontSave;
            _itemBg = new GUIStyle();
            _itemBg.normal.background = bgTex;
            _itemBg.padding = new RectOffset(2, 2, 2, 2);
            _itemBg.alignment = TextAnchor.MiddleCenter;

            // 选中态(亮黄边)
            var activeTex = new Texture2D(1, 1);
            activeTex.SetPixel(0, 0, new Color(0.55f, 0.45f, 0.10f, 1f));
            activeTex.Apply();
            activeTex.hideFlags = HideFlags.HideAndDontSave;
            _itemBgActive = new GUIStyle();
            _itemBgActive.normal.background = activeTex;
            _itemBgActive.padding = new RectOffset(2, 2, 2, 2);
            _itemBgActive.alignment = TextAnchor.MiddleCenter;

            // 名字文字(右下角半透明黑底)
            _itemText = new GUIStyle(GUI.skin.label);
            _itemText.fontSize = 12;
            _itemText.alignment = TextAnchor.LowerCenter;
            _itemText.normal.textColor = Color.white;
            _itemText.wordWrap = false;

            _closeBtn = new GUIStyle(GUI.skin.button);
            _closeBtn.fontSize = 18;
            _closeBtn.padding = new RectOffset(12, 12, 8, 8);

            // 占位背景(深灰)
            _placeholderTex = new Texture2D(1, 1);
            _placeholderTex.SetPixel(0, 0, new Color(0.18f, 0.18f, 0.22f, 1f));
            _placeholderTex.Apply();
            _placeholderTex.hideFlags = HideFlags.HideAndDontSave;
        }

        Texture2D GetThumb(string id)
        {
            if (_thumbs.TryGetValue(id, out var cached)) return cached;

#if UNITY_EDITOR
            var path = "Assets/Thumbnails/" + id + ".png";
            var tex = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (tex != null)
            {
                _thumbs[id] = tex;
                return tex;
            }
#endif
            _thumbs[id] = _placeholderTex;  // 缺失用占位
            return _placeholderTex;
        }

        public void Toggle()
        {
            IsOpen = !IsOpen;
        }

        public void Close()
        {
            IsOpen = false;
        }

        void Update()
        {
            if (IsOpen && Input.GetKeyDown(KeyCode.Escape))
                IsOpen = false;
        }

        void OnGUI()
        {
            if (!IsOpen) return;
            if (placement == null) return;
            EnsureStyles();

            // 全屏 overlay
            GUI.Box(new Rect(0, 0, Screen.width, Screen.height), GUIContent.none, _overlay);

            // 顶部标题 + 关闭
            GUILayout.BeginArea(new Rect(0, 20, Screen.width, 50));
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label("选择物件", _title);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("✕ 关闭", _closeBtn, GUILayout.Width(100), GUILayout.Height(40)))
                IsOpen = false;
            GUILayout.EndHorizontal();
            GUILayout.EndArea();

            // scroll 区域
            float topY = 90;
            float botY = Screen.height - 40;
            GUILayout.BeginArea(new Rect(20, topY, Screen.width - 40, botY - topY));
            _scroll = GUILayout.BeginScrollView(_scroll, false, true);

            // 缩略图格子尺寸:Game view 692px → 4 列,每格 ~160px
            const int cellW = 160;
            const int cellH = 130;  // 缩略图 110 + 名字 20
            const int thumbH = 110;
            const int cols = 4;

            foreach (var (gTitle, kinds, tint) in Groups)
            {
                var entries = new List<int>();
                for (int i = 0; i < ObjectDatabase.Count; i++)
                {
                    var k = ObjectDatabase.Get(i).kind;
                    if (System.Array.IndexOf(kinds, k) >= 0) entries.Add(i);
                }
                if (entries.Count == 0) continue;

                GUILayout.Space(8);
                GUILayout.Label($"▎{gTitle}({entries.Count})", _groupLabel);

                int row = 0;
                while (row < entries.Count)
                {
                    GUILayout.BeginHorizontal();
                    for (int c = 0; c < cols && row < entries.Count; c++, row++)
                    {
                        int entryIdx = entries[row];
                        var entry = ObjectDatabase.Get(entryIdx);
                        bool active = (placement.currentIndex == entryIdx);
                        var style = active ? _itemBgActive : _itemBg;

                        // 一个 cell = 整格按钮(背景色 = 选中色/普通色)
                        if (GUILayout.Button(GUIContent.none, style, GUILayout.Width(cellW), GUILayout.Height(cellH)))
                        {
                            placement.currentIndex = entryIdx;
                            placement.currentSelected = null;
                            IsOpen = false;
                        }

                        // 在刚才画的按钮 Rect 上覆盖:缩略图 + 名字条
                        var last = GUILayoutUtility.GetLastRect();
                        var thumbRect = new Rect(last.x + 2, last.y + 2, last.width - 4, thumbH);
                        var nameRect = new Rect(last.x, last.y + thumbH + 2, last.width, cellH - thumbH - 2);

                        var thumb = GetThumb(entry.id);
                        if (thumb != null)
                            GUI.DrawTexture(thumbRect, thumb, ScaleMode.ScaleToFit);

                        // 名字(右下角)
                        GUI.Label(nameRect, entry.displayName, _itemText);
                    }
                    GUILayout.EndHorizontal();
                }
            }

            GUILayout.EndScrollView();
            GUILayout.EndArea();

            // 底部提示
            GUI.Label(new Rect(0, Screen.height - 30, Screen.width, 24),
                      "点选切换当前物件类型 · Esc 关闭",
                      _groupLabel);
        }

        Vector2 _scroll;
    }
}