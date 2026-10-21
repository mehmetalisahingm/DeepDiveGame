"""Q03 ölçek çizimi ve tempo modeli (öneri, uygulama değil).

Tek kaynak: aşağıdaki rota çizgileri (Unity dünyası, metre, +x doğu, +z kuzey/deniz, y yukarı).
Çıktılar: q03-tempo.json (mesafe/süre/tempo tabloları) ve q03-plan.svg (ölçekli kuşbakışı).
Çalıştırma: python -I q03_scale.py  (yalnız standart kütüphane; dosyalar bu klasöre yazılır)

Kaynaklı sabitler:
  WALK 4 m/s, SWIM 3 m/s          NetworkDiver.prefab walkSpeed/swimSpeed
  O2 120 s, 1/s                   NetworkDiver.prefab maxOxygen/oxygenDrainPerSecond
  Gün 08:00-24:00 = 1200 s        DayEngine.GameMinutesPerRealSecond = 960 / (20*60)
  Uyarı 22:00 / 23:00             DayIds.FirstWarningMinute / SecondWarningMinute -> 150 s / 75 s kala
  Çanta 20 kg, balık 0,9-2,6 kg    InventoryManager.CapacityGrams, Species/*.asset weight
  Bugünkü rotalar                 docs/reports/P4-ROUTE-172.md (a0 58,6 m; b 69,7 m; c 67,9 m)
Varsayımlar (ölçüm değil, oynama testinde değişir) TEMPO sözlüğündedir.
"""
import json
import math
import os

HERE = os.path.dirname(os.path.abspath(__file__))

WALK = 4.0
DAY_SECONDS = (24 * 60 - 8 * 60) / ((24 * 60 - 8 * 60) / (20 * 60))  # 1200
WARN_1 = (24 * 60 - 22 * 60) / ((24 * 60 - 8 * 60) / (20 * 60))       # 150
WARN_2 = (24 * 60 - 23 * 60) / ((24 * 60 - 8 * 60) / (20 * 60))       # 75

TEMPO = {
    "dive_cycle_s": [120, 180],   # sudan girişten çıkışa: 120 s tank + yüzme payı; iki uç
    "sell_stop_s": 10,            # balıkçıda teslim etkileşimi
    "equip_stop_s": 15,           # sabah ekipman dükkânı
    "evening_home_s": 30,         # PC/yayın + yatağa geçiş
    "house_exit_m": 14.5,         # spawn -> kapı, #172 ölçümü (ev içi değişmez)
    "swim_back_s": 60,            # 22:00 uyarısında sudaki oyuncunun çıkış payı (varsayım)
}

# ---------------------------------------------------------------------------------------------
# Sabit kıyı (değişmez): #172 / #188 / #194 ölçümleri
FIXED = {
    "Beach_TownGate": (0.0, -13.5),
    "dock-town-1": (9.0, -3.7),
    "DiveExit": (9.0, -9.0),
    "Beach_Wade top": (-6.5, -10.97),
    "repair-boat-cradle": (3.6, -12.6),
    "boat-part-hull": (-12.0, -12.5),
    "boat-part-engine": (-2.5, -12.75),
    "boat-part-fuel-tank": (12.5, -12.5),
    "repair-drop-0": (7.5, -14.5),
}
COAST_RECT = (-15.0, -15.0, 15.0, -3.5)       # kıyı zone (x0, z0, x1, z1)
SWIM_RECT = (-15.0, -15.0, 15.0, 65.0)        # SwimVolume / region-near-1
SEA_PLANE = (-30.0, -30.0, 30.0, 70.0)        # görünen deniz düzlemi (collider yok)

# Bugünkü kasaba (ghost çizim için, #172 kuşbakışı)
TODAY = {
    "ground": (-14.75, -55.5, 14.75, -3.5),
    "square": (0.0, -29.0),
    "home_door": (0.0, -42.0),
    "home": (-6.0, -54.0, 6.0, -42.0),
}

# Gate -> iskele (P): bütün adaylarda aynı
P_PATH = [(0.0, -13.5), (7.0, -13.0), (9.0, -9.0), (9.0, -3.7)]

