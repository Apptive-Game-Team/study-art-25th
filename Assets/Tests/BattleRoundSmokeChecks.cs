#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace ArtUnityWorkshop
{
    public sealed class BattleRoundSmokeChecks : MonoBehaviour
    {
        readonly List<string> results = new List<string>();
        Keyboard keyboard;
        InputRouter input;
        BattleCoordinator battle;
        bool background;
        InputSettings.BackgroundBehavior behavior;
        InputSettings.EditorInputBehaviorInPlayMode editorBehavior;
        UnityEngine.Random.State randomState;
        int resolved;

        [MenuItem("Tools/Art Workshop/Run Battle Round Checks (Fresh Battle Play)")]
        public static void Run()
        {
            if (!Application.isPlaying) throw new InvalidOperationException("Battle을 새로 Play한 뒤 실행하세요.");
            new GameObject("BattleRoundSmokeChecks").AddComponent<BattleRoundSmokeChecks>().StartCoroutine("Checks");
        }
        void Check(string name, bool pass) => results.Add((pass ? "PASS " : "FAIL ") + name);
        void Press(params Key[] keys) => InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys));
        IEnumerator Tap()
        {
            Press(); yield return new WaitForSecondsRealtime(.08f);
            Press(Key.F); yield return new WaitForSecondsRealtime(.08f);
            Press(); yield return new WaitForSecondsRealtime(.08f);
        }
        void Slots(Hand player, Hand enemy)
        {
            foreach (string name in new[] { "PlayerLeft", "PlayerRight", "EnemyLeft", "EnemyRight" })
                typeof(BattleCoordinator).GetProperty(name).SetValue(battle, name.StartsWith("Player") ? player : enemy);
        }
        IEnumerator Checks()
        {
            background = Application.runInBackground;
            behavior = InputSystem.settings.backgroundBehavior;
            editorBehavior = InputSystem.settings.editorInputBehaviorInPlayMode;
            randomState = UnityEngine.Random.state;
            Application.runInBackground = true;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            yield return null;
            input = FindAnyObjectByType<InputRouter>();
            keyboard = InputSystem.AddDevice<Keyboard>("RoundTestKeyboard");
            input.RuntimeActions.devices = new InputDevice[] { keyboard };
            input.RuntimeActions.FindAction("Gameplay/Interact").ApplyBindingOverride(0, "<Keyboard>/f");
            battle = FindAnyObjectByType<BattleCoordinator>();
            // This check isolates round input; the complete-flow check covers the return button.
            var sceneFlow = FindAnyObjectByType<SceneFlow>();
            if (sceneFlow != null) sceneFlow.enabled = false;
            var player = FindAnyObjectByType<PlayerMovement>().GetComponent<Combatant>();
            var enemies = FindObjectsByType<EnemyMovement>().OrderBy(e => e.transform.position.x).ToArray();
            var enemy = enemies[0].GetComponent<Combatant>();
            var enemySettings = new SerializedObject(enemy.Stats);
            enemySettings.FindProperty("maxHealth").intValue = player.Stats.AttackPower * 3;
            enemySettings.ApplyModifiedPropertiesWithoutUndo();
            enemy.Stats.RestoreHealth(enemy.Stats.MaxHealth);
            foreach (var trigger in FindObjectsByType<BattleTrigger>()) trigger.enabled = false;
            battle.RoundResolved += () => resolved++;
            int[,] expected = { {0,1,-1}, {-1,0,1}, {1,-1,0} };
            for (int p = 0; p < 3; p++) for (int e = 0; e < 3; e++)
                Check($"judge {p}/{e}", BattleCoordinator.Judge((Hand)p, (Hand)e) == expected[p,e]);
            Check("begin", battle.TryBegin(player, enemy));
            Press(Key.Space); yield return new WaitForSecondsRealtime(.1f); Press();
            Check("old Interact binding ignored in battle", battle.Phase == BattlePhase.StopLeft);
            var left = battle.EnemyLeft; var right = battle.EnemyRight;
            yield return new WaitForSecondsRealtime(.4f);
            Check("enemy candidates stay public and fixed", battle.EnemyLeft == left && battle.EnemyRight == right && battle.EnemySelectedIndex == -1);
            yield return Tap();
            var stopped = battle.PlayerLeft;
            Check("first press only stops left", battle.Phase == BattlePhase.StopRight);
            yield return new WaitForSecondsRealtime(.4f);
            Check("stopped left stays fixed", battle.PlayerLeft == stopped);
            yield return Tap();
            Check("second press starts selection, not resolution", battle.Phase == BattlePhase.Selecting && resolved == 0 && battle.SelectedIndex == 1 && battle.RemainingTime > 2.7f);
            Press(Key.A); yield return new WaitForSecondsRealtime(.1f);
            Check("left selection", battle.SelectedIndex == 0);
            Press(Key.A, Key.D); yield return new WaitForSecondsRealtime(.1f);
            Check("simultaneous selection preserves highlight", battle.SelectedIndex == 0);
            Press(Key.D); yield return new WaitForSecondsRealtime(.1f);
            Check("right selection", battle.SelectedIndex == 1);
            Slots(Hand.Rock, Hand.Rock);
            int hp = player.Stats.CurrentHealth, ehp = enemy.Stats.CurrentHealth;
            Press(); yield return new WaitForSecondsRealtime(3.1f);
            Check("timeout resolves once with identical candidates and no draw damage", resolved == 1 && hp == player.Stats.CurrentHealth && ehp == enemy.Stats.CurrentHealth);
            yield return new WaitForSecondsRealtime(.7f);
            Check("draw starts next round and remains locked", battle.RoundNumber == 2 && battle.Phase == BattlePhase.StopLeft && enemies.All(e => e.IsBattleLocked));
            yield return Tap(); yield return Tap();
            Slots(Hand.Rock, Hand.Scissors);
            yield return Tap();
            Check("winner attacks exactly once", resolved == 2 && enemy.Stats.CurrentHealth == ehp - player.Stats.AttackPower && player.Stats.CurrentHealth == hp);
            Press(Key.F); yield return new WaitForSecondsRealtime(1);
            Check("held input cannot stop next round", resolved == 2 && battle.Phase == BattlePhase.StopLeft);
            Press();
            yield return Tap(); yield return Tap();
            Slots(Hand.Scissors, Hand.Rock);
            yield return Tap();
            Check("loser takes enemy attack power once", resolved == 3 && player.Stats.CurrentHealth == hp - enemy.Stats.AttackPower);
            yield return new WaitForSecondsRealtime(1);
            // Same random state must select the same enemy slot despite a different player choice.
            var seed = UnityEngine.Random.state;
            yield return Tap(); yield return Tap(); Slots(Hand.Rock, Hand.Rock);
            UnityEngine.Random.state = seed;
            yield return Tap(); int firstChoice = battle.EnemySelectedIndex;
            yield return new WaitForSecondsRealtime(1);
            yield return Tap(); yield return Tap(); Slots(Hand.Paper, Hand.Rock);
            UnityEngine.Random.state = seed;
            yield return Tap();
            Check("enemy choice independent of player hand", firstChoice == battle.EnemySelectedIndex);
            yield return new WaitForSecondsRealtime(1);
            int finished = 0; bool victory = false;
            battle.BattleFinished += won => { finished++; victory = won; };
            yield return Tap(); yield return Tap(); Slots(Hand.Paper, Hand.Rock);
            yield return Tap();
            Check("death waits for presentation", battle.Phase == BattlePhase.Dying && finished == 0);
            yield return new WaitForSecondsRealtime(1.3f);
            Check("victory ends once after hiding visual and unlocks", !battle.IsBattleActive && finished == 1 && victory && !enemy.transform.Find("Visual").gameObject.activeSelf && enemies.All(e => !e.IsBattleLocked));
            input.enabled = false;
            Check("missing active input rejects battle without locking", !battle.TryBegin(player, enemies[1].GetComponent<Combatant>()) && !battle.IsBattleActive);
            input.enabled = true;
            Check("new battle after victory", battle.TryBegin(player, enemies[1].GetComponent<Combatant>()));
            player.TakeDamage(player.Stats.CurrentHealth - 1);
            yield return Tap(); yield return Tap(); Slots(Hand.Scissors, Hand.Rock);
            yield return Tap();
            yield return new WaitForSecondsRealtime(1.3f);
            Check("defeat waits then stays locked for return flow", battle.Phase == BattlePhase.Defeated && finished == 2 && !victory && !player.transform.Find("Visual").gameObject.activeSelf && enemies.All(e => e.IsBattleLocked));
            yield return Tap();
            Check("defeat ignores old gameplay input", battle.Phase == BattlePhase.Defeated && finished == 2);
            battle.enabled = false;
            Check("disable cleans up battle and movement", !battle.IsBattleActive && enemies.All(e => !e.IsBattleLocked));
            System.IO.Directory.CreateDirectory("ValidationCaptures/Stage6");
            System.IO.File.WriteAllLines("ValidationCaptures/Stage6/battle-round-checks.txt", results);
            Debug.Log(string.Join("\n", results));
            Destroy(gameObject);
        }
        void OnDestroy()
        {
            if (input != null && input.RuntimeActions != null)
            {
                input.RuntimeActions.FindAction("Gameplay/Interact").RemoveAllBindingOverrides();
                input.RuntimeActions.devices = null;
            }
            if (keyboard != null && keyboard.added) InputSystem.RemoveDevice(keyboard);
            InputSystem.settings.backgroundBehavior = behavior;
            InputSystem.settings.editorInputBehaviorInPlayMode = editorBehavior;
            Application.runInBackground = background;
            UnityEngine.Random.state = randomState;
        }
    }
}
#endif
