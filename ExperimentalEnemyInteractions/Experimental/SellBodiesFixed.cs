using System;
using System.Collections.Generic;
using System.Text;
using HarmonyLib;
using CleaningCompany;
using System.Linq;
using UnityEngine.TextCore.Text;
using BepInEx.Logging;
using Unity.Netcode;
using UnityEngine;
using System.Reflection.Emit;
using CleaningCompany.Monos;

namespace NaturalSelection.Compatibility
{

    internal class SellBodiesTraceScript : MonoBehaviour
    {
        GameObject instance;

        public SellBodiesTraceScript()
        {
            instance = this.gameObject;
        }

        private void Awake()
        {
            instance = this.gameObject;
            Script.Logger.Log(LogLevel.Message,$"Successfully added SellBodiesTraceScript of {instance.name}");

            if (!SellBodiesFixedPatch.deadEnemyBodies.Contains(instance))
            {
                SellBodiesFixedPatch.deadEnemyBodies.Add(instance);
            }
        }

        private void OnDestroy()
        {
            Script.Logger.Log(LogLevel.Message,$"Removing SellBodiesTraceScript {instance.name} from list");
            SellBodiesFixedPatch.deadEnemyBodies.Remove(instance);
        }
    }


    internal class SellBodiesFixedPatch
    {
        public static List<GameObject> deadEnemyBodies = new List<GameObject>();

        public static void AddTracerScriptToPrefabs()
        {
            foreach (KeyValuePair<string,string> pair in Plugin.instance.pathToName)
            {
                Item item = Plugin.instance.bundle.LoadAsset<Item>(pair.Key);

                item.spawnPrefab.gameObject.AddComponent<SellBodiesTraceScript>();

                Script.Logger.Log(LogLevel.Message,$"Added SellBodiesTracerScript to {pair.Value} at {pair.Key}");

            }
        }

        /*
        [HarmonyPatch(typeof(CleaningCompany.Plugin), nameof(CleaningCompany.Plugin.SetupScrap))]
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> SellBodiesPluginPatchT(IEnumerable<CodeInstruction> instructions)
        {
            if (test.Count > 0)
            {
                Script.Logger.Log(LogLevel.Message,"Found BodySyncer in list");
            }
            foreach (var bodySyncer in test)
            {
                bodySyncer.gameObject.AddComponent<SellBodiesTraceScript>();
            }



            Script.Logger.LogWarning("Fired Transpiller for SellBodiesPluginPatchT");
            CodeMatcher matcher = new CodeMatcher(instructions);
            int offset = 0;

            matcher.MatchForward(true,
            new CodeMatch(OpCodes.Ldloc_2),
            new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(Item), nameof(Item.spawnPrefab))),
            new CodeMatch(OpCodes.Callvirt),
            new CodeMatch(OpCodes.Pop)
            )
            .ThrowIfInvalid("Could not find match for SellBodiesPluginPatchT")
            .Advance(1)
            .InsertAndAdvance(new CodeInstruction(OpCodes.Ldstr, "Successfully Transpiled SellBodiesFixed by NaturalSelection"))
            .InsertAndAdvance(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Debug), nameof(Debug.Log), [typeof(string)])))

            .InsertAndAdvance(new CodeInstruction(OpCodes.Ldloc_2))
            .InsertAndAdvance(new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(Item), nameof(Item.spawnPrefab))))
            .InsertAndAdvance(new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(GameObject), nameof(GameObject.AddComponent), generics: [typeof(SellBodiesTraceScript)])))
            .Insert(new CodeInstruction(OpCodes.Pop))
            ;


            for (int i = 0; i < instructions.ToList().Count; i++)
            {
                //if (!debug) break;
                try
                {
                    if (matcher.Instructions().ToList()[i].ToString() != instructions.ToList()[i - offset].ToString())
                    {
                        Script.Logger.LogError($"{matcher.Instructions().ToList()[i]} : {instructions.ToList()[i - offset]}");

                        if (matcher.Instructions().ToList()[i].ToString() != instructions.ToList()[i - offset].ToString())
                        {
                            offset++;
                        }
                    }
                    else Script.Logger.LogInfo(instructions.ToList()[i]);
                }
                catch
                {
                    Script.Logger.LogError("Failed to read instructions");
                }
            }
            return instructions;
        }*/
    }
}
