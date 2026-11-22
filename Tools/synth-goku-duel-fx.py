"""Render original, deterministic mono 16-bit effects for the Goku duel."""
from pathlib import Path
from random import Random
from math import exp, sin, pi, tanh
import sys
from array import array
import wave

RATE = 22050
CUES = {
    "punch": (.23, 106, 26, .42, .013),
    "heavy": (.38, 77, 17, .54, .022),
    "sweep": (.38, 124, 14, .54, .115),
    "charge": (1.50, 100, 1.1, .19, .80),
    "beam": (1.17, 72, 1.15, .42, .035),
    "beam-tail": (.43, 94, 7.8, .25, .03),
    "land": (.34, 56, 18, .35, .024),
    "cloth": (.33, 135, 13, .49, .080),
    "dodge": (.24, 180, 15, .47, .065),
}
REFERENCE_CUES = {
    "punch": (.16, 134, 32, .55, .010),
    "heavy": (.27, 98, 24, .54, .016),
    "sweep": (.34, 162, 15, .67, .070),
    "uppercut": (.30, 139, 17, .63, .025),
    "guard": (.18, 201, 23, .62, .017),
    "charge": (.44, 150, 1.8, .18, .28),
    "beam": (.82, 94, 1.2, .54, .026),
    "beam-tail": (.38, 116, 8.2, .29, .03),
    "land": (.31, 63, 20, .38, .022),
    "cloth": (.28, 160, 14, .54, .06),
    "dodge": (.22, 215, 18, .47, .046),
}

def envelope(t, duration, attack, decay):
    return min(1, t/attack, (duration-t)/.045) * exp(-decay*max(0,t-attack))
def write_cue(folder, name, settings):
    duration, frequency, decay, noise_level, attack = settings
    rng = Random(name)
    track = array("h")
    slow_noise = phase = 0
    for i in range(round(duration * RATE)):
        t = i / RATE
        white = rng.uniform(-1, 1)
        slow_noise += .17 * (white - slow_noise)
        if name == "uppercut":
            hz = frequency + 115 * min(1, t / .12)
        elif name == "charge":
            hz = frequency + 210 * (t / duration) ** 2
        elif name == "beam":
            hz = frequency * (1 + .14*sin(t*26))
        else:
            hz = frequency * (.64 + .68*exp(-t*10))
        phase += 2 * pi * hz / RATE
        tone = sin(phase) + .30*sin(2*phase+.5) + .11*sin(3*phase)
        texture = slow_noise * 1.7 + .55*(white - slow_noise)
        transient = .22*white*exp(-t*68) if name in ("punch", "heavy", "land", "beam") else 0
        loud = .75 if name in ("beam", "heavy") else .59
        sample = loud*tanh(envelope(t, duration, attack, decay) *
                           (.65*tone + noise_level*texture + transient))
        track.append(round(32767*sample))
    with wave.open(str(folder/(name+".wav")), "wb") as wav:
        wav.setparams((1, 2, RATE, 0, "NONE", "not compressed"))
        wav.writeframes(track.tobytes())

def shape_voice(file, max_duration):
    with wave.open(str(file), "rb") as wav:
        rate=wav.getframerate()
        assert wav.getnchannels()==1 and wav.getsampwidth()==2
        samples=array("h", wav.readframes(wav.getnframes()))
    end_to_end=max(abs(s) for s in samples)
    assert end_to_end>700, file
    audible=[i for i,value in enumerate(samples) if abs(value)>end_to_end*.019]
    first=max(0,audible[0]-round(rate*.017))
    last=min(len(samples),audible[-1]+round(rate*.055))
    source=samples[first:last]
    length=min(len(source),round(rate*max_duration))
    gain=min(2.2,22000/end_to_end)
    result=array("h")
    for i in range(length):
        t=i*(len(source)-1)/max(1,length-1)
        index=int(t)
        next_value=source[min(index+1,len(source)-1)]
        signal=source[index]+(next_value-source[index])*(t-index)
        fade=min(1,i/(rate*.012),(length-1-i)/(rate*.028))
        result.append(round(signal*fade*gain))
    with wave.open(str(file),"wb") as wav:
        wav.setparams((1,2,rate,0,"NONE","not compressed"))
        wav.writeframes(result.tobytes())

if __name__ == "__main__":
    folder=Path(sys.argv[1])
    folder.mkdir(parents=True, exist_ok=True)
    reference = "--reference-timing" in sys.argv[2:]
    for name, settings in (REFERENCE_CUES if reference else CUES).items():
        write_cue(folder, name, settings)
    lengths={"gather":1.40,"fire":.63,"entry":.78,"punch":.46,"kick":.42,"opening":.89}
    if reference:
        lengths.update({"gather":.43,"fire":.59,"punch":.24,"kick":.22})
    for suffix, duration in lengths.items():
        file=folder/("voice-"+suffix+".wav")
        if file.exists():
            shape_voice(file,duration)
