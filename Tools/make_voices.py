#!/usr/bin/env python3
"""
Generate spoken voice lines for every Say(...) and Note(...) in the chapter directors with Kokoro,
an open-source text-to-speech model (Apache 2.0 licence: the audio can be used commercially).
Runs fully offline on this Mac. Output: Assets/Game/Resources/Voice/<key>.wav, where
key = first 12 hex chars of md5("SPEAKER|line"), exactly what GameUI.VoiceKey() computes at runtime.

One-time setup (about 350 MB, kept outside the project):
    D=~/Library/Caches/tomorrow-tts
    uv venv --python 3.12 $D/venv && uv pip install --python $D/venv/bin/python kokoro-onnx soundfile numpy
    curl -L -o $D/models/kokoro-v1.0.onnx https://github.com/thewh1teagle/kokoro-onnx/releases/download/model-files-v1.0/kokoro-v1.0.onnx
    curl -L -o $D/models/voices-v1.0.bin  https://github.com/thewh1teagle/kokoro-onnx/releases/download/model-files-v1.0/voices-v1.0.bin

Run from the project folder:   ~/Library/Caches/tomorrow-tts/venv/bin/python Tools/make_voices.py         (only missing lines)
                                ... Tools/make_voices.py --all    (remakes everything)
                                ... Tools/make_voices.py --cast   (shows the cast)
Change a character's voice in VOICES below, then run with --all.
"""
import glob, hashlib, os, re, sys, wave, array

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SCRIPTS = os.path.join(ROOT, "Assets/Game/Scripts")
OUT = os.path.join(ROOT, "Assets/Game/Resources/Voice")
MODELS = os.environ.get("KOKORO_MODELS", os.path.expanduser("~/Library/Caches/tomorrow-tts/models"))

# speaker -> (Kokoro voice, speed). Voice names: a = American, b = British; f = female, m = male.
# Full list: af_heart af_bella af_nicole af_sarah af_sky am_michael am_puck am_echo am_liam
#            bf_emma bf_isabella bf_alice bf_lily bm_george bm_lewis bm_daniel bm_fable ...
VOICES = {
    "PIP":           ("am_puck",     1.05),   # young lead, bright
    "PIP (OLDER)":   ("am_michael",  0.95),   # grown-up Pip
    "TOCK":          ("bm_fable",    1.12),   # small, quick, clockwork
    "CAPTAIN OSKAR": ("bm_george",   0.92),   # deep, steady sailor
    "NELL":          ("af_sky",      1.04),   # girl
    "MIRA":          ("af_heart",    1.0),    # warm
    "GRANDMA ADA":   ("bf_emma",     0.9),    # older, gentle
    "BRAM":          ("bm_lewis",    1.0),    # jolly baker
    "NARRATOR":      ("bf_isabella", 0.97),
}

_tts = None
def tts():
    global _tts
    if _tts is None:
        from kokoro_onnx import Kokoro
        _tts = Kokoro(os.path.join(MODELS, "kokoro-v1.0.onnx"), os.path.join(MODELS, "voices-v1.0.bin"))
    return _tts

def render(voice, speed, text, dst):
    import numpy as np
    samples, sr = tts().create(text, voice=voice, speed=speed, lang="en-gb" if voice[0] == "b" else "en-us")
    pcm = (np.clip(samples, -1, 1) * 32767).astype("<i2")
    with wave.open(dst, "wb") as w:
        w.setnchannels(1); w.setsampwidth(2); w.setframerate(sr); w.writeframes(pcm.tobytes())

SKIP = {"SEAGULL"}  # gets a synthesized squawk instead
STR = r'"((?:[^"\\]|\\.)*)"'

def unescape(s):
    return s.replace('\\"', '"').replace("\\n", "\n").replace("\\\\", "\\")

def speakers():
    src = open(os.path.join(SCRIPTS, "ChapterDirector.cs"), encoding="utf-8").read()
    return dict(re.findall(r'(\w+)\s*=\s*"([^"]+)"', src.split("protected const string", 1)[1].split(";", 1)[0]))

def lines():
    consts = speakers()
    found = set()
    for path in glob.glob(os.path.join(SCRIPTS, "Chapter*Director.cs")):
        src = open(path, encoding="utf-8").read()
        for c, text in re.findall(r'\((\w+),\s*' + STR + r'\s*\)', src):         # Say(PIP, "...") and (PIP, "...") tuples
            if c in consts: found.add((consts[c], unescape(text)))
        for title, body, c in re.findall(r'Note\(\s*' + STR + r',\s*' + STR + r'(?:,\s*(\w+))?\s*\)', src, re.S):
            found.add((consts.get(c, "NARRATOR") if c else "NARRATOR", unescape(body)))
    return sorted(found)

def key(speaker, text):
    return hashlib.md5((speaker + "|" + text).encode("utf-8")).hexdigest()[:12]

def speakable(text):
    t = re.sub(r"\([^)]*\)", " ", text)            # stage directions: (quietly), (He laughs.)
    t = t.replace("…", "... ").replace("—", ", ").replace('"', "").replace("\n", " ")
    return re.sub(r"\s+", " ", t).strip()

def normalize(path, target_rms=0.09, peak=0.95):
    """Even out loudness so every character speaks at the same level."""
    with wave.open(path) as w:
        params = w.getparams(); data = array.array("h", w.readframes(w.getnframes()))
    if not data: return
    rms = (sum(v * v for v in data) / len(data)) ** 0.5 / 32767
    top = max(abs(v) for v in data) / 32767
    g = min(target_rms / max(rms, 1e-6), peak / max(top, 1e-6))
    out = array.array("h", (max(-32767, min(32767, int(v * g))) for v in data))
    with wave.open(path, "wb") as w:
        w.setparams(params); w.writeframes(out.tobytes())

def main():
    remake = "--all" in sys.argv
    print("Cast:")
    for sp, (v, s) in VOICES.items(): print(f"  {sp:14s} -> {v} x{s}")
    if "--cast" in sys.argv: return
    if "--normalize" in sys.argv:
        for f in glob.glob(os.path.join(OUT, "*.wav")): normalize(f)
        print("normalized"); return
    os.makedirs(OUT, exist_ok=True)
    todo = [(s, t) for s, t in lines() if s not in SKIP]
    keep = set(); made = 0
    for speaker, text in todo:
        k = key(speaker, text); keep.add(k)
        dst = os.path.join(OUT, k + ".wav")
        if os.path.exists(dst) and not remake: continue
        say_text = speakable(text)
        if not say_text: continue
        voice, speed = VOICES.get(speaker, VOICES["NARRATOR"])
        render(voice, speed, say_text, dst)
        normalize(dst)
        made += 1
        print(f"{speaker:14s} {k}  {say_text[:70]}")
    removed = 0
    for f in glob.glob(os.path.join(OUT, "*.wav")):
        if os.path.splitext(os.path.basename(f))[0] not in keep:
            os.remove(f); removed += 1
            if os.path.exists(f + ".meta"): os.remove(f + ".meta")
    print(f"\n{len(todo)} spoken lines · {made} generated · {removed} stale removed → {OUT}")

if __name__ == "__main__":
    main()
