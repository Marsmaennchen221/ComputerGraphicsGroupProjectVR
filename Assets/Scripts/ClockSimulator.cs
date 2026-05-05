using System;
using UnityEngine;

public class ClockSimulator : MonoBehaviour
{
    [Header("Clock Hand Objects")]
    public Transform hourHand;
    public Transform minuteHand;
    public Transform secondHand;

    [Header("Time Mode")]
    public bool useRealTime = true;
    public float simulationSpeed = 1f;

    [Range(0, 23)] public int startHour = 12;
    [Range(0, 59)] public int startMinute = 0;
    [Range(0, 59)] public int startSecond = 0;

    [Header("Rotation Settings")]
    public Vector3 localRotationAxis = Vector3.forward;

    [Tooltip("Use this if the hands rotate backward.")]
    public bool clockwise = true;

    private Quaternion hourStartRotation;
    private Quaternion minuteStartRotation;
    private Quaternion secondStartRotation;

    private float simulatedSeconds;

    void Start()
    {
        if (hourHand != null) hourStartRotation = hourHand.localRotation;
        if (minuteHand != null) minuteStartRotation = minuteHand.localRotation;
        if (secondHand != null) secondStartRotation = secondHand.localRotation;

        simulatedSeconds =
            startHour * 3600f +
            startMinute * 60f +
            startSecond;
    }

    void Update()
    {
        float secondsToday;

        if (useRealTime)
        {
            DateTime now = DateTime.Now;

            secondsToday =
                now.Hour * 3600f +
                now.Minute * 60f +
                now.Second +
                now.Millisecond / 1000f;
        }
        else
        {
            simulatedSeconds += Time.deltaTime * simulationSpeed;
            secondsToday = simulatedSeconds % 86400f;
        }

        RotateHands(secondsToday);
    }

    void RotateHands(float secondsToday)
    {
        float seconds = secondsToday % 60f;
        float minutes = (secondsToday / 60f) % 60f;
        float hours = (secondsToday / 3600f) % 12f;

        float secondAngle = seconds * 6f;
        float minuteAngle = minutes * 6f;
        float hourAngle = hours * 30f;

        if (clockwise)
        {
            secondAngle *= -1f;
            minuteAngle *= -1f;
            hourAngle *= -1f;
        }

        if (secondHand != null)
            secondHand.localRotation =
                secondStartRotation * Quaternion.AngleAxis(secondAngle, localRotationAxis);

        if (minuteHand != null)
            minuteHand.localRotation =
                minuteStartRotation * Quaternion.AngleAxis(minuteAngle, localRotationAxis);

        if (hourHand != null)
            hourHand.localRotation =
                hourStartRotation * Quaternion.AngleAxis(hourAngle, localRotationAxis);
    }
}