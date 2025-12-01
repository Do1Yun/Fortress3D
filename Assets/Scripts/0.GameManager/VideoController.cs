using UnityEngine;
using UnityEngine.Video;
using System.Collections.Generic;
using TMPro; // 1. TextMeshPro를 사용하기 위해 네임스페이스 추가 (일반 Text라면 UnityEngine.UI 사용)

public class VideoController : MonoBehaviour
{
    [Header("연결 요소")]
    public VideoPlayer mainVideoPlayer;
    public TextMeshProUGUI descriptionText; // 2. 텍스트를 변경할 UI 컴포넌트 연결

    // 영상 종류를 정의하는 ENUM
    public enum VideoType
    {
        Intro = 0,
        SkillDescription = 1,
        Ending = 2,
        //추가 가능
    }

    [Header("리소스 리스트 (순서 중요!)")]
    // 3. 비디오 클립 리스트
    public List<VideoClip> videoClips;

    // 4. 비디오에 대응하는 텍스트 리스트 (인스펙터에서 작성 가능)
    [TextArea(3, 5)] // 인스펙터에서 텍스트 입력 칸을 넓게(3~5줄) 보여줍니다.
    public List<string> videoDescriptions;

    // 버튼에서 호출할 함수 (int로 받음)
    public void PlayVideo(int index)
    {
        // 5. 안전장치: 비디오 리스트와 텍스트 리스트의 범위를 모두 확인해야 함
        if (index < 0 || index >= videoClips.Count || index >= videoDescriptions.Count)
        {
            Debug.LogError($"인덱스 오류! Index: {index}. (비디오 혹은 텍스트 리스트의 개수가 부족합니다.)");
            return;
        }

        // Enum 변환 (로직 확인용)
        VideoType type = (VideoType)index;
        Debug.Log($"선택된 영상 타입: {type}");

        // 6. 비디오 교체 및 재생
        mainVideoPlayer.clip = videoClips[index];
        mainVideoPlayer.Play();

        // 7. 텍스트 교체 로직 추가
        descriptionText.text = videoDescriptions[index];
    }
}