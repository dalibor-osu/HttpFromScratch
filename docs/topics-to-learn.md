# Topics to learn

Only the first group is really needed before starting. The rest can be learned when you reach the corresponding phase of
the [implementation plan](implementation-plan.md).

## Before starting

### 1. Layered model and encapsulation

Link, IP, transport, application, and how each layer wraps the next as header + payload. This is the mental model for
the whole project.

*Links:* [Internet protocol suite (Wikipedia)](https://en.wikipedia.org/wiki/Internet_protocol_suite), [Networking! ACK! zine](https://wizardzines.com/zines/networking/)

### 2. IP addressing basics

What `10.0.0.2/24` means, subnets, what a route is, why the kernel sends packets for `10.0.0.2` into `tun0`.

*Links:* [CIDR (Wikipedia)](https://en.wikipedia.org/wiki/Classless_Inter-Domain_Routing), [Bite Size Networking zine](https://wizardzines.com/zines/bite-size-networking/)

### 3. Linux network tooling

`ip addr`, `ip link`, `ip route`, `ss -tan`, `nc`, `curl -v`. Enough to bring an interface up and see connections.

*Links:* [ip (8)](https://man7.org/linux/man-pages/man8/ip.8.html), [ip-tuntap (8)](https://man7.org/linux/man-pages/man8/ip-tuntap.8.html), [ss (8)](https://man7.org/linux/man-pages/man8/ss.8.html), [curl (1)](https://curl.se/docs/manpage.html)

### 4. TUN/TAP concept

A virtual interface where the "wire" is a file descriptor in your process. The kernel thinks it is sending to a network
card, you receive the bytes.

*Links:* [Universal TUN/TAP device driver (kernel docs)](https://docs.kernel.org/networking/tuntap.html)

### 5. Packet capture

`tcpdump -i <iface> -n` and Wireshark: start a capture, filter (`icmp`, `tcp.port == 80`), expand a packet's header
tree, follow a TCP stream. One evening capturing a `curl` to a real site covers it.

*Links:* [tcpdump (1)](https://www.tcpdump.org/manpages/tcpdump.1.html), [Wireshark User's Guide](https://www.wireshark.org/docs/wsug_html_chunked/), [Wireshark display filters](https://wiki.wireshark.org/DisplayFilters)

## Before phases 1-3 (device, IPv4)

### 6. File descriptors and syscalls in practice

`open`, `read`, `write`, `ioctl`, `poll`, `errno`.

*Links:* [open (2)](https://man7.org/linux/man-pages/man2/open.2.html), [read (2)](https://man7.org/linux/man-pages/man2/read.2.html), [write (2)](https://man7.org/linux/man-pages/man2/write.2.html), [ioctl (2)](https://man7.org/linux/man-pages/man2/ioctl.2.html), [poll (2)](https://man7.org/linux/man-pages/man2/poll.2.html), [errno (3)](https://man7.org/linux/man-pages/man3/errno.3.html)

### 7. Byte order

Big-endian on the wire, little-endian on x86, and why reading a 16-bit value straight from a buffer gives the wrong
value.

*Links:* [Endianness (Wikipedia)](https://en.wikipedia.org/wiki/Endianness)

### 8. Binary header parsing

Fixed layouts, bit fields (IHL, flags), and the Internet checksum.

*Links:* [RFC 791 section 3.1 (IPv4 header)](https://www.rfc-editor.org/rfc/rfc791#section-3.1), [RFC 1071 (computing the checksum)](https://www.rfc-editor.org/rfc/rfc1071)

### 9. Reading RFCs

How the header diagrams and MUST/SHOULD language work. Practice on RFC 791 section 3.1.

*Links:* [RFC 2119 (MUST/SHOULD keywords)](https://www.rfc-editor.org/rfc/rfc2119), [RFC 791](https://www.rfc-editor.org/rfc/rfc791)

## Before phases 5-11 (TCP)

### 10. TCP conceptually

Handshake, sequence and ack numbers as byte offsets, sliding window, retransmission, the state diagram. Learn it from a
book or video before opening RFC 9293.

*Links:* [Transmission Control Protocol (Wikipedia)](https://en.wikipedia.org/wiki/Transmission_Control_Protocol), [RFC 9293](https://www.rfc-editor.org/rfc/rfc9293), [RFC 6298 (retransmission timer)](https://www.rfc-editor.org/rfc/rfc6298)

### 11. Timers in an event loop

Driving timeouts from `poll`'s timeout argument.

*Links:* [poll (2)](https://man7.org/linux/man-pages/man2/poll.2.html), [timerfd_create (2)](https://man7.org/linux/man-pages/man2/timerfd_create.2.html)

## Before phase 13 (HTTP)

### 12. HTTP/1.1 message format

Request line, headers, CRLF rules, `Content-Length`, keep-alive. Look at raw traffic with `curl -v` or `nc` against a
real server.

*Links:* [HTTP messages (MDN)](https://developer.mozilla.org/en-US/docs/Web/HTTP/Guides/Messages), [RFC 9112 (HTTP/1.1)](https://www.rfc-editor.org/rfc/rfc9112)

## Resources

- [**Beej's Guide to Network Programming**](https://beej.us/guide/bgnet/): sockets from the user side, grounding for the
  basics.
- **Julia Evans' networking zines and blog** ([wizardzines.com](https://wizardzines.com/), [jvns.ca](https://jvns.ca/)):
  fast, intuitive intros to tcpdump, TCP and Linux networking.
- [**TCP/IP Illustrated, Vol. 1 (
  Fall/Stevens)**](https://www.informit.com/store/tcp-ip-illustrated-volume-1-the-protocols-9780321336316): reference
  for layering, IP and TCP. Read chapters as needed.
- [**Wireshark User's Guide**](https://www.wireshark.org/docs/wsug_html_chunked/): "Capturing" and "Display filters"
  chapters.
-

**RFCs:** [791 (IPv4)](https://www.rfc-editor.org/rfc/rfc791), [792 (ICMP)](https://www.rfc-editor.org/rfc/rfc792), [9293 (TCP)](https://www.rfc-editor.org/rfc/rfc9293), [9112 (HTTP/1.1)](https://www.rfc-editor.org/rfc/rfc9112).

- [**Sami Niiranen, "Let's code a TCP/IP stack"**](https://www.saminiir.com/lets-code-tcp-ip-stack-1-ethernet-arp/):
  blog series implementing a userspace stack over TAP.

## Warm-up exercise

Write a tiny TUN program that just hexdumps packets, ping it, and match each byte of the dump to the fields Wireshark
shows for the same packet. Covers most of the "before starting" group in one go.
