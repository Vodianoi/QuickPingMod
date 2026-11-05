#if DEBUG
using BepInEx.Configuration;
using QuickPing.Patches;
using QuickPing.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace QuickPing.Tests
{
    /// <summary>
    /// Tests run in a loaded world. Pings go through the real path: the camera is aimed at a spawned
    /// object, then the same code as the ping keys finds the target and adds the pin.
    /// Every test cleans up its objects, pins and saved names.
    /// </summary>
    internal static class InGameTests
    {
        internal class Test
        {
            public string Name;
            public Func<IEnumerator> Body;
        }

        public static readonly Test[] All =
        {
            new Test { Name = "Location names are translated", Body = Translations },
            new Test { Name = "Old key binding is migrated", Body = KeyMigration },
            new Test { Name = "Ping pins a tin deposit", Body = PingPinsDeposit },
            new Test { Name = "Pinging twice adds one pin", Body = PingTwiceOnePin },
            new Test { Name = "Destroying a pinned object removes its pin", Body = DestroyRemovesPin },
            new Test { Name = "Ping pins a raspberry bush", Body = PingPinsBush },
            new Test { Name = "Portal pin uses the portal tag", Body = PortalPinUsesTag },
            new Test { Name = "Force ping pins the ground", Body = ForcePingPinsGround },
            new Test { Name = "Custom Name Ping window shows its text", Body = CustomNamePingTextVisible },
            new Test { Name = "Custom Name Ping blocks game input", Body = CustomNamePingBlocksInput },
            new Test { Name = "Custom Name Ping sets name and icon", Body = CustomNamePingSetsPin },
            new Test { Name = "Remember this name starts unticked", Body = RememberStartsUnticked },
            new Test { Name = "Plain ping remembers no name", Body = PlainPingRemembersNothing },
            new Test { Name = "Custom Name Ping closed without saving adds no pin", Body = CustomNamePingCancel },
            new Test { Name = "Remembered name is used for the next ping", Body = RememberedName },
        };

        private static readonly List<GameObject> spawned = new();
        private static readonly Dictionary<ZDOID, string> spawnedZDOs = new();
        private static HashSet<Minimap.PinData> pinsBefore;
        private static Dictionary<string, string> namesBefore;
        private static int spawnIndex;
        private static HashSet<ItemDrop> dropsBefore;

        #region Setup

        public static void BeforeEach()
        {
            pinsBefore = new HashSet<Minimap.PinData>(Minimap.instance.m_pins);
            namesBefore = new Dictionary<string, string>(DataManager.CustomNames);
            spawnIndex = 0;
            dropsBefore = new HashSet<ItemDrop>(ItemDrop.s_instances);
        }

        public static IEnumerator AfterEach()
        {
            if (Minimap_Patch.IsNaming)
            {
                Minimap_Patch.CloseCustomNamePing();
                yield return WaitUntil(() => !Minimap_Patch.IsNaming, "Custom Name Ping window to close");
            }

            foreach (GameObject go in spawned.Where(go => go))
                ZNetScene.instance.Destroy(go);
            spawned.Clear();

            // Items dropped by destroyed objects (ore...), not the ones already in the world
            foreach (ItemDrop drop in ItemDrop.s_instances.Where(d => d && !dropsBefore.Contains(d)).ToList())
                ZNetScene.instance.Destroy(drop.gameObject);

            foreach (Minimap.PinData pin in Minimap.instance.m_pins.Where(p => !pinsBefore.Contains(p)).ToList())
                Minimap.instance.RemovePin(pin);

            DataManager.CustomNames.Clear();
            foreach (var name in namesBefore)
                DataManager.CustomNames[name.Key] = name.Value;

            yield return null;

            // A spawned object whose saved data survives comes back the next time the world loads
            var leftovers = new List<string>();
            foreach (var saved in spawnedZDOs)
            {
                ZDO zdo = ZDOMan.instance.GetZDO(saved.Key);
                if (zdo == null) continue;
                leftovers.Add($"{saved.Value} at {zdo.GetPosition()}");
                ZDOMan.instance.DestroyZDO(zdo);
            }
            spawnedZDOs.Clear();
            Assert.True(leftovers.Count == 0, $"left in the world after cleanup: {string.Join(", ", leftovers)}");
        }

        /// <summary>
        /// Prefabs the tests spawn: leftovers of an earlier run are removed by <see cref="RemoveLeftovers"/>.
        /// </summary>
        private static readonly string[] SpawnedPrefabs = { "MineRock_Tin", "RaspberryBush", "portal_wood" };

        /// <summary>
        /// Remove test objects left near the player by an earlier run. Only for the test world.
        /// </summary>
        public static int RemoveLeftovers(float range)
        {
            var hashes = new HashSet<int>(SpawnedPrefabs.Select(name => name.GetStableHashCode()));
            Vector3 player = Player.m_localPlayer.transform.position;
            List<ZDO> leftovers = ZDOMan.instance.m_objectsByID.Values
                .Where(zdo => hashes.Contains(zdo.GetPrefab()) && Vector3.Distance(zdo.GetPosition(), player) < range)
                .ToList();
            foreach (ZDO zdo in leftovers)
            {
                ZNetView view = ZNetScene.instance.FindInstance(zdo);
                if (view) ZNetScene.instance.Destroy(view.gameObject);
                else ZDOMan.instance.DestroyZDO(zdo);
            }
            return leftovers.Count;
        }

        #endregion

        #region Tests

        private static IEnumerator Translations()
        {
            Assert.Equal("Village", Localization.instance.Localize("$quickping_location_village"), "village name");
            Assert.Equal("Leviathan", Localization.instance.Localize("$quickping_leviathan"), "leviathan name");
            yield break;
        }

        private static IEnumerator KeyMigration()
        {
            var defaultKey = new KeyboardShortcut(KeyCode.T, KeyCode.LeftAlt);

            // Changed binding under the old name: kept under the new one, old line removed
            string changed = Path.GetTempFileName();
            File.WriteAllText(changed, "[Bindings]\nRenameInputKey = V\n");
            var config = new ConfigFile(changed, true);
            ConfigEntry<KeyboardShortcut> entry = config.Bind("Bindings", "CustomNamePingInputKey", defaultKey, "test");
            Settings.MigrateKey(config, "RenameInputKey", entry, defaultKey);
            Assert.Equal(new KeyboardShortcut(KeyCode.V), entry.Value, "migrated binding");
            Assert.True(!File.ReadAllText(changed).Contains("RenameInputKey"), "old key still in the config file");
            File.Delete(changed);

            // No old binding: default kept, nothing left behind
            string fresh = Path.GetTempFileName();
            config = new ConfigFile(fresh, true);
            entry = config.Bind("Bindings", "CustomNamePingInputKey", defaultKey, "test");
            Settings.MigrateKey(config, "RenameInputKey", entry, defaultKey);
            Assert.Equal(defaultKey, entry.Value, "binding without migration");
            Assert.True(!File.ReadAllText(fresh).Contains("RenameInputKey"), "old key written to a fresh config file");
            File.Delete(fresh);
            yield break;
        }

        private static IEnumerator PingPinsDeposit()
        {
            GameObject tin = Spawn("MineRock_Tin");
            yield return Settle();

            HoverObject ping = PingAt(tin, Player_Patch.OnPlayerPing);
            AssertPin(ping.center, Minimap.PinType.Icon2, Localization.instance.Localize("$piece_deposit_tin"));
        }

        private static IEnumerator PingTwiceOnePin()
        {
            GameObject tin = Spawn("MineRock_Tin");
            yield return Settle();

            HoverObject ping = PingAt(tin, Player_Patch.OnPlayerPing);
            PingAt(tin, Player_Patch.OnPlayerPing);
            Assert.Equal(1, PinsAt(ping.center).Count(), "pins on the deposit");
        }

        private static IEnumerator DestroyRemovesPin()
        {
            GameObject tin = Spawn("MineRock_Tin");
            yield return Settle();

            HoverObject ping = PingAt(tin, Player_Patch.OnPlayerPing);
            Minimap.PinData pin = AssertPin(ping.center, Minimap.PinType.Icon2, null);

            Assert.NotNull(tin.GetComponent<Destructible>(), "tin deposit Destructible").Destroy();
            yield return Settle();
            Assert.True(!Minimap.instance.m_pins.Contains(pin), "pin still on the map after the deposit was destroyed");
        }

        private static IEnumerator PingPinsBush()
        {
            GameObject bush = Spawn("RaspberryBush");
            yield return Settle();

            HoverObject ping = PingAt(bush, Player_Patch.OnPlayerPing);
            AssertPin(ping.center, Minimap.PinType.Icon3, null);
        }

        private static IEnumerator PortalPinUsesTag()
        {
            GameObject portal = Spawn("portal_wood");
            yield return Settle();
            Assert.NotNull(portal.GetComponentInChildren<TeleportWorld>(), "portal TeleportWorld").SetText("QP Test");
            yield return Settle();

            HoverObject ping = PingAt(portal, Player_Patch.OnPlayerPing);
            AssertPin(ping.center, null, "QP Test");
        }

        private static IEnumerator ForcePingPinsGround()
        {
            HoverObject ping = AimAt(GroundPoint());
            Player_Patch.OnPlayerForcePing.Invoke(ping);
            AssertPin(ping.center, Settings.DefaultPinType.Value, null);
            yield break;
        }

        private static IEnumerator CustomNamePingTextVisible()
        {
            // Pointing at an object, so the "Remember this name" checkbox is shown
            GameObject tin = Spawn("MineRock_Tin");
            yield return Settle();
            yield return OpenCustomNamePing(AimAt(tin));
            Minimap_Patch.nameInput.text = "QP custom";
            yield return null;
            Canvas.ForceUpdateCanvases();

            Text[] texts = Minimap_Patch.panel.GetComponentsInChildren<Text>()
                .Where(t => t.isActiveAndEnabled && !string.IsNullOrEmpty(t.text))
                .ToArray();
            Assert.True(texts.Any(t => t.text == "QP custom"), "typed name not found in the window");
            Assert.True(texts.Any(t => t.text == "Remember this name"), "checkbox label not found in the window");

            foreach (Text text in texts)
            {
                string what = $"text '{text.text}' ({text.name})";
                Assert.NotNull(text.font, $"{what} font");
                Assert.True(text.color.a > 0f, $"{what} is transparent");
                // Unity hides lines that don't fit: then no character is visible
                Assert.True(text.cachedTextGenerator.characterCountVisible > 0, $"{what} is hidden (no visible character)");
            }
        }

        private static IEnumerator CustomNamePingBlocksInput()
        {
            yield return OpenCustomNamePing(AimAt(GroundPoint()));
            Assert.True(InputBlocker.Blocked, "input not blocked while the window is open");
            Assert.True(TextInput.IsVisible(), "the game doesn't see a text input open");
            Assert.True(!Player.m_localPlayer.TakeInput(), "the player still takes input");

            Minimap_Patch.CloseCustomNamePing();
            yield return WaitUntil(() => !Minimap_Patch.IsNaming, "Custom Name Ping window to close");
            Assert.True(!InputBlocker.Blocked, "input still blocked after closing");
            Assert.True(Player.m_localPlayer.TakeInput(), "the player doesn't take input after closing");
        }

        private static IEnumerator CustomNamePingSetsPin()
        {
            HoverObject target = AimAt(GroundPoint());
            yield return OpenCustomNamePing(target);
            Minimap_Patch.nameInput.text = "QP custom";
            Minimap_Patch.SelectIcon(Minimap.PinType.Icon4);
            Minimap_Patch.ConfirmCustomNamePing();
            yield return WaitUntil(() => !Minimap_Patch.IsNaming, "Custom Name Ping window to close");

            AssertPin(target.center, Minimap.PinType.Icon4, "QP custom");
        }

        private static IEnumerator RememberStartsUnticked()
        {
            GameObject tin = Spawn("MineRock_Tin");
            yield return Settle();
            yield return OpenCustomNamePing(AimAt(tin));
            Assert.True(Minimap_Patch.toggleSaveName.gameObject.activeInHierarchy, "\"Remember this name\" hidden for an object");
            Assert.True(!Minimap_Patch.toggleSaveName.isOn, "\"Remember this name\" starts ticked");

            Minimap_Patch.nameInput.text = "QP not remembered";
            Minimap_Patch.ConfirmCustomNamePing();
            yield return WaitUntil(() => !Minimap_Patch.IsNaming, "Custom Name Ping window to close");
            Assert.True(!DataManager.CustomNames.ContainsKey("$piece_deposit_tin"), "name remembered without ticking the box");
        }

        private static IEnumerator PlainPingRemembersNothing()
        {
            yield return OpenCustomNamePing(AimAt(GroundPoint()));
            Assert.True(!Minimap_Patch.toggleSaveName.gameObject.activeInHierarchy, "\"Remember this name\" shown for a plain ping");

            // Even if it were ticked, a plain ping has no kind of object to remember a name for
            Minimap_Patch.toggleSaveName.isOn = true;
            Minimap_Patch.nameInput.text = "QP plain";
            Minimap_Patch.ConfirmCustomNamePing();
            yield return WaitUntil(() => !Minimap_Patch.IsNaming, "Custom Name Ping window to close");
            Assert.True(!DataManager.CustomNames.ContainsKey(Settings.DefaultPingText), "name remembered for every plain ping");
        }

        private static IEnumerator CustomNamePingCancel()
        {
            HoverObject target = AimAt(GroundPoint());
            yield return OpenCustomNamePing(target);
            Minimap_Patch.nameInput.text = "QP cancelled";
            Minimap_Patch.CloseCustomNamePing();
            yield return WaitUntil(() => !Minimap_Patch.IsNaming, "Custom Name Ping window to close");

            Assert.True(!NewPins().Any(), "a pin was added");
        }

        private static IEnumerator RememberedName()
        {
            GameObject tin = Spawn("MineRock_Tin");
            yield return Settle();
            yield return OpenCustomNamePing(AimAt(tin));
            Minimap_Patch.nameInput.text = "My tin";
            Minimap_Patch.toggleSaveName.isOn = true;
            Minimap_Patch.ConfirmCustomNamePing();
            yield return WaitUntil(() => !Minimap_Patch.IsNaming, "Custom Name Ping window to close");

            Assert.True(DataManager.CustomNames.TryGetValue("$piece_deposit_tin", out string saved), "name not remembered");
            Assert.Equal("My tin", saved, "remembered name");

            GameObject otherTin = Spawn("MineRock_Tin");
            yield return Settle();
            HoverObject ping = PingAt(otherTin, Player_Patch.OnPlayerPing);
            AssertPin(ping.center, Minimap.PinType.Icon2, "My tin");
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Side angles of the spawns of one test, so its objects don't overlap.
        /// </summary>
        private static readonly float[] SpawnAngles = { 0f, 30f, -30f, 60f, -60f };

        /// <summary>
        /// Spawn a prefab on the ground close in front of the player (far objects can be hidden behind others).
        /// </summary>
        private static GameObject Spawn(string prefabName)
        {
            GameObject prefab = Assert.NotNull(ZNetScene.instance.GetPrefab(prefabName), $"prefab {prefabName}");
            Vector3 pos = GroundPoint(6f, SpawnAngles[spawnIndex++ % SpawnAngles.Length]);
            GameObject go = UnityEngine.Object.Instantiate(prefab, pos, Quaternion.identity);
            spawned.Add(go);
            ZDO zdo = go.GetComponent<ZNetView>()?.GetZDO();
            if (zdo != null) spawnedZDOs[zdo.m_uid] = prefabName;
            return go;
        }

        /// <param name="angle">Degrees to the right of where the player faces</param>
        private static Vector3 GroundPoint(float distance = 5f, float angle = 0f)
        {
            Transform player = Player.m_localPlayer.transform;
            Vector3 forward = Quaternion.Euler(0f, angle, 0f) * Vector3.ProjectOnPlane(player.forward, Vector3.up).normalized;
            Vector3 pos = player.position + forward * distance;
            pos.y = ZoneSystem.instance.GetGroundHeight(pos);
            return pos;
        }

        /// <summary>
        /// A point of the object that the player's eyes can see: the center of a collider can be empty
        /// (the opening of a portal) or hidden behind something else.
        /// </summary>
        private static Vector3 AimPoint(GameObject go)
        {
            int layers = Player_Patch.PingLayers;
            Collider[] colliders = go.GetComponentsInChildren<Collider>()
                .Where(c => c.enabled && !c.isTrigger && (layers & (1 << c.gameObject.layer)) != 0)
                .OrderByDescending(c => c.bounds.size.sqrMagnitude)
                .ToArray();
            Assert.True(colliders.Length > 0, $"{go.name} has no collider a ping can hit");

            Vector3 eye = Player.m_localPlayer.GetEyePoint();
            foreach (Collider collider in colliders)
            {
                Bounds bounds = collider.bounds;
                Vector3[] candidates =
                {
                    bounds.center,
                    collider.transform.position,
                    bounds.center + Vector3.up * bounds.extents.y * 0.5f,
                    bounds.center - Vector3.up * bounds.extents.y * 0.5f,
                    bounds.center + Vector3.right * bounds.extents.x * 0.5f,
                    bounds.center - Vector3.right * bounds.extents.x * 0.5f,
                };
                foreach (Vector3 point in candidates)
                {
                    if (Physics.Raycast(eye, point - eye, out RaycastHit hit, Vector3.Distance(eye, point) + 1f, layers)
                        && hit.collider.transform.IsChildOf(go.transform))
                        return point;
                }
            }
            throw new TestFailure($"no point of {go.name} visible from the player's eyes");
        }

        /// <summary>
        /// Let spawned objects start and their colliders reach the physics scene.
        /// </summary>
        private static IEnumerator Settle()
        {
            yield return null;
            yield return null;
            Physics.SyncTransforms();
        }

        /// <summary>
        /// Put the camera at the player's eyes, turn it to a point and find the ping target there,
        /// like the ping keys do. The game moves the camera back on its next update.
        /// </summary>
        private static HoverObject AimAt(Vector3 point)
        {
            Transform camera = GameCamera.instance.transform;
            camera.position = Player.m_localPlayer.GetEyePoint();
            camera.LookAt(point);
            Physics.SyncTransforms();
            return Player_Patch.FindPingTarget();
        }

        /// <summary>
        /// Aim at an object and check the ping target is that object.
        /// </summary>
        private static HoverObject AimAt(GameObject go)
        {
            HoverObject target = AimAt(AimPoint(go));
            Assert.True(target.Hover && (target.Hover.transform.IsChildOf(go.transform) || go.transform.IsChildOf(target.Hover.transform)),
                $"aimed at {go.name} but hit {(target.Hover ? target.Hover.name : "nothing")}");
            return target;
        }

        private static HoverObject PingAt(GameObject go, UnityEngine.Events.UnityEvent<HoverObject> pingEvent)
        {
            HoverObject ping = AimAt(go);
            pingEvent.Invoke(ping);
            return ping;
        }

        private static IEnumerator OpenCustomNamePing(HoverObject target)
        {
            Minimap_Patch.OpenCustomNamePing(target);
            Assert.True(Minimap_Patch.IsNaming, "Custom Name Ping window not opened");
            Assert.NotNull(Minimap_Patch.panel, "Custom Name Ping window");
            yield return null;
        }

        private static IEnumerable<Minimap.PinData> NewPins() => Minimap.instance.m_pins.Where(p => !pinsBefore.Contains(p));

        private static IEnumerable<Minimap.PinData> PinsAt(Vector3 pos) => NewPins().Where(p => Vector3.Distance(p.m_pos, pos) < 0.5f);

        /// <param name="type">Expected icon, or null for any</param>
        /// <param name="name">Expected name, or null for any</param>
        private static Minimap.PinData AssertPin(Vector3 pos, Minimap.PinType? type, string name)
        {
            Minimap.PinData pin = PinsAt(pos).FirstOrDefault();
            if (pin == null)
            {
                string others = string.Join(", ", NewPins().Select(p => $"'{p.m_name}' {p.m_type} at {p.m_pos}"));
                throw new TestFailure($"no pin at {pos} (new pins: {(others == "" ? "none" : others)})");
            }
            if (type.HasValue) Assert.Equal(type.Value, pin.m_type, $"pin '{pin.m_name}' icon");
            if (name != null) Assert.Equal(name, pin.m_name, "pin name");
            return pin;
        }

        private static IEnumerator WaitUntil(Func<bool> condition, string what, float timeout = 2f)
        {
            float end = Time.realtimeSinceStartup + timeout;
            while (!condition())
            {
                if (Time.realtimeSinceStartup > end)
                    throw new TestFailure($"timed out waiting for {what}");
                yield return null;
            }
        }

        #endregion
    }
}
#endif
