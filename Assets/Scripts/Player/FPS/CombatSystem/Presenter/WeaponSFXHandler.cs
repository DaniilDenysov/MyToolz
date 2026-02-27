using Mirror;
using MyToolz.Extensions;
using MyToolz.Player.FPS.CombatSystem.Model;
using MyToolz.Audio;
using UnityEngine;
using Zenject;

namespace MyToolz.Player.FPS.CombatSystem.Presenter
{
    public class WeaponSFXHandler : NetworkBehaviour
    {
        #region Private fields
        private WeaponSO weaponSO
        {
            get
            {
                return weaponModel.GetItemSO();
            }
        }

        private CombatSystemController combatSystemController;

        private WeaponModel weaponModel
        {
            get
            {
                return combatSystemController.WeaponModel;
            }
        }

        [SerializeField] private AudioSource weaponAudioSource;
        [SerializeField] private AudioSource bulletCasingAudioSource;
        [SerializeField] private AudioSource equipingAudioSource;
        [SerializeField] private AudioSource uiAudioSource;

        private float lastPlayedBulletCase = float.MinValue;
        private float lastPlayedReload = float.MinValue;
        private float lastPlayedWeaponShot = float.MinValue;
        private float lastPlayedEmptyMag = float.MinValue;
        private float lastPlayedEquip = float.MinValue;
        private float lastPlayerdHitmarker = float.MinValue;

        #endregion

        [Inject]
        public void Construct(CombatSystemController combatSystemController)
        {
            this.combatSystemController = combatSystemController;
        }

        public void PlayReloadSFX()
        {
            var weaponSO = weaponModel.GetItemSO();
            if (weaponSO == null) return;
            PlayReload(weaponSO.ReloadAudioClip);
        }

        public void CancelReloadSFX()
        {
            CancelReload();
        }

        public void PlayEquipSFX()
        {
            var weaponSO = weaponModel.GetItemSO();
            if (weaponSO == null) return;
            PlayEquip(weaponSO.EquipAudioClip);
        }

        public void PlayReload(AudioClipSO audioClip)
        {
            weaponAudioSource.Play(audioClip, 0f);
        }

        public void CancelReload()
        {
            weaponAudioSource.Stop();
        }

        public void PlayEquip(AudioClipSO audioClip, float delay = 0f)
        {
            equipingAudioSource.Play(audioClip, delay);
        }

        public void PlayHitmarkerSFX(AudioClipSO audioClipSO)
        {
            uiAudioSource.Play(audioClipSO, 0f);
        }

        public void PlayEmptyMagSFX()
        {
            weaponAudioSource.Play(weaponSO.EemptyMagAudioClip, 0f);
        }

        public void PlayLocalSFX()
        {
            bulletCasingAudioSource.Play(weaponSO.ShellCasingAudioClip, 0.75f);
        }

        [Command(requiresAuthority = false)]
        public void CmdPlaySFX()
        {
            RpcPlaySFX();
        }

        [ClientRpc]
        public void RpcPlaySFX()
        {
            if (weaponModel == null) return;
            var weaponSO = weaponModel.GetItemSO();
            if (weaponSO == null) return;
            weaponAudioSource.Play(weaponSO.ShotAudioClip, 0f);
        }
    }
}
