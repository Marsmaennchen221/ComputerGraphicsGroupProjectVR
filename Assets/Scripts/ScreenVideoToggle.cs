using UnityEngine;
using UnityEngine.Video;

public class ScreenVideoToggle : MonoBehaviour
{
    public VideoPlayer videoPlayer;
    public Renderer screenRenderer;

    public Texture imageTexture;
    public RenderTexture videoRenderTexture;

    private Material screenMat;

    private readonly string baseMap = "_BaseMap";
    private readonly string emissionMap = "_EmissionMap";

    private bool videoOn = false;

    void Start()
    {
        screenMat = screenRenderer.material;

        videoPlayer.targetTexture = videoRenderTexture;
        //ShowVideo();
        ShowImage();
    }

    public void ToggleVideo()
    {
        if (videoOn)
            ShowImage();
        else
            ShowVideo();
    }

    private void ShowVideo()
    {
        videoOn = true;

        videoPlayer.enabled = true;
        videoPlayer.targetTexture = videoRenderTexture;

        screenMat.SetTexture(baseMap, videoRenderTexture);
        screenMat.SetTexture(emissionMap, videoRenderTexture);
        screenMat.EnableKeyword("_EMISSION");

        videoPlayer.Play();
    }

    private void ShowImage()
    {
        videoOn = false;

        videoPlayer.Stop();
        videoPlayer.targetTexture = null;
        videoPlayer.enabled = false;

        ClearRenderTexture(videoRenderTexture);

        screenMat.SetTexture(baseMap, imageTexture);
        screenMat.SetTexture(emissionMap, imageTexture);
        screenMat.EnableKeyword("_EMISSION");
    }

    private void ClearRenderTexture(RenderTexture rt)
    {
        RenderTexture current = RenderTexture.active;
        RenderTexture.active = rt;
        GL.Clear(true, true, Color.black);
        RenderTexture.active = current;
    }
}