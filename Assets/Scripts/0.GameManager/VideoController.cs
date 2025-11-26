using UnityEngine;
using UnityEngine.Video;
using System.Collections.Generic; // 리스트를 쓰기 위해 필요

public class VideoController : MonoBehaviour
{
    [Header("연결 요소")]
    public VideoPlayer mainVideoPlayer;

    // 1. 영상 종류를 정의하는 ENUM
    public enum VideoType
    {
        Intro = 0,
        SkillDescription = 1,
        Ending = 2,
        //추가 가능
    }

    [Header("영상 리스트 (순서 중요!)")]
    // 2. 실제 비디오 클립들을 담을 리스트
    // 인스펙터에서 Enum 순서와 똑같이 넣어줘야 함
    public List<VideoClip> videoClips;

    // 3. 버튼에서 호출할 함수 (int로 받음)
    public void PlayVideo(int index)
    {
        // 리스트 범위를 벗어나지 않는지 안전장치
        if (index < 0 || index >= videoClips.Count)
        {
            Debug.LogError("비디오 인덱스가 리스트 범위를 벗어났습니다!");
            return;
        }

        // 입력받은 int를 Enum으로 변환 (로그 찍거나 로직 처리할 때 좋음)
        VideoType type = (VideoType)index;
        Debug.Log($"선택된 영상 타입: {type}");

        // 비디오 교체 및 재생
        mainVideoPlayer.clip = videoClips[index];
        mainVideoPlayer.Play();
    }
}