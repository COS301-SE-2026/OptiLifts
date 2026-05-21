# Repository Notes

## Release Versioning

Releases are created automatically when a pull request is merged into `main`.

The workflow reads the PR title and body to decide the bump:

- `main release`, `major release`, `release type: main`, or `breaking` bumps the release by `1`.
- `minor release`, `release type: minor`, or no explicit main-release marker bumps the release by `0.1`.

Release tags use the format `v<major>.<minor>`, for example `v1.0`, `v1.1`, and `v2.0`.