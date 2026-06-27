using UnityEngine;

namespace NanoCamping
{
    /// <summary>
    /// v0.3 物件清单:30 件,按 6 主题(帐篷/火/桌椅/人物/自然/道具)。
    ///
    /// 字段:
    ///   id          : 短 ID(用于命名)
    ///   displayName : 中文显示名
    ///   kind        : ObjectItem.Kind 分类
    ///   height      : 估算占地高度(m,仅菜单/列表显示,实际落位用真实 Renderer.bounds)
    ///   stackable   : 能否堆叠(其他物件放在它顶上)
    ///   prefabPath  : 相对 Assets/ 的 prefab 路径
    ///
    /// 注:篝火 prefab 拼写是 Bornfire(漏字母 g),引用时严格保留。
    /// </summary>
    public static class ObjectDatabase
    {
        public struct Entry
        {
            public string id;
            public string displayName;
            public ObjectItem.Kind kind;
            public float height;
            public bool stackable;
            public string prefabPath;
        }

        public static readonly Entry[] All = new[]
        {
            // ── 帐篷(5)──
            new Entry { id = "tent1",      displayName = "帐篷 A", kind = ObjectItem.Kind.Tent, height = 1.2f, stackable = false,
                        prefabPath = "Harpia Games/Camping Pack/Prefabs/Camping/Tend camping 1.prefab" },
            new Entry { id = "tent2",      displayName = "帐篷 B", kind = ObjectItem.Kind.Tent, height = 1.2f, stackable = false,
                        prefabPath = "Harpia Games/Camping Pack/Prefabs/Camping/Tend camping 2.prefab" },
            new Entry { id = "tent_p1",    displayName = "军用帐篷", kind = ObjectItem.Kind.Tent, height = 1.0f, stackable = false,
                        prefabPath = "Pandazole_Ultimate_Pack/Pandazole Survival Crafting Pack/Prefabs/Tent_01.prefab" },
            new Entry { id = "tent_p2",    displayName = "露营帐", kind = ObjectItem.Kind.Tent, height = 1.0f, stackable = false,
                        prefabPath = "Pandazole_Ultimate_Pack/Pandazole Survival Crafting Pack/Prefabs/Tent_02.prefab" },
            new Entry { id = "tent_p3",    displayName = "尖顶帐", kind = ObjectItem.Kind.Tent, height = 1.1f, stackable = false,
                        prefabPath = "Pandazole_Ultimate_Pack/Pandazole Survival Crafting Pack/Prefabs/Tent_03.prefab" },

            // ── 火(4)──
            new Entry { id = "fire1",      displayName = "篝火",   kind = ObjectItem.Kind.Fire, height = 0.5f, stackable = false,
                        prefabPath = "Harpia Games/Camping Pack/Prefabs/Camping/Bornfire.prefab" },
            new Entry { id = "fire_wood",  displayName = "柴堆",   kind = ObjectItem.Kind.Fire, height = 0.5f, stackable = false,
                        prefabPath = "Harpia Games/Camping Pack/Prefabs/Camping/Bornfire wood logs.prefab" },
            new Entry { id = "fire_p",     displayName = "营火",   kind = ObjectItem.Kind.Fire, height = 0.5f, stackable = false,
                        prefabPath = "Pandazole_Ultimate_Pack/Pandazole Survival Crafting Pack/Prefabs/Campfire.prefab" },
            new Entry { id = "stove",      displayName = "便携炉", kind = ObjectItem.Kind.Stove, height = 0.4f, stackable = false,
                        prefabPath = "Harpia Games/Camping Pack/Prefabs/Camping/Portable stove.prefab" },

            // ── 桌椅(4)──
            new Entry { id = "table1",     displayName = "野餐桌", kind = ObjectItem.Kind.Table, height = 0.8f, stackable = true,
                        prefabPath = "Harpia Games/Camping Pack/Prefabs/Camping/Camping table 1.prefab" },
            new Entry { id = "table2",     displayName = "圆桌",   kind = ObjectItem.Kind.Table, height = 0.8f, stackable = true,
                        prefabPath = "DenysAlmaral/CityPeople/Prefabs/PROPS/dummy props/dprop_roundTable.prefab" },
            new Entry { id = "table_pic",  displayName = "野餐桌 2", kind = ObjectItem.Kind.Table, height = 0.8f, stackable = true,
                        prefabPath = "Harpia Games/Camping Pack/Prefabs/Camping/Picinic table.prefab" },
            new Entry { id = "chair",      displayName = "椅子",   kind = ObjectItem.Kind.Chair, height = 0.7f, stackable = false,
                        prefabPath = "DenysAlmaral/CityPeople/Prefabs/PROPS/dummy props/dprop_chair.prefab" },

            // ── 人物(6)──
            new Entry { id = "male_g",     displayName = "男士",   kind = ObjectItem.Kind.Person, height = 1.8f, stackable = false,
                        prefabPath = "DenysAlmaral/CityPeople/Prefabs/city/casual_Male_G.prefab" },
            new Entry { id = "female_g",   displayName = "女士",   kind = ObjectItem.Kind.Person, height = 1.7f, stackable = false,
                        prefabPath = "DenysAlmaral/CityPeople/Prefabs/city/casual_Female_G.prefab" },
            new Entry { id = "doctor",     displayName = "医生",   kind = ObjectItem.Kind.Person, height = 1.8f, stackable = false,
                        prefabPath = "DenysAlmaral/CityPeople/Prefabs/professions/Doctor_Male_B.prefab" },
            new Entry { id = "police",     displayName = "警察",   kind = ObjectItem.Kind.Person, height = 1.7f, stackable = false,
                        prefabPath = "DenysAlmaral/CityPeople/Prefabs/professions/police_Female_A.prefab" },
            new Entry { id = "elder",      displayName = "老人",   kind = ObjectItem.Kind.Person, height = 1.6f, stackable = false,
                        prefabPath = "DenysAlmaral/CityPeople/Prefabs/elder/elder_Female_A.prefab" },
            new Entry { id = "boy",        displayName = "男孩",   kind = ObjectItem.Kind.Person, height = 1.2f, stackable = false,
                        prefabPath = "DenysAlmaral/CityPeople/Prefabs/little_kids/little_boy_B.prefab" },

            // ── 自然(6)──
            new Entry { id = "pine1",      displayName = "松树",   kind = ObjectItem.Kind.Tree, height = 2.5f, stackable = false,
                        prefabPath = "Harpia Games/Camping Pack/Prefabs/Nature/Pine 1.prefab" },
            new Entry { id = "pine3",      displayName = "松树 B", kind = ObjectItem.Kind.Tree, height = 2.5f, stackable = false,
                        prefabPath = "Harpia Games/Camping Pack/Prefabs/Nature/Pine tree 3.prefab" },
            new Entry { id = "pine4",      displayName = "松树 C", kind = ObjectItem.Kind.Tree, height = 2.5f, stackable = false,
                        prefabPath = "Harpia Games/Camping Pack/Prefabs/Nature/Pine tree 4.prefab" },
            new Entry { id = "bush",       displayName = "灌木丛", kind = ObjectItem.Kind.Tree, height = 1.0f, stackable = false,
                        prefabPath = "Harpia Games/Camping Pack/Prefabs/Nature/Bush 3 with flowers.prefab" },
            new Entry { id = "flower",     displayName = "花",     kind = ObjectItem.Kind.Flower, height = 0.5f, stackable = false,
                        prefabPath = "Harpia Games/Camping Pack/Prefabs/Nature/Flower 5.prefab" },
            new Entry { id = "mushroom",   displayName = "蘑菇",   kind = ObjectItem.Kind.Flower, height = 0.4f, stackable = false,
                        prefabPath = "Harpia Games/Camping Pack/Prefabs/Nature/Mushroom 5.prefab" },

            // ── 道具(5)──
            new Entry { id = "cooler",     displayName = "冷藏箱", kind = ObjectItem.Kind.Box, height = 0.6f, stackable = true,
                        prefabPath = "Harpia Games/Camping Pack/Prefabs/Camping/Cooler.prefab" },
            new Entry { id = "crate",      displayName = "板条箱", kind = ObjectItem.Kind.Box, height = 0.6f, stackable = true,
                        prefabPath = "Pandazole_Ultimate_Pack/Pandazole Survival Crafting Pack/Prefabs/Crate.prefab" },
            new Entry { id = "kettle",     displayName = "水壶",   kind = ObjectItem.Kind.Lantern, height = 0.5f, stackable = false,
                        prefabPath = "Harpia Games/Camping Pack/Prefabs/Cooking/Italian cefetiere.prefab" },
            new Entry { id = "lantern",    displayName = "露营灯", kind = ObjectItem.Kind.Lantern, height = 0.5f, stackable = false,
                        prefabPath = "Harpia Games/Camping Pack/Prefabs/Camping/Camping lantern.prefab" },
            new Entry { id = "firstaid",   displayName = "急救箱", kind = ObjectItem.Kind.Medical, height = 0.4f, stackable = false,
                        prefabPath = "Harpia Games/Camping Pack/Prefabs/Medical/First aid kit.prefab" },
        };

        public static Entry Get(int index)
        {
            if (index < 0 || index >= All.Length)
            {
                // 越界返回 invalid Entry(以前 wrap 到 All[0],导致 currentIndex=-1 后还会误放第一个物件)
                return new Entry
                {
                    id = "",
                    displayName = "(invalid)",
                    kind = ObjectItem.Kind.Prop,
                    height = 0f,
                    stackable = false,
                    prefabPath = ""
                };
            }
            return All[index];
        }

        public static bool IsValid(Entry e) => !string.IsNullOrEmpty(e.id) && !string.IsNullOrEmpty(e.prefabPath);

        public static int Count => All.Length;

        /// <summary>
        /// 加载 prefab。MVP 用 AssetDatabase(只 Editor)。
        /// </summary>
        public static GameObject LoadPrefab(string assetPath)
        {
#if UNITY_EDITOR
            var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/" + assetPath);
            if (prefab == null)
                Debug.LogError($"[ObjectDatabase] Prefab not found at: Assets/{assetPath}");
            return prefab;
#else
            Debug.LogError("[ObjectDatabase] AssetDatabase not available at runtime in build. Use Resources/ folder for shipping.");
            return null;
#endif
        }
    }
}
