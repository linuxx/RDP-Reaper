# RDP Reaper

RDP Reaper is a Windows Server 2016/2019/2022 service and WinUI 3 GUI that monitors
RDP authentication failures, blocks brute force attempts, and provides local visibility
and control via a localhost-only API.

## Repo layout

- `src/` application source (service, API host, GUI)
- `docs/` architecture and versioning notes
- `Tech Spec.txt` primary requirements

## Configuration

The service and GUI share a JSON config file that holds the localhost binding and
GUI connection settings.

Default location:
`C:\ProgramData\RdpReaper\config.json`

## Secrets

The service generates a machine-based API secret at runtime on first start and
stores it in the registry. The GUI reads this secret and includes it with API calls.
Deleting the registry value causes a new secret to be generated on the next start.

Default registry location:
`HKLM\SOFTWARE\RdpReaper\ApiSecret`

## Versioning

Release versions follow SemVer with a v1.0 baseline. See `docs/versioning.md`.
