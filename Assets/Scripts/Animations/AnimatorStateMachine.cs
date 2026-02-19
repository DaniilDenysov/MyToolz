using MyToolz.DesignPatterns.StateMachine;
using MyToolz.DesignPatterns.StateMachine.PriorityBased;
using MyToolz.EditorToolz;
using MyToolz.Utilities.Debug;
using System;
using UnityEngine;

namespace MyToolz.Animations
{
    [Serializable]
    public abstract class AnimatorState : PriorityState
    {
        public bool Loop => (animationClip != null && animationClip.isLooping) || loop;
        public int AnimationHash => animationNameHash;

        [SerializeField, Required, OnValueChanged(nameof(Update))] protected AnimationClip animationClip;
        [SerializeField, Required, OnValueChanged(nameof(Update))] protected AnimationClip[] animationClips;
        [SerializeField] protected bool randomize = false;
        [ReadOnly, ShowInInspector, HideIf("@randomize")] protected int animationNameHash;
        [ReadOnly, ShowInInspector, ShowIf("@randomize")] protected int[] animationNameHashes;
        [SerializeField] protected bool loop;

        public override void Initialize()
        {
            Update();
        }

        public override void OnEnter()
        {
            if (randomize && animationNameHashes != null && animationNameHashes.Length > 0)
                animationNameHash = SelectRandomHash();
        }

        public override void OnExit() { }

        protected void HashAnimationName()
        {
            if (randomize)
            {
                int len = animationClips == null ? 0 : animationClips.Length;
                animationNameHashes = len == 0 ? Array.Empty<int>() : new int[len];
                bool allLoop = len > 0;
                for (int i = 0; i < len; i++)
                {
                    var clip = animationClips[i];
                    if (clip == null)
                    {
                        animationNameHashes[i] = 0;
                        allLoop = false;
                        continue;
                    }
                    animationNameHashes[i] = Animator.StringToHash(clip.name);
                    if (!clip.isLooping) allLoop = false;
                }
                loop = allLoop;
                animationNameHash = len > 0 ? animationNameHashes[0] : 0;
            }
            else
            {
                if (animationClip == null)
                {
                    animationNameHash = 0;
                    loop = false;
                    animationNameHashes = null;
                    return;
                }
                animationNameHash = Animator.StringToHash(animationClip.name);
                loop = animationClip.isLooping;
                animationNameHashes = null;
            }
        }

        private int SelectRandomHash()
        {
            if (animationNameHashes == null || animationNameHashes.Length == 0) return 0;
            int idx = UnityEngine.Random.Range(0, animationNameHashes.Length);
            return animationNameHashes[idx];
        }

        protected void Update()
        {
            if (randomize)
            {
                int len = animationClips == null ? 0 : animationClips.Length;
                if (len == 0)
                {
                    animationNameHashes = Array.Empty<int>();
                    animationNameHash = 0;
                    loop = false;
                    return;
                }

                animationNameHashes = new int[len];
                bool allLoop = true;
                for (int i = 0; i < len; i++)
                {
                    var clip = animationClips[i];
                    if (clip == null)
                    {
                        animationNameHashes[i] = 0;
                        allLoop = false;
                        continue;
                    }
                    animationNameHashes[i] = Animator.StringToHash(clip.name);
                    if (!clip.isLooping) allLoop = false;
                }
                loop = allLoop;
                animationNameHash = SelectRandomHash();
            }
            else
            {
                if (animationClip == null)
                {
                    animationNameHash = 0;
                    loop = false;
                    return;
                }
                animationNameHash = Animator.StringToHash(animationClip.name);
                loop = animationClip.isLooping;
            }
        }
    }

    public interface IAnimatorStateMachine<T> : IStateMachine<T> where T : IState
    {
        public float GetCurrentAninationDuration();
    }

    public abstract class AnimatorStateMachine<T> : PriorityStateMachine<T>, IAnimatorStateMachine<T> where T : AnimatorState
    {
        [SerializeField, Required] protected Animator animator;

        protected override void DoUpdate()
        {
            var next = SelectNextState();
            if (next == null) return;

            if (current == null)
            {
                ChangeState(next);
                return;
            }

            if (current.Loop)
            {
                if (!HasHigherPriority(next, current))
                {
                    if (IsCurrentFinished())
                    {
                        if (current.IsConditionFullfilled())
                        {
                            ChangeState(current);
                            return;
                        }
                        else
                        {
                            ChangeState(next);
                            return;
                        }
                    }
                }
                else
                {
                    if (current.Interuptable || IsCurrentFinished())
                    {
                        ChangeState(next);
                        return;
                    }
                }
                return;
            }

            if (!current.IsConditionFullfilled())
            {
                ChangeState(next);
                return;
            }

            if (HasHigherPriority(next, current) && current.Interuptable || IsCurrentFinished())
            {
                ChangeState(next);
                return;
            }
        }

        protected override T SelectNextState()
        {
            for (int i = 0; i < statesCount; i++)
            {
                var s = behaviourStates[i];
                if (s == null) continue;
                if (s.AnimationHash == 0) s.Initialize();
                if (s.IsConditionFullfilled()) return s;
            }
            return null;
        }

        protected override bool IsCurrentFinished()
        {
            var info = animator.GetCurrentAnimatorStateInfo(0);
            return info.normalizedTime >= 1f && (current?.IsCurrentFinished() ?? false);
        }

        public override void ChangeState(T state)
        {
            if (state == null) return;
            if (state.AnimationHash == 0) return;
            current?.OnExit();
            current = state;
            current.OnEnter();
            animator.CrossFade(state.AnimationHash, 0f);
            DebugUtility.Log(this, $"Animator switched to {state.GetType().Name}");
        }

        public float GetCurrentAninationDuration()
        {
            AnimatorStateInfo animState = animator.GetCurrentAnimatorStateInfo(0);
            float currentTime = animState.normalizedTime % 1 * animState.speedMultiplier;
            return currentTime;
        }
    }
}
