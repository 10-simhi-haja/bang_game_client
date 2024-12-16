using System;
using System.Collections;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Vivox;
using UnityEngine;

[Serializable]
public class Channel3DSetting
{

    //가청거리
    [SerializeField] private int audibleDistance = 8;

    //작아지기 시작하는 거리
    [SerializeField] private int conversationalDistance = 5;

    //FadeModel에 따른 감쇠 강도
    [SerializeField] private float audioFadeIntensityByDistance = 0.5f;

    //위치에따른 음량 감쇠 모델
    [SerializeField] private AudioFadeModel audioFadeModel = AudioFadeModel.InverseByDistance;

    public Channel3DProperties GetChannel3DProperties()
    {
        // 디버그를 위한 로그 추가
        Debug.Log($"초기 설정값 - 가청거리: {audibleDistance}, 시작거리: {conversationalDistance}, 감쇠강도: {audioFadeIntensityByDistance}");

        // 1. 가청거리(audibleDistance) 검증
        if (audibleDistance <= 0)
        {
            Debug.LogWarning("가청거리는 0보다 커야 합니다. 기본값 32로 설정합니다.");
            audibleDistance = 32;
        }

        // 2. 시작거리(conversationalDistance) 검증
        if (conversationalDistance <= 0)
        {
            Debug.LogWarning("시작거리는 0보다 커야 합니다. 기본값 1로 설정합니다.");
            conversationalDistance = 1;
        }

        // 3. 시작거리가 가청거리보다 작은지 확인
        if (conversationalDistance >= audibleDistance)
        {
            Debug.LogWarning("시작거리는 가청거리보다 작아야 합니다. 값을 조정합니다.");
            conversationalDistance = audibleDistance - 1;
        }

        // 4. 감쇠 강도 검증
        if (audioFadeIntensityByDistance <= 0.1f || audioFadeIntensityByDistance > 1.0f)
        {
            Debug.LogWarning("감쇠 강도가 올바르지 않습니다. 기본값 1.0으로 설정합니다.");
            audioFadeIntensityByDistance = 1.0f;
        }

        // 최종 설정값 로그
        Debug.Log($"최종 설정값 - 가청거리: {audibleDistance}, 시작거리: {conversationalDistance}, 감쇠강도: {audioFadeIntensityByDistance}");

        return new Channel3DProperties(audibleDistance, conversationalDistance, audioFadeIntensityByDistance, audioFadeModel);
    }


}

public class VivoxController : MonoBehaviour
{

    //채널 3D 설정
    [SerializeField] private Channel3DSetting channel3DSetting;

    //위치가 업데이트되는 주기
    [SerializeField] private float positonUpdateRate = 0.5f;

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

        await VivoxService.Instance.JoinGroupChannelAsync(channelName, ChatCapability.AudioOnly);

    }
    public async Task LeaveChannelAsync(string channelName)
    {
        await VivoxService.Instance.LeaveChannelAsync(channelName);
    }

    public async void Join3DChannel(GameObject speakeObj, string channelName)
    {

        //위치 음성 채널에 접속
        await VivoxService.Instance.JoinPositionalChannelAsync(channelName, ChatCapability.AudioOnly, channel3DSetting.GetChannel3DProperties());

        //위치를 주기적으로 업데이트
        StartCoroutine(Update3DPositionCo(speakeObj, channelName));

    }

    private IEnumerator Update3DPositionCo(GameObject speakeObj, string channelName)
    {

        while (true)
        {

            //위치를 업데이트
            VivoxService.Instance.Set3DPosition(speakeObj, channelName);
            yield return new WaitForSeconds(positonUpdateRate);

        }

    }

}

