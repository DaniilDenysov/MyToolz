using Mirror;
using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Collections;
using UnityEngine.UI;
using TMPro;
using MyToolz.UI.Management;
using MyToolz.Utilities.Debug;
using MyToolz.Tweener.UI;
using MyToolz.EditorToolz;
using MyToolz.Events;
using MyToolz.InputManagement.Commands;

namespace MyToolz.Networking.TextChat
{
    public static class UILayoutUtil
    {
        public static IEnumerator RebuildNextFrame(RectTransform container, RectTransform justAdded = null)
        {
            Canvas.ForceUpdateCanvases();

            if (justAdded != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(justAdded);

            LayoutRebuilder.ForceRebuildLayoutImmediate(container);

            yield return new WaitForEndOfFrame();

            Canvas.ForceUpdateCanvases();

            if (justAdded != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(justAdded);

            LayoutRebuilder.ForceRebuildLayoutImmediate(container);
        }
    }

    [System.Serializable]
    public struct TextMessageDTO
    {
        public bool PublicAccess;
        public string SendersTeamGuid;
        public string Sender;
        public string Message;
    }

    public class TextChatManager : NetworkBehaviour, IEventListener
    {
        [SerializeField] private SyncDictionary<DateTime, TextMessageDTO> messageDTOs = new SyncDictionary<DateTime, TextMessageDTO>();
        [SerializeField, Range(0, 100)] private int historyLimit = 50;
        [SerializeField, Required] private ChatTextMessage chatTextMessagePrefab;
        [SerializeField, Required] private UISubScreen sessionChatBody;
        [SerializeField, Required] private UISubScreen teamChatBody;
        [SerializeField, Required] private UIScreen screen;

        [SerializeField, Required] private Transform sessionMessageContainer, teamMessageContainer;
        [Header("Chat box")]
        [SerializeField, Required] private UITweener messageFlag;
        [SerializeField, Required] private TMP_Text binding;
        [SerializeField, Required] private string binndingAlias;
        [SerializeField, Required] private InputCommandSO openChantInputCommandSO;
        [SerializeField, Required] private InputCommandSO closeChantInputCommandSO;
        [SerializeField, Required] private InputCommandSO switchChantInputCommandSO;
        private bool isDirty
        {
            set => messageFlag.SetActive(value);
            get => messageFlag.gameObject.activeInHierarchy;
        }

        private void OnEnable()
        {
            RegisterEvents();
        }

        private void OnDisable()
        {
            UnregisterEvents();
        }


        private void Start()
        {
            DisplayMessageHistory();
        }


        public void OnDestroy()
        {
            UnregisterEvents();
        }

        private void RefreshRebind()//InputModelData data)
        {
            //binding.text = $"{binndingAlias}{data.HotKeyName}";
        }

        private bool IsPublicChat() => sessionChatBody.IsActive || sessionChatBody.gameObject.activeInHierarchy;

        private void OnSwitchChat()
        {
            if (!sessionChatBody.IsActive || !sessionChatBody.gameObject.activeInHierarchy) sessionChatBody.Open();
            else teamChatBody.Open();
        }

        private void OpenTextChat()
        {
            isDirty = false;
            screen.Open();
        }

        public void CloseTextChat()
        {
            DebugUtility.Log(this, "Closing text chat!");
            screen.Close();
        }

        private void DisplayMessageHistory()
        {
            List<DateTime> timeCodes = GetSortedHistoryTimestamps();
            foreach (var timeCode in timeCodes)
            {
                DisplayMessage(timeCode);
            }
        }

        private void OnNewMessageDto(DateTime obj)
        {
            DisplayMessage(obj);
        }

        private void DisplayMessage(DateTime obj)
        {
            if (messageDTOs.TryGetValue(obj, out TextMessageDTO textMessageDTO))
            {
                if (!textMessageDTO.PublicAccess && Core.NetworkPlayer.LocalPlayerInstance == null) return;
                if (!textMessageDTO.PublicAccess && !Core.NetworkPlayer.LocalPlayerInstance.IsFriendly(textMessageDTO.SendersTeamGuid)) return;
                if (!screen.IsActive)
                {
                    isDirty = true;
                }
                Transform container = textMessageDTO.PublicAccess ? sessionMessageContainer : teamMessageContainer;
                var messageInstance = Instantiate(chatTextMessagePrefab, container);
                messageInstance.Construct(obj, textMessageDTO, container.childCount);
                var containerRt = (RectTransform)container;
                var msgRt = messageInstance.GetComponent<RectTransform>();

                if (containerRt == null || msgRt == null) return;

                StartCoroutine(UILayoutUtil.RebuildNextFrame(containerRt, msgRt));
            }
        }

        public void SendMessageToServer(string message)
        {
            if (string.IsNullOrEmpty(message) || string.IsNullOrWhiteSpace(message)) return;
            CmdSendMessageToServer(message, IsPublicChat());
        }

        [Command(requiresAuthority = false)]
        private void CmdSendMessageToServer(string message, bool isPublic, NetworkConnectionToClient conn = null)
        {
            if (conn.identity.TryGetComponent(out Core.NetworkPlayer networkPlayer))
            {
                messageDTOs.Add(DateTime.Now, CreateMessage(networkPlayer.TeamGuid, networkPlayer.Nickname, message, isPublic));
                ValidateHistoryLimit();
            }
        }

        private List<DateTime> GetSortedHistoryTimestamps() => messageDTOs.Keys.OrderBy((code) => code.TimeOfDay).ToList();

        private void ValidateHistoryLimit()
        {
            if (messageDTOs.Keys.Count > 50)
            {
                List<DateTime> timeCodes = GetSortedHistoryTimestamps();
                messageDTOs.Remove(timeCodes.Last());
            }
        }

        public TextMessageDTO CreateMessage(string teamGuid, string sender, string message, bool isPublic)
        {
            return new TextMessageDTO()
            {
                PublicAccess = isPublic,
                SendersTeamGuid = teamGuid,
                Message = message,
                Sender = sender
            };
        }

        public void RegisterEvents()
        {
            messageDTOs.OnAdd += OnNewMessageDto;
            //bindingReference.OnRebinded += RefreshRebind;
            RefreshRebind();//bindingReference.Model);
            openChantInputCommandSO.OnPerformed += OpenTextChat;
            switchChantInputCommandSO.OnStarted += OnSwitchChat;
            closeChantInputCommandSO.OnPerformed += CloseTextChat;
        }

        public void UnregisterEvents()
        {
            messageDTOs.OnAdd -= OnNewMessageDto;
            //bindingReference.OnRebinded -= RefreshRebind;
            openChantInputCommandSO.OnPerformed -= OpenTextChat;
            switchChantInputCommandSO.OnStarted -= OnSwitchChat;
            closeChantInputCommandSO.OnPerformed -= CloseTextChat;
        }
    }
}
    