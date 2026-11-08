# HttpFromScratch

Practice project: an HTTP/1.1 server on a userspace TCP/IP stack, built from scratch. The goal is learning, so
correctness and understanding matter more than performance or features. Full plan: `docs/implementation-plan.md`.
Learning topics with links: `docs/topics-to-learn.md`. Original: `docs/http-server-from-scratch.docx`.

## How Claude assists

- The developer writes the code. Claude's role: research, explaining topics, putting them in the context of this
  application, and helping when the developer hits a dead end.
- **Never write or edit project code.** Hints, explanations, pseudocode-level guidance and pointing at bugs are fine.
- If asked to write code, ask for confirmation first, then proceed.

## Current status

- Phase 0. Projects from the structure below exist, mostly empty. `Common` has a debug-only `Assert`. The compile-time
  device wiring in `App` is not set up yet.
- Development is on Windows for now; the project is not yet set up for Linux. WSL2 with Arch Linux is ready, Rider still
  needs to be installed there. The developer will do that later.

## Key decisions

- **C# (.NET 11, C# 15)**, written simply enough that a later port to C is mostly mechanical.
- **Always publishable as Native AOT.**
- **Boundary is a TUN device**: the kernel hands raw IP packets to a fd; the project implements IPv4, ICMP, TCP, HTTP.
  TAP (Ethernet + ARP) is an optional later step.
- **Linux only for now, built with cross-platform in mind** (run in WSL2). A Windows backend (Wintun) may come later;
  only the device layer would differ: device open/read/write, the event-loop wait (`poll` vs Wintun's read-wait event +
  `WaitForSingleObject`), setup commands and test tooling (`tc netem` has no Windows equivalent).
- The stack is userspace, so the HTTP server calls the stack's own API, not `socket()`.
- TCP checksum needs the IP pseudo-header, so TCP code needs src/dst IPs.

## Project structure

All projects live in `src/`.

- `Common`: shared helpers, e.g. `Assert` (`[Conditional("DEBUG")]`, calls are removed from Release builds).
- `Stack`: buffers, checksum, `NetIf` contract, event loop, IPv4, ICMP, TCP, socket-like API. IP and TCP stay in one
  project (folders) because they depend on each other (protocol dispatch, sending, pseudo-header).
- `Http`: depends only on the socket-like API.
- `Device.Linux`: TUN implementation of `NetIf`. `Device.Windows` (Wintun) later.
- `App` (exe): wires everything. The device is chosen at compile time: conditional `ProjectReference` with
  `$([MSBuild]::IsOSPlatform('Linux'))` plus a `LINUX`/`WINDOWS` define, and a single `#if` where `App` creates the
  device. Build OS = target OS anyway, since Native AOT can't cross-compile between OSes.

## Build settings

- `Directory.Build.props` holds the shared settings: `net11.0`, C# 15, nullable, implicit usings, `IsAotCompatible`
  (trim/AOT/single-file analyzers) and warnings as errors, so any IL2xxx/IL3xxx warning fails the normal build.
- csproj files keep only what is project-specific. `App` adds `PublishAot` and `InvariantGlobalization`.
- Analyzers don't catch everything; the full check is a real publish in WSL:
  `dotnet publish src/App -c Release -r linux-x64` (needs `clang` and `zlib` on Arch). Set `TrimmerSingleWarn=false`
  to see individual warnings instead of one per assembly.

## Architecture constraints

- **`NetIf`**: `send_ip(dst_ip, packet)` + receive callback. IP code never writes to the device directly (keeps TAP and
  Windows cheap to add).
- **Single-threaded event loop**; the wait timeout drives timers. The wait itself ("block until readable or N ms")
  belongs to the device layer (`poll` on the TUN fd on Linux). No async/await or threads in the TCP state machine.
- Plain structs, explicit buffers, state machines; no LINQ/heavy interfaces in hot paths.
- Packet buffer with headroom for prepending headers; `ArrayPool<byte>` or preallocated buffers.
- Parsing via `Span<byte>` + `BinaryPrimitives` big-endian reads/writes.
- Tests are external shell scripts (`ping`, `nc`, `curl`, `tc netem` against `10.0.0.2`), reusable for a C version.

## Native interop

`[LibraryImport("libc", SetLastError = true)]` for `ioctl` (TUNSETIFF, the only call with no managed equivalent),
`poll`, and optionally `open/read/write/close` (preferred over FileStream, whose buffering can merge/split packets).

- `struct ifreq`: 40 bytes, name (16 bytes) at offset 0, `short flags` at 16.
  `[StructLayout(LayoutKind.Explicit, Size = 40)]`.
- `TUNSETIFF = 0x400454ca`, `IFF_TUN = 0x0001`, `IFF_TAP = 0x0002`, `IFF_NO_PI = 0x1000` (verify against
  `<linux/if_tun.h>`).
- Random ISS: `RandomNumberGenerator`.

## Network setup

Kernel `10.0.0.1`, stack `10.0.0.2`. Persistent device (no root per run):

```
sudo ip tuntap add dev tun0 mode tun user $USER
sudo ip addr add 10.0.0.1/24 dev tun0
sudo ip link set tun0 up
```

## Phases

0. Groundwork: hexdump, RFC 1071 checksum, tcpdump/Wireshark
1. TUN device read loop (`ping` prints packets)
2. Packet buffer, `NetIf`, poll loop
3. IPv4 in/out
4. ICMP echo (`ping` replies)
5. TCP parsing + RST for closed ports
6. TCB + listener table
7. Handshake
8. Receive
9. Send (echo server)
10. Retransmission (`netem loss 10%`)
11. Teardown
12. Socket-like API
13. HTTP/1.1 (`curl`, browser)
14. Optional: OOO reassembly, zero-window probe, delayed ACK, Nagle, congestion control, TAP + ARP

Phases 7-11 take most of the time.

**Scope limits:** no congestion control, basic retransmission, drop out-of-order segments and IP fragments.

## References

RFC 791, 792, 1071, 9293, 6298, 9112; https://docs.kernel.org/networking/tuntap.html; Sami Niiranen's "Let's code a
TCP/IP stack"; TCP/IP Illustrated Vol. 1; Beej's Guide.
