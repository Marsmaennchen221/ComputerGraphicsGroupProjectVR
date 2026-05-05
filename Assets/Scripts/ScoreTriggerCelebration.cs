using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HoopScoreAndCelebration : MonoBehaviour
{
    [Header("Score")]
    public int score = 0;
    public int scoreIncrement = 1;
    public string ballTag = "Ball";
    public float scoreCooldown = 0.75f;

    [Header("UI Text")]
    public TMP_Text scoreText;

    [Header("Canvas Lights")]
    public RectTransform canvasRect;
    public int lightsPerEdge = 8;
    public float bulbSize = 22f;
    public float edgePadding = 18f;
    public float celebrationDuration = 1.5f;
    public float flashInterval = 0.12f;

    private Image[] lights;
    private bool canScore = true;
    private Coroutine celebrationRoutine;

    private readonly Color[] colors =
    {
        Color.red,
        Color.yellow,
        Color.green,
        Color.cyan,
        Color.magenta,
        Color.white
    };

    private void Start()
    {
        UpdateScoreText();
        GenerateLights();
        SetLightsVisible(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!canScore) return;
        if (!other.CompareTag(ballTag)) return;

        score += scoreIncrement;
        UpdateScoreText();

        if (celebrationRoutine != null)
            StopCoroutine(celebrationRoutine);

        celebrationRoutine = StartCoroutine(Celebrate());

        StartCoroutine(ScoreCooldown());
    }

    private IEnumerator ScoreCooldown()
    {
        canScore = false;
        yield return new WaitForSeconds(scoreCooldown);
        canScore = true;
    }

    private void UpdateScoreText()
    {
        if (scoreText != null)
            scoreText.text = "Score: " + score;
    }

    private void GenerateLights()
    {
        if (canvasRect == null) return;

        int totalLights = lightsPerEdge * 4;
        lights = new Image[totalLights];

        float width = canvasRect.rect.width;
        float height = canvasRect.rect.height;

        int index = 0;

        // Top edge
        for (int i = 0; i < lightsPerEdge; i++)
        {
            float x = Mathf.Lerp(-width / 2f + edgePadding, width / 2f - edgePadding, i / (float)(lightsPerEdge - 1));
            float y = height / 2f - edgePadding;
            lights[index++] = CreateLightBulb(new Vector2(x, y));
        }

        // Bottom edge
        for (int i = 0; i < lightsPerEdge; i++)
        {
            float x = Mathf.Lerp(-width / 2f + edgePadding, width / 2f - edgePadding, i / (float)(lightsPerEdge - 1));
            float y = -height / 2f + edgePadding;
            lights[index++] = CreateLightBulb(new Vector2(x, y));
        }

        // Left edge
        for (int i = 0; i < lightsPerEdge; i++)
        {
            float x = -width / 2f + edgePadding;
            float y = Mathf.Lerp(-height / 2f + edgePadding, height / 2f - edgePadding, i / (float)(lightsPerEdge - 1));
            lights[index++] = CreateLightBulb(new Vector2(x, y));
        }

        // Right edge
        for (int i = 0; i < lightsPerEdge; i++)
        {
            float x = width / 2f - edgePadding;
            float y = Mathf.Lerp(-height / 2f + edgePadding, height / 2f - edgePadding, i / (float)(lightsPerEdge - 1));
            lights[index++] = CreateLightBulb(new Vector2(x, y));
        }
    }

    private Image CreateLightBulb(Vector2 anchoredPosition)
    {
        GameObject bulb = new GameObject("Celebration Light");
        bulb.transform.SetParent(canvasRect, false);

        RectTransform rect = bulb.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(bulbSize, bulbSize);
        rect.anchoredPosition = anchoredPosition;

        Image image = bulb.AddComponent<Image>();
        image.sprite = CreateCircleSprite();
        image.color = Color.white;

        return image;
    }

    private Sprite CreateCircleSprite()
    {
        int size = 64;
        Texture2D texture = new Texture2D(size, size);
        texture.filterMode = FilterMode.Bilinear;

        Vector2 center = new Vector2(size / 2f, size / 2f);
        float radius = size / 2f - 2f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);

                if (distance <= radius)
                {
                    float alpha = 1f - Mathf.Clamp01(distance / radius);
                    alpha = Mathf.Lerp(0.25f, 1f, alpha);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
                else
                {
                    texture.SetPixel(x, y, Color.clear);
                }
            }
        }

        texture.Apply();

        return Sprite.Create(
            texture,
            new Rect(0, 0, size, size),
            new Vector2(0.5f, 0.5f)
        );
    }

    private IEnumerator Celebrate()
    {
        float timer = 0f;
        SetLightsVisible(true);

        while (timer < celebrationDuration)
        {
            for (int i = 0; i < lights.Length; i++)
            {
                if (lights[i] != null)
                {
                    lights[i].color = colors[Random.Range(0, colors.Length)];
                    lights[i].transform.localScale = Vector3.one * Random.Range(0.8f, 1.35f);
                }
            }

            timer += flashInterval;
            yield return new WaitForSeconds(flashInterval);
        }

        SetLightsVisible(false);
    }

    private void SetLightsVisible(bool visible)
    {
        if (lights == null) return;

        foreach (Image light in lights)
        {
            if (light != null)
                light.gameObject.SetActive(visible);
        }
    }
}