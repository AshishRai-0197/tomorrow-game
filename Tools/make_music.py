"""
Cozy background music for Tomorrow, synthesised offline (no samples, no online services).

Run with any Python that has numpy, e.g. Blender's bundled one:
  /Applications/Blender.app/Contents/Resources/5.1/python/bin/python3.13 Tools/make_music.py

Writes Assets/Game/Resources/Music/<name>.wav. Every track loops seamlessly: the reverb
is applied as a circular convolution, so the tail of the last bar rings into the first.

  title    - F major, music box over a soft pad (title screen)
  workshop - C major, warm electric piano, light shaker (Chapter 1)
  harbor   - D major 6/8, accordion and whistle, sea-shanty lilt (Chapter 2)
  market   - G major, marimba and ukulele, bright and bouncy (Chapter 3)
"""
import os, wave
import numpy as np

SR = 32000
OUT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "Assets", "Game", "Resources", "Music")


def hz(n): return 440.0 * 2 ** ((n - 69) / 12.0)


def tt(sec): return np.arange(int(sec * SR)) / SR


# ---------------------------------------------------------------- instruments
def release(sig, dur, rel):
    t = np.arange(len(sig)) / SR
    env = np.where(t < dur, 1.0, np.exp(-(t - dur) / max(rel, 1e-3) * 4))
    return sig * env


def epiano(f, dur):
    t = tt(dur + 1.0)
    idx = 1.6 * np.exp(-t * 5) + 0.25
    s = np.sin(2 * np.pi * f * t + idx * np.sin(2 * np.pi * f * t))
    s += 0.12 * np.sin(2 * np.pi * f * 4.0 * t) * np.exp(-t * 9)
    env = np.exp(-t * 1.6) * (1 - np.exp(-t * 400))
    return release(s * env, dur, 0.35)


def musicbox(f, dur):
    t = tt(2.2)
    s = np.sin(2 * np.pi * f * t) * np.exp(-t * 1.8)
    s += 0.30 * np.sin(2 * np.pi * f * 2.0 * t) * np.exp(-t * 4)
    s += 0.07 * np.sin(2 * np.pi * f * 5.4 * t) * np.exp(-t * 10)
    return s * (1 - np.exp(-t * 900))


def marimba(f, dur):
    t = tt(1.4)
    s = np.sin(2 * np.pi * f * t) * np.exp(-t * 3.2)
    s += 0.22 * np.sin(2 * np.pi * f * 3.93 * t) * np.exp(-t * 11)
    s += 0.05 * np.sin(2 * np.pi * f * 9.2 * t) * np.exp(-t * 25)
    return s * (1 - np.exp(-t * 700))


def pluck(f, dur):
    t = tt(dur + 0.6)
    s = np.zeros_like(t)
    for k in range(1, 11):
        if f * k > SR / 2.2: break
        s += (1 / k ** 1.3) * np.sin(2 * np.pi * f * k * t) * np.exp(-t * (2.0 + 1.6 * k))
    return release(s * (1 - np.exp(-t * 900)), dur, 0.2)


def pad(f, dur):
    t = tt(dur + 1.2)
    s = np.zeros_like(t)
    for det in (-0.0018, 0.0, 0.0021):
        for k in range(1, 8):
            s += (1 / k ** 1.8) * np.sin(2 * np.pi * f * (1 + det) * k * t + k * 1.3)
    env = 1 - np.exp(-t * 2.2)
    return release(s * env / 3, dur, 0.9)


def accordion(f, dur):
    t = tt(dur + 0.3)
    s = np.zeros_like(t)
    for det in (0.0, 0.0035):
        for k in range(1, 10):
            if f * k > SR / 2.5: break
            s += (1 / k ** 1.25) * np.sin(2 * np.pi * f * (1 + det) * k * t + k)
    trem = 1 + 0.08 * np.sin(2 * np.pi * 5.0 * t)
    env = (1 - np.exp(-t * 30)) * trem
    return release(s * env / 2, dur, 0.12)


def whistle(f, dur):
    t = tt(dur + 0.25)
    vib = 1 + 0.006 * np.sin(2 * np.pi * 5.2 * t) * np.clip((t - 0.15) * 4, 0, 1)
    ph = 2 * np.pi * np.cumsum(f * vib) / SR
    s = np.sin(ph) + 0.08 * np.sin(2 * ph)
    s += 0.02 * np.random.default_rng(int(f)).standard_normal(len(t))
    env = 1 - np.exp(-t * 25)
    return release(s * env, dur, 0.1)


