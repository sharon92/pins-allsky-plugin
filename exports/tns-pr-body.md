## Summary

Adds a `pins-allsky` Touch-N-Stars frontend plugin for the companion `PINS AllSky` backend on Linux/PINS.

## Backend Install After This Frontend Is Merged

Once this frontend is part of a normal TNS release, a user only needs to install the backend plugin on the Pi:

```bash
cd /home/pi
git clone https://github.com/sharon92/pins-allsky-plugin.git
cd pins-allsky-plugin
./scripts/install-backend-plugin.sh --restart-pins
```

This installs `PINS AllSky` into `~/.local/share/NINA/Plugins/3.0.0/PINS AllSky` and restarts PINS.

## Use

1. Update to a TNS release that includes this merged frontend plugin.
2. Run the backend install commands above once on the Pi.
3. Open Touch-N-Stars and go to `AllSky Capture`.
4. Save settings and start capture manually or with `Auto-start with sequence`.

## Review Notes

- Frontend only. The backend remains in: https://github.com/sharon92/pins-allsky-plugin
- The frontend expects the local backend API on `127.0.0.1:19091`.
- Camera mode fields (`metering`, `awb`, `denoise`) use constrained selects from `rpicam-still`.
- Session management is included in the UI: per-session delete, delete-all, storage usage, and an optional max storage cap.
