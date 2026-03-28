## Summary

This PR adds a new Touch-N-Stars frontend plugin, `pins-allsky`, that controls the companion `PINS AllSky` backend plugin running inside PINS/NINA on Linux.

The goal is to let a Raspberry Pi side camera capture ambient sky frames during a normal imaging session and generate:

- timelapse videos
- keograms
- startrail composites

This frontend PR is only the TNS side. The companion backend/plugin repository is here:

- https://github.com/sharon92/pins-allsky-plugin

## How It Works

- The TNS plugin adds an `AllSky Capture` page.
- That page talks to the local `PINS AllSky` backend REST API on port `19091`.
- The backend plugin manages Pi camera captures with `rpicam-still`.
- Captured frames are stored in per-session folders.
- Timelapse rendering uses `ffmpeg`.
- Keogram and startrail generation reuse the existing AllSky helper tools.

The result is a side-channel sky monitor that can run during normal astrophotography without interfering with the main imaging camera workflow.

## Activation / Setup

For a full working install, both parts are required:

1. Install the `PINS AllSky` backend plugin into the normal PINS/NINA plugin directory.
2. Build Touch-N-Stars with this `src/plugins/pins-allsky` plugin included.
3. Restart PINS.
4. Open Touch-N-Stars and go to `AllSky Capture`.
5. Configure camera/product settings and save them.
6. Either:
   - start manual capture from the UI, or
   - enable auto-start so capture follows the NINA/PINS sequence state.

## End-User Installation And Run Flow

If this frontend PR is merged, the user flow becomes:

1. Install a Touch-N-Stars release that already contains the merged `pins-allsky` frontend plugin.
2. Install the companion `PINS AllSky` backend plugin from the separate backend repository release:
   - https://github.com/sharon92/pins-allsky-plugin
3. Copy the backend plugin folder into the normal PINS/NINA plugin directory on the Pi/Linux host.
4. Restart PINS/NINA.
5. Open Touch-N-Stars and go to `AllSky Capture`.
6. Check `Backend Dependencies` to confirm the runtime is ready.
7. Save camera/product settings.
8. Start a manual capture session or let it auto-start with a running sequence.

In other words:

- after merge, the TNS/frontend part should come from the normal TNS build/release
- the backend part still ships as a separate PINS/NINA plugin and must be installed separately

## Why Backend Install Is Separate

This PR intentionally keeps the TNS frontend plugin separate from backend installation.

The TNS plugin runs as frontend code inside the Touch-N-Stars web app. In the current architecture it does **not** have permission to:

- write files into the PINS/NINA plugin directory
- install backend binaries/tools on the Pi host
- restart the PINS/NINA process after installation

So a first-run "self-install the backend plugin" flow is **not** part of this PR.

That would require an additional privileged installer mechanism in PINS/TNS itself, for example:

- a backend-managed plugin install API
- a packaged installer outside the browser
- or explicit core support for downloading/installing companion plugins

Until such a mechanism exists, the safe and reviewable model is:

- TNS provides the UI
- the companion backend plugin is installed separately on the host

## Review Notes

- TNS plugin discovery is build-time, so this must live under `src/plugins` and be included in the normal TNS build.
- The plugin assumes the backend companion plugin is installed and serving on `http://127.0.0.1:19091`.
- Camera mode fields that map to `rpicam-still` (`metering`, `awb`, `denoise`) are exposed as constrained selects, not free text.
- Field inputs now include tooltips because many settings are backend/tool specific and otherwise not self-explanatory.
- The frontend also handles the backend's `DateTimeOffset` object payloads correctly, so session dates and last-capture timestamps do not show `Invalid Date`.
- Startrail generation remains optional. On equatorial mounts the output may not resemble classic circular fixed-tripod startrails, but some users may still want the composite.

## Suggested Manual Test

1. Install the companion backend plugin.
2. Build and launch TNS with this plugin enabled.
3. Open `AllSky Capture`.
4. Set `Camera -> Interval (s)` to `5`.
5. Save settings.
6. Start a manual session.
7. Confirm that the live preview/session counters advance roughly every 5 seconds.
8. Stop the session and confirm product generation works as expected.

## Notes For Companion Backend Review

The backend repo contains a required config-save fix: EmbedIO camel-case request binding was not persisting nested config updates reliably for this plugin, so the backend now performs case-insensitive raw-body JSON deserialization explicitly. Without that fix, changing `camera.intervalSeconds` in the UI can appear to save while the old values remain active.
