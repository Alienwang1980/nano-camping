using UnityEngine;

namespace NanoCamping
{
    /// <summary>
    /// v0.3 屏幕 UI:
    ///   左上 ≡ → 打开物件菜单(ObjectPickerMenu)
    ///   右上 ✕ → 删除当前选中(只在 currentSelected != null 时高亮)
    ///   右下 ⟲ ⟳ → 旋转 90°(RepeatButton 长按累积)
    ///   顶中 状态条(当前类型 / 选中物件 / 物件数)
    ///   底部 帮助行
    ///
    /// 物件菜单打开时本 HUD 的物件条隐藏(避免重复)。
    /// </summary>
    public class HUD : MonoBehaviour
    {
        public PlacementManager placement;
        public ObjectPickerMenu picker;        // 由 Bootstrap 注入
        public bool menuOpen => picker != null && picker.IsOpen;

        // 旋转速度(秒/90°):长按 1 秒转 90°
        public float rotateStepDeg = 90f;
        public float rotateSpeedPerSec = 180f;  // 长按时累积速度

        // 样式
        GUIStyle _label, _labelActive;
        GUIStyle _btnBig, _btnBigActive;
        GUIStyle _btnSmall, _btnSmallActive;
        GUIStyle _help;

        // 共享背景纹理(只创建一次,避免 NRE)
        Texture2D _texBtnNormal;
        Texture2D _texBtnActive;

        void EnsureStyles()
        {
            if (_label != null) return;

            _texBtnNormal = MakeTex(new Color(0.1f, 0.1f, 0.15f, 0.85f));
            _texBtnActive = MakeTex(new Color(0.4f, 0.35f, 0.1f, 0.95f));

            _label = new GUIStyle(GUI.skin.box);
            _label.normal.textColor = Color.white;
            _label.fontSize = 13;
            _label.padding = new RectOffset(10, 10, 6, 6);
            _label.alignment = TextAnchor.MiddleCenter;

            _labelActive = new GUIStyle(_label);
            _labelActive.normal.textColor = Color.yellow;

            // 大按钮(≡ ✕ ⟲⟳)
            _btnBig = MakeBtn(22, Color.white, _texBtnNormal);
            _btnBigActive = MakeBtn(22, Color.yellow, _texBtnActive);

            // 小按钮(目前未用,保留兼容)
            _btnSmall = _btnBig;
            _btnSmallActive = _btnBigActive;

            _help = new GUIStyle(_label);
            _help.fontSize = 11;
            _help.normal.textColor = new Color(1, 1, 1, 0.7f);
        }

        Texture2D MakeTex(Color c)
        {
            var t = new Texture2D(1, 1);
            t.SetPixel(0, 0, c);
            t.Apply();
            t.hideFlags = HideFlags.HideAndDontSave;
            return t;
        }

        GUIStyle MakeBtn(int fontSize, Color text, Texture2D tex)
        {
            var s = new GUIStyle();
            s.fontSize = fontSize;
            s.padding = new RectOffset(6, 6, 4, 4);
            s.alignment = TextAnchor.MiddleCenter;
            s.normal.background = tex;
            s.normal.textColor = text;
            s.hover.background = tex;
            s.hover.textColor = text;
            s.active.background = tex;
            s.active.textColor = text;
            s.wordWrap = true;
            return s;
        }

        void OnGUI()
        {
            if (placement == null) return;
            EnsureStyles();
            if (menuOpen) return;  // 菜单打开时让位

            DrawTopBar();
            DrawMenuButton();
            DrawHelpLine();

            // 选中态:屏幕底部固定 UI 横排 ⟲ ⟳ ✕ ✓(跟物体无关,纯 UI 位置)
            if (placement.currentSelected != null)
            {
                DrawActionButtonsNearObject();
            }
        }

