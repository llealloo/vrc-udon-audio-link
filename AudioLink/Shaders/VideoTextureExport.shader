Shader "AudioLink/Internal/VideoTextureExport"
{
    Properties
    {
        [HideInInspector] _MainTex ("MainTex", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "Queue"="Geometry" "RenderType"="Opaque" "AudioLinkVideoExport"="AudioLinkVideoExport" }
        Pass
        {
            Tags { "LightMode"="Vertex" }
            ColorMask 0
            ZTest Off
        }
        GrabPass
        {
            Tags { "LightMode"="Vertex" }
            "_VideoTexture"
        }
        Pass
        {
            Tags { "LightMode"="ForwardBase" }
            ColorMask 0
            ZTest Off
        }
    }
}
