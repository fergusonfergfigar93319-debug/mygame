"""Synthesise per-theme ambience beds and surface impact SFX for PenguinRun.

The six map themes previously shared one ambience clip and one set of impact
sounds, so every track sounded identical once the BGM was muted or repeated --
the whole audio difference between "ice lake" and "coral reef" was the music.
This builds a distinct, loopable bed plus a matching impact timbre per theme.

Everything here is generated from noise and oscillators, so the output carries no
third-party licence, matching the existing "Generated Project SFX" pipeline in
docs/AUDIO_CREDITS.md.

Run:  python tools/generate_theme_audio.py
"""

import os
import wave

import numpy as np
from scipy import signal as sps

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), ".."))
OUT_DIR = os.path.join(ROOT, "Assets", "Resources", "PenguinRun")

SAMPLE_RATE = 22050
AMBIENCE_SECONDS = 8.0
# Crossfading the tail back over the head is what makes the bed seamless; the
# clips loop for the whole run so any discontinuity turns into an audible tick.
LOOP_FADE_SECONDS = 0.75


def write_wav(name, samples, sample_rate=SAMPLE_RATE):
    peak = np.max(np.abs(samples))
    if peak > 0:
        samples = samples / peak
    pcm = np.clip(samples * 0.89, -1.0, 1.0)
    data = (pcm * 32767.0).astype("<i2")
    path = os.path.join(OUT_DIR, name)
    with wave.open(path, "wb") as stream:
        stream.setnchannels(1)
        stream.setsampwidth(2)
        stream.setframerate(sample_rate)
        stream.writeframes(data.tobytes())
    return path, len(data) / sample_rate


def band_noise(rng, count, low_hz, high_hz, order=3):
    noise = rng.standard_normal(count)
    nyquist = SAMPLE_RATE * 0.5
    low = max(low_hz / nyquist, 1e-4)
    high = min(high_hz / nyquist, 0.999)
    if low >= high:
        return noise
    coeffs = sps.butter(order, [low, high], btype="band", output="sos")
    return sps.sosfilt(coeffs, noise)


def lfo(count, hz, depth, phase=0.0):
    """Amplitude modulation whose period divides the loop, so it survives looping."""
    cycles = max(1, round(hz * AMBIENCE_SECONDS))
    t = np.linspace(0.0, 1.0, count, endpoint=False)
    return 1.0 - depth + depth * (0.5 + 0.5 * np.sin(2 * np.pi * cycles * t + phase))


def sparse_events(rng, count, events, builder):
    """Scatter short one-shots through the bed to give it a recognisable texture."""
    track = np.zeros(count)
    for index in range(events):
        grain = builder(rng, index)
        start = rng.integers(0, max(1, count - len(grain)))
        track[start:start + len(grain)] += grain
    return track


def decay_burst(rng, duration, low_hz, high_hz, decay):
    count = int(duration * SAMPLE_RATE)
    t = np.linspace(0.0, duration, count, endpoint=False)
    return band_noise(rng, count, low_hz, high_hz) * np.exp(-decay * t)


def chirp(duration, start_hz, end_hz, decay, sample_rate=SAMPLE_RATE):
    count = int(duration * sample_rate)
    t = np.linspace(0.0, duration, count, endpoint=False)
    sweep = np.linspace(start_hz, end_hz, count)
    phase = 2 * np.pi * np.cumsum(sweep) / sample_rate
    return np.sin(phase) * np.exp(-decay * t)


def loopify(samples):
    fade = int(LOOP_FADE_SECONDS * SAMPLE_RATE)
    if len(samples) <= fade * 2:
        return samples
    ramp = np.linspace(0.0, 1.0, fade)
    head = samples[:fade].copy()
    samples[:fade] = head * ramp + samples[-fade:] * (1.0 - ramp)
    return samples[:-fade]


# ── Ambience beds ────────────────────────────────────────────────────────────
# Each theme gets its own frequency centre and texture so they stay
# distinguishable even through a phone speaker at low volume.

def ambience_ice_lake(rng, count):
    # Wide, empty and cold: low wind plus sparse cracks echoing off the lake.
    bed = band_noise(rng, count, 60, 900) * lfo(count, 0.25, 0.45) * 0.8
    cracks = sparse_events(rng, count, 7,
        lambda r, i: decay_burst(r, 0.35, 1800, 7000, 16.0) * 0.55)
    shimmer = band_noise(rng, count, 3000, 6500) * lfo(count, 0.5, 0.8) * 0.06
    return bed + cracks + shimmer