# ---------------------------------------------------------------------------------------------
# Adaylar. H = ev kapısı -> balıkçı (meydan üstünden), C = balıkçı -> Beach_TownGate (satış turu bunu iki kez yürür).
# y = yürüme yüzeyi yüksekliği; kıyı 8.4 sabit.
CANDIDATES = {
    "B": {
        "label": "B — önerilen: uzak ev, balıkçı meydanın kıyı köşesinde, orta sahil yolu",
        "C_path": [(-4, -96, 14.0), (-6, -80, 13.6), (-11, -56, 11.8), (-6, -34, 9.6), (0, -15, 8.4), (0, -13.5, 8.4)],
        "H_path": [(-4, -330, 15.4), (-4, -318, 15.4), (6, -295, 15.2), (-10, -265, 15.0), (-20, -232, 14.8),
                   (-14, -200, 14.6), (0, -170, 14.4), (8, -140, 14.2), (8, -122, 14.0), (8, -108, 14.0), (-4, -96, 14.0)],
        "square": (-10, -122, 26, -94),
        "services": {
            "buyer-fish (balıkçı)": (-4, -96),
            "shop-equipment": (-8, -112),
            "buyer-recording": (24, -102),
            "vendor-vehicle (liman, öneri)": (13, -20),
        },
        "home": (-10, -342, 2, -330),
        "views": {
            "1 evden meydan: kuleler (−20,−232) virajında görünür": (-20, -232),
            "2 meydandan su: (−6,−80) sırtında deniz açılır": (-6, -80),
            "3 iskeleden kasaba: teras silueti ~95 m güneyde": (9, -3.7),
        },
    },
    "B+": {
        "label": "B+ — ilk çizim: balıkçı meydanın arkasında (sahil yolu 125 m)",
        "C_path": [(8, -133, 14.4), (8, -119, 14.4), (2, -105, 14.0), (-8, -85, 12.6),
                   (-12, -60, 10.8), (-6, -35, 9.2), (0, -15, 8.4), (0, -13.5, 8.4)],
        "H_path": [(-4, -330, 15.4), (-4, -318, 15.4), (6, -295, 15.2), (-10, -265, 15.0),
                   (-20, -232, 14.8), (-14, -200, 14.6), (0, -170, 14.4), (8, -147, 14.4), (8, -133, 14.4)],
    },
    "A": {
        "label": "A — kıyıya yakın merkez, çok uzak ev",
        "C_path": [(4, -80, 11.4), (4, -66, 11.4), (-4, -40, 9.6), (0, -15, 8.4), (0, -13.5, 8.4)],
        "H_path": [(-4, -330, 12.4), (-4, -318, 12.4), (6, -290, 12.2), (-12, -250, 12.0), (-18, -200, 11.8),
                   (-8, -150, 11.6), (2, -110, 11.4), (4, -94, 11.4), (4, -80, 11.4)],
    },
    "C": {
        "label": "C — merkez evin yanında, uzun sahil yolu",
        "C_path": [(0, -270, 18.4), (0, -256, 18.4), (-14, -220, 17.0), (-20, -170, 15.0), (-10, -120, 13.0),
                   (-14, -75, 11.0), (-6, -35, 9.2), (0, -15, 8.4), (0, -13.5, 8.4)],
        "H_path": [(-4, -330, 18.4), (-4, -318, 18.4), (6, -300, 18.4), (0, -284, 18.4), (0, -270, 18.4)],
    },
    "D": {
        "label": "D — 500 m üst sınır (B'nin ev sokağı uzatılmış)",
        "C_path": None,  # B ile aynı
        "H_path": [(-4, -490, 15.4), (-4, -478, 15.4), (10, -450, 15.4), (-12, -410, 15.4), (-24, -360, 15.2),
                   (-10, -300, 15.0), (-20, -232, 14.8), (-14, -200, 14.6), (0, -170, 14.4), (8, -140, 14.2),
                   (8, -122, 14.0), (8, -108, 14.0), (-4, -96, 14.0)],
    },
}
CANDIDATES["D"]["C_path"] = CANDIDATES["B"]["C_path"]


def length(path):
    total = 0.0
    for a, b in zip(path, path[1:]):
        ay = a[2] if len(a) > 2 else 8.4
        by = b[2] if len(b) > 2 else 8.4
        total += math.sqrt((a[0] - b[0]) ** 2 + (a[1] - b[1]) ** 2 + (ay - by) ** 2)
    return total


def max_grade(path):
    worst = 0.0
    for a, b in zip(path, path[1:]):
        run = math.hypot(a[0] - b[0], a[1] - b[1])
        if run > 0 and len(a) > 2:
            worst = max(worst, math.degrees(math.atan2(abs(a[2] - b[2]), run)))
    return worst


