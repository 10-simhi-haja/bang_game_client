using System;
using System.Data;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Vivox;
using UnityEngine;

public class VivoxController : MonoBehaviour
{

    public event Action OnLoginEndEvent;

    public static VivoxController Instance { get; private set; }

    private void Awake()
    {

        DontDestroyOnLoad(gameObject);
        Instance = this;

    }

    private async void Start()
    {

        await UnityServices.InitializeAsync();
        await AuthenticationService.Instance.SignInAnonymouslyAsync();
        await VivoxService.Instance.InitializeAsync();

        await LoginAsync();

        OnLoginEndEvent?.Invoke();

    }

    private async Task LoginAsync()
    {

        LoginOptions options = new LoginOptions();
        options.DisplayName = Guid.NewGuid().ToString();

        await VivoxService.Instance.LoginAsync(options);

    }

    public async void JoinVoiceChannel(string channelName)
    {

        //음성채팅 채널에 접속
        await VivoxService.Instance.JoinGroupChannelAsync(channelName, ChatCapability.AudioOnly);

    }

    public async Task LeaveChannelAsync(string channelName)
    {
        await VivoxService.Instance.LeaveChannelAsync(channelName);
    }

}