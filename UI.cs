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
        public static readonly Sprite TEXT_FIELD_VERT_LEFT = sprites.First(f => f.name.Contains("TEXT_FIELD_VERT_LEFT"));
        public static readonly Sprite ICON_ACTION_REVERT_SELECTED = sprites.First(f => f.name.Contains("ICON_ACTION_REVERT_SELECTED"));
    }
}