def tempo(H, C, P):
    house = TEMPO["house_exit_m"]
    home_to_pier = house + H + C + P
    morning = (house + H + C + P) / WALK + TEMPO["equip_stop_s"]
    evening = (P + C + H + house) / WALK + TEMPO["evening_home_s"]
    sale_walk = 2 * (C + P) / WALK + TEMPO["sell_stop_s"]
    rows = []
    for dive in TEMPO["dive_cycle_s"]:
        play = DAY_SECONDS - morning - evening
        each = dive + sale_walk                       # her dalıştan sonra satış
        each_pair = (2 * dive + sale_walk) / 2        # iki dalışta bir satış
        rows.append({
            "dive_cycle_s": dive,
            "dives_sell_every_dive": math.floor(play / each),
            "dives_sell_every_2": math.floor(play / each_pair),
            "non_dive_share_sell_every_dive": round(1 - (math.floor(play / each) * dive) / DAY_SECONDS, 3),
        })
    return {
        "H_m": round(H, 1), "C_m": round(C, 1), "P_m": round(P, 1),
        "home_to_pier_m": round(home_to_pier, 1), "home_to_pier_s": round(home_to_pier / WALK, 1),
        "morning_s": round(morning, 1), "evening_s": round(evening, 1),
        "sale_round_trip_s": round(sale_walk, 1),
        "warn_23_exit_to_buyer_s": round((C + P) / WALK + TEMPO["sell_stop_s"], 1),
        "warn_23_ok": (C + P) / WALK + TEMPO["sell_stop_s"] <= WARN_2,
        "warn_22_in_water_ok": TEMPO["swim_back_s"] + (C + P) / WALK + TEMPO["sell_stop_s"] <= WARN_1,
        "per_dive": rows,
    }


def main():
    P = length(P_PATH)
    out = {"constants": {"walk_mps": WALK, "day_s": DAY_SECONDS, "warn_22_left_s": WARN_1,
                         "warn_23_left_s": WARN_2},
           "assumptions": TEMPO, "P_gate_to_pier_m": round(P, 1), "candidates": {}}
    # Bugün: balıkçı müşteri noktası (-3.3, -27.3) (#172 slot 1); kapı (0,-42); gate (0,-13.5)
    today_C = math.hypot(-3.3 - 0, -27.3 + 13.5)     # 14.2
    today_H = math.hypot(-3.3 - 0, -27.3 + 42)       # 15.1
    out["candidates"]["today"] = {"label": "Bugün (#172)", **tempo(today_H, today_C, P)}
    out["candidates"]["today"]["note"] = "#172 gerçek a0 sürüşü 58,6 m / 14,7 s; model 4 m/s düz çizgi"
    for key, cand in CANDIDATES.items():
        H, C = length(cand["H_path"]), length(cand["C_path"])
        row = {"label": cand["label"], **tempo(H, C, P),
               "max_grade_deg": round(max(max_grade(cand["H_path"]), max_grade(cand["C_path"])), 1)}
        out["candidates"][key] = row
    with open(os.path.join(HERE, "q03-tempo.json"), "w", encoding="utf-8") as f:
        json.dump(out, f, ensure_ascii=False, indent=2)
    write_svg(out)
    for k, v in out["candidates"].items():
        print(k, v["home_to_pier_m"], "m", v["home_to_pier_s"], "s | satış turu", v["sale_round_trip_s"],
              "s | 23:00", v["warn_23_ok"], "| dalış/gün", [r["dives_sell_every_dive"] for r in v["per_dive"]])


