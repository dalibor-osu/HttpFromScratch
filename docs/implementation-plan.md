# Implementation plan

Userspace TCP/IP stack on Linux via TUN, up to a working HTTP/1.1 server. Kernel side of the TUN link is `10.0.0.1`,
your stack is `10.0.0.2`. Each phase ends with something testable. What to learn for each
phase: [topics to learn](topics-to-learn.md).

## Phase 0: Groundwork

1. Set up the project and build tooling.
2. Write a hexdump helper and the Internet checksum (RFC 1071, one's complement sum). IP, ICMP and TCP all use it.
3. Get comfortable with `tcpdump -i tun0 -X` or Wireshark on `tun0`.

## Phase 1: TUN device

1. Create a persistent device once so you don't need root every run:
   ```
   ip tuntap add dev tun0 mode tun user $USER
   ip addr add 10.0.0.1/24 dev tun0
   ip link set tun0 up
   ```
2. Open `/dev/net/tun`, `ioctl(TUNSETIFF)` with `IFF_TUN | IFF_NO_PI` and name `tun0`.
3. Read loop, hexdump everything.

> **Test:** `ping 10.0.0.2` prints packets.

## Phase 2: Structure

1. Packet buffer type (byte slice + length, headroom for prepending headers on output).
2. `NetIf` abstraction with `send_ip` and a receive callback, so TAP can be added later without touching IP/TCP.
3. Main event loop with `poll` on the TUN fd plus a timeout for timers.

## Phase 3: IPv4

1. Input: check version 4, IHL, total length vs bytes read, header checksum, destination is `10.0.0.2`. Drop fragments
   (MF set or offset nonzero). Dispatch on protocol (1 ICMP, 6 TCP).
2. Output: build header, incrementing ID, TTL 64, DF set, checksum.

## Phase 4: ICMP echo

1. Type 8 in, type 0 out, same id/seq/payload, checksum over the whole ICMP message.

> **Test:** `ping 10.0.0.2` gets replies. First real milestone.

## Phase 5: TCP parsing and RST

1. Parse header, data offset, flags. Parse the MSS option, skip unknown options correctly.
2. Checksum with the pseudo-header (src, dst, zero, protocol, TCP length). Most common early bug.
3. For segments to closed ports, send RST per RFC 9293 rules.

> **Test:** `nc 10.0.0.2 1234` reports "connection refused" immediately instead of hanging.

## Phase 6: Connection state

1. TCB keyed by 4-tuple: state, SND.UNA, SND.NXT, SND.WND, ISS, RCV.NXT, RCV.WND, IRS, send and receive buffers.
2. Listener table (port to accept queue).

## Phase 7: Handshake

1. LISTEN + SYN: create TCB, random ISS, send SYN-ACK with your MSS, go to SYN-RECEIVED.
2. Valid ACK: go to ESTABLISHED, push onto accept queue.

> **Test:** `nc 10.0.0.2 80` connects and stays open.

## Phase 8: Receiving data

1. Segment acceptability check (sequence in window).
2. Accept in-order data only, drop out-of-order (rare on TUN, retransmission covers it).
3. Copy to receive buffer, advance RCV.NXT, send ACK, advertise window from free buffer space.

> **Test:** Typing in `nc` shows up in your program.

## Phase 9: Sending data

1. Send buffer. Segment by min (MSS, peer window).
2. On ACK: advance SND.UNA, free acked bytes, update SND.WND.

> **Test:** Echo server works with `nc`.

## Phase 10: Retransmission

1. One timer per connection on the oldest unacked segment. Fixed 1s RTO first, RFC 6298 RTT estimation later.
2. Exponential backoff, abort after N tries.

> **Test:** `tc qdisc add dev tun0 root netem loss 10%` and the echo server still works.

## Phase 11: Teardown

1. Passive close: FIN in, ESTABLISHED to CLOSE-WAIT, your close sends FIN, LAST-ACK, CLOSED.
2. Active close: FIN-WAIT-1, FIN-WAIT-2, TIME-WAIT (keep it short, a few seconds), plus CLOSING for simultaneous close.
3. Handle incoming RST in every state.

> **Test:** `nc` exits cleanly from both sides; Wireshark shows no stray RSTs.

## Phase 12: Socket-like API

1. `tcp_listen`, `tcp_accept`, `tcp_read`, `tcp_write`, `tcp_close`, driven by the event loop.
2. Rewrite the echo server on top of it. If that is awkward, fix the API now, before HTTP.

## Phase 13: HTTP/1.1

1. Buffer until `\r\n\r\n`, parse request line and headers, read body by `Content-Length`. Reject oversized requests.
2. Respond with status line, `Content-Length`, `Content-Type`. Start with `Connection: close`.
3. Serve static files from a directory, reject `..` in paths.
4. Then add keep-alive (multiple requests per connection).

> **Test:** `curl -v http://10.0.0.2/`, then a browser.

## Phase 14: Optional

Out-of-order reassembly, zero-window probing, many concurrent connections, delayed ACKs, Nagle, congestion control, then
TAP + ARP.

Phases 7 to 11 are where most of the time goes.
