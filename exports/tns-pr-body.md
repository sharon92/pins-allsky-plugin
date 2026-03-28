## Summary

Adds a `pins-allsky` Touch-N-Stars frontend plugin for Pi HQ camera side-channel capture on PINS.

The plugin provides:
- manual AllSky capture control
- timelapse, keogram, and startrails session management
- storage/status visibility and cleanup actions
- sequence-aware automation when the existing Advanced API plugin is available

## Backend Install

After this frontend ships in a normal TNS release, install the backend once on the Pi:

```bash
cd /home/pi
git clone https://github.com/sharon92/pins-allsky-plugin.git
cd pins-allsky-plugin
./scripts/install-backend-plugin.sh --restart-pins
```

This installs `PINS AllSky` into `~/.local/share/NINA/Plugins/3.0.0/PINS AllSky` and restarts PINS.

## Advanced API Dependency

This PR does not replace the existing Advanced API plugin.

Runtime split:
- this PR: TNS frontend plugin
- companion backend: `PINS AllSky` on `127.0.0.1:19091`
- existing Advanced API plugin: used only for sequence state detection / `Auto-start with sequence`

Manual AllSky capture and product generation use the companion `PINS AllSky` backend directly.
The existing Advanced API plugin is only needed for sequence-aware automation.

## Use After Merge

1. Update to a TNS release that includes this merged frontend plugin.
2. Run the backend install commands above once on the Pi.
3. Open Touch-N-Stars and go to `AllSky Capture`.
4. Configure `Automation`, `Camera`, and `Outputs`.
5. Start capture manually, or enable `Auto-start with sequence` if Advanced API is installed/enabled.

## Tested

- `npm run format`
- `npm run lint:fix`
- `npm run testbuild`
- manual verification against a live PINS/TNS setup on Raspberry Pi 5 with Pi HQ camera

## Notes

- Frontend only. Backend implementation stays in: https://github.com/sharon92/pins-allsky-plugin
- Review focus here should be the TNS plugin integration, i18n usage, and UI flow.
