using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class SoundManager : MonoBehaviour
{
    // �ν����Ϳ��� Audio Mixer�� ����
    public AudioMixer masterMixer;

    // UI �����̴��� ����
    public Slider bgmSlider;
    public Slider sfxSlider;

    // Start() �Լ��� �����̴��� �ʱⰪ�� ����
    void Start()
    {
        // �����̴��� �̺�Ʈ �����ʸ� �߰�
        bgmSlider.onValueChanged.AddListener(SetBGMVolume);
        sfxSlider.onValueChanged.AddListener(SetSFXVolume);
    }

    // �����̴��� ������ BGM ������ ����
    public void SetBGMVolume(float volume)
    {
        // �����̴� ��(0.0~1.0)�� ���ú��� ��ȯ�Ͽ� ���� ����
        masterMixer.SetFloat("BGMVolume", Mathf.Log10(volume) * 20);
    }

    // �����̴��� ������ SFX ������ ����
    public void SetSFXVolume(float volume)
    {
        masterMixer.SetFloat("SFXVolume", Mathf.Log10(volume) * 20);
    }
}