def bass(f, dur):
    t = tt(dur + 0.3)
    s = np.sin(2 * np.pi * f * t) + 0.25 * np.sin(4 * np.pi * f * t) + 0.08 * np.sin(6 * np.pi * f * t)
    env = (1 - np.exp(-t * 200)) * (0.55 + 0.45 * np.exp(-t * 3))
    return release(s * env, dur, 0.08)


_noise = np.random.default_rng(7).standard_normal(SR)


def shaker(_f=0, _d=0):
    n = int(0.09 * SR)
    t = np.arange(n) / SR
    x = np.diff(_noise[:n + 1])  # crude high-pass
    return x * np.exp(-t * 45) * (1 - np.exp(-t * 600))


def kick(_f=0, _d=0):
    t = tt(0.35)
    f = 45 + 55 * np.exp(-t * 25)
    return np.sin(2 * np.pi * np.cumsum(f) / SR) * np.exp(-t * 11)


# ---------------------------------------------------------------- track
class Track:
    def __init__(self, bpm, bpb, bars):
        self.spb, self.bpb, self.bars = 60.0 / bpm, bpb, bars
        self.n = int(round(bars * bpb * self.spb * SR))
        self.buf = np.zeros(self.n)

    def put(self, sig, beat, gain=1.0):
        start = int(round(beat * self.spb * SR)) % self.n
        sig = sig * gain
        while len(sig):
            m = min(len(sig), self.n - start)
            self.buf[start:start + m] += sig[:m]
            sig, start = sig[m:], 0

    def note(self, inst, n, beat, beats, gain=1.0, human=0.0, rng=None):
        jitter = (rng.uniform(-human, human) if rng is not None and human else 0.0)
        self.put(inst(hz(n), beats * self.spb), beat + jitter, gain)

    def render(self, wet=0.22, tail=2.4, bright=7000):
        x = self.buf
        t = np.arange(int(tail * SR)) / SR
        ir = np.random.default_rng(3).standard_normal(len(t)) * np.exp(-t * 3.0)
        ir = np.convolve(ir, np.ones(6) / 6, mode="same")  # darker tail
        ir[: int(0.018 * SR)] = 0  # pre-delay
        ir /= np.sqrt(np.sum(ir ** 2))
        pad_ir = np.zeros(self.n); pad_ir[: min(len(ir), self.n)] = ir[: self.n]
        X = np.fft.rfft(x)
        y = x + wet * np.fft.irfft(X * np.fft.rfft(pad_ir), self.n)
        Y = np.fft.rfft(y)
        fr = np.fft.rfftfreq(self.n, 1 / SR)
        Y *= 1 / np.sqrt(1 + (fr / bright) ** 4)  # gentle low-pass
        Y[fr < 35] = 0
        y = np.fft.irfft(Y, self.n)
        y = np.tanh(1.2 * y / (np.max(np.abs(y)) + 1e-9)) / np.tanh(1.2) * 0.85
        return y


def save(name, y):
    os.makedirs(OUT, exist_ok=True)
    p = os.path.join(OUT, name + ".wav")
    with wave.open(p, "wb") as w:
        w.setnchannels(1); w.setsampwidth(2); w.setframerate(SR)
        w.writeframes((y * 32767).astype(np.int16).tobytes())
    print(f"{name}: {len(y) / SR:.1f}s -> {p}")


# ---------------------------------------------------------------- harmony
NOTE = {"C": 0, "C#": 1, "Db": 1, "D": 2, "Eb": 3, "E": 4, "F": 5, "F#": 6, "Gb": 6, "G": 7, "Ab": 8, "A": 9, "Bb": 10, "B": 11}


def chord(name, octave=4):
    q = "m" if name.endswith("m") else ""
    root = NOTE[name[:-1] if q else name] + 12 * (octave + 1)
    return [root, root + (3 if q else 4), root + 7]


def scale(key, minor=False):
    steps = [0, 2, 3, 5, 7, 8, 10] if minor else [0, 2, 4, 5, 7, 9, 11]
    r = NOTE[key]
    return sorted({(r + s) % 12 for s in steps})


def melody(prog, rhythms, sc, bpb, rng, lo, hi, start):
    """Chord tones on strong beats, stepwise scale motion elsewhere, phrase ends on the root."""
    out, p = [], start
    for bar, (ch, rh) in enumerate(zip(prog, rhythms)):
        tones = {n % 12 for n in chord(ch)}
        for i, (b, d) in enumerate(rh):
            last = bar == len(prog) - 1 and i == len(rh) - 1
            strong = b == 0 or (bpb == 4 and b == 2) or (bpb == 6 and b == 3)
            pool = [n for n in range(lo, hi + 1) if (n % 12 in tones if (strong or last) else n % 12 in sc)]
            if last:
                root = NOTE[ch.rstrip("m")]
                pool = [n for n in pool if n % 12 == root] or pool
            w = np.array([np.exp(-abs(n - p) / 2.2) * (0.35 if n == p else 1.0) for n in pool])
            p = int(rng.choice(pool, p=w / w.sum()))
            out.append((bar * bpb + b, d, p))
    return out


