using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using UnityEngine;
using static UnityEngine.GridBrushBase;

namespace QuickPing.Patches
{

    [HarmonyPatch(typeof(ChatPing_Patch))]
    [HarmonyPatch("CheckForErrors")]
    [HarmonyDebug]
    public static class ChatPing_Patch
    {
        [HarmonyPatch(typeof(Chat))]
        [HarmonyPatch("AddInworldText")]
        [HarmonyDebug]
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {

            var end = false;
            string pingText;
            var start = false;
            int startIndex = -1, endIndex = -1;
            var codes = new List<CodeInstruction>(instructions);
            //Label startLabel = il.DefineLabel();
            //Label endLabel = il.DefineLabel();

            for (int i = 0; i < instructions.Count(); i++)
            {
                var instruction = codes[i];
                if (!start && instruction.opcode == OpCodes.Switch)
                    start = true;
                if (!start) continue;
                if (end)
                {
                    QuickPing.Log.LogWarning("END " + i);

                    endIndex = i; // include current 'ret'
                    break;
                }
                else
                {
                    QuickPing.Log.LogWarning("START " + (i + 1));
                    startIndex = i + 1; // exclude current 'switch'
                    //codes[startIndex].labels.Add(startLabel);
                    for (int j = startIndex; j < codes.Count; j++)
                    {
                        if (codes[j].opcode == OpCodes.Ldloc_0)
                        {
                            //codes[j].labels.Add(endLabel);
                            end = true;
                            break;
                        }
                    }
                }



            }
            if (startIndex > -1 && endIndex > -1)
            {

                //codes.RemoveRange(startIndex + 1, endIndex - startIndex - 1);
                // we cannot remove the first code of our range since some jump actually jumps to
                // it, so we replace it with a no-op instead of fixing that jump (easier).
                //codes[startIndex].opcode = OpCodes.Nop;
                int index = startIndex + 1; //IL_00a1: br.s IL_00fe
                List<CodeInstruction> newColorValues = new List<CodeInstruction> {

                    new CodeInstruction(OpCodes.Ldc_R4, Settings.ShoutColor.Value.r),
                    new CodeInstruction(OpCodes.Ldc_R4, Settings.ShoutColor.Value.g),
                    new CodeInstruction(OpCodes.Ldc_R4, Settings.ShoutColor.Value.b),
                    new CodeInstruction(OpCodes.Ldc_R4, Settings.ShoutColor.Value.a)
                };
                //CodeInstruction blankLabelInstruction = new CodeInstruction(OpCodes.Nop);
                //Label newLabel = il.DefineLabel();
                //blankLabelInstruction.labels.Add(newLabel);
                //newColorValues.Add(blankLabelInstruction);

                codes.InsertRange(index, newColorValues);
                index += newColorValues.Count;

                //Might not be the way to do, check here if errors (maybe try adding lines for r g b a in IL)
                //codes[index++] = new CodeInstruction(OpCodes.Call, typeof(Color).GetConstructors()[0]);
                index += 7;
                codes[index++] = new CodeInstruction(OpCodes.Ldc_R4, Settings.WhisperColor.Value.r);
                codes[index++] = new CodeInstruction(OpCodes.Ldc_R4, Settings.WhisperColor.Value.g);
                codes[index++] = new CodeInstruction(OpCodes.Ldc_R4, Settings.WhisperColor.Value.b);
                codes[index++] = new CodeInstruction(OpCodes.Ldc_R4, Settings.WhisperColor.Value.a);
                //codes[index++] = new CodeInstruction(OpCodes.Call, typeof(Color).GetConstructors()[0]);
                index++;
                index += 5;
                codes[index++] = new CodeInstruction(OpCodes.Ldc_R4, Settings.PingColor.Value.r);
                codes[index++] = new CodeInstruction(OpCodes.Ldc_R4, Settings.PingColor.Value.g);
                codes[index++] = new CodeInstruction(OpCodes.Ldc_R4, Settings.PingColor.Value.b);
                codes[index++] = new CodeInstruction(OpCodes.Ldc_R4, Settings.PingColor.Value.a);
                //codes[index++] = new CodeInstruction(OpCodes.Call, typeof(Color).GetConstructors()[0]);
                index++;
                codes[index++] = new CodeInstruction(OpCodes.Ldstr, QuickPing.Instance.PingText.ToString());
            }
            if (end is false)
                UnityEngine.Debug.LogError("Cannot find <Stdfld someField> in OriginalType.OriginalMethod");
            return codes.AsEnumerable();
        }
    }

}
