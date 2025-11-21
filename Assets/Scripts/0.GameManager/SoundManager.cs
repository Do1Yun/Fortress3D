using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class SoundManager : MonoBehaviour
{
    // 인스펙터에서 Audio Mixer를 연결
    public AudioMixer masterMixer;

    // UI 슬라이더를 연결
    public Slider bgmSlider;
    public Slider sfxSlider;
    public Slider casterSlider;

    // Start() 함수는 슬라이더의 초기값을 설정
    void Start()
    {
        // 슬라이더의 이벤트 리스너를 추가
        bgmSlider.onValueChanged.AddListener(SetBGMVolume);
        sfxSlider.onValueChanged.AddListener(SetSFXVolume);
        casterSlider.onValueChanged.AddListener(SetCasterVolume);
    }

    public void SetBGMVolume(float volume)
    {
        // 볼륨 0일 때 -80dB 처리 (Log10(0)은 -Infinity)
        float dB = (volume <= 0.001f) ? -80f : Mathf.Log10(volume) * 20;
        masterMixer.SetFloat("BGMVolume", dB);
    }

    public void SetSFXVolume(float volume)
    {
        float dB = (volume <= 0.001f) ? -80f : Mathf.Log10(volume) * 20;
        masterMixer.SetFloat("SFXVolume", dB);
    }

    public void SetCasterVolume(float volume)
    {
        // 로그 스케일 변환 적용
        float dB = (volume <= 0.001f) ? -80f : Mathf.Log10(volume) * 20;
        masterMixer.SetFloat("CasterVolume", dB);
    }
}