# Build
Build the Firmload.demo.sln file, this will both generate binaries
but also create bundles needed to run.


# Configure firmload
1. Launch Firmload.exe
2. Click on settings (cog-wheel upper right corner)
3. Click 'Input devices', under 'Jig' select 'Setup'
4. Select 'Accept connections (server)', click save.
5. Click 'Test settings', under bundles uncheck "...(auto select bundle)"
6. Close the settings dialog

***

# Run demo: Minimal jig
1. Change bundle by clicking on the box icon to the top left, select "Firmload minimal jig"
2. Launch "Virtual.Device.exe" - verify that Firmload now shows a "Connected" text.
3. Run 'Firmload.Minimal.Jig.exe'
4. Close the "Virtual.Device.exe" process


# Run demo: Interactive jig
1. Change bundle by clicking on the box icon to the top left, select "Firmload interactive jig"
2. Launch "Virtual.Device.exe" - verify that Firmload now shows a "Connected" text.
3. Run 'Firmload.Interactive.Jig.exe'
4. Close the "Virtual.Device.exe" process