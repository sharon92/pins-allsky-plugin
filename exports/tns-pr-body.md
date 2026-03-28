## Summary

Adds a `pins-allsky` Touch-N-Stars frontend plugin for the companion `PINS AllSky` backend on Linux/PINS. The UI controls the Pi HQ camera during a normal session and renders timelapse, keogram, and startrails products.

## Backend Install

After this frontend ships in a normal TNS release, install the backend once on the Pi:

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
4. Configure `Automation`, `Camera`, and `Outputs` from the compact modal actions in the header.
5. Start capture manually or enable `Auto-start with sequence`.
6. Use `Show Status` to inspect dependencies, storage, and trigger a backend self-update after the backend repo is installed.

## Notes

- Frontend only. The backend stays in: https://github.com/sharon92/pins-allsky-plugin
- The frontend expects the local backend API on `127.0.0.1:19091`.
- Review focus should be the UI/plugin integration in TNS. Backend implementation and installer/update scripts live in the standalone backend repo.
