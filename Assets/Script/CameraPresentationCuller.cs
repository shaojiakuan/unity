using System.Collections.Generic;
using UnityEngine;

/// <summary>Suspends presentation work outside a configured region of the main camera's view.</summary>
[DefaultExecutionOrder(300)]
public sealed class CameraPresentationCuller : MonoBehaviour
{
    [Tooltip("World-space region covering the full movement or particle spread.")]
    public Bounds visibilityRegion;
    public Behaviour[] animations = new Behaviour[0];
    public ParticleSystem[] particles = new ParticleSystem[0];

    private const float CheckInterval = 0.1f;
    private const float HideDelay = 0.4f;
    private readonly Plane[] planes = new Plane[6];
    private readonly List<Behaviour> suspendedAnimations = new List<Behaviour>();
    private readonly HashSet<ParticleSystem> suspendedParticles = new HashSet<ParticleSystem>();
    private float nextCheck;
    private float outsideSince = -1f;

    private void LateUpdate()
    {
        float now = Time.unscaledTime;
        if (now < nextCheck) return;
        nextCheck = now + CheckInterval;

        Camera camera = Camera.main;
        if (camera == null || IsInView(camera))
        {
            outsideSince = -1f;
            ResumePresentation();
            return;
        }

        if (outsideSince < 0f) outsideSince = now;
        if (now - outsideSince < HideDelay) return;

        foreach (Behaviour animation in animations)
        {
            if (animation == null || !animation.isActiveAndEnabled) continue;
            animation.enabled = false;
            if (!suspendedAnimations.Contains(animation)) suspendedAnimations.Add(animation);
        }

        foreach (ParticleSystem system in particles)
        {
            if (system == null || !system.gameObject.activeInHierarchy || system.isPaused || !system.isPlaying) continue;
            system.Pause(false);
            suspendedParticles.Add(system);
        }
    }

    private bool IsInView(Camera camera)
    {
        GeometryUtility.CalculateFrustumPlanes(camera, planes);
        return GeometryUtility.TestPlanesAABB(planes, visibilityRegion);
    }

    private void OnDisable()
    {
        outsideSince = -1f;
        nextCheck = 0f;
        ResumePresentation();
    }

    private void ResumePresentation()
    {
        foreach (Behaviour animation in suspendedAnimations)
        {
            if (animation != null) animation.enabled = true;
        }
        suspendedAnimations.Clear();

        foreach (ParticleSystem system in suspendedParticles)
        {
            if (system != null && system.gameObject.activeInHierarchy && system.isPaused) system.Play(false);
        }
        suspendedParticles.Clear();
    }
}
