"""
Synthesises the game's sound effects into Assets/Sounds as 16-bit mono WAV.
Everything is generated from scratch, so the project carries no third-party audio licences.

Run from the repo root:  python3 Tools/build_sounds.py
"""
import os
import wave
import numpy as np

RATE = 44100
OUT = os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__))), "Assets", "Sounds")
rng = np.random.default_rng(7)  # fixed seed -> identical files on every run


def t(seconds):
    return np.arange(int(RATE * seconds)) / RATE


def env(length, attack=0.005, decay=0.2):
    """Exponential decay envelope with a short linear attack."""
    x = t(length)
    a = np.clip(x / max(attack, 1e-6), 0, 1)
    return a * np.exp(-x / decay)


def tone(freq, length, decay=0.2, shape="sine"):
    x = t(length)
    phase = 2 * np.pi * freq * x
    if shape == "square":
        wave_ = np.sign(np.sin(phase)) * 0.6
    elif shape == "tri":
        wave_ = 2 / np.pi * np.arcsin(np.sin(phase))
    else:
        wave_ = np.sin(phase) + 0.3 * np.sin(2 * phase)
    return wave_ * env(length, decay=decay)


def place(buffer, clip, at):
    start = int(at * RATE)
    end = min(len(buffer), start + len(clip))
    buffer[start:end] += clip[: end - start]


def save(name, samples, peak=0.85):
    samples = samples / (np.max(np.abs(samples)) or 1) * peak
    os.makedirs(OUT, exist_ok=True)
    with wave.open(os.path.join(OUT, name), "wb") as f:
        f.setnchannels(1)
        f.setsampwidth(2)
        f.setframerate(RATE)
        f.writeframes((samples * 32767).astype(np.int16).tobytes())


def click(length=0.03, freq=2500):
    noise = rng.uniform(-1, 1, int(RATE * length))
    return (noise * 0.6 + tone(freq, length, 0.01)) * env(length, 0.001, 0.008)


def lever_pull():
    buf = np.zeros(int(RATE * 0.55))
    for i in range(7):                                  # ratchet teeth
        place(buf, click(0.025, 1800 + i * 120) * 0.7, 0.02 + i * 0.045)
    thunk = tone(90, 0.25, 0.08) + tone(140, 0.25, 0.05) * 0.5
    place(buf, thunk, 0.34)
    return buf


def reel_spin_loop():
    """Seamless 0.48 s loop: mechanical ticks over a low whirr."""
    length = 0.48
    x = t(length)
    whirr = 0.12 * np.sin(2 * np.pi * 75 * x) + 0.05 * rng.uniform(-1, 1, len(x))
    buf = whirr.copy()
    for i in range(8):                                  # 8 ticks -> exact loop period
        place(buf, click(0.02, 3000) * 0.5, i * length / 8)
    return buf


def reel_stop():
    buf = tone(110, 0.2, 0.05) + tone(220, 0.2, 0.03) * 0.3
    place(buf, click(0.03, 1200) * 0.8, 0)
    return buf


def button_click():
    buf = tone(900, 0.06, 0.02) * 0.5
    place(buf, click(0.04, 1500), 0)
    return buf


def coin_tick():
    return tone(1980, 0.08, 0.03, "tri") + tone(2970, 0.08, 0.02, "tri") * 0.4


def win_small():
    buf = np.zeros(int(RATE * 0.9))
    for i, f in enumerate([523.25, 659.25, 783.99, 1046.5]):   # C major arpeggio
        place(buf, tone(f, 0.45, 0.18, "tri"), i * 0.09)
    return buf


def win_big():
    buf = np.zeros(int(RATE * 2.2))
    melody = [523.25, 659.25, 783.99, 1046.5, 783.99, 1046.5, 1318.5]
    for i, f in enumerate(melody):
        place(buf, tone(f, 0.5, 0.2, "square") * 0.5 + tone(f, 0.5, 0.25, "tri"), i * 0.12)
    for f in (523.25, 659.25, 783.99, 1046.5):                   # final chord
        place(buf, tone(f, 1.2, 0.5, "tri") * 0.6, 0.9)
    for i in range(14):                                          # coin shower
        place(buf, coin_tick() * 0.4, 1.0 + i * 0.07 + rng.uniform(0, 0.03))
    return buf


def free_spins():
    buf = np.zeros(int(RATE * 1.6))
    for i in range(16):                                          # rising sparkle sweep
        f = 600 * (2 ** (i / 8))
        place(buf, tone(f, 0.3, 0.1, "tri") * 0.7, i * 0.06)
    for f in (783.99, 987.77, 1174.66):
        place(buf, tone(f, 0.7, 0.3, "tri") * 0.5, 0.95)
    return buf


def main():
    save("Lever_Pull.wav", lever_pull())
    save("Reel_Spin_Loop.wav", reel_spin_loop(), peak=0.5)
    save("Reel_Stop.wav", reel_stop())
    save("Button_Click.wav", button_click(), peak=0.6)
    save("Coin_Tick.wav", coin_tick(), peak=0.5)
    save("Win_Small.wav", win_small())
    save("Win_Big.wav", win_big())
    save("Free_Spins.wav", free_spins())
    print("Sounds written to", OUT)


if __name__ == "__main__":
    main()
