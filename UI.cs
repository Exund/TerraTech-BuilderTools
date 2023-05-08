using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace BuilderTools
{
    public static class UI
    {
        public static readonly Font[] fonts = Resources.FindObjectsOfTypeAll<Font>();
        public static readonly Sprite[] sprites = Resources.FindObjectsOfTypeAll<Sprite>();

        public static readonly Font ExoRegular = fonts.First(f => f.name == "Exo-Regular");

        public static readonly Sprite
            TEXT_FIELD_VERT_LEFT = sprites.First(f => f.name.Contains("TEXT_FIELD_VERT_LEFT")),
            GUI_TriangleRight = sprites.First(f => f.name.Contains("GUI_TriangleRight")),
            ICON_NAV_CLOSE = sprites.First(f => f.name.Contains("ICON_NAV_CLOSE"));

        public static GUIStyle
            RBRC_panel,
            Blue_btn,
            Expand_toggle;

        public static Color
            normal_text = new Color(0.6784f, 0.6784f, 0.6784f),
            orange = new Color(0.718f, 0.553f, 0.149f, 1),
            grey = new Color(0.278f, 0.278f, 0.278f, 1),
            dark_grey = new Color(0.235f, 0.235f, 0.235f, 1),
            blue = new Color(0.4666f, 0.7529f, 1f),
            transparent_tint = new Color(1, 1, 1, 0.627f);

        public static void Init(ModContents contents)
        {
            RBRC_panel = new GUIStyle()
            {
                border = new RectOffset(15, 15, 15, 15),
                normal = new GUIStyleState()
                {
                    background = contents.FindAllAssets("RBRC_PANEL").FirstOrDefault(o => o is Texture2D) as Texture2D,
                },
                padding = new RectOffset(5, 5, 5, 5)
            };

            Blue_btn = new GUIStyle()
            {
                border = new RectOffset(12, 12, 12, 12),
                normal = new GUIStyleState()
                {
                    background = contents.FindAllAssets("GREY_BG").FirstOrDefault(o => o is Texture2D) as Texture2D,
                    textColor = Color.white
                },
                active = new GUIStyleState()
                {
                    background = contents.FindAllAssets("BLUE_BG").FirstOrDefault(o => o is Texture2D) as Texture2D,
                    textColor = Color.white
                },
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
                fontSize = 14,
                margin = new RectOffset(0, 5, 0, 5),
                padding = new RectOffset(3, 3, 3, 3)
            };

            Blue_btn.onNormal = Blue_btn.active;

            Expand_toggle = new GUIStyle()
            {
                normal = new GUIStyleState()
                {
                    background = GUI_TriangleRight.texture
                },
                onNormal = new GUIStyleState()
                {
                    background = ICON_NAV_CLOSE.texture
                }
            };
        }
    }
}
