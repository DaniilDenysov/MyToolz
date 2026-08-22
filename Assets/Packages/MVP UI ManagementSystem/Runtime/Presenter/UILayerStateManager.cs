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
    }

}
