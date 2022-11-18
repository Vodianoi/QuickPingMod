using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace QuickPing.Patches
{

    [HarmonyPatch(typeof(ChatPing_Patch))]
    [HarmonyPatch("CheckForErrors")]
    public static class ChatPing_Patch
    {
        [HarmonyPatch(typeof(Chat))]
        [HarmonyPatch("AddInworldText")]
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            var found = false;
            foreach (var instruction in instructions)
            {
                //Plugin.Instance.LogInfo($"Instruction found : {instruction.opcode} => {instruction.operand}");

                if (instruction.opcode == OpCodes.Ldstr && instruction.operand.ToString() == "PING")
                {
                    MethodInfo method = typeof(string).GetMethods()
                        .Where(x => x.Name == "ToUpper").First();

                    foreach (var m in typeof(string).GetMethods()
                        .Where(x => x.Name == "ToUpper"))
                    {
                        QuickPing.Log.LogInfo(m);
                    }
                    yield return new CodeInstruction(OpCodes.Ldarg_S, (byte)6);
                    //il.EmitCall(OpCodes.Callvirt, method, new Type[] { });
                    yield return new CodeInstruction(OpCodes.Call, method);
                    found = true;
                }
                else
                    yield return instruction;
            }

            if (found is false)
                UnityEngine.Debug.LogError("Cannot find <Stdfld someField> in OriginalType.OriginalMethod");
        }
    }
}
