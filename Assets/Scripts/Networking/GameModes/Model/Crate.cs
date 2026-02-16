using Mirror;
using MyToolz.Networking.PickUpSystem;

namespace MyToolz.Networking.GameModes.Model

{
    public class Crate : Pickable
    {
        [Command(requiresAuthority = false)]
        public void CmdForceLock()
        {
            CmdChangeState(typeof(LockedState).ToString());
        }
    }
}
