# Height Is Time

A 2D puzzle platformer where your height is the clock.

Climb to move the world forward in time, drop to rewind it. Hold Shift to freeze time, then carry that moment to a new height: make a future bridge appear over a pit, or keep a past gate open on a high ledge.

**▶ Play in the browser:** https://height-is-time-team.github.io/PROTOTYPE-X/

**Genre + twist:** Puzzle Platformer + Height-Controlled Time

## Controls

| Key | Action |
|---|---|
| A / D (or ← / →) | Move |
| Space | Jump |
| S (or ↓) | Fast fall (dive straight down, rewinding time quickly) |
| Hold Shift | Freeze time (4 second meter, refills when released) |
| R | Respawn |

## How it works

Each room has its own clock. The world time is your height above the room's floor, at 1 second per meter:

```
time = clamp((playerY - groundY) * secondsPerMeter, 0, maxTime)
```

Objects in the room (walls, bridges, gates) read that time and move or appear to match it. Freezing time locks the clock where it is, so you can climb or drop without changing the world.

The level has three short rooms, each teaching one idea:

1. **Sinking Wall** (jumping moves time) – a wall too tall to jump over sinks by exactly as much as you rise, so jumping makes it duck under you.
2. **Future Bridge** (freeze high, then go low) – the bridge over the pit only exists from time 4. Climb the stairs, freeze, then drop onto the bridge and run across.
3. **Past Gate** (freeze low, then go high) – the gate on the ledge is only open before time 2. Freeze on the floor, climb up while frozen and walk through to the exit.

While time is frozen and the meter drops below 25%, the player flashes red as a warning.

## Running the project

1. Install **Unity 6000.6.0f1** through Unity Hub.
2. Clone the repo and open the folder in Unity Hub.
3. Open `Assets/_Project/Scenes/HeightIsTime.unity` and press **Play**.

### Tests

**Window ▸ General ▸ Test Runner**, then **Run All** under both **EditMode** and **PlayMode**. There are 20 tests: the height-to-time math, and full playthroughs of each room, freeze time and fast fall.

### WebGL build

**Prototype ▸ Build WebGL** writes the build to `Builds/WebGL`. To try it locally:

```bash
cd Builds/WebGL && python3 -m http.server 8765
```

Then open http://localhost:8765. The live site is served from the `gh-pages` branch.

## Project layout

```
Assets/_Project/
  Scenes/HeightIsTime.unity     the level
  Scripts/Runtime/Time/         WorldClock, TimeMath and the time-driven objects
  Scripts/Runtime/Player/       movement, fast fall, freeze time, respawn
  Scripts/Runtime/Level/        rooms, hazards, exit
  Scripts/Runtime/UI/           time HUD
  Editor/                       level builder and WebGL build script
  Tests/                        EditMode and PlayMode tests
```

All visuals use Unity's built-in shapes; there are no external assets.

## Team

- **Kushal** – web page controls and credits, deployment
- **Vraj** – low-meter warning flash
- **Viraj** – fast fall dive

Made for a paired prototype assignment.
