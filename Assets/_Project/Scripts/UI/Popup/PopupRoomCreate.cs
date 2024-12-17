using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ironcow;
using UnityEngine.UI;
using TMPro;

public class PopupRoomCreate : UIBase
{
    [SerializeField] private TMP_InputField roomName;
    [SerializeField] private TMP_Dropdown count;

    public override void Opened(object[] param)
    {
        var roomNameSample = new List<string>() { "너만 오면 고!", "개념있는 사람만", "어딜 넘봐?", "즐거운 게임 한판 하쉴?", "빵야! 빵야!" };
        roomName.text = roomNameSample.RandomValue();
    }

    public override void HideDirect()
    {
        UIManager.Hide<PopupRoomCreate>();
    }

    public void OnClickCreate()
    {
        if (SocketManager.instance.isConnected)
        {
            GamePacket packet = new GamePacket();
            packet.CreateRoomRequest = new C2SCreateRoomRequest() { MaxUserNum = count.value + 4, Name = roomName.text };
            SocketManager.instance.Send(packet);
        }
        else
        {
            OnRoomCreateResult(true, new RoomData() { Id = 1, MaxUserNum = count.value + 4, Name = roomName.text, OwnerId = UserInfo.myInfo.id, State = 0 });
        }
    }

    public void OnRoomCreateResult(bool isSuccess, RoomData roomData)
    {
        if(isSuccess)
        {
            UIManager.Show<UIRoom>(roomData);
            HideDirect();
        }
    }
}