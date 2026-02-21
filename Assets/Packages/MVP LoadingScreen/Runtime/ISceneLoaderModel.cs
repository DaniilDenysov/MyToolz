using MyToolz.Events;
using System;

namespace MyToolz.UI.LoadingScreen
{
    public interface ISceneLoaderModel : IEventListener
    {
        event Action OnLoadingStarted;
        event Action OnLoadingFinished;
        event Action OnProgressChanged;

        float CurrentProgress { get; }
        bool IsLoading { get; }
    }
}
