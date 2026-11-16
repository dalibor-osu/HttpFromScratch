#!/usr/bin/env bash
# Note: written by Claude (AI assistant), not by the developer.
# Phase 4 test: ICMP echo replies from the userspace stack.
# The app must already be running (it holds tun0 open).
#
# Usage: Scripts/test-ping.sh [target-ip] [device]

set -uo pipefail

TARGET="${1:-10.0.0.2}"
DEVICE="${2:-tun0}"
COUNT=3
# Payload sizes: empty, odd length (odd-byte checksum path), default, near MTU.
SIZES=(0 1 56 1400)

failures=0

# --- Preconditions ----------------------------------------------------------

if ! link="$(ip link show "$DEVICE" 2>/dev/null)"; then
    echo "FAIL: device $DEVICE does not exist (see Network setup in CLAUDE.md)"
    exit 1
fi

if [[ "$link" != *"<"*UP*">"* ]]; then
    echo "FAIL: device $DEVICE is down (sudo ip link set $DEVICE up)"
    exit 1
fi

# A TUN device has no carrier while no process holds it open.
if [[ "$link" == *NO-CARRIER* ]]; then
    echo "FAIL: $DEVICE has no carrier - is the app running?"
    exit 1
fi

# --- Tests ------------------------------------------------------------------

for size in "${SIZES[@]}"; do
    output="$(ping -n -q -c "$COUNT" -i 0.2 -W 1 -s "$size" "$TARGET" 2>&1)"
    received="$(grep -oE '[0-9]+ received' <<<"$output" | grep -oE '^[0-9]+')"
    received="${received:-0}"

    if [[ "$received" -eq "$COUNT" ]]; then
        echo "PASS: payload $size bytes ($received/$COUNT replies)"
    else
        echo "FAIL: payload $size bytes ($received/$COUNT replies)"
        failures=$((failures + 1))
    fi
done

# --- Summary ----------------------------------------------------------------

echo
if [[ "$failures" -eq 0 ]]; then
    echo "All ${#SIZES[@]} ping tests passed."
    exit 0
fi

echo "$failures of ${#SIZES[@]} ping tests failed."
exit 1
