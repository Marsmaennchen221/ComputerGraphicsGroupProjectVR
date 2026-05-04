using System;
using UnityEngine;

public class LightSwitch : MonoBehaviour
{
    public bool isOn;
    public Transform lightSwitchTransform;
    public Light lightForSwitch;
    public AudioSource audioSource;
    public AudioClip audioClip;
    public float audioDelayTime;

    public void Start()
    {
        audioSource.clip = audioClip;
        audioSource.time = audioDelayTime;
    }

    public void SwitchLight()
    {
        audioSource.time = audioDelayTime;
        isOn = !isOn;
        lightForSwitch.enabled = isOn;
        if (lightSwitchTransform != null)
        {
            Vector3 currentRotation = lightSwitchTransform.eulerAngles;
            currentRotation.x = isOn ? 12f : 0f;
            lightSwitchTransform.rotation = Quaternion.Euler(currentRotation);
        }
        audioSource.Play();
    }
}