def play_melody(tr, inst, notes, bar0, gain, rng, human=0.012):
    for b, d, n in notes:
        tr.note(inst, n, bar0 * tr.bpb + b, d * 0.95, gain, human, rng)


# ---------------------------------------------------------------- tracks
def title():
    rng = np.random.default_rng(11)
    prog = ["F", "Dm", "Bb", "C", "F", "Am", "Bb", "C"]
    progB = ["Dm", "Bb", "F", "C", "Dm", "Bb", "C", "C"]
    full = prog + prog + progB + prog  # 32 bars
    tr = Track(76, 4, len(full))
    sc = scale("F")
    for i, ch in enumerate(full):
        c = chord(ch, 3)
        for n in c: tr.note(pad, n + 12, i * 4, 4, 0.10)
        tr.note(bass, c[0] - 12, i * 4, 3.5, 0.35)
        arp = [c[0] + 12, c[1] + 12, c[2] + 12, c[1] + 24]
        for k in range(8):
            tr.note(musicbox, arp[k % 4] + (12 if k >= 4 else 0) - 12, i * 4 + k * 0.5, 0.5, 0.10, 0.006, rng)
    rA = [[(0, 1.5), (1.5, 0.5), (2, 2)], [(0, 1), (1, 1), (2, 1.5), (3.5, 0.5)]] * 3 + [[(0, 2), (2, 2)], [(0, 4)]]
    rB = [[(0, 0.5), (0.5, 0.5), (1, 1), (2, 2)], [(0, 3), (3, 1)]] * 3 + [[(0, 1), (1, 1), (2, 2)], [(0, 4)]]
    A = melody(prog, rA, sc, 4, rng, 69, 84, 72)
    A2 = melody(prog, rA, sc, 4, rng, 69, 84, 77)
    B = melody(progB, rB, sc, 4, rng, 72, 88, 81)
    for bar0, m in ((0, A), (8, A2), (16, B), (24, A)): play_melody(tr, musicbox, m, bar0, 0.42, rng)
    save("title", tr.render(wet=0.3, bright=6500))


def workshop():
    rng = np.random.default_rng(21)
    prog = ["C", "Am", "F", "G", "C", "Em", "F", "G"]
    progB = ["Am", "F", "C", "G", "Am", "F", "G", "G"]
    full = prog + prog + progB + prog
    tr = Track(88, 4, len(full))
    sc = scale("C")
    for i, ch in enumerate(full):
        c = chord(ch, 3)
        voic = [c[1], c[2], c[0] + 12]
        for b, d in ((0, 1.4), (1.5, 0.9), (2.5, 1.4)):
            for n in voic: tr.note(epiano, n + 12, i * 4 + b, d, 0.11, 0.01, rng)
        tr.note(bass, c[0] - 12, i * 4, 1.8, 0.45)
        tr.note(bass, c[2] - 12, i * 4 + 2, 1.4, 0.38)
        tr.note(bass, c[0] - 12, i * 4 + 3.5, 0.45, 0.30)
        if i >= 8:
            tr.put(kick(), i * 4, 0.35); tr.put(kick(), i * 4 + 2.5, 0.2)
            for k in range(8): tr.put(shaker(), i * 4 + k * 0.5 + rng.uniform(-0.01, 0.01), 0.06 if k % 2 else 0.035)
    rA = [[(0, 1), (1, 0.5), (1.5, 0.5), (2, 2)], [(0, 0.5), (0.5, 0.5), (1, 1), (2, 1), (3, 1)]] * 3 + [[(0, 1), (1, 1), (2, 2)], [(0, 4)]]
    rB = [[(0, 2), (2, 1), (3, 1)], [(0, 1.5), (1.5, 0.5), (2, 2)]] * 3 + [[(0, 0.5), (0.5, 0.5), (1, 1), (2, 2)], [(0, 4)]]
    A = melody(prog, rA, sc, 4, rng, 67, 81, 72)
    A2 = melody(prog, rA, sc, 4, rng, 67, 81, 76)
    B = melody(progB, rB, sc, 4, rng, 69, 84, 76)
    for bar0, m in ((0, A), (8, A2), (16, B), (24, A)): play_melody(tr, epiano, m, bar0, 0.36, rng)
    save("workshop", tr.render(wet=0.22, bright=6000))


