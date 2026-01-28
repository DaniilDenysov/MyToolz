using System.Collections.Generic;
using MyToolz.Utilities.Debug;

namespace MyToolz.UI
{
    public interface IUILayer : IUIState
    {
        UILayerSO Layer { get; }
    }

    public class UILayerStateManager
    {
        private readonly Dictionary<UILayerSO, HashSet<IUILayer>> layerStacks = new();
        private readonly Stack<UILayerSO> layerStackList = new();

        public UILayerSO CurrentLayer => layerStackList.Count > 0 ? layerStackList.Peek() : null;

        public void AddLayer(IUILayer layer)
        {
            var so = layer.Layer;

            if (so == null)
            {
                DebugUtility.LogError(this, "Layer is null!");
                return;
            }

            if (!layerStacks.ContainsKey(so))
                layerStacks[so] = new HashSet<IUILayer>();

            if (layerStacks[so].Add(layer))
                DebugUtility.Log(this, $"[UILayer] Added {layer} to layer {so.name}");
        }

        public void RemoveLayer(IUILayer layer)
        {
            var so = layer.Layer;
            if (so == null || !layerStacks.ContainsKey(so)) return;

            if (layerStacks[so].Remove(layer))
                DebugUtility.Log(this, $"[UILayer] Removed {layer} from layer {so.name}");
        }

        public void ChangeState(IUILayer layer)
        {
            var so = layer.Layer;
            if (so == null)
            {
                DebugUtility.LogError(this, "Layer is null!");
                return;
            }

            AddLayer(layer);

            if (so == CurrentLayer || layerStackList.Contains(so))
            {
                DebugUtility.Log(this, $"Re-entering screen in active layer {so.name}");
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

        private void EnterLayer(UILayerSO so)
        {
            if (so == null) return;

            if (!layerStacks.TryGetValue(so, out var screens)) return;

            screens.RemoveWhere((l) => l == default);

            foreach (var layer in screens)
                layer.OnEnter();

            if (CurrentLayer == so)
            {
                DebugUtility.Log(this, $"[UILayer] Skipping duplicate push for {so?.name}");
            }
            else
            {
                DebugUtility.Log(this, $"[UILayer] Pushing layer {so?.name}");
                layerStackList.Push(so);
            }
        }

        private void ExitLayer(bool pop = true)
        {
            if (CurrentLayer == null) return;

            if (!layerStacks.TryGetValue(CurrentLayer, out var screens)) return;

            screens.RemoveWhere((l) => l == default);

            foreach (var layer in screens)
                layer.OnExit();

            if (pop)
            {
                DebugUtility.Log(this, $"[UILayer] Popping layer {CurrentLayer?.name}");
                layerStackList.Pop();
            }
        }

        private void ExitAllLayers()
        {
            do
            {
                ExitLayer();
            } while (CurrentLayer);
            layerStackList.Clear();
        }
    }

}