# ---------------------------------------------------------------------------------------------
def write_svg(out):
    b = CANDIDATES["B"]
    s = 2.2                      # px / m
    x0, x1, z0, z1 = -60.0, 60.0, 32.0, -352.0
    pad, legend_w = 30, 300
    W = int((x1 - x0) * s) + 2 * pad + legend_w
    H = int((z0 - z1) * s) + 2 * pad

    def X(x): return pad + (x - x0) * s
    def Y(z): return pad + (z0 - z) * s

    def rect(r, fill, stroke="none", dash=None, op=1.0):
        xa, za, xb, zb = r
        d = f' stroke-dasharray="{dash}"' if dash else ""
        return (f'<rect x="{X(min(xa, xb)):.1f}" y="{Y(max(za, zb)):.1f}" width="{abs(xb - xa) * s:.1f}" '
                f'height="{abs(zb - za) * s:.1f}" fill="{fill}" stroke="{stroke}"{d} opacity="{op}"/>')

    def poly(path, color, w, dash=None):
        pts = " ".join(f"{X(p[0]):.1f},{Y(p[1]):.1f}" for p in path)
        d = f' stroke-dasharray="{dash}"' if dash else ""
        return f'<polyline points="{pts}" fill="none" stroke="{color}" stroke-width="{w}"{d} stroke-linejoin="round"/>'

    def dot(p, color, r=3.5):
        return f'<circle cx="{X(p[0]):.1f}" cy="{Y(p[1]):.1f}" r="{r}" fill="{color}"/>'

    def text(x, y, t, size=11, anchor="start", color="#1f2933", weight="normal"):
        return (f'<text x="{x:.1f}" y="{y:.1f}" font-size="{size}" text-anchor="{anchor}" fill="{color}" '
                f'font-family="Segoe UI, Arial, sans-serif" font-weight="{weight}">{t}</text>')

    e = [f'<svg xmlns="http://www.w3.org/2000/svg" width="{W}" height="{H}" viewBox="0 0 {W} {H}">',
         f'<rect width="{W}" height="{H}" fill="#fbfaf7"/>']
    e.append(rect(SEA_PLANE, "#cfe3ee"))
    e.append(rect(SWIM_RECT, "#a9cfe3"))
    e.append(rect(COAST_RECT, "#efe2bf"))
    e.append(rect(TODAY["ground"], "none", "#9aa5b1", "4 3"))
    e.append(rect(TODAY["home"], "none", "#9aa5b1", "4 3"))
    e.append(text(X(-14) + 2, Y(-55.5) + 12, "bugünkü kasaba", 9, color="#7b8794"))
    e.append(rect(b["square"], "#f3d9a4", "#b7791f"))
    e.append(rect(b["home"], "#e8c8b0", "#9c4221"))
    e.append(poly(b["C_path"], "#2b6cb0", 4))
    e.append(poly(b["H_path"], "#c05621", 4))
    e.append(poly(P_PATH, "#2f855a", 3))
    for name, p in FIXED.items():
        e.append(dot(p, "#2f855a", 2.5))
    for name, p in b["services"].items():
        e.append(dot(p, "#6b46c1", 4.5))
        e.append(text(X(p[0]) + 7, Y(p[1]) + 4, name, 10, color="#44337a"))
    for i, (name, p) in enumerate(b["views"].items()):
        e.append(f'<circle cx="{X(p[0]):.1f}" cy="{Y(p[1]):.1f}" r="8" fill="none" stroke="#d53f8c" stroke-width="2"/>')
        e.append(text(X(p[0]), Y(p[1]) + 4, name[0], 10, "middle", "#d53f8c", "bold"))
    e.append(text(X(-4), Y(-342) + 22, "EV (kapı kuzeye bakar)", 11, "middle", "#9c4221", "bold"))
    e.append(text(X(8), Y(-133) + 4, "MEYDAN", 11, "middle", "#7b341e", "bold"))
    e.append(text(X(0), Y(26), "DENİZ (SwimVolume x ±15)", 11, "middle", "#2a4365", "bold"))
    e.append(text(X(20), Y(-8), "kıyı / iskele (sabit)", 10, color="#744210"))
    # Ölçek çubuğu ve kuzey
    sx, sy = X(-55), Y(-340)
    e.append(f'<line x1="{sx}" y1="{sy}" x2="{sx + 50 * s}" y2="{sy}" stroke="#1f2933" stroke-width="2"/>')
    e.append(text(sx, sy - 5, "50 m", 10))
    e.append(text(X(-52), Y(5), "▲ kuzey (+z, deniz)", 10))
    # Lejant
    lx, ly = W - legend_w + 10, pad + 10
    r = out["candidates"]["B"]
    lines = [
        ("Q03 ölçek adayı B (öneri, uygulama değil)", 12, "bold", "#1f2933"),
        (f"Ev → iskele: {r['home_to_pier_m']} m ≈ {r['home_to_pier_s']} s", 11, "normal", "#1f2933"),
        (f"Ev sokağı (turuncu) H = {r['H_m']} m", 11, "normal", "#c05621"),
        (f"Sahil yolu (mavi) C = {r['C_m']} m", 11, "normal", "#2b6cb0"),
        (f"Kapı → iskele (yeşil) P = {r['P_m']} m", 11, "normal", "#2f855a"),
        (f"Satış turu kıyı↔balıkçı: {r['sale_round_trip_s']} s", 11, "normal", "#1f2933"),
        (f"En dik eğim: {r['max_grade_deg']}° (CC sınırı 45°)", 11, "normal", "#1f2933"),
        ("Mor: hizmet noktaları; pembe: kompozisyonlar", 11, "normal", "#44337a"),
        ("Kesikli gri: bugünkü kasaba ve ev", 11, "normal", "#7b8794"),
        ("Açık mavi: görünen deniz düzlemi (yüzülemez)", 11, "normal", "#2a4365"),
    ]
    for t, size, weight, color in lines:
        e.append(text(lx, ly, t, size, weight=weight, color=color))
        ly += 18
    ly += 8
    for name in b["views"]:
        e.append(text(lx, ly, name, 10, color="#97266d"))
        ly += 16
    e.append("</svg>")
    with open(os.path.join(HERE, "q03-plan.svg"), "w", encoding="utf-8") as f:
        f.write("\n".join(e))


if __name__ == "__main__":
    main()
