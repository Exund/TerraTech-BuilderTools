using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace BuilderTools
{
    internal class PhysicsInfo : MonoBehaviour
    {
        private static readonly int PhysicsInfo_ID = 7782;

        internal static GameObject COM;
        internal static GameObject COT;
        internal static GameObject COL;

        private float reference_velocity = 100;
        private bool use_tech_velocity = false;

        private static float width = 300;
        private static float height = 200;
        private static Rect rect = new Rect((Screen.width - width) * 0.5f, 0, width, height);

        private static readonly Type T_BoosterJet = typeof(BoosterJet);
        private static readonly FieldInfo BJ_m_Effector = AccessTools.Field(T_BoosterJet, "m_Effector"),
            BJ_m_Force = AccessTools.Field(T_BoosterJet, "m_Force");

        private static readonly Type T_FanJet = typeof(FanJet);
        private static readonly FieldInfo FJ_m_Effector = AccessTools.Field(T_FanJet, "m_Effector"),
            FJ_force = AccessTools.Field(T_FanJet, "force");

        //static Type T_ModuleWing = typeof(ModuleWing);
        //static FieldInfo m_FoilState = T_ModuleWing.GetField("m_FoilState", flags);
        //static FieldInfo attackAngleModifier = T_ModuleWing.GetNestedType("AerofoilState", BindingFlags.NonPublic).GetField("attackAngleModifier");

        private void Awake()
        {
            useGUILayout = false;
            COM.SetActive(false);
            COT.SetActive(false);
            COL.SetActive(false);
        }

        private void Update()
        {
            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(Main.config.CentersKey))
            {
                useGUILayout = !useGUILayout;
            }

            if (Singleton.playerTank)
            {
                if (COM.activeInHierarchy)
                {
                    COM.transform.position = Singleton.playerTank.WorldCenterOfMass;
                    COM.transform.rotation = Singleton.playerTank.trans.rotation;
                }

                if (COT.activeInHierarchy)
                {
                    var rocket_boosters = Singleton.playerTank.GetComponentsInChildren<BoosterJet>();
                    var fan_boosters = Singleton.playerTank.GetComponentsInChildren<FanJet>();
                    if (rocket_boosters.Length != 0 || fan_boosters.Length != 0)
                    {
                        var pos = Vector3.zero;
                        var direction = Vector3.zero;
                        var thrust = 0f;

                        foreach (var rb in rocket_boosters)
                        {
                            var force = (float)BJ_m_Force.GetValue(rb);
                            var effector = (Transform)BJ_m_Effector.GetValue(rb);

                            thrust += force;
                            var rb_force = effector.forward * force;
                            var rb_pos = effector.position * force;

                            pos += rb_pos;
                            direction += rb_force;
                        }

                        foreach (var fb in fan_boosters)
                        {
                            var force = (float)FJ_force.GetValue(fb);
                            var effector = (Transform)FJ_m_Effector.GetValue(fb);

                            thrust += force;
                            var jb_force = effector.forward * force;
                            var jb_pos = effector.position * force;

                            pos += jb_pos;
                            direction += jb_force;
                        }

                        pos /= thrust;
                        direction /= thrust;

                        COT.transform.position = pos;
                        COT.transform.rotation = Singleton.playerTank.trans.rotation;

                        var COTlr = COT.GetComponentInChildren<LineRenderer>();
                        COTlr.SetPositions(new Vector3[] { pos, pos + direction * 5 });
                    }
                    else
                    {
                        COT.SetActive(false);
                    }
                }

                if (COL.activeInHierarchy)
                {
                    var wings = Singleton.playerTank.GetComponentsInChildren<ModuleWing>();
                    if (wings.Length != 0)
                    {
                        var pos = Vector3.zero;
                        var direction = Vector3.zero;
                        var lift = 0f;

                        var pointVelocity = Singleton.playerTank.trans.forward * (use_tech_velocity ? Singleton.playerTank.GetForwardSpeed() : reference_velocity);

                        foreach (var wing in wings)
                        {
                            //Array aerofoilStates = (Array)m_FoilState.GetValue(wing);
                            Vector3 b = wing.block.tank.rbody.position - wing.block.tank.trans.position;
                            for (int i = 0; i < wing.m_Aerofoils.Length; i++)
                            {
                                ModuleWing.Aerofoil aerofoil = wing.m_Aerofoils[i];
                                //object aerofoilState = aerofoilStates.GetValue(i);
                                Vector3 vector = aerofoil.trans.position + b;
                                Vector3 b2 = Vector3.Dot(pointVelocity, aerofoil.trans.right) * aerofoil.trans.right;
                                Vector3 lhs = pointVelocity - b2;
                                float magnitude = lhs.magnitude;
                                if (magnitude >= Globals.inst.m_WingAirspeedIgnore)
                                {
                                    float num = Mathf.Acos(Mathf.Clamp(Vector3.Dot(lhs, aerofoil.trans.up) / magnitude, -1f, 1f)) * 57.29578f - 90f;
                                    //num += (float)attackAngleModifier.GetValue(aerofoilState);
                                    //aerofoilState.attackAngleDamped = Mathf.Lerp(aerofoilState.attackAngleDamped, num, Mathf.Min(wing.m_AttackAngleDamping * Time.deltaTime, 1f));
                                    float num2 = aerofoil.liftCurve.Evaluate( /*aerofoilState.attackAngleDamped*/num);
                                    float num3 = magnitude * magnitude * num2 * aerofoil.liftStrength;
                                    if (Mathf.Abs(num3) >= Globals.inst.m_WingLiftIgnore)
                                    {
                                        pos += vector * num3;
                                        direction += aerofoil.trans.up * num3;
                                        lift += num3;
                                    }
                                }
                            }
                        }

                        pos /= lift;
                        direction /= lift;

                        COL.transform.position = pos;
                        COL.transform.rotation = Singleton.playerTank.trans.rotation;

                        var COLlr = COL.GetComponentInChildren<LineRenderer>();
                        COLlr.SetPositions(new Vector3[] { pos, pos + direction * 10 });
                    }
                    else
                    {
                        COL.SetActive(false);
                    }
                }
            }
        }

        private void OnGUI()
        {
            if (!useGUILayout)
                return;

            rect = GUI.Window(PhysicsInfo_ID, rect, DoWindow, "Physics info");
        }

        private void DoWindow(int id)
        {
            GUILayout.FlexibleSpace();
            GUILayout.Label("Centers");
            GUILayout.BeginHorizontal();
            {
                COM.SetActive(GUILayout.Toggle(COM.activeInHierarchy, "COM", GUI.skin.button));
                COT.SetActive(GUILayout.Toggle(COT.activeInHierarchy, "COT", GUI.skin.button));
                COL.SetActive(GUILayout.Toggle(COL.activeInHierarchy, "COL", GUI.skin.button));
            }
            GUILayout.EndHorizontal();
            GUILayout.Label("COT Reference velocity");
            float.TryParse(GUILayout.TextField(reference_velocity.ToString()), out reference_velocity);
            use_tech_velocity = GUILayout.Toggle(use_tech_velocity, "Use tech velocity");
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Close"))
            {
                useGUILayout = false;
            }

            GUI.DragWindow();
        }
    }
}
