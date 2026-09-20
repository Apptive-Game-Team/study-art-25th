#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace ArtUnityWorkshop
{
    public sealed class WorkshopFlowChecks : MonoBehaviour
    {
        readonly List<string> results = new List<string>();
        Keyboard keyboard;
        InputRouter input;
        bool background;
        InputSettings.BackgroundBehavior behavior;
        InputSettings.EditorInputBehaviorInPlayMode editorBehavior;
        [MenuItem("Tools/Art Workshop/Run Complete Workshop Checks (Fresh Play)")]
        public static void Run()
        {
            if (!Application.isPlaying) throw new InvalidOperationException("먼저 Play를 시작하세요.");
            var go = new GameObject("WorkshopFlowChecks"); DontDestroyOnLoad(go);
            go.AddComponent<WorkshopFlowChecks>().StartCoroutine("Checks");
        }
        void Check(string name, bool pass) { results.Add((pass ? "PASS " : "FAIL ") + name); Save(); }
        void Save() { System.IO.Directory.CreateDirectory("ValidationCaptures/Stage12"); System.IO.File.WriteAllLines("ValidationCaptures/Stage12/workshop-checks.txt", results); }
        void Bind()
        {
            input = FindAnyObjectByType<InputRouter>(); input.RuntimeActions.devices = new InputDevice[] { keyboard };
        }
        void Press(params Key[] keys) => InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys));
        IEnumerator Tap(Key key = Key.Space)
        {
            Press(); yield return new WaitForSecondsRealtime(.1f);
            Press(key); yield return new WaitForSecondsRealtime(.15f);
            Press(); yield return new WaitForSecondsRealtime(.15f);
        }
        IEnumerator SceneReady(string name)
        {
            float limit = Time.realtimeSinceStartup + 10;
            while ((SceneManager.GetActiveScene().name != name || GameSession.Transitioning) && Time.realtimeSinceStartup < limit) yield return null;
            yield return new WaitForSecondsRealtime(.3f);
            Check("loaded " + name, SceneManager.GetActiveScene().name == name && !GameSession.Transitioning);
            Bind();
        }
        EnemyMovement[] Enemies() => FindObjectsByType<EnemyMovement>(FindObjectsInactive.Include).OrderBy(e => e.name).ToArray();
        IEnumerator Checks()
        {
            background = Application.runInBackground; Application.runInBackground = true;
            behavior = InputSystem.settings.backgroundBehavior; editorBehavior = InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            keyboard = InputSystem.AddDevice<Keyboard>("WorkshopTestKeyboard");
            GameSession.Reset(); SceneManager.LoadSceneAsync("Town"); yield return SceneReady("Town");
            var flow = FindAnyObjectByType<SceneFlow>();
            Check("fresh player maximum health", flow.Player.Stats.CurrentHealth == flow.Player.Stats.MaxHealth);
            Check("fresh trophy empty", !GameSession.TrophyOwned && !FindAnyObjectByType<TrophyDisplay>().IsDisplayed);
            Check("respawn rounding 4/15/25/100", GameSession.RespawnHealth(4)==1 && GameSession.RespawnHealth(15)==2 && GameSession.RespawnHealth(25)==3 && GameSession.RespawnHealth(100)==10);
            var rightAction=input.RuntimeActions.FindAction("Gameplay/MoveRight");rightAction.ApplyBindingOverride(0,"<Keyboard>/rightArrow");
            float startX=flow.Player.transform.position.x;Press(Key.D);yield return new WaitForSecondsRealtime(.25f);
            Check("old movement binding ignored",Mathf.Abs(flow.Player.transform.position.x-startX)<.02f);
            Press(Key.RightArrow);yield return new WaitForSecondsRealtime(.25f);Press();
            Check("new movement binding works",flow.Player.transform.position.x>startX+.2f);rightAction.RemoveAllBindingOverrides();
            var interact=input.RuntimeActions.FindAction("Gameplay/Interact");interact.ApplyBindingOverride(0,"<Keyboard>/f");
            flow.Player.TakeDamage(1);flow.Player.GetComponent<Rigidbody2D>().position=new Vector2(-1,.8f);yield return Tap(Key.F);
            Check("new Interact binding heals and appears in label",flow.Player.Stats.CurrentHealth==100&&input.BindingLabel("Interact").Contains("F"));interact.RemoveAllBindingOverrides();
            var combatSettings=new SerializedObject(flow.Player);var particleProperty=combatSettings.FindProperty("hitParticlePrefab");var originalParticle=particleProperty.objectReferenceValue;
            particleProperty.objectReferenceValue=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/HitEffect1.prefab").GetComponent<ParticleSystem>();combatSettings.ApplyModifiedPropertiesWithoutUndo();
            flow.Player.TakeDamage(1);yield return new WaitForSecondsRealtime(.1f);
            Check("alternative particle prefab plays",FindObjectsByType<ParticleSystem>().Any(p=>p.name.StartsWith("HitEffect1")));
            yield return new WaitForSecondsRealtime(1.4f);particleProperty.objectReferenceValue=null;combatSettings.ApplyModifiedPropertiesWithoutUndo();flow.Player.TakeDamage(1);
            Check("missing particle keeps damage without spawning",flow.Player.Stats.CurrentHealth==98&&FindObjectsByType<ParticleSystem>().Length==0);
            particleProperty.objectReferenceValue=originalParticle;combatSettings.ApplyModifiedPropertiesWithoutUndo();
            var preview=FindAnyObjectByType<TrophyDisplay>();var previewSettings=new SerializedObject(preview);previewSettings.FindProperty("displayImmediately").boolValue=true;previewSettings.ApplyModifiedPropertiesWithoutUndo();yield return new WaitForSecondsRealtime(.2f);
            Check("forced display does not award trophy",preview.IsDisplayed&&!GameSession.TrophyOwned);previewSettings.FindProperty("displayImmediately").boolValue=false;previewSettings.ApplyModifiedPropertiesWithoutUndo();
            flow.Player.TakeDamage(35);
            flow.Player.GetComponent<Rigidbody2D>().position = new Vector2(-1,.8f); yield return Tap();
            Check("healing via Interact", flow.Player.Stats.CurrentHealth == flow.Player.Stats.MaxHealth);
            flow.Player.GetComponent<Rigidbody2D>().position = new Vector2(10,.8f); yield return Tap(); yield return SceneReady("Battle");
            flow=FindAnyObjectByType<SceneFlow>(); var enemies=Enemies(); var battle=FindAnyObjectByType<BattleCoordinator>();
            Check("three initial enemies and two fixed portals", enemies.Length==3 && FindObjectsByType<InteractionPoint>().Length==2);
            Check("four tilemap layers", FindObjectsByType<UnityEngine.Tilemaps.Tilemap>().Length==4);
            Check("post processing configured", FindAnyObjectByType<UnityEngine.Rendering.Volume>().sharedProfile.components.Count==3);
            Check("single combined slot panel", FindObjectsByType<BattleSlotPanel>(FindObjectsInactive.Include).Length==1 && FindObjectsByType<HeadSlotPanel>(FindObjectsInactive.Include).Length==0);
            var enemy=enemies[0].GetComponent<Combatant>();
            Check("battle begins",battle.TryBegin(flow.Player,enemy));
            enemy.TakeDamage(10);
            yield return new WaitForSecondsRealtime(.1f);
            Check("hit particle spawned", FindObjectsByType<ParticleSystem>().Length>0);
            enemy.TakeDamage(int.MaxValue); yield return new WaitForSecondsRealtime(1.3f);
            Check("victory awards trophy and ends battle", GameSession.TrophyOwned && !battle.IsBattleActive);
            Check("dead colliders disabled", enemy.GetComponentsInChildren<Collider2D>(true).All(c=>!c.enabled));
            Check("no victory portal duplication", FindObjectsByType<InteractionPoint>().Length==2);
            enemies[1].GetComponent<Combatant>().TakeDamage(7);
            yield return new WaitForSecondsRealtime(1.5f);
            Check("particles cleaned", FindObjectsByType<ParticleSystem>().Length==0);
            flow.Player.GetComponent<Rigidbody2D>().position = new Vector2(-8,.8f); yield return Tap(); yield return SceneReady("Town");
            var trophy=FindAnyObjectByType<TrophyDisplay>();
            Check("trophy survives scene change", GameSession.TrophyOwned && trophy.IsDisplayed);
            flow=FindAnyObjectByType<SceneFlow>(); flow.Player.GetComponent<Rigidbody2D>().position = new Vector2(4,.8f);
            int pose=trophy.PoseIndex; yield return Tap(); Check("Interact advances pose", trophy.PoseIndex!=pose);
            var t=new SerializedObject(trophy);t.FindProperty("modelPrefab").objectReferenceValue=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/TrophyModelB.prefab");t.ApplyModifiedPropertiesWithoutUndo();
            yield return new WaitForSecondsRealtime(.4f);
            var model=t.FindProperty("modelRoot").objectReferenceValue as Transform;
            Check("alternate Humanoid model connected", model.GetComponentInChildren<Animator>().avatar.isHuman);
            flow.Travel("Battle"); yield return SceneReady("Battle"); flow=FindAnyObjectByType<SceneFlow>(); enemies=Enemies(); battle=FindAnyObjectByType<BattleCoordinator>();
            Check("dead enemy stays absent while others survive", !enemies[0].gameObject.activeSelf);
            Check("enemy damaged health restored", enemies[1].GetComponent<CharacterStats>().CurrentHealth==23);
            Check("remaining enemy not duplicated", enemies.Length==3 && enemies.Count(e=>e.gameObject.activeSelf)==2);
            enemy=enemies[1].GetComponent<Combatant>(); battle.TryBegin(flow.Player,enemy);
            var animator=flow.Player.GetComponentInChildren<Animator>();
            animator.runtimeAnimatorController=AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Animations/Variants/CharacterStyle1.overrideController");
            int before=enemy.Stats.CurrentHealth;flow.Player.TryAttack(enemy,flow.Player.NextAttackSequence);
            Check("alternate clips preserve damage",enemy.Stats.CurrentHealth==before-flow.Player.Stats.AttackPower);
            int expectedEnemyHealth = enemy.Stats.CurrentHealth;
            string opponentName = enemy.name;
            flow.Player.TakeDamage(int.MaxValue); yield return new WaitForSecondsRealtime(1.2f);
            var button=FindObjectsByType<UnityEngine.UI.Button>().FirstOrDefault(b=>b.name=="ReturnToTownButton");
            Check("defeat button shown and focused", battle.Phase==BattlePhase.Defeated && button!=null && UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject==button.gameObject);
            Check("death hides player visual", !flow.Player.transform.Find("Visual").gameObject.activeSelf);
            Check("battle portal blocked during defeat",!flow.Travel("Town"));
            input.RuntimeActions.FindAction("Gameplay/Interact").ApplyBindingOverride(0,"<Keyboard>/f");
            yield return Tap();
            Check("old Interact binding cannot return after defeat",SceneManager.GetActiveScene().name=="Battle");
            yield return Tap(Key.F); yield return SceneReady("Town"); flow=FindAnyObjectByType<SceneFlow>();
            Check("Interact returns and revives at ten percent",flow.Player.Stats.CurrentHealth==10 && Mathf.Abs(flow.Player.transform.position.x+5)<.1f);
            Check("defeat stores opponent health",GameSession.Enemies[opponentName]==expectedEnemyHealth);
            flow.Player.GetComponent<Rigidbody2D>().position=new Vector2(-1,.8f);yield return Tap();
            Check("recovery after defeat",flow.Player.Stats.CurrentHealth==100);
            flow.Travel("Battle");yield return SceneReady("Battle");flow=FindAnyObjectByType<SceneFlow>();enemies=Enemies();
            foreach(var e in enemies.Where(e=>e.gameObject.activeSelf))e.GetComponent<Combatant>().TakeDamage(int.MaxValue);
            yield return new WaitForSecondsRealtime(1.2f);flow.Travel("Town");yield return SceneReady("Town");FindAnyObjectByType<SceneFlow>().Travel("Battle");yield return SceneReady("Battle");
            Check("all dead respawn as new wave",Enemies().All(e=>e.gameObject.activeSelf&&e.GetComponent<CharacterStats>().CurrentHealth==30));
            Check("trophy remains single flag",GameSession.TrophyOwned);
            Debug.Log(string.Join("\n",results));Destroy(gameObject);
        }
        private void OnDestroy()
        {
            if(input!=null&&input.RuntimeActions!=null)input.RuntimeActions.devices=null;
            if(keyboard!=null&&keyboard.added)InputSystem.RemoveDevice(keyboard);
            InputSystem.settings.backgroundBehavior=behavior;InputSystem.settings.editorInputBehaviorInPlayMode=editorBehavior;
            Application.runInBackground=background;
        }
    }
}
#endif
