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
    public sealed class BattleEntrySmokeChecks : MonoBehaviour
    {
        private readonly List<string> results = new List<string>();
        private Keyboard keyboard;
        private InputRouter input;
        private GameObject lateEnemy;
        private bool previousBackground;
        private InputSettings.BackgroundBehavior previousBehavior;
        private InputSettings.EditorInputBehaviorInPlayMode previousEditorBehavior;

        [MenuItem("Tools/Art Workshop/Run Battle Entry Checks (Fresh Battle Play)")]
        public static void Run()
        {
            if (!Application.isPlaying || UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "Battle")
                throw new InvalidOperationException("Battle Scene을 새로 Play한 뒤 실행해 주세요.");
            new GameObject("BattleEntrySmokeChecks").AddComponent<BattleEntrySmokeChecks>().StartCoroutine("Checks");
        }

        private void Check(string name, bool pass) => results.Add((pass ? "PASS " : "FAIL ") + name);
        private void Press(params Key[] keys) => InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys));

        private IEnumerator Checks()
        {
            previousBackground = Application.runInBackground;
            previousBehavior = InputSystem.settings.backgroundBehavior;
            previousEditorBehavior = InputSystem.settings.editorInputBehaviorInPlayMode;
            Application.runInBackground = true;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            input = FindAnyObjectByType<InputRouter>();
            keyboard = InputSystem.AddDevice<Keyboard>("BattleEntryTestKeyboard");
            input.RuntimeActions.devices = new InputDevice[] { keyboard };
            var battle = FindAnyObjectByType<BattleCoordinator>();
            var player = FindAnyObjectByType<PlayerMovement>();
            var playerBody = player.GetComponent<Rigidbody2D>();
            var playerCombat = player.GetComponent<Combatant>();
            var enemies = FindObjectsByType<EnemyMovement>().OrderBy(e => e.transform.position.x).ToArray();
            int entries = 0;
            battle.BattleStarted += () => entries++;
            yield return new WaitForSecondsRealtime(.4f);
            Check("three enemies", enemies.Length == 3);
            Check("not in battle before contact", !battle.IsBattleActive);
            float x = playerBody.position.x;
            Press(Key.D);
            yield return new WaitForSecondsRealtime(.25f);
            Check("PlayerMovement forwards right input", playerBody.position.x > x + .2f);
            Press(Key.A, Key.D);
            yield return new WaitForSecondsRealtime(.1f);
            x = playerBody.position.x;
            yield return new WaitForSecondsRealtime(.25f);
            Check("simultaneous movement neutral", Mathf.Abs(playerBody.position.x - x) < .02f);
            Press(Key.A);
            yield return new WaitForSecondsRealtime(.35f);
            Check("PlayerMovement forwards left and flips Visual", playerBody.position.x < x && player.transform.Find("Visual").localScale.x < 0);
            Press();
            bool positive = false, negative = false, inBounds = true, facing = true;
            float end = Time.realtimeSinceStartup + 6;
            while (Time.realtimeSinceStartup < end)
            {
                var body = enemies[0].GetComponent<Rigidbody2D>();
                positive |= body.linearVelocity.x > .1f;
                negative |= body.linearVelocity.x < -.1f;
                if (Mathf.Abs(body.linearVelocity.x) > .1f)
                    facing &= enemies[0].transform.Find("Visual").localScale.x * body.linearVelocity.x > 0 && enemies[0].transform.localScale.x == 1;
                inBounds &= body.position.x >= -2.03f && body.position.x <= 2.03f;
                yield return null;
            }
            Check("patrol reverses both directions", positive && negative);
            Check("patrol stays in spawn-relative range", inBounds);
            Check("Presenter faces enemy along patrol without flipping root", facing);
            Check("enemy movement animation", enemies[0].GetComponentInChildren<Animator>().GetCurrentAnimatorStateInfo(0).IsName("Move"));
            var opponent = enemies[0].GetComponent<Combatant>();
            playerBody.position = enemies[0].GetComponent<Rigidbody2D>().position + new Vector2(-1.3f, 0);
            yield return new WaitForSecondsRealtime(.3f);
            Check("physical trigger enters battle once", battle.IsBattleActive && entries == 1 && battle.Opponent == opponent);
            Check("duplicate opponent rejected", !battle.TryBegin(playerCombat, opponent));
            Check("second opponent rejected", !battle.TryBegin(playerCombat, enemies[1].GetComponent<Combatant>()));
            Check("all movement locked", player.GetComponent<PlayerMovement>().IsBattleLocked && enemies.All(e => e.IsBattleLocked));
            var positions = enemies.Select(e => e.GetComponent<Rigidbody2D>().position).ToArray();
            var playerPosition = playerBody.position;
            Press(Key.D);
            playerBody.AddForce(Vector2.up * 100);
            yield return new WaitForSecondsRealtime(.5f);
            Check("player XY frozen under input and force", Vector2.Distance(playerPosition, playerBody.position) < .001f);
            Check("all enemies XY frozen", enemies.Select((e, i) => Vector2.Distance(e.GetComponent<Rigidbody2D>().position, positions[i]) < .001f).All(value => value));
            Check("Idle during battle", enemies.All(e => e.GetComponentInChildren<Animator>().GetCurrentAnimatorStateInfo(0).IsName("Idle")) && player.GetComponentInChildren<Animator>().GetCurrentAnimatorStateInfo(0).IsName("Idle"));
            Check("trigger stay does not reenter", entries == 1);
            int healthBeforeAttack = opponent.Stats.CurrentHealth;
            Check("Attack remains usable while locked", playerCombat.TryAttack(opponent, 0) && opponent.Stats.CurrentHealth == healthBeforeAttack - playerCombat.Stats.AttackPower);
            yield return new WaitForSecondsRealtime(.5f);
            Check("action returns to Idle without unlocking", enemies[0].GetComponentInChildren<Animator>().GetCurrentAnimatorStateInfo(0).IsName("Idle") && enemies[0].IsBattleLocked);
            lateEnemy = Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemy.prefab"), new Vector3(17, 2, 0), Quaternion.identity);
            yield return new WaitForSecondsRealtime(.2f);
            Check("late spawned enemy locked before movement", lateEnemy.GetComponent<EnemyMovement>().IsBattleLocked && Mathf.Abs(lateEnemy.transform.position.y - 2) < .001f);
            Press();
            battle.enabled = false;
            yield return new WaitForSecondsRealtime(.3f);
            Check("disabled coordinator releases constraints", !battle.IsBattleActive && !player.GetComponent<PlayerMovement>().IsBattleLocked && enemies.All(e => !e.IsBattleLocked));
            Check("missing active coordinator does not enter", entries == 1);
            Check("enemy patrol resumes", Mathf.Abs(enemies[1].GetComponent<Rigidbody2D>().linearVelocity.x) > .1f);
            playerBody.position = new Vector2(-5, .76f);
            battle.enabled = true;
            opponent.TakeDamage(int.MaxValue);
            Check("dead enemy rejected", !battle.TryBegin(playerCombat, opponent));
            Check("null participant rejected", !battle.TryBegin(null, opponent));
            playerCombat.TakeDamage(int.MaxValue);
            Check("dead player rejected", !battle.TryBegin(playerCombat, enemies[1].GetComponent<Combatant>()));
            System.IO.Directory.CreateDirectory("ValidationCaptures/Stage5");
            System.IO.File.WriteAllLines("ValidationCaptures/Stage5/battle-entry-checks.txt", results);
            Debug.Log(string.Join("\n", results));
            Destroy(gameObject);
        }

        private void OnDestroy()
        {
            if (input != null && input.RuntimeActions != null) input.RuntimeActions.devices = null;
            if (keyboard != null && keyboard.added) InputSystem.RemoveDevice(keyboard);
            if (lateEnemy != null) Destroy(lateEnemy);
            InputSystem.settings.backgroundBehavior = previousBehavior;
            InputSystem.settings.editorInputBehaviorInPlayMode = previousEditorBehavior;
            Application.runInBackground = previousBackground;
        }
    }
}
#endif
