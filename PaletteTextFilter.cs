using System;
using System.Reflection;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace BuilderTools
{
    internal class PaletteTextFilter : MonoBehaviour
    {
        private static readonly FieldInfo m_UpdateGrid = BlockPicker.T_UIPaletteBlockSelect.GetField("m_UpdateGrid", BindingFlags.NonPublic | BindingFlags.Instance);
        public static MethodInfo SetUIInputMode = typeof(ManInput).GetMethod("SetUIInputMode", BindingFlags.NonPublic | BindingFlags.Instance);

        public static readonly Font[] fonts = Resources.FindObjectsOfTypeAll<Font>();
        public static readonly Sprite[] sprites = Resources.FindObjectsOfTypeAll<Sprite>();

        public static readonly Font ExoRegular = fonts.First(f => f.name == "Exo-Regular");
        public static readonly Sprite TEXT_FIELD_VERT_LEFT = sprites.First(f => f.name.Contains("TEXT_FIELD_VERT_LEFT"));

        private static UIInputMode mode;
        private static bool wasFocused = false;
        private static InputField inputField;
        private static RectTransform inputFieldRect;
        private static UIPaletteBlockSelect blockPalette;

        private static string filter = "";

        public static bool BlockFilterFunction(BlockTypes blockType)
        {
            if (filter == "")
            {
                return true;
            }

            var blockName = StringLookup.GetItemName(ObjectTypes.Block, (int)blockType).ToLower();
            return blockName.Contains(filter.ToLower());
        }

        private static void OnTextChanged(string text)
        {
            filter = text;
            m_UpdateGrid.SetValue(blockPalette, true);
        }

        public static void Init(UIPaletteBlockSelect palette)
        {
            blockPalette = palette;
            var inputFieldGo = DefaultControls.CreateInputField(new DefaultControls.Resources()
            {
                inputField = TEXT_FIELD_VERT_LEFT
            });

            inputField = inputFieldGo.GetComponent<InputField>();
            inputField.onValueChanged.AddListener(OnTextChanged);

            var image = inputFieldGo.GetComponent<Image>();
            image.color = new Color(0.2745f, 0.2745f, 0.2745f);

            var textColor = new Color(0.4666f, 0.7529f, 1f);

            foreach (var text in inputFieldGo.GetComponentsInChildren<Text>())
            {
                text.text = "";
                text.alignment = TextAnchor.MiddleLeft;
                text.font = ExoRegular;
                text.fontSize = 20;
                text.fontStyle = FontStyle.Italic;
                text.color = textColor;
                text.lineSpacing = 1;
            }

            inputField.placeholder.enabled = true;
            var placeholderText = inputField.placeholder.GetComponent<Text>();
            placeholderText.text = "Block name";
            placeholderText.color = new Color(0.6784f, 0.6784f, 0.6784f);

            var inputField_height = 40f;
            var heightVec = new Vector2(0, inputField_height);

            inputField.transform.SetParent(blockPalette.transform.Find("HUD_BlockPainting_BG"), false);
            var rect = inputFieldGo.GetComponent<RectTransform>();
            rect.pivot = rect.anchorMax = rect.anchorMin = new Vector2(1, 1);
            rect.anchoredPosition3D = new Vector3(-5, -5, 77);
            rect.sizeDelta = new Vector2(210, inputField_height);

            var scrollviewRect = (RectTransform)blockPalette.transform.Find("HUD_BlockPainting_BG/Scroll View");
            scrollviewRect.anchoredPosition -= heightVec;
            scrollviewRect.sizeDelta -= heightVec;

            var scrollbarRect = (RectTransform)blockPalette.transform.Find("HUD_BlockPainting_BG/Scrollbar");
            scrollbarRect.anchoredPosition -= heightVec;
            scrollbarRect.sizeDelta -= heightVec;

            inputFieldRect = rect;

            Singleton.Manager<ManGameMode>.inst.ModeSwitchEvent.Subscribe(OnModeChange);
        }

        private void Update()
        {
            HandleInputFieldFocus();
        }

        internal static void HandleInputFieldFocus()
        {
            if (inputField)
            {
                if (inputField.isFocused)
                {
                    if (!wasFocused)
                    {
                        wasFocused = true;
                        mode = ManInput.inst.GetCurrentUIInputMode();
                        Singleton.Manager<ManInput>.inst.SetControllerMapsForUI(ManUI.inst, true, UIInputMode.FullscreenUI);
                        SetUIInputMode.Invoke(ManInput.inst, new object[] { mode, UIInputMode.FullscreenUI });
                    }
                }
                else if (wasFocused)
                {
                    wasFocused = false;
                    Singleton.Manager<ManInput>.inst.SetControllerMapsForUI(ManUI.inst, true, UIInputMode.BlockBuilding);
                    SetUIInputMode.Invoke(ManInput.inst, new object[] { UIInputMode.FullscreenUI, UIInputMode.BlockBuilding });
                }
            }
        }

        public static bool PreventPause()
        {
            return !(!inputField || inputField.isFocused);
        }

        internal static void OnPaletteCollapse(bool collapse)
        {
            if (Main.config.clearOnCollapse && collapse)
            {
                ClearInput();
            }
        }

        private static void OnModeChange()
        {
            ClearInput();
        }

        private static void ClearInput()
        {
            if (inputField)
            {
                inputField.text = "";
            }
        }
    }
}
