using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(AudioSource))]
public class PlayerAudioObserver : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private FirstPersonController playerController;
    
    [Header("Audio Clips")]
    [SerializeField] private AudioClip[] footstepClips;
    [SerializeField] private AudioClip jumpClip;
    [SerializeField] private AudioClip landClip;
    [SerializeField] private AudioClip crouchClip; // Fabric swoosh sound
    
    [Header("Settings")]
    [SerializeField] private float baseFootstepPitch = 1f;
    [SerializeField] private float pitchRandomization = 0.1f;



    private AudioSource _audioSource;

    private void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
        
        // Safety Check
        if (playerController == null)
            playerController = GetComponentInParent<FirstPersonController>();
            
        if (playerController == null)
        {
            Debug.LogError("[AudioObserver] No PlayerController found!");
            enabled = false;
        }
    }

    private void OnEnable()
    {
        if (playerController == null) return;

        // SUBSCRIBE to events
        playerController.OnJumpPerformed += PlayJumpSound;
        playerController.OnLandPerformed += PlayLandSound;
        playerController.OnStepTaken += PlayFootstep;
        playerController.OnCrouchChanged += PlayCrouchSound;
    }

    private void OnDisable()
    {
        if (playerController == null) return;

        // UNSUBSCRIBE to prevent memory leaks
        playerController.OnJumpPerformed -= PlayJumpSound;
        playerController.OnLandPerformed -= PlayLandSound;
        playerController.OnStepTaken -= PlayFootstep;
        playerController.OnCrouchChanged -= PlayCrouchSound;
    }

    // --- AUDIO LOGIC ---

    private void PlayFootstep(float volumeIntensity)
    {
        if (footstepClips.Length == 0) return;

        // Pick random clip
        AudioClip clip = footstepClips[UnityEngine.Random.Range(0, footstepClips.Length)];
        
        // Randomize pitch slightly for variety
        _audioSource.pitch = baseFootstepPitch + UnityEngine.Random.Range(-pitchRandomization, pitchRandomization);
        
        _audioSource.PlayOneShot(clip, volumeIntensity);
    }

    private void PlayJumpSound()
    {
        if (jumpClip) _audioSource.PlayOneShot(jumpClip);
    }

    private void PlayLandSound()
    {
        // Reset pitch for mechanical sounds
        _audioSource.pitch = 1f;
        if (landClip) _audioSource.PlayOneShot(landClip);
    }

    private void PlayCrouchSound(bool isCrouching)
    {
        // Only play sound on crouching down, usually silent when standing up
        if (isCrouching && crouchClip) 
        {
            _audioSource.pitch = 1f;
            _audioSource.PlayOneShot(crouchClip, 0.5f);
        }
    }
}