def harbor():
    rng = np.random.default_rng(31)
    prog = ["D", "G", "D", "A", "D", "Bm", "G", "A"]
    progB = ["Bm", "G", "D", "A", "Bm", "G", "A", "A"]
    full = prog + prog + progB + prog
    tr = Track(210, 6, len(full))  # beats are eighth notes, 6/8
    sc = scale("D")
    for i, ch in enumerate(full):
        c = chord(ch, 3)
        for b in (1, 4):
            for n in c: tr.note(accordion, n + 12, i * 6 + b, 1.6, 0.07, 0.01, rng)
        tr.note(bass, c[0] - 12, i * 6, 2.5, 0.45)
        tr.note(bass, c[2] - 24, i * 6 + 3, 2.5, 0.38)
        if i >= 8:
            for b in (0, 3): tr.put(kick(), i * 6 + b, 0.25)
            for b in range(6): tr.put(shaker(), i * 6 + b, 0.05 if b in (0, 3) else 0.03)
    rA = [[(0, 2), (2, 1), (3, 2), (5, 1)], [(0, 1), (1, 1), (2, 1), (3, 3)]] * 3 + [[(0, 3), (3, 2), (5, 1)], [(0, 6)]]
    rB = [[(0, 3), (3, 3)], [(0, 1), (1, 1), (2, 1), (3, 2), (5, 1)]] * 3 + [[(0, 2), (2, 1), (3, 3)], [(0, 6)]]
    A = melody(prog, rA, sc, 6, rng, 69, 86, 74)
    A2 = melody(prog, rA, sc, 6, rng, 69, 86, 78)
    B = melody(progB, rB, sc, 6, rng, 71, 88, 81)
    for bar0, m in ((0, A), (8, A2), (16, B), (24, A)): play_melody(tr, whistle, m, bar0, 0.30, rng, 0.02)
    save("harbor", tr.render(wet=0.26, bright=6500))


def market():
    rng = np.random.default_rng(41)
    prog = ["G", "C", "D", "G", "Em", "C", "D", "D"]
    progB = ["C", "D", "Bm", "Em", "C", "D", "G", "D"]
    full = prog + prog + progB + prog
    tr = Track(112, 4, len(full))
    sc = scale("G")
    strum = [(0, 1, 1), (1, 0.5, -1), (1.5, 0.5, 1), (2.5, 0.5, -1), (3, 0.5, 1), (3.5, 0.5, -1)]
    for i, ch in enumerate(full):
        c = chord(ch, 4)
        voic = [c[2] - 12, c[0], c[1], c[2]]
        for b, d, dirn in strum:
            notes = voic if dirn > 0 else voic[::-1]
            for k, n in enumerate(notes): tr.note(pluck, n, i * 4 + b + k * 0.03, d, 0.07 if dirn > 0 else 0.045)
        tr.note(bass, c[0] - 24, i * 4, 0.9, 0.45)
        tr.note(bass, c[2] - 24, i * 4 + 2, 0.9, 0.40)
        tr.put(kick(), i * 4, 0.3); tr.put(kick(), i * 4 + 2, 0.3)
        for k in range(8): tr.put(shaker(), i * 4 + k * 0.5, 0.06 if k % 2 else 0.035)
    rA = [[(0, 0.5), (0.5, 0.5), (1, 1), (2, 0.5), (2.5, 0.5), (3, 1)], [(0, 1.5), (1.5, 0.5), (2, 2)]] * 3 + [[(0, 0.5), (0.5, 0.5), (1, 0.5), (1.5, 0.5), (2, 2)], [(0, 4)]]
    rB = [[(0, 1), (1, 1), (2, 1), (3, 1)], [(0, 0.5), (0.5, 0.5), (1, 1), (2, 2)]] * 3 + [[(0, 1), (1, 1), (2, 2)], [(0, 4)]]
    A = melody(prog, rA, sc, 4, rng, 71, 86, 79)
    A2 = melody(prog, rA, sc, 4, rng, 71, 86, 83)
    B = melody(progB, rB, sc, 4, rng, 72, 88, 84)
    for bar0, m in ((0, A), (8, A2), (16, B), (24, A)): play_melody(tr, marimba, m, bar0, 0.42, rng, 0.006)
    save("market", tr.render(wet=0.18, bright=7500))


if __name__ == "__main__":
    title(); workshop(); harbor(); market()
