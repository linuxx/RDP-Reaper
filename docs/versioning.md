# Versioning scheme (v1.0)

We use SemVer with a v1.0 baseline and git tags for releases.

## Version format

`MAJOR.MINOR.PATCH`

- MAJOR: breaking changes (v2.0+)
- MINOR: new features, backwards compatible (1.1, 1.2, ...)
- PATCH: fixes and small improvements (1.0.1, 1.0.2, ...)

## Pre-release and build metadata

- Pre-release builds may use `-alpha.N`, `-beta.N`, or `-rc.N`
- Optional build metadata may be appended as `+yyyyMMdd.HHmm`

Examples:

- `1.0.0-alpha.1`
- `1.0.0-rc.2`
- `1.0.1+20260222.1145`

## Source of truth

- `VERSION` file tracks the current intended release version.
- Git tags use the `v` prefix (e.g., `v1.0.0`).

## Assembly versions

- AssemblyVersion: `MAJOR.0.0.0`
- FileVersion: `MAJOR.MINOR.PATCH.0`
- InformationalVersion: full SemVer including pre-release or metadata
