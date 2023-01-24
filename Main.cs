using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using HarmonyLib;
using Nuterra.NativeOptions;
using ModHelper;

namespace BuilderTools
{
    public class Main : ModBase
    {
        internal const string HarmonyID = "Exund.BuilderTools";
        internal static Harmony harmony = new Harmony(HarmonyID);

        internal static Logger logger;
        internal static ModConfig configFile = new ModConfig();
        internal static Config config = new Config();

        private static GameObject holder;

        private static void SetupCOM()
        {
            var ontop = new Material(BuilderToolsContainer.Contents.FindAsset("OnTop") as Shader);
            var go = new GameObject();
            var mr = go.AddComponent<MeshRenderer>();
            mr.material = ontop;
            Texture2D texture = BuilderToolsContainer.Contents.FindAsset("CO_Icon") as Texture2D;
            mr.material.mainTexture = texture;

            var mf = go.AddComponent<MeshFilter>();
            Mesh mesh = null;
            foreach (UnityEngine.Object obj in BuilderToolsContainer.Contents.FindAllAssets("CO.obj"))
            {
                if (obj != null)
                {
                    if (obj is Mesh m)
                    {
                        mesh = m;
                        break;
                    }
                    else if (obj is GameObject gameObject)
                    {
                        mesh = gameObject.GetComponentInChildren<MeshFilter>().sharedMesh;
                        break;
                    }
                }
            }

            mf.sharedMesh = mf.mesh = mesh;

            var line = new GameObject();
            var lr = line.AddComponent<LineRenderer>();
            lr.startWidth = 0.5f;
            lr.endWidth = 0;
            lr.SetPositions(new Vector3[] { Vector3.zero, Vector3.zero });
            lr.useWorldSpace = true;
            lr.material = ontop;
            lr.material.color = lr.material.color.SetAlpha(1);
            line.transform.SetParent(go.transform, false);

            go.SetActive(false);
            go.transform.SetParent(holder.transform, false);

            PhysicsInfo.COM = GameObject.Instantiate(go);
            var commat = PhysicsInfo.COM.GetComponent<MeshRenderer>().material;
            commat.color = Color.yellow;
            commat.renderQueue = 1;
            PhysicsInfo.COM.GetComponentInChildren<LineRenderer>().enabled = false;

            PhysicsInfo.COT = GameObject.Instantiate(go);
            PhysicsInfo.COT.transform.localScale *= 0.8f;
            var cotmat = PhysicsInfo.COT.GetComponent<MeshRenderer>().material;
            cotmat.color = Color.magenta;
            cotmat.renderQueue = 3;
            var COTlr = PhysicsInfo.COT.GetComponentInChildren<LineRenderer>();
            COTlr.material.renderQueue = 2;
            COTlr.startColor = COTlr.endColor = Color.magenta;

            PhysicsInfo.COL = GameObject.Instantiate(go);
            PhysicsInfo.COL.transform.localScale *= 0.64f;
            var colmat = PhysicsInfo.COL.GetComponent<MeshRenderer>().material;
            colmat.color = Color.cyan;
            colmat.renderQueue = 5;
            var COLlr = PhysicsInfo.COL.GetComponentInChildren<LineRenderer>();
            COLlr.startColor = COLlr.endColor = Color.cyan;
            COLlr.material.renderQueue = 4;
        }

        private static void Load()
        {
            logger = new Logger(HarmonyID);

            try
            {
                holder = new GameObject();
                holder.AddComponent<BlockPicker>();

                UnityEngine.Object.DontDestroyOnLoad(holder);

                configFile.BindConfig(config, "open_inventory");
                configFile.BindConfig(config, "global_filters");
                configFile.BindConfig(config, "block_picker_key");
                configFile.BindConfig(config, "clearOnCollapse");
                configFile.BindConfig(config, "centers_key");
                configFile.BindConfig(config, "kbdCategroryKeys");

                string modName = "Builder Tools";
                OptionKey blockPickerKey = new OptionKey("Block Picker activation key", modName, config.BlockPickerKey);
                blockPickerKey.onValueSaved.AddListener(() =>
                {
                    configFile["block_picker_key"] = (int)blockPickerKey.SavedValue;
                });

                OptionToggle globalFilterToggle = new OptionToggle("Block Picker - Use global filters", modName, config.global_filters);
                globalFilterToggle.onValueSaved.AddListener(() =>
                {
                    configFile["global_filters"] = globalFilterToggle.SavedValue;
                });

                OptionToggle openInventoryToggle = new OptionToggle("Block Picker - Automatically open the inventory when picking a block", modName, config.open_inventory);
                openInventoryToggle.onValueSaved.AddListener(() =>
                {
                    configFile["open_inventory"] = openInventoryToggle.SavedValue;
                });

                OptionToggle clearOnCollapse = new OptionToggle("Block Search - Clear filter when closing inventory", modName, config.clearOnCollapse);
                clearOnCollapse.onValueSaved.AddListener(() =>
                {
                    configFile["clearOnCollapse"] = clearOnCollapse.SavedValue;
                });

                OptionKey centersKey = new OptionKey("Open physics info menu (Ctrl + ?)", modName, config.CentersKey);
                centersKey.onValueSaved.AddListener(() =>
                {
                    configFile["centers_key"] = (int)centersKey.SavedValue;
                });

                OptionToggle enableKbdCategroryKeys = new OptionToggle("Use numerical keys (1-9) to select block category", modName, config.kbdCategroryKeys);
                enableKbdCategroryKeys.onValueSaved.AddListener(() =>
                {
                    configFile["kbdCategroryKeys"] = enableKbdCategroryKeys.SavedValue;
                });

                NativeOptionsMod.onOptionsSaved.AddListener(() => { configFile.WriteConfigJsonFile(); });

                SetupCOM();
                holder.AddComponent<PhysicsInfo>();
                holder.AddComponent<PaletteTextFilter>();
                holder.AddComponent<BlockLine>();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private static bool Inited = false;
        private static ModContainer BuilderToolsContainer;

        public override void EarlyInit()
        {
            if (!Inited)
            {
                Dictionary<string, ModContainer> mods = (Dictionary<string, ModContainer>)AccessTools.Field(typeof(ManMods), "m_Mods").GetValue(Singleton.Manager<ManMods>.inst);
                if (mods.TryGetValue("BuilderTools", out ModContainer thisContainer))
                {
                    BuilderToolsContainer = thisContainer;
                }
                else
                {
                    Console.WriteLine("FAILED TO FETCH BuilderTools ModContainer");
                }

                Inited = true;
                Load();
            }
        }

        public override bool HasEarlyInit()
        {
            return true;
        }

        public override void DeInit()
        {
            harmony.UnpatchAll(HarmonyID);
        }

        public override void Init()
        {
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }

        public class Config
        {
            public bool open_inventory = false;
            public bool global_filters = true;
            public int block_picker_key = (int)KeyCode.LeftShift;
            public bool clearOnCollapse = true;

            public int centers_key = (int)KeyCode.M;
            public bool kbdCategroryKeys = false;

            public KeyCode BlockPickerKey
            {
                get => (KeyCode)block_picker_key;
            }

            public KeyCode CentersKey
            {
                get => (KeyCode)centers_key;
            }
        }
    }
}
