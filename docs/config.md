# Configuration

RDP Reaper uses a shared JSON config for the service and GUI.

Default path:
`C:\ProgramData\RdpReaper\config.json`

Example:

```json
{
  "apiListenAddress": "127.0.0.1",
  "apiListenPort": 5055,
  "guiServerAddress": "127.0.0.1",
  "guiServerPort": 5055,
  "databasePath": "C:\\ProgramData\\RdpReaper\\rdp-reaper.db",
  "ipFailureThreshold": 8,
  "ipWindowSeconds": 120,
  "ipBanDurationSeconds": 3600,
  "firewallEnabled": true
}
```

## API secret

The service generates a DPAPI-protected API secret and stores it at:
`HKLM\SOFTWARE\RdpReaper\ApiSecret`

The GUI reads this value (admin required) to authenticate to the localhost API.
