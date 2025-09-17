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

    // Start() 함수는 슬라이더의 초기값을 설정
    void Start()
    {
        // 슬라이더의 이벤트 리스너를 추가
        bgmSlider.onValueChanged.AddListener(SetBGMVolume);
        sfxSlider.onValueChanged.AddListener(SetSFXVolume);
    }

    // 슬라이더의 값으로 BGM 볼륨을 조절
    public void SetBGMVolume(float volume)
    {
        // 슬라이더 값(0.0~1.0)을 데시벨로 변환하여 볼륨 설정
        masterMixer.SetFloat("BGMVolume", Mathf.Log10(volume) * 20);
    }

    // 슬라이더의 값으로 SFX 볼륨을 조절
    public void SetSFXVolume(float volume)
    {
        masterMixer.SetFloat("SFXVolume", Mathf.Log10(volume) * 20);
    }
}