        void DrawTopBar()
        {
            GUILayout.BeginArea(new Rect(10, 10, Screen.width - 20, 30));
            GUILayout.BeginHorizontal();
            // 状态名 + 物件名(明确的 3 态机显示)
            string stateText;
            if (placement.currentSelected != null)
                stateText = $"【编辑态】{placement.currentSelected.name}  (拖动 / ⟲⟳ 旋转 / ✕ 删除 / ✓ 确认)";
            else if (placement.currentIndex >= 0)
                stateText = $"【放置态】{placement.CurrentSpec.displayName}  (左键地面放置,放完即退)";
            else
                stateText = "【空闲】  (≡ 菜单选物件 / 点已放置物件进入编辑 / 右键旋转 / 滚轮缩放 / Shift+拖动平移)";

            string typeText = placement.currentIndex >= 0
                ? $"类型: {placement.CurrentSpec.displayName}"
                : "类型: (无,≡ 菜单选择)";
            GUILayout.Label(typeText, _label, GUILayout.Width(180));
            GUILayout.Label(stateText, placement.currentSelected ? _labelActive : _label, GUILayout.Width(660));
            GUILayout.Label($"已放置: {placement.placed.Count}", _label, GUILayout.Width(120));
            GUILayout.FlexibleSpace();
            GUILayout.Label("NanoCamping · 露营场景拼装", _label);
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        void DrawMenuButton()
        {
            // 左上角 ≡ 菜单按钮
            var rect = new Rect(10, 50, 60, 60);
            if (GUI.Button(rect, "≡\n菜单", _btnBig))
            {
                if (picker != null) picker.Toggle();
            }
        }

        void DrawActionButtonsNearObject()
        {
            // ✕ / ⟲ / ⟳ + ✓ 4 个按钮,**固定 UI**,不跟随物件 bbox/旋转/拖动。
            // 屏幕底部居中横排,玩家永远在同一个位置点到。
            GameObject sel = placement.currentSelected;
            if (sel == null) return;

            var cam = Camera.main;
            if (cam == null) return;
            Rect pr = cam.pixelRect;
            float viewW = pr.width;
            float viewH = pr.height;

            int btnSize = 56;
            int gap = 8;
            // 4 个按钮宽度:3 个操作 + 1 个确认,中间多空一点隔开
            int confirmGap = 16;
            float totalW = btnSize * 4 + gap * 2 + confirmGap;
            float rowY = viewH - btnSize - 24f;  // 底部上方 24px(留 22 给帮助行)
            float startX = (viewW - totalW) * 0.5f;

            // 从左到右:⟲ ⟳ ✕  | (gap) | ✓
            var rot1Rect = new Rect(startX,                          rowY, btnSize, btnSize);
            var rot2Rect = new Rect(startX + (btnSize + gap),        rowY, btnSize, btnSize);
            var delRect  = new Rect(startX + (btnSize + gap) * 2,    rowY, btnSize, btnSize);
            var okRect   = new Rect(startX + (btnSize + gap) * 3 + confirmGap, rowY, btnSize, btnSize);

            var style = _btnBigActive;
            float deltaAngle = rotateSpeedPerSec * Time.deltaTime;
            if (GUI.RepeatButton(rot1Rect, "⟲", style))
                placement.RotateSelected(-deltaAngle);
            if (GUI.RepeatButton(rot2Rect, "⟳", style))
                placement.RotateSelected(deltaAngle);
            if (GUI.Button(delRect, "✕", style))
                placement.DeleteSelected();
            if (GUI.Button(okRect, "✓", style))
                placement.ConfirmSelection();
        }

        void DrawHelpLine()
        {
            GUILayout.BeginArea(new Rect(0, camHeightSafe() - 22, camWidthSafe(), 22));
            string text;
            if (placement.currentSelected != null)
                text = "【编辑态】拖动移动 · ⟲⟳ 旋转 · ✕ 删除 · ✓ 确认(退出)";
            else if (placement.currentIndex >= 0)
                text = "【放置态】左键地面放置(放完即退)· 地面跟随 cyan ghost";
            else
                text = "≡ 菜单选物件 / 点已放置物件进入编辑 / 右键旋转 / 滚轮缩放 / Shift+拖动平移";
            GUILayout.Label(text, _help);
            GUILayout.EndArea();
        }

        // 顶部条用 Screen.width/height 时,Editor Game view 可能溢出。
        // 这里 fallback 到 cam.pixelRect,Editor 没相机时用 Screen。
        float camWidthSafe() => Camera.main != null ? Camera.main.pixelRect.width : Screen.width;
        float camHeightSafe() => Camera.main != null ? Camera.main.pixelRect.height : Screen.height;
    }
}
