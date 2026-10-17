using System.Collections.Generic;
using MyToolz.Utilities.Debug;

namespace MyToolz.UI.Management
{
    public interface IUILayer : IUIState
    {
        UILayerSO Layer { get; }
    }

    public class UILayerStateManager
    {
        private readonly Dictionary<UILayerSO, HashSet<IUILayer>> layerStacks = new();
        private readonly Stack<UILayerSO> layerStackList = new();

        // Screens a cover opened over. They stay up behind it until their scene unloads (see ChangeState).
        private readonly List<IUILayer> hiddenByCover = new();

        public UILayerSO CurrentLayer => layerStackList.Count > 0 ? layerStackList.Peek() : null;

        public void AddLayer(IUILayer layer)
        {
            UILayerSO so = layer.Layer;

            if (so == null)
            {
                DebugUtility.LogError(this, "Layer is null!");
                return;
            }

            if (!layerStacks.ContainsKey(so))
            {
                layerStacks[so] = new HashSet<IUILayer>();
            }

            if (layerStacks[so].Add(layer))
            {
                DebugUtility.Log(this, $"[UILayer] Added {layer} to layer {so.name}");
            }
        }

        public void RemoveLayer(IUILayer layer)
        {
            hiddenByCover.Remove(layer);

            UILayerSO so = layer.Layer;
            if (so == null || !layerStacks.ContainsKey(so))
            {
                return;
            }

            if (layerStacks[so].Remove(layer))
            {
                DebugUtility.Log(this, $"[UILayer] Removed {layer} from layer {so.name}");
            }
        }

        public void ChangeState(IUILayer layer)
        {
            UILayerSO so = layer.Layer;
            if (so == null)
            {
                DebugUtility.LogError(this, "Layer is null!");
                return;
            }

            AddLayer(layer);

            if (so.ActivationMode == ActivationMode.Cover)
            {
                // A cover hides everything, so the stack underneath starts over. It is never pushed, so
                // the screens the next scene opens beneath it cannot exit it. The screens it opens over stay
                // drawn behind it while it fades in (exiting them now would show the empty frame through
                // it); their scene unloading removes them.
                PopAllLayersUnderCover();
                if (!layer.IsActive)
                    layer.OnEnter();
                return;
            }

            // A screen opening under a cover starts the new stack, so any screen the cover hid can go.
            hiddenByCover.Remove(layer);
            ExitHiddenByCover();

            if (so == CurrentLayer || layerStackList.Contains(so))
            {
                DebugUtility.Log(this, $"[UILayer] Re-entering {layer} in active layer {so.name}");
                if (!layer.IsActive)
                    layer.OnEnter();
                if (layerStacks.TryGetValue(so, out var screens))
                {
                    screens.Add(layer);
                }
                return;
            }

            switch (so.ActivationMode)
            {
                case ActivationMode.Override:
                    ExitAllLayers();
                    EnterLayer(so);
                    break;

                case ActivationMode.Additive:
                    ExitLayer(false);
                    EnterLayer(so);
                    break;

                case ActivationMode.Blend:
                    EnterLayer(so);
                    break;
            }
        }

        public void ExitState()
        {
            ExitLayer();
            EnterLayer(CurrentLayer);
        }

        /// <summary>
        /// Closes <paramref name="layer"/>. A cover exits on its own and leaves the stack as it is; any
        /// other layer closes the top of the stack, as <see cref="ExitState()"/> does.
        /// </summary>
        public void ExitState(IUILayer layer)
        {
            UILayerSO so = layer?.Layer;
            if (so != null && so.ActivationMode == ActivationMode.Cover)
            {
                ExitHiddenByCover();
                if (layer.IsActive)
                    layer.OnExit();
                return;
            }

            ExitState();
        }

        // Exits the screens a cover opened over that are still up, e.g. ones in a scene that was not unloaded.
        private void ExitHiddenByCover()
        {
            // Copied first: exiting a screen can open or close others.
            IUILayer[] hidden = hiddenByCover.ToArray();
            hiddenByCover.Clear();

            foreach (IUILayer layer in hidden)
            {
                if (layer.IsActive)
                    layer.OnExit();
            }
        }

        private void EnterLayer(UILayerSO so)
        {
            if (so == null)
            {
                return;
            }

            if (!layerStacks.TryGetValue(so, out HashSet<IUILayer> screens))
            {
                return;
            }

            if (CurrentLayer == so)
            {
                DebugUtility.Log(this, $"[UILayer] Skipping duplicate push for {so?.name}");
            }
            else
            {
                DebugUtility.Log(this, $"[UILayer] Pushing layer {so?.name}");
                layerStackList.Push(so);
            }

            screens.RemoveWhere((l) => l == default);

            foreach (IUILayer layer in screens)
            {
                if (!layer.IsActive)
                    layer.OnEnter();
            }
        }

        private void ExitLayer(bool pop = true)
        {
            if (CurrentLayer == null)
            {
                return;
            }

            if (!layerStacks.TryGetValue(CurrentLayer, out HashSet<IUILayer> screens))
            {
                return;
            }

            screens.RemoveWhere((l) => l == default);

            foreach (IUILayer layer in screens)
            {
                if (layer.IsActive)
                    layer.OnExit();
            }

            if (pop)
            {
                DebugUtility.Log(this, $"[UILayer] Popping layer {CurrentLayer?.name}");
                layerStackList.Pop();
            }
        }

        private void ExitAllLayers()
        {
            while (layerStackList.Count > 0)
            {
                UILayerSO so = layerStackList.Pop();
                DebugUtility.Log(this, $"[UILayer] Popping layer {so?.name}");

                if (!layerStacks.TryGetValue(so, out HashSet<IUILayer> screens))
                {
                    continue;
                }

                screens.RemoveWhere((l) => l == default);

                foreach (IUILayer layer in screens)
                {
                    if (layer.IsActive)
                        layer.OnExit();
                }
            }
        }

        // Pops the whole stack like ExitAllLayers, but leaves its screens up for ExitHiddenByCover.
        private void PopAllLayersUnderCover()
        {
            while (layerStackList.Count > 0)
            {
                UILayerSO so = layerStackList.Pop();
                DebugUtility.Log(this, $"[UILayer] Popping layer {so?.name} under a cover");

                if (!layerStacks.TryGetValue(so, out HashSet<IUILayer> screens))
                {
                    continue;
                }

                screens.RemoveWhere((l) => l == default);

                foreach (IUILayer layer in screens)
                {
                    if (layer.IsActive && !hiddenByCover.Contains(layer))
                        hiddenByCover.Add(layer);
                }
            }
        }
    }

}
