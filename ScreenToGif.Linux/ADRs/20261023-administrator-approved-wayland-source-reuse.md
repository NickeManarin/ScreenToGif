# Administrator-approved Wayland source reuse

Date: 2026-10-23

Status: Accepted

Scope: Recorder, Options, Linux packaging

## Context

The user wants an administrator-protected setting after which capture source selection is automatic. Their chosen order is once-approved source reuse, then investigation of a supported administrator-authorized first-use path, retaining once-approved reuse if that second path is unavailable. The default portal workflow in the [Wayland capture decision](20261022-wayland-recording-through-portal-and-gstreamer.md) asks for a source at each recording.

The ScreenCast portal supports persistent grants and single-use restore tokens. Its host registry associates a connection with the installed desktop-file identifier. GNOME restores valid source data before considering its picker. The public API does not document an administrator policy that supplies the first source; administrative approval of an app setting does not create a portal source grant.

## Decision

Keep the GUI and capture process unprivileged. A fixed-purpose, root-owned helper changes one administrator grant for the invoking desktop UID under `/var/lib/screentogif/source-reuse/`. Options invokes it through `pkexec` with a fixed-path polkit action requiring `auth_admin` without retained authorization; only `enable` and `disable` are accepted, and authentication cancellation preserves the grant. The helper never reads user configuration or accepts arbitrary paths. Debian packaging installs it with root ownership and removes administrator grants on purge.

With that grant enabled, request `persist_mode=2` on portals supporting version 4 or later. Register the `screentogif` desktop-file identity before portal calls, handling unavailable host registration gracefully. The first source still needs desktop approval. Consume an existing restore token atomically so concurrent starts cannot reuse it, and save a returned replacement only after native preparation succeeds and administrator permission remains enabled. Store tokens in a separate user-private file with mode 0600. Cancellation or token-file failure may require fresh source selection; recording never depends on token storage succeeding. Tokens are never included in logs, screenshots, settings exports or PR evidence.

Options offers forgetting the remembered source without administrator authentication. Policy changes apply to future recordings, preserving the active session's source ownership. A revoked grant or unavailable source follows the ordinary portal picker. The once-approved-source option is the fallback specified by the user; no first-use automation support is claimed without a supported platform mechanism and live proof.

## Alternatives considered

- A user-editable Boolean after an authentication prompt would allow enabling reuse through a settings edit, contrary to the requested administrator control.
- Running the GUI as root or adding a privileged capture daemon would expand privileges and duplicate capture ownership. The helper changes only the administrator policy.
- Writing GNOME-private restore data or weakening compositor checks would depend on an unsupported permission mechanism. The supported portal grant remains the source of capture authority.

## Consequences

The optional setting needs the installed helper, `pkexec`, a desktop authentication agent and portal persistence support. Unsupported installations explain why the control is unavailable. Ordinary recording remains available with fresh consent. The same token mechanism works with whatever selected source the desktop can legitimately restore; it makes no claim that a missing or changed window can be recovered automatically.

Enforced by `ScreenCastPortalTests`, `ScreenCastRestoreTokenStoreTests`, the package acceptance helper checks, and native administrator/consent verification. Native persistence and authentication evidence remain required for feature acceptance; compilation and controlled fixtures alone do not prove them.

## Sources

- [ScreenCast persistence](https://flatpak.github.io/xdg-desktop-portal/docs/doc-org.freedesktop.portal.ScreenCast.html)
- [Host application registry](https://github.com/flatpak/xdg-desktop-portal/blob/main/data/org.freedesktop.host.portal.Registry.xml)
- [GNOME source restoration and picker fallback](https://github.com/GNOME/xdg-desktop-portal-gnome/blob/main/src/screencast.c)
- [pkexec authentication and invoking UID](https://www.freedesktop.org/software/polkit/docs/latest/pkexec.1.html)
