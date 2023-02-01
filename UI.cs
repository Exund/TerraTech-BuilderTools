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
            Panel_BLUE_BG = sprites.First(f => f.name.Contains("Panel_BLUE_BG")),
            Panel_BLUE_disabled = sprites.First(f => f.name.Contains("Panel_BLUE_disabled"));

        public static GUIStyle
            RBRC_panel,
            Blue_btn;

        public static Color
            normal_text = new Color(0.6784f, 0.6784f, 0.6784f),
            orange = new Color(0.718f, 0.553f, 0.149f, 1),
            grey = new Color(0.278f, 0.278f, 0.278f, 1),
            blue = new Color(0.4666f, 0.7529f, 1f);

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
                    background = Panel_BLUE_disabled.texture,
                    textColor = grey
                },
                active = new GUIStyleState()
                {
                    background = Panel_BLUE_BG.texture,
                    textColor = Color.white
                },
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
                margin = new RectOffset(0, 5, 0, 5),
                fontSize = 14
            };

            Blue_btn.onNormal = Blue_btn.active;
        }
    }
}