def ambience_cedar_ruins(rng, count):
    # Sheltered woodland: mid-band wind with slow timber creaks.
    bed = band_noise(rng, count, 120, 1600) * lfo(count, 0.375, 0.35) * 0.75
    creaks = sparse_events(rng, count, 5,
        lambda r, i: chirp(0.55, 190 + i * 18, 130 + i * 12, 5.0)
        * (1.0 + 0.3 * np.sin(np.linspace(0, 24, int(0.55 * SAMPLE_RATE)))) * 0.30)
    return bed + creaks


def ambience_aurora_field(rng, count):
    # Electromagnetic: a drifting hum plus hiss that sweeps like the aurora.
    t = np.linspace(0.0, AMBIENCE_SECONDS, count, endpoint=False)
    hum = (np.sin(2 * np.pi * 110 * t) * 0.16 + np.sin(2 * np.pi * 165 * t) * 0.09) \
        * lfo(count, 0.125, 0.6)
    hiss = band_noise(rng, count, 2600, 8000) * lfo(count, 0.25, 0.85) * 0.30
    return hum + hiss + band_noise(rng, count, 80, 400) * 0.20


def ambience_mist_dike(rng, count):
    # Visibility is the mechanic here, so the bed is muffled: everything above
    # ~700 Hz is rolled off and a distant horn marks the only far-field cue.
    bed = band_noise(rng, count, 40, 700, order=5) * lfo(count, 0.125, 0.4) * 0.9
    horns = sparse_events(rng, count, 3,
        lambda r, i: chirp(1.6, 96 + i * 6, 88 + i * 6, 1.1) * 0.34)
    return bed + horns


def ambience_ocean_reef(rng, count):
    # Underwater: slow surge with rising bubble trails.
    surge = band_noise(rng, count, 50, 500, order=5) * lfo(count, 0.25, 0.7) * 0.85
    bubbles = sparse_events(rng, count, 22,
        lambda r, i: chirp(0.14, 420 + r.integers(0, 500), 1500 + r.integers(0, 900), 22.0) * 0.20)
    return surge + bubbles + band_noise(rng, count, 900, 2600) * 0.06


def ambience_sky_flight(rng, count):
    # Open air: broadband rush with a thin whistle riding on top.
    t = np.linspace(0.0, AMBIENCE_SECONDS, count, endpoint=False)
    rush = band_noise(rng, count, 200, 5200) * lfo(count, 0.5, 0.5) * 0.85
    whistle = np.sin(2 * np.pi * 1450 * t) * lfo(count, 0.25, 0.95) * 0.05
    return rush + whistle


# ── Surface impact one-shots ─────────────────────────────────────────────────
# Played on landing and on damage, so the ground itself sounds like the theme.

def impact_ice(rng):
    return decay_burst(rng, 0.30, 1400, 8000, 22.0) + chirp(0.30, 900, 260, 20.0) * 0.30


def impact_wood(rng):
    return decay_burst(rng, 0.26, 180, 1500, 26.0) + chirp(0.26, 260, 120, 22.0) * 0.55


def impact_static(rng):
    return decay_burst(rng, 0.22, 2200, 9000, 34.0) * 0.8 + chirp(0.22, 1800, 420, 26.0) * 0.35


def impact_muffled(rng):
    return decay_burst(rng, 0.34, 60, 620, 14.0) + chirp(0.34, 150, 70, 12.0) * 0.5


def impact_splash(rng):
    return decay_burst(rng, 0.30, 500, 5200, 18.0) + chirp(0.30, 700, 1600, 16.0) * 0.25


def impact_air(rng):
    return decay_burst(rng, 0.26, 700, 4200, 16.0) * 0.9


THEMES = (
    ("ice_lake", ambience_ice_lake, impact_ice),
    ("cedar_ruins", ambience_cedar_ruins, impact_wood),
    ("aurora_field", ambience_aurora_field, impact_static),
    ("mist_dike", ambience_mist_dike, impact_muffled),
    ("ocean_reef", ambience_ocean_reef, impact_splash),
    ("sky_flight", ambience_sky_flight, impact_air),
)


def main():
    os.makedirs(OUT_DIR, exist_ok=True)
    count = int((AMBIENCE_SECONDS + LOOP_FADE_SECONDS) * SAMPLE_RATE)
    written = []

    for index, (slug, ambience_fn, impact_fn) in enumerate(THEMES):
        # Fixed seed per theme so rebuilds are byte-identical and diffs stay clean.
        rng = np.random.default_rng(20260726 + index)
        bed = loopify(ambience_fn(rng, count))
        written.append(write_wav(f"ambience_theme_{slug}.wav", bed))
        written.append(write_wav(f"surface_{slug}.wav", impact_fn(rng)))

    for path, seconds in written:
        print(f"{os.path.basename(path):34s} {seconds:6.2f}s  {os.path.getsize(path) / 1024:8.1f} KB")
    print(f"\n{len(written)} files -> {OUT_DIR}")


if __name__ == "__main__":
    main()
