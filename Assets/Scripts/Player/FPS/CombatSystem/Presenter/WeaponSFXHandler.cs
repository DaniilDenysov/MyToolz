using Mirror;
using MyToolz.Extensions;
using MyToolz.Player.FPS.CombatSystem.Model;
using MyToolz.ScriptableObjects.Audio;
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
            lastPlayedReload = weaponAudioSource.Play(audioClip, lastPlayedReload, 0f);
        }

        public void CancelReload()
        {
            weaponAudioSource.Stop();
        }

        public void PlayEquip(AudioClipSO audioClip, float delay = 0f)
        {
            lastPlayedEquip = equipingAudioSource.Play(audioClip, lastPlayedEquip, delay);
        }

        public void PlayHitmarkerSFX(AudioClipSO audioClipSO)
        {
            lastPlayerdHitmarker = uiAudioSource.Play(audioClipSO, lastPlayerdHitmarker, 0);
        }

        public void PlayEmptyMagSFX()
        {
            lastPlayedEmptyMag = weaponAudioSource.Play(weaponSO.EemptyMagAudioClip, lastPlayedEmptyMag, 0f);
        }

        public void PlayLocalSFX()
        {
            lastPlayedBulletCase = bulletCasingAudioSource.Play(weaponSO.ShellCasingAudioClip, lastPlayedBulletCase, 0.75f);
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
            lastPlayedWeaponShot = weaponAudioSource.Play(weaponSO.ShotAudioClip, lastPlayedWeaponShot, 0f);
        }
    }
}
