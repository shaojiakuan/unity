#ifndef TANK_MIXING_FLOW_INCLUDED
#define TANK_MIXING_FLOW_INCLUDED

float _MixingStartTime;

// Both the surface and submerged traces use this signed circulation phase.
float MixingPhase(float time)
{
    float elapsed = max(0, time - _MixingStartTime);
    return elapsed * 0.85 + 0.12 * sin(elapsed * 0.21);
}

float2 MixingCenter(float phase)
{
    return float2(sin(phase * 0.23), cos(phase * 0.19)) * 0.022;
}

float3 MixingPosition(float4 seed, float phase)
{
    float offset = seed.z * 6.2831853;
    float angle = phase + offset + 0.10 * sin(phase * 0.37 + offset);
    float radius = lerp(0.16, 0.34, seed.w)
        + 0.027 * sin(phase * 0.43 + offset)
        + 0.012 * sin(phase * 0.17 + seed.w * 8);
    float height = (0.27 + 0.045 * sin(phase * 0.23 + offset))
        * sin(angle + seed.w * 6.2831853)
        + 0.025 * sin(phase * 0.61 + offset);
    float2 center = MixingCenter(phase);
    return float3(center.x + cos(angle) * radius, height,
        center.y + sin(angle) * radius * (0.92 + 0.04 * sin(phase * 0.29 + offset)));
}

void SampleMixingSurface(float2 position, float phase, out float wave, out float2 slope)
{
    float2 p = position - MixingCenter(phase);
    float radius = max(length(p), 0.035);
    float angle = atan2(p.y, p.x);
    float spiral = radius * 22 + (angle - phase) * 3;
    float secondary = radius * 31 + (angle - phase) * 2 + sin(phase * 0.31) * 0.3;
    float s1, c1, s2, c2;
    sincos(spiral, s1, c1);
    sincos(secondary, s2, c2);
    float2 radial = p / radius;
    float2 angular = float2(-p.y, p.x) / (radius * radius);
    float centerFade = smoothstep(0.035, 0.13, radius);
    wave = (s1 * 0.68 + s2 * 0.32) * centerFade;
    slope = (c1 * (radial * 22 + angular * 3) * 0.68
        + c2 * (radial * 31 + angular * 2) * 0.32) * centerFade * 0.08;
}
#endif
