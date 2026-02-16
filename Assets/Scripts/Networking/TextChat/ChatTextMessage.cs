using Sirenix.OdinInspector;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MyToolz.Networking.TextChat
{
    public class ChatTextMessage : MonoBehaviour
    {
        [SerializeField, Required] private TMP_Text timeStampDisplay;
        [SerializeField, Required] private TMP_Text senderNicknameDisplay;
        [SerializeField, Required] private TMP_Text messageTextDisplay;
        [SerializeField, Required] private Image background;
        [SerializeField] private string timeFormat = "dddd, MMMM dd yyyy HH:mm:ss";

        public void Construct(DateTime dateTime, TextMessageDTO textMessageDTO, int order)
        {
            if (order % 2 == 0)
            {
                Color clr = background.color;
                clr.a *= 2;
                background.color = clr;
            }
            timeStampDisplay.text = dateTime.ToString(timeFormat);
            senderNicknameDisplay.text = textMessageDTO.Sender;
            messageTextDisplay.text = textMessageDTO.Message;
            if (Core.NetworkPlayer.LocalPlayerInstance == null) return;
            senderNicknameDisplay.color = Core.NetworkPlayer.LocalPlayerInstance.IsFriendly(textMessageDTO.SendersTeamGuid) ? Color.green : Color.red;
        }
    }
}
