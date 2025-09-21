Shader "Custom/ContourMapBuiltIn"
{
    // 인스펙터 창에 표시될 변수들을 정의합니다.
    Properties
    {
        // --- 지형 색상 설정 ---
        _LowColor ("Low Color", Color) = (0.1, 0.3, 0.1, 1)   // 낮은 지대의 색 (어두운 녹색)
        _MidColor ("Mid Color", Color) = (0.9, 0.8, 0.5, 1)   // 중간 지대의 색 (모래색)
        _HighColor ("High Color", Color) = (1, 1, 1, 1)      // 높은 지대의 색 (흰색)
        _MinHeight ("Min Height", Float) = 0                 // 그라데이션 시작 높이
        _MaxHeight ("Max Height", Float) = 20                // 그라데이션 끝 높이

        // --- 재질 설정 ---
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200
        Cull Off

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0

        // Properties 변수 선언
        fixed4 _LowColor;
        fixed4 _MidColor;
        fixed4 _HighColor;
        float _MinHeight;
        float _MaxHeight;
        half _Glossiness;
        half _Metallic;

        struct Input
        {
            float3 worldPos;
        };

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // 1. 현재 픽셀의 월드 높이를 0~1 사이 값으로 정규화합니다.
            float heightPercent = saturate((IN.worldPos.y - _MinHeight) / (_MaxHeight - _MinHeight));
            
            // 2. 하단 절반(낮은곳 <-> 중간)과 상단 절반(중간 <-> 높은곳)의 그라데이션을 각각 계산합니다.
            //    (heightPercent를 각 구간에 맞게 0~1로 다시 매핑하여 계산)
            fixed4 lowerGradient = lerp(_LowColor, _MidColor, heightPercent * 2.0);
            fixed4 upperGradient = lerp(_MidColor, _HighColor, (heightPercent - 0.5) * 2.0);

            // 3. 현재 높이가 전체 높이의 절반(0.5)을 넘었는지에 따라 두 그라데이션 중 하나를 선택합니다.
            //    step(0.5, heightPercent)는 heightPercent가 0.5 미만이면 0, 이상이면 1을 반환합니다.
            fixed4 finalColor = lerp(lowerGradient, upperGradient, step(0.5, heightPercent));

            o.Albedo = finalColor.rgb;
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
        }
        ENDCG
    }
    FallBack "Diffuse"
}