"""
Genera la música de Spirit Dragon: Abyss Hunter (4 pistas en bucle, sintetizadas sin librerías externas).

Uso:  python Tools/GenerarMusica.py [carpeta_salida]
Salida por defecto: Assets/Resources/Audio/Musica/

  musica_nivel1.wav  Bosque        D mayor   108 bpm  aventura cálida
  musica_nivel2.wav  Rey Cerdo     E menor   128 bpm  marcha pesada y burlona
  musica_nivel3.wav  Montaña       A menor    84 bpm  frío, amplio, épico
  musica_boss.wav    Señor Abismo  D menor   148 bpm  ataque constante

Las melodías están escritas en rejilla de corcheas: 8 símbolos por compás.
  nota ("F#5") = empieza nota, "_" = la sostiene, "-" = silencio.
"""
import math
import os
import random
import struct
import sys
import wave

SR = 22050
TS = 2048
random.seed(7)

# ---------------------------------------------------------------- tablas de onda
def _tabla(f):
    t = [f(i / TS) for i in range(TS)]
    m = max(abs(x) for x in t) or 1.0
    return [x / m for x in t]

def _suma(hs):
    return _tabla(lambda p: sum(a * math.sin(2 * math.pi * h * p) for h, a in hs))

SINE = _suma([(1, 1.0)])
TRI = _suma([(h, ((-1) ** ((h - 1) // 2)) / (h * h)) for h in range(1, 16, 2)])
SAW = _suma([(h, 1.0 / h) for h in range(1, 15)])
BRASS = _suma([(h, 1.0 / (h ** 0.75)) for h in range(1, 10)])
SOFT = _suma([(1, 1.0), (2, 0.35), (3, 0.15)])

def pulso(d):
    return _tabla(lambda p: sum((2 / (math.pi * h)) * math.sin(math.pi * h * d) *
                                 math.cos(2 * math.pi * h * (p - d / 2)) for h in range(1, 14)))

P25 = pulso(0.25)
P125 = pulso(0.125)
P50 = pulso(0.5)

# ---------------------------------------------------------------- notas
SEMI = {"C": 0, "D": 2, "E": 4, "F": 5, "G": 7, "A": 9, "B": 11}

def midi(n):
    base = SEMI[n[0]]
    i = 1
    while i < len(n) and n[i] in "#b":
        base += 1 if n[i] == "#" else -1
        i += 1
    return 12 * (int(n[i:]) + 1) + base

def hz(m):
    return 440.0 * 2 ** ((m - 69) / 12.0)

class Pista:
    def __init__(self, bpm, compases):
        self.bpm = bpm
        self.compases = compases
        self.paso = 60.0 / bpm / 2.0            # una corchea en segundos
        self.n = int(round(compases * 8 * self.paso * SR))
        self.seco = [0.0] * self.n
        self.humedo = [0.0] * self.n

    def t(self, compas, paso8=0.0):
        return (compas * 8 + paso8) * self.paso

    # ---- una voz con envolvente simple
    def voz(self, buf, inicio, dur, freq, tabla, vol, a=0.005, decaimiento=0.25, sost=0.6, r=0.08, vib=0.0):
        buf = self.seco if buf == "s" else self.humedo
        n = self.n
        na = max(1, int(a * SR))
        nd = int(dur * SR)
        nr = max(1, int(r * SR))
        tau = max(1e-3, decaimiento * SR)
        inc = freq * TS / SR
        fase = 0.0
        pos = int(inicio * SR)
        for i in range(nd + nr):
            g = i / na if i < na else sost + (1.0 - sost) * math.exp(-(i - na) / tau)
            if i >= nd:
                g *= 1.0 - (i - nd) / nr
            f = inc * (1.0 + vib * math.sin(i * 0.0006)) if vib else inc
            fase += f
            buf[(pos + i) % n] += tabla[int(fase) & (TS - 1)] * g * vol

    def melodia(self, buf, compas0, filas, tabla, vol, **kw):
        """filas: una lista de 8 símbolos por compás."""
        for c, fila in enumerate(filas):
            i = 0
            while i < 8:
                s = fila[i]
                if s in ("-", "_"):
                    i += 1
                    continue
                j = i + 1
                while j < 8 and fila[j] == "_":
                    j += 1
                dur = (j - i) * self.paso * 0.96
                self.voz(buf, self.t(compas0 + c, i), dur, hz(midi(s)), tabla, vol, **kw)
                i = j

    # ---- percusión
    def bombo(self, inicio, vol=0.5, grave=48.0):
        n = self.n
        pos = int(inicio * SR)
        fase = 0.0
        for i in range(int(0.28 * SR)):
            f = grave + 110.0 * math.exp(-i / (0.025 * SR))
            fase += 2 * math.pi * f / SR
            self.seco[(pos + i) % n] += math.sin(fase) * math.exp(-i / (0.09 * SR)) * vol

    def caja(self, inicio, vol=0.35):
        n = self.n
        pos = int(inicio * SR)
        fase = 0.0
        for i in range(int(0.2 * SR)):
            fase += 2 * math.pi * 190.0 / SR
            e = math.exp(-i / (0.045 * SR))
            self.seco[(pos + i) % n] += (random.uniform(-1, 1) * 0.8 + math.sin(fase) * 0.5) * e * vol

    def hat(self, inicio, vol=0.14, largo=0.035):
        n = self.n
        pos = int(inicio * SR)
        prev = 0.0
        for i in range(int(largo * 3 * SR)):
            x = random.uniform(-1, 1)
            self.seco[(pos + i) % n] += (x - prev) * math.exp(-i / (largo * SR)) * vol
            prev = x

    def timbal(self, inicio, freq=80.0, vol=0.5):
        n = self.n
        pos = int(inicio * SR)
        fase = 0.0
        for i in range(int(0.5 * SR)):
            fase += 2 * math.pi * (freq + 30.0 * math.exp(-i / (0.05 * SR))) / SR
            self.seco[(pos + i) % n] += math.sin(fase) * math.exp(-i / (0.16 * SR)) * vol

    def campana(self, inicio, dur, freq, vol=0.2):
        for mult, v, d in ((1.0, 1.0, 0.9), (2.0, 0.45, 0.5), (3.01, 0.25, 0.35), (4.2, 0.12, 0.2)):
            self.voz("h", inicio, dur, freq * mult, SINE, vol * v, a=0.003, decaimiento=d, sost=0.0, r=0.35)

    # ---- reverb barata (peines circulares, vale para bucles)
    def reverb(self, mezcla=0.55):
        n = self.n
        out = self.humedo[:]
        for retraso, fb in ((int(0.173 * SR), 0.34), (int(0.247 * SR), 0.28), (int(0.331 * SR), 0.22)):
            y = out[:]
            for _ in range(2):
                for i in range(n):
                    y[i] = out[i] + fb * y[(i - retraso) % n]
            out = [(a + b) * 0.5 for a, b in zip(out, y)]
        return [s + o * mezcla for s, o in zip(self.seco, out)]

    def guardar(self, ruta, volumen_pico=0.82):
        mezcla = self.reverb()
        mezcla = [math.tanh(x * 1.1) for x in mezcla]
        pico = max(abs(x) for x in mezcla) or 1.0
        # microfundido (4 ms) en los extremos: el bucle enlaza sin chasquido
        f = int(0.004 * SR)
        for i in range(f):
            mezcla[i] *= i / f
            mezcla[-1 - i] *= i / f
        k = volumen_pico * 32767.0 / pico
        datos = struct.pack("<%dh" % len(mezcla), *[int(x * k) for x in mezcla])
        with wave.open(ruta, "wb") as w:
            w.setnchannels(1)
            w.setsampwidth(2)
            w.setframerate(SR)
            w.writeframes(datos)
        print("%s  %.1f s  (%d compases a %d bpm)" % (os.path.basename(ruta), len(mezcla) / SR, self.compases, self.bpm))


# ---------------------------------------------------------------- acordes
def acorde(raiz, tipo):
    m = midi(raiz)
    return [m + i for i in {"M": (0, 4, 7), "m": (0, 3, 7), "7": (0, 4, 7, 10)}[tipo]]


# ================================================================ NIVEL 1 · BOSQUE
def nivel1():
    p = Pista(108, 16)
    ac = [("D3", "M"), ("A2", "M"), ("B2", "m"), ("G2", "M")] * 2 + \
         [("D3", "M"), ("A2", "M"), ("B2", "m"), ("G2", "M"), ("G2", "M"), ("A2", "M"), ("D3", "M"), ("D3", "M")]
    ac = [("D3", "M"), ("A2", "M"), ("B2", "m"), ("G2", "M"),
          ("D3", "M"), ("A2", "M"), ("G2", "M"), ("A2", "M"),
          ("D3", "M"), ("A2", "M"), ("B2", "m"), ("G2", "M"),
          ("G2", "M"), ("A2", "M"), ("D3", "M"), ("D3", "M")]

    A = [["F#5", "_", "A5", "_", "B5", "A5", "F#5", "_"],
         ["E5", "_", "C#5", "_", "E5", "F#5", "E5", "_"],
         ["D5", "_", "F#5", "_", "B5", "_", "A5", "F#5"],
         ["G5", "_", "F#5", "_", "E5", "_", "D5", "_"]]
    B = [["A5", "_", "D6", "_", "C#6", "B5", "A5", "_"],
         ["B5", "_", "C#6", "_", "E6", "_", "C#6", "_"],
         ["B5", "_", "D6", "_", "B5", "A5", "G5", "_"],
         ["A5", "_", "C#6", "_", "E5", "_", "-", "-"]]
    C = [["B5", "_", "A5", "_", "G5", "_", "B5", "_"],
         ["C#6", "_", "B5", "_", "A5", "_", "E5", "_"],
         ["D6", "_", "A5", "_", "F#5", "_", "A5", "_"],
         ["D5", "_", "-", "-", "F#5", "_", "A5", "_"]]
    melodia = A + B + A + C

    for c, (raiz, tipo) in enumerate(ac):
        notas = acorde(raiz, tipo)
        fund = notas[0]
        # pad suave
        for nt in notas:
            p.voz("h", p.t(c), 8 * p.paso, hz(nt + 12), SOFT, 0.085, a=0.35, decaimiento=2.0, sost=0.9, r=0.5)
        # bajo: raíz, quinta, raíz
        for paso, val in ((0, fund), (2, fund + 7), (4, fund), (6, fund + 7 if c % 2 else fund + 12)):
            p.voz("s", p.t(c, paso), 1.6 * p.paso, hz(val), TRI, 0.36, a=0.004, decaimiento=0.18, sost=0.5, r=0.05)
        # arpegio de la guitarra-pluma
        for k in range(8):
            nt = notas[(k * 2 + (k // 4)) % len(notas)] + 24
            p.voz("h", p.t(c, k), 1.4 * p.paso, hz(nt), P125, 0.12, a=0.002, decaimiento=0.07, sost=0.0, r=0.06)
        # percusión: entra en el compás 5
        if c >= 4:
            p.bombo(p.t(c, 0), 0.34)
            p.bombo(p.t(c, 4), 0.30)
            p.caja(p.t(c, 2), 0.16)
            p.caja(p.t(c, 6), 0.16)
            for k in range(1, 8, 2):
                p.hat(p.t(c, k), 0.10)
        elif c >= 2:
            p.bombo(p.t(c, 0), 0.22)
            for k in (3, 7):
                p.hat(p.t(c, k), 0.08)

    # la melodía entra en el compás 3 con la flauta y se refuerza desde el 5 con el pulso
    p.melodia("h", 0, melodia, P25, 0.30, a=0.012, decaimiento=0.5, sost=0.75, r=0.12, vib=0.004)
    p.melodia("s", 0, melodia, SOFT, 0.10, a=0.01, decaimiento=0.5, sost=0.7, r=0.1)
    return p


# ================================================================ NIVEL 2 · REY CERDO
def nivel2():
    p = Pista(128, 16)
    ac = [("E3", "m"), ("A2", "m"), ("B2", "M"), ("E3", "m"),
          ("E3", "m"), ("C3", "M"), ("B2", "M"), ("E3", "m"),
          ("A2", "m"), ("E3", "m"), ("B2", "M"), ("E3", "m"),
          ("C3", "M"), ("D3", "M"), ("B2", "M"), ("B2", "M")]

    A = [["E5", "_", "-", "G5", "B5", "_", "G5", "E5"],
         ["A5", "_", "-", "C6", "B5", "_", "A5", "E5"],
         ["F#5", "_", "-", "F#5", "D#5", "_", "F#5", "B5"],
         ["B5", "_", "G5", "_", "E5", "_", "-", "-"]]
    A2 = [["E5", "_", "-", "G5", "B5", "_", "E6", "_"],
          ["C6", "_", "B5", "_", "G5", "_", "E5", "_"],
          ["D#5", "F#5", "B5", "_", "A5", "F#5", "D#5", "_"],
          ["E5", "_", "-", "-", "B4", "_", "E5", "_"]]
    B = [["C6", "_", "A5", "C6", "E6", "_", "C6", "A5"],
         ["G5", "_", "B5", "_", "E6", "_", "B5", "G5"],
         ["F#5", "_", "A5", "_", "B5", "_", "D#6", "_"],
         ["E6", "_", "B5", "_", "G5", "_", "E5", "_"]]
    C = [["E6", "_", "D6", "_", "C6", "_", "G5", "_"],
         ["F#6", "_", "E6", "_", "D6", "_", "A5", "_"],
         ["D#6", "_", "B5", "_", "F#5", "_", "D#5", "_"],
         ["B5", "_", "-", "B5", "B5", "_", "-", "-"]]
    melodia = A + A2 + B + C

    for c, (raiz, tipo) in enumerate(ac):
        notas = acorde(raiz, tipo)
        fund = notas[0]
        # bajo "oom": raíz grave en 1 y 3
        for paso in (0, 4):
            p.voz("s", p.t(c, paso), 1.5 * p.paso, hz(fund), SAW, 0.30, a=0.003, decaimiento=0.12, sost=0.35, r=0.04)
        # "pah": acorde corto en 2 y 4 (trombones)
        for paso in (2, 6):
            for nt in notas:
                p.voz("s", p.t(c, paso), 1.0 * p.paso, hz(nt + 12), BRASS, 0.11, a=0.004, decaimiento=0.08, sost=0.2, r=0.03)
        # bombo y caja de marcha
        p.bombo(p.t(c, 0), 0.50, 52.0)
        p.bombo(p.t(c, 4), 0.46, 52.0)
        p.caja(p.t(c, 2), 0.34)
        p.caja(p.t(c, 6), 0.34)
        if c % 2 == 1:
            p.caja(p.t(c, 7), 0.16)
        for k in range(8):
            p.hat(p.t(c, k), 0.07 if k % 2 == 0 else 0.11)
        # el compás final de cada frase de 4: redoble
        if c % 4 == 3:
            for k in range(4):
                p.caja(p.t(c, 6 + k * 0.5), 0.18 + 0.05 * k)

    p.melodia("s", 0, melodia, BRASS, 0.28, a=0.01, decaimiento=0.3, sost=0.7, r=0.07, vib=0.006)
    # contramelodía burlona una octava abajo en la segunda mitad
    p.melodia("s", 8, melodia[8:], SAW, 0.07, a=0.01, decaimiento=0.3, sost=0.6, r=0.05)
    return p


# ================================================================ NIVEL 3 · MONTAÑA
def nivel3():
    p = Pista(84, 16)
    ac = [("A2", "m"), ("F2", "M"), ("C3", "M"), ("G2", "M"),
          ("A2", "m"), ("F2", "M"), ("D3", "m"), ("E2", "M"),
          ("A2", "m"), ("F2", "M"), ("C3", "M"), ("G2", "M"),
          ("F2", "M"), ("G2", "M"), ("A2", "m"), ("A2", "m")]

    A = [["E5", "_", "_", "_", "A5", "_", "_", "_"],
         ["C6", "_", "_", "_", "A5", "_", "F5", "_"],
         ["G5", "_", "_", "_", "E5", "_", "G5", "_"],
         ["D5", "_", "B4", "_", "D5", "_", "_", "_"]]
    B = [["A5", "_", "C6", "_", "E6", "_", "_", "_"],
         ["F6", "_", "_", "_", "C6", "_", "A5", "_"],
         ["D6", "_", "_", "_", "A5", "_", "F5", "_"],
         ["G#5", "_", "B5", "_", "E6", "_", "_", "_"]]
    C = [["A5", "_", "C6", "_", "F6", "_", "C6", "_"],
         ["B5", "_", "D6", "_", "G6", "_", "D6", "_"],
         ["E6", "_", "C6", "_", "A5", "_", "E5", "_"],
         ["A5", "_", "_", "_", "_", "_", "_", "_"]]
    melodia = A + B + A + C

    for c, (raiz, tipo) in enumerate(ac):
        notas = acorde(raiz, tipo)
        fund = notas[0]
        # colchón amplio: dos capas desafinadas
        for nt in notas:
            for desv, vol in ((-0.12, 0.060), (0.12, 0.060)):
                p.voz("h", p.t(c), 8.4 * p.paso, hz(nt + 12) * (1 + desv / 100.0), SAW, vol * 0.55,
                      a=1.1, decaimiento=3.0, sost=0.85, r=1.0)
            p.voz("h", p.t(c), 8.4 * p.paso, hz(nt + 24), SINE, 0.05, a=1.3, decaimiento=3.0, sost=0.9, r=1.0)
        # bajo profundo
        p.voz("s", p.t(c), 8 * p.paso, hz(fund), SINE, 0.42, a=0.05, decaimiento=3.0, sost=0.8, r=0.4)
        p.voz("s", p.t(c), 8 * p.paso, hz(fund - 12), SINE, 0.18, a=0.08, decaimiento=3.0, sost=0.8, r=0.4)
        # latido de timbal
        if c >= 2:
            p.timbal(p.t(c, 0), 70.0, 0.34)
            if c >= 8:
                p.timbal(p.t(c, 4), 62.0, 0.22)
        # arpegio de campanas desde el compás 5
        if c >= 4:
            for k in range(8):
                nt = notas[(k % len(notas))] + 24 + (12 if k % 4 == 3 else 0)
                p.campana(p.t(c, k), 1.4 * p.paso, hz(nt), 0.050)

    p.melodia("h", 0, melodia, SOFT, 0.19, a=0.09, decaimiento=1.0, sost=0.8, r=0.5, vib=0.003)
    # campana que dobla la melodía
    for c, fila in enumerate(melodia):
        for i, s in enumerate(fila):
            if s not in ("-", "_") and i % 2 == 0:
                p.campana(p.t(c, i), 3 * p.paso, hz(midi(s) + 12), 0.075)
    return p


# ================================================================ BOSS · SEÑOR DEL ABISMO
def boss():
    p = Pista(148, 24)
    ciclo1 = [("D3", "m"), ("D3", "m"), ("A#2", "M"), ("C3", "M"), ("D3", "m"), ("G2", "m"), ("A2", "M"), ("A2", "M")]
    ciclo3 = [("D3", "m"), ("A#2", "M"), ("C3", "M"), ("A2", "M"), ("D3", "m"), ("G2", "m"), ("A2", "M"), ("A2", "M")]
    ac = ciclo1 + ciclo1 + ciclo3

    L = [["D5", "_", "F5", "A5", "_", "G5", "F5", "D5"],
         ["E5", "F5", "A5", "_", "D6", "_", "C6", "A5"],
         ["A#5", "_", "D6", "_", "F6", "_", "D6", "A#5"],
         ["C6", "_", "E6", "_", "G6", "_", "E6", "C6"],
         ["D6", "_", "A5", "D6", "_", "F6", "E6", "D6"],
         ["A#5", "_", "D6", "G6", "_", "F6", "D6", "A#5"],
         ["C#6", "_", "E6", "A6", "_", "G6", "E6", "C#6"],
         ["A5", "_", "_", "_", "E5", "F5", "G5", "A5"]]
    # sección 3: mismo dibujo una octava arriba
    L3 = [[(s[:-1] + str(int(s[-1]) + 1)) if s[-1].isdigit() else s for s in fila] for fila in L]

    for c, (raiz, tipo) in enumerate(ac):
        notas = acorde(raiz, tipo)
        fund = notas[0]
        seccion = c // 8
        # bajo en corcheas con salto de octava
        for k in range(8):
            nt = fund + (12 if k % 4 == 3 else 0)
            p.voz("s", p.t(c, k), 0.9 * p.paso, hz(nt), SAW, 0.27, a=0.002, decaimiento=0.07, sost=0.25, r=0.03)
        # acordes cortantes en contratiempo
        if seccion >= 0:
            for paso in (2, 5):
                for nt in notas:
                    p.voz("s", p.t(c, paso), 0.8 * p.paso, hz(nt + 12), BRASS, 0.075, a=0.003, decaimiento=0.06, sost=0.2, r=0.03)
        # percusión
        for k in range(0, 8, 2):
            p.bombo(p.t(c, k), 0.46, 46.0)
        p.caja(p.t(c, 2), 0.34)
        p.caja(p.t(c, 6), 0.34)
        saltos = 0.5 if seccion == 2 else 1.0
        k = 0.0
        while k < 8:
            p.hat(p.t(c, k), 0.07)
            k += saltos
        # arpegio rápido en la tercera sección
        if seccion == 2:
            for k in range(16):
                nt = notas[k % len(notas)] + 24
                p.voz("h", p.t(c, k * 0.5), 0.5 * p.paso, hz(nt), P125, 0.07, a=0.002, decaimiento=0.05, sost=0.0, r=0.03)
        # redoble antes de cada cambio de sección
        if c % 8 == 7:
            for k in range(8):
                p.caja(p.t(c, 4 + k * 0.5), 0.14 + 0.03 * k)
            p.timbal(p.t(c, 0), 60.0, 0.4)

    # colchón oscuro
    for c, (raiz, tipo) in enumerate(ac):
        for nt in acorde(raiz, tipo):
            p.voz("h", p.t(c), 8 * p.paso, hz(nt), SAW, 0.045, a=0.2, decaimiento=2.0, sost=0.85, r=0.3)

    # melodía: compases 5-8 de la sección 1, 9-16 completa, 17-24 octava arriba
    p.melodia("s", 4, L[4:], P25, 0.26, a=0.006, decaimiento=0.4, sost=0.8, r=0.06, vib=0.004)
    p.melodia("s", 8, L, SAW, 0.20, a=0.006, decaimiento=0.4, sost=0.8, r=0.06, vib=0.004)
    p.melodia("s", 16, L3, P25, 0.22, a=0.006, decaimiento=0.4, sost=0.8, r=0.06, vib=0.004)
    p.melodia("s", 16, L, SAW, 0.14, a=0.006, decaimiento=0.4, sost=0.8, r=0.06)
    return p


if __name__ == "__main__":
    salida = sys.argv[1] if len(sys.argv) > 1 else os.path.join("Assets", "Resources", "Audio", "Musica")
    os.makedirs(salida, exist_ok=True)
    for nombre, fn in (("musica_nivel1.wav", nivel1), ("musica_nivel2.wav", nivel2),
                       ("musica_nivel3.wav", nivel3), ("musica_boss.wav", boss)):
        fn().guardar(os.path.join(salida, nombre))
