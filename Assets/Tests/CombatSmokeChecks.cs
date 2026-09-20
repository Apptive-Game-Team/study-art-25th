#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ArtUnityWorkshop
{
    // Editor-only smoke check; no test objects or input bindings are saved in game scenes.
    public sealed class CombatSmokeChecks : MonoBehaviour
    {
        private readonly List<string> results = new List<string>();
        private readonly List<GameObject> objects = new List<GameObject>();

        [MenuItem("Tools/Art Workshop/Run Combat Checks (Play Mode)")]
        public static void Run()
        {
            if (!Application.isPlaying) throw new InvalidOperationException("Play Mode에서 실행해 주세요.");
            new GameObject("CombatSmokeChecks").AddComponent<CombatSmokeChecks>().StartCoroutine("CheckAll");
        }

        private void Check(string name, bool pass) => results.Add((pass ? "PASS " : "FAIL ") + name);
        private static void Set(UnityEngine.Object target, string field, UnityEngine.Object value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(field).objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private Combatant Create(bool animated, out CharacterAnimationPresenter presenter, out GameObject visual)
        {
            var root = new GameObject("CheckCharacter");
            root.SetActive(false);
            objects.Add(root);
            root.AddComponent<CharacterStats>();
            presenter = root.AddComponent<CharacterAnimationPresenter>();
            visual = new GameObject("Visual");
            visual.transform.SetParent(root.transform);
            visual.AddComponent<SpriteRenderer>();
            if (animated)
            {
                var animator = visual.AddComponent<Animator>();
                animator.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Animations/CharacterPrototype.overrideController");
                Set(presenter, "animator", animator);
            }
            Set(presenter, "visualRoot", visual);
            var combatant = root.AddComponent<Combatant>();
            Set(combatant, "animationPresenter", presenter);
            root.SetActive(true);
            return combatant;
        }

        private IEnumerator CheckAll()
        {
            var attacker = Create(true, out var attackPresenter, out var attackVisual);
            var victim = Create(true, out var presenter, out var visual);
            visual.transform.localScale = new Vector3(2, 3, 1);
            presenter.SetMovement(-1);
            Check("Presenter flips Visual only and preserves size", visual.transform.localScale == new Vector3(-2, 3, 1) && victim.transform.localScale == Vector3.one);
            presenter.SetMovement(0);
            Check("stopping preserves facing", visual.transform.localScale.x == -2);
            presenter.SetMovement(1);
            Check("Presenter faces right", visual.transform.localScale == new Vector3(2, 3, 1));
            // Slow only these assertions so a long editor frame cannot skip a short Clip.
            attackVisual.GetComponent<Animator>().speed = .01f;
            visual.GetComponent<Animator>().speed = .01f;
            int changes = 0, deaths = 0, completed = 0;
            victim.Stats.HealthChanged += () => changes++;
            victim.Died += () => deaths++;
            victim.DeathPresentationCompleted += () => completed++;
            Check("starts at maximum", victim.Stats.CurrentHealth == 100);
            Check("negative damage ignored", victim.TakeDamage(-10) == 0 && changes == 0);
            Check("zero damage ignored", victim.TakeDamage(0) == 0 && changes == 0);
            Check("attack applies once", attacker.TryAttack(victim, 1) && victim.Stats.CurrentHealth == 90);
            Check("duplicate and old sequence rejected", !attacker.TryAttack(victim, 1) && !attacker.TryAttack(victim, 0) && victim.Stats.CurrentHealth == 90);
            Check("self and null rejected", !attacker.TryAttack(attacker, 2) && !attacker.TryAttack(null, 2));
            yield return null;
            yield return null;
            yield return null;
            Check("Attack state", attackVisual.GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).IsName("Attack"));
            Check("Hit state", visual.GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).IsName("Hit"));
            attackVisual.GetComponent<Animator>().speed = 1;
            visual.GetComponent<Animator>().speed = 1;
            yield return new WaitForSecondsRealtime(.5f);
            Check("returns to Idle", visual.GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).IsName("Idle"));
            Check("damage clamps without integer overflow", victim.TakeDamage(int.MaxValue) == 90 && victim.Stats.CurrentHealth == 0);
            Check("death event once", deaths == 1);
            Check("dead target rejects attack", !attacker.TryAttack(victim, 2));
            Check("repeated lethal damage ignored", victim.TakeDamage(1) == 0 && deaths == 1);
            Check("visual remains until death animation", visual.activeSelf && completed == 0);
            yield return new WaitForSecondsRealtime(.6f);
            Check("death completed once and only Visual hidden", completed == 1 && !visual.activeSelf && victim.gameObject.activeSelf);
            var fallback = Create(false, out var fallbackPresenter, out var fallbackVisual);
            int fallbackDone = 0;
            fallback.DeathPresentationCompleted += () => fallbackDone++;
            fallback.TakeDamage(int.MaxValue);
            yield return new WaitForSecondsRealtime(1.2f);
            Check("missing Animator fallback", fallbackDone == 1 && !fallbackVisual.activeSelf);
            var interrupted = Create(true, out var interruptedPresenter, out var interruptedVisual);
            int interruptedDone = 0;
            interrupted.DeathPresentationCompleted += () => interruptedDone++;
            interrupted.TakeDamage(int.MaxValue);
            interruptedPresenter.enabled = false;
            Check("disabled presenter completes once", interruptedDone == 1 && !interruptedVisual.activeSelf);
            var stalled = Create(true, out var stalledPresenter, out var stalledVisual);
            stalledVisual.GetComponent<Animator>().speed = 0;
            int stalledDone = 0;
            stalled.DeathPresentationCompleted += () => stalledDone++;
            stalled.TakeDamage(int.MaxValue);
            yield return new WaitForSecondsRealtime(1.3f);
            Check("stopped Animator fallback", stalledDone == 1 && !stalledVisual.activeSelf);
            var longDeath = Create(true, out var longPresenter, out var longVisual);
            var replacement = new AnimationClip();
            replacement.SetCurve("", typeof(SpriteRenderer), "m_Color.a", AnimationCurve.Linear(0, 1, 1.6f, 0));
            var longAnimator = longVisual.GetComponent<Animator>();
            var replacementController = new AnimatorOverrideController(longAnimator.runtimeAnimatorController);
            replacementController["Death"] = replacement;
            longAnimator.runtimeAnimatorController = replacementController;
            int longDone = 0;
            longDeath.DeathPresentationCompleted += () => longDone++;
            longDeath.TakeDamage(int.MaxValue);
            yield return new WaitForSecondsRealtime(1.2f);
            Check("long replacement not cut by fallback timeout", longDone == 0 && longVisual.activeSelf);
            yield return new WaitForSecondsRealtime(.7f);
            Check("long replacement completes", longDone == 1 && !longVisual.activeSelf);
            Destroy(replacementController);
            Destroy(replacement);
            attacker.Stats.RestoreHealth(int.MaxValue);
            Check("restore clamps to max", attacker.Stats.CurrentHealth == 100);
            attacker.Stats.RestoreHealth(-1);
            Check("restore clamps to zero", attacker.Stats.CurrentHealth == 0);
            Check("dead attacker cannot attack", !attacker.TryAttack(interrupted, 99));
            System.IO.Directory.CreateDirectory("ValidationCaptures/Stage4");
            System.IO.File.WriteAllLines("ValidationCaptures/Stage4/combat-checks.txt", results);
            Debug.Log(string.Join("\n", results));
            foreach (var item in objects) Destroy(item);
            Destroy(gameObject);
        }
    }
}
#endif
