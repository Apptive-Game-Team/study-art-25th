using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ArtUnityWorkshop
{
    public enum BattlePhase { Inactive, StopLeft, StopRight, Selecting, Resolving, Dying, Defeated }
    public enum Hand { Rock, Scissors, Paper }

    [DisallowMultipleComponent]
    public sealed class BattleCoordinator : MonoBehaviour
    {
        [SerializeField, InspectorName("입력 연결"), Tooltip("비워 두면 같은 Scene의 InputRouter를 찾습니다.")]
        private InputRouter inputRouter;
        [SerializeField, Min(.02f), InspectorName("슬롯 회전 간격"), Tooltip("손 모양이 바뀌는 간격(초), 최소 0.02초입니다.")]
        private float slotInterval = .12f;
        [SerializeField, Min(0), InspectorName("라운드 결과 대기"), Tooltip("다음 라운드까지 결과를 표시하는 최소 시간(초)입니다. 공격·피격 표현이 끝날 때까지 기다립니다.")]
        private float roundDelay = .6f;
        public BattlePhase Phase { get; private set; }
        public Hand PlayerLeft { get; private set; }
        public Hand PlayerRight { get; private set; }
        public Hand EnemyLeft { get; private set; }
        public Hand EnemyRight { get; private set; }
        public int SelectedIndex { get; private set; }
        public int EnemySelectedIndex { get; private set; } = -1;
        public int RoundNumber { get; private set; }
        public int Result { get; private set; }
        public float RemainingTime => Phase == BattlePhase.Selecting ? Mathf.Max(0, deadline - Time.unscaledTime) : 0;
        public event Action RoundResolved;
        public event Action<bool> BattleFinished;
        private float deadline, nextSpin;
        public float SlotScrollProgress => Mathf.Clamp01(1 - (nextSpin - Time.unscaledTime) / Mathf.Max(.02f, slotInterval));
        private int enteredFrame, interactFrame = -1;

        private void FindInput()
        {
            if (inputRouter == null)
                foreach (var root in gameObject.scene.GetRootGameObjects())
                {
                    inputRouter = root.GetComponentInChildren<InputRouter>();
                    if (inputRouter != null) break;
                }
        }

        // Process after Input System has collected the entire frame (including simultaneous buttons).
        private void LateUpdate()
        {
            if (!IsBattleActive) return;
            if (Player == null || Opponent == null || !Player.isActiveAndEnabled || !Opponent.isActiveAndEnabled)
            { EndBattle(); return; }
            if (Phase == BattlePhase.Dying || Phase == BattlePhase.Defeated) return;
            if (Time.frameCount == enteredFrame) return;
            if (Phase == BattlePhase.StopLeft || Phase == BattlePhase.StopRight)
            {
                if (Time.unscaledTime >= nextSpin)
                {
                    if (Phase == BattlePhase.StopLeft) PlayerLeft = Next(PlayerLeft);
                    PlayerRight = Next(PlayerRight);
                    nextSpin = Time.unscaledTime + Mathf.Max(.02f, slotInterval);
                }
                if (interactFrame == Time.frameCount)
                {
                    if (Phase == BattlePhase.StopLeft) Enter(BattlePhase.StopRight);
                    else { SelectedIndex = 1; deadline = Time.unscaledTime + 3; Enter(BattlePhase.Selecting); }
                }
            }
            else if (Phase == BattlePhase.Selecting)
            {
                // Once expired, the already highlighted candidate wins over late input.
                if (RemainingTime <= 0) { Resolve(); return; }
                int horizontal = inputRouter != null ? inputRouter.Horizontal : 0;
                if (horizontal != 0) SelectedIndex = horizontal < 0 ? 0 : 1;
                if (interactFrame == Time.frameCount) Resolve();
            }
            else if (Phase == BattlePhase.Resolving && Time.unscaledTime >= deadline &&
                !IsPresenting(Player) && !IsPresenting(Opponent)) BeginRound();
        }

        private static bool IsPresenting(Combatant actor)
        {
            var presenter = actor.GetComponent<CharacterAnimationPresenter>();
            return presenter != null && presenter.IsPresenting;
        }
        private void OnInteract() => interactFrame = Time.frameCount;
        private void Enter(BattlePhase phase) { Phase = phase; enteredFrame = Time.frameCount; }
        private static Hand Next(Hand hand) => (Hand)(((int)hand + 1) % 3);
        private static Hand RandomHand() => (Hand)UnityEngine.Random.Range(0, 3);
        public static int Judge(Hand player, Hand enemy) => player == enemy ? 0 : Next(player) == enemy ? 1 : -1;

        private void BeginRound()
        {
            RoundNumber++;
            PlayerLeft = RandomHand(); PlayerRight = RandomHand();
            EnemyLeft = RandomHand(); EnemyRight = RandomHand();
            EnemySelectedIndex = -1;
            SelectedIndex = 1;
            nextSpin = Time.unscaledTime + Mathf.Max(.02f, slotInterval);
            Enter(BattlePhase.StopLeft);
        }

        private void Resolve()
        {
            Enter(BattlePhase.Resolving);
            deadline = Time.unscaledTime + Mathf.Max(0, roundDelay);
            EnemySelectedIndex = UnityEngine.Random.Range(0, 2);
            Result = Judge(SelectedIndex == 0 ? PlayerLeft : PlayerRight,
                EnemySelectedIndex == 0 ? EnemyLeft : EnemyRight);
            if (Result > 0) Player.TryAttack(Opponent, Player.NextAttackSequence);
            else if (Result < 0) Opponent.TryAttack(Player, Opponent.NextAttackSequence);
            RoundResolved?.Invoke();
        }

        private void OnDeath() => Enter(BattlePhase.Dying);
        private void OnDeathCompleted()
        {
            bool won = Player.Stats.IsAlive;
            if (won) EndBattle();
            else Enter(BattlePhase.Defeated); // Keep movement locked until the later return-to-town flow.
            BattleFinished?.Invoke(won);
        }
        public bool IsBattleActive { get; private set; }
        public Combatant Player { get; private set; }
        public Combatant Opponent { get; private set; }
        public event Action BattleStarted;

        public static BattleCoordinator FindInScene(Scene scene)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                var result = root.GetComponentInChildren<BattleCoordinator>();
                if (result != null && result.isActiveAndEnabled) return result;
            }
            return null;
        }

        public bool TryBegin(Combatant player, Combatant opponent)
        {
            FindInput();
            if (!isActiveAndEnabled || IsBattleActive || player == null || opponent == null || player == opponent ||
                inputRouter == null || !inputRouter.isActiveAndEnabled || inputRouter.RuntimeActions == null ||
                !player.isActiveAndEnabled || !opponent.isActiveAndEnabled || !player.Stats.IsAlive || !opponent.Stats.IsAlive ||
                player.gameObject.scene != gameObject.scene || opponent.gameObject.scene != gameObject.scene ||
                player.GetComponent<PlayerMovement>() == null || opponent.GetComponent<EnemyMovement>() == null) return false;
            Player = player;
            Opponent = opponent;
            IsBattleActive = true;
            SetMovementLocked(true);
            Player.Died += OnDeath;
            Opponent.Died += OnDeath;
            Player.DeathPresentationCompleted += OnDeathCompleted;
            Opponent.DeathPresentationCompleted += OnDeathCompleted;
            if (inputRouter != null) inputRouter.Interacted += OnInteract;
            RoundNumber = 0;
            BeginRound();
            BattleStarted?.Invoke();
            return true;
        }

        public void EndBattle()
        {
            if (!IsBattleActive) return;
            if (inputRouter != null) inputRouter.Interacted -= OnInteract;
            if (Player != null) { Player.Died -= OnDeath; Player.DeathPresentationCompleted -= OnDeathCompleted; }
            if (Opponent != null) { Opponent.Died -= OnDeath; Opponent.DeathPresentationCompleted -= OnDeathCompleted; }
            IsBattleActive = false;
            Enter(BattlePhase.Inactive);
            SetMovementLocked(false);
            Player = null;
            Opponent = null;
        }

        private void SetMovementLocked(bool locked)
        {
            foreach (var root in gameObject.scene.GetRootGameObjects())
            {
                foreach (var motor in root.GetComponentsInChildren<PlayerMovement>(true)) motor.SetBattleLocked(locked);
                foreach (var enemy in root.GetComponentsInChildren<EnemyMovement>(true)) enemy.SetBattleLocked(locked);
            }
        }

        private void OnDisable() => EndBattle();
    }
}
