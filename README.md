# TeleCasino Keno Game

A command-line Keno game animation and result generator built with .NET.  
Users pick 7–15 numbers (1–80), place a wager ($1, $2, or $5), and receive an animated video of the draw, plus a JSON summary.

---

## Features

- **Animated draw**:  
  1. **Flash** the 20 drawn balls in random order on a dark-gray background  
  2. **Reveal** them on a felt-green background  
  3. **Highlight** hits (crimson) vs. misses (sea-green)  
  4. **Summary grid** (5×4) of all drawn balls on black, with white circles around the player’s picks  
- **JSON output**: Detailed result including wager, picks, drawn numbers, hits, payout, net gain, video file.  
- **House edge**: Pays 95% of the fair odds.

---

## Installation

1. Ensure [.NET 9.0 SDK](https://dotnet.microsoft.com/download) is installed.  
2. Clone or download this repository.  
3. Add dependencies:

   ```bash
   dotnet add package SkiaSharp
   dotnet add package Svg.Skia
   dotnet add package Newtonsoft.Json
   ```

4. Place your `ball1.svg` … `ball80.svg` files in the `images/` directory.

---

## Build & Publish

```bash
# Clean and build
rm -rf bin obj
dotnet clean
dotnet restore
dotnet publish -c Release

# The single-file, self-contained binary will be in:
#   bin/Release/net9.0/<RID>/publish/TeleCasino.KenoGame
```

---

## Usage

```bash
TeleCasino.KenoGame <SpinId> <Wager> --bet <n1,n2,...> [--json]
```

- `<SpinId>`: Unique identifier used for output filenames (`<SpinId>.mp4` and `<SpinId>.json`).  
- `<Wager>`: Must be 1, 2, or 5.  
- `--bet <n1,n2,...>`: Comma-separated picks, exactly 7–15 unique integers between 1 and 80.  
- `--json` (optional): Print the JSON summary to console.

### Example

```bash
TeleCasino.KenoGame abc123 5 --bet 8,4,16,10,22,7 --json
```

- Generates `abc123.mp4` (video) and `abc123.json`:

```json
{
  "SpinId": "abc123",
  "Wager": 5,
  "PlayerNumbers": [8,4,16,10,22,7],
  "DrawnNumbers": [...20 numbers...],
  "Hits": 4,
  "Payout": 33.25,
  "NetGain": 28.25,
  "VideoFile": "abc123.mp4"
}
```

---

## Rules & Parameters

- **Pick count**: 7–15 numbers out of 80.  
- **Draw count**: 20 balls.  
- **Minimum hits for payout**: 4 hits (otherwise payout = 0).  
- **House edge**: 5% (pays 95% of fair odds).  

---

## Payout Formula

Let:

- \(k\) = number of picks  
- \(h\) = number of hits  
- total draw = 20  

1. **Probability of exactly _h_ hits**  
   $$
   P(h) \;=\; \frac{\binom{k}{h}\,\binom{80 - k}{20 - h}}{\binom{80}{20}}
   $$

2. **Fair odds** (the true “x : 1” payout for _h_ hits)  
   $$
   \text{fairOdds}
   = \frac{1}{P(h)} \;-\; 1
   $$

3. **House‑adjusted multiplier** (e.g. 5 % edge)  
   $$
   \text{multi}
   = \text{fairOdds} \;\times\; 0.95
   $$

4. **Compute payout and net gain**  
   $$
   \text{Payout} = \text{Wager} \;\times\; \text{multi}, 
   \quad
   \text{NetGain} = \text{Payout} \;-\; \text{Wager}
   $$

---

## License

This project is released under the MIT License.
