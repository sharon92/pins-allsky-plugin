# PINS AllSky

`PINS AllSky` adds a Pi HQ camera side-channel to a PINS and Touch-N-Stars rig so you can capture ambient sky frames during a normal astrophotography session and turn them into:

- timelapse videos with `ffmpeg`
- keograms with AllSky's `keogram`
- startrail composites with AllSky's `startrails`

The backend is a NINA/PINS plugin. The frontend is a Touch-N-Stars plugin that must be compiled into the Touch-N-Stars web app because TNS discovers plugins at build time.

## Disclaimer And Credits

- This plugin was generated entirely with the help of OpenAI Codex. A human review is still required before any production or unattended use.
- The project was inspired by the work of the [AllSky Team](https://github.com/AllskyTeam/allsky), whose tooling and approach informed the timelapse, keogram, and startrail pipeline used here.

## Repository Layout

- `backend/PinsAllSky`: NINA/PINS plugin backend and local REST API
- `frontend/pins-allsky`: Touch-N-Stars frontend plugin
- `assets/tools/linux-arm64`: bundled ARM64 AllSky helper binaries
- `scripts/build-plugin.sh`: builds backend package and Touch-N-Stars app bundle
- `scripts/install-live.sh`: installs the built artifacts into the live Pi environment
- `scripts/smoke-test.sh`: verifies the live backend by capturing frames and generating artifacts

## Build

```bash
./scripts/build-plugin.sh
```

This creates:

- `artifacts/backend-plugin/PINS AllSky`
- `artifacts/touch-n-stars-app`

## Install On The Pi

```bash
./scripts/install-live.sh
```

Add `--restart-pins` if you want the script to restart `/home/pi/pins/NINA` after installation.

## Verification

```bash
./scripts/smoke-test.sh
```

The smoke test:

1. updates the backend config for a short capture interval
2. starts a manual session
3. waits for multiple frames to be captured
4. stops the session
5. waits for timelapse, keogram, and startrail outputs

## Notes

- `rpicam-still` rotation is limited to `0` or `180`.
- On an equatorial mount, startrail generation is still available but the output may not show classic circular trails.
- The bundled `keogram` and `startrails` tools come from AllSky and are documented in `THIRD_PARTY_NOTICES.md`.
