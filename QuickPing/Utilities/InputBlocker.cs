using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace QuickPing.Utilities
{
    /// <summary>
    /// Stops the game from using the keyboard and mouse while a QuickPing window is open:
    /// no movement, camera look, zoom or inventory, and the cursor is free.
    /// </summary>
    internal static class InputBlocker
    {
        public static bool Blocked { get; private set; }

        public static void Block(bool state)
        {
            if (Blocked == state) return;
            Blocked = state;

            if (GameCamera.instance)
            {
                GameCamera.instance.m_mouseCapture = !state;
                GameCamera.instance.UpdateMouseCapture();
            }
        }

        /// <summary>
        /// Leaving the world with a window open must not keep the input blocked in the next one.
        /// </summary>
        [HarmonyPatch(typeof(Game), nameof(Game.Start))]
        [HarmonyPostfix]
        private static void GameStart()
        {
            Blocked = false;
        }

        [HarmonyPatch(typeof(PlayerController), nameof(PlayerController.TakeInput))]
        [HarmonyPatch(typeof(Player), nameof(Player.TakeInput))]
        [HarmonyPostfix]
        private static void TakeInput(ref bool __result)
        {
            if (Blocked) __result = false;
        }

        /// <summary>
        /// The game frees the cursor and ignores most keys while a text input is open.
        /// </summary>
        [HarmonyPatch(typeof(TextInput), nameof(TextInput.IsVisible))]
        [HarmonyPostfix]
        private static void TextInputIsVisible(ref bool __result)
        {
            if (Blocked) __result = true;
        }

        /// <summary>
        /// Inventory keys and camera zoom only check for an open menu: treat a blocked input like one.
        /// </summary>
        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Update))]
        [HarmonyPatch(typeof(GameCamera), nameof(GameCamera.UpdateCamera))]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> SkipWhenBlocked(IEnumerable<CodeInstruction> instructions, MethodBase original)
        {
            Label? skip = null;
            var matcher = new CodeMatcher(instructions)
                .MatchForward(true,
                    new CodeMatch(i => i.Calls(AccessTools.Method(typeof(Menu), nameof(Menu.IsVisible)))),
                    new CodeMatch(i => i.Branches(out skip)));

            // Keep the game code as is (and the other patches working) if a game update changed it
            if (matcher.IsInvalid)
            {
                QuickPingPlugin.Log.LogWarning($"{original.DeclaringType.Name}.{original.Name}: menu check not found, input not blocked there");
                return matcher.InstructionEnumeration();
            }

            return matcher
                .Advance(1)
                .InsertAndAdvance(
                    new CodeInstruction(OpCodes.Call, AccessTools.PropertyGetter(typeof(InputBlocker), nameof(Blocked))),
                    new CodeInstruction(OpCodes.Brtrue, skip))
                .InstructionEnumeration();
        }
    }
}
