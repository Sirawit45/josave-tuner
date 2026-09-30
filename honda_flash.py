#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
JOSAVE TUNER - Python K-Line Flasher & Diagnostics Suite
Supports Keihin & Shindengen Honda ECUs over FTDI D2XX / Serial COM K-Line
"""

import sys
import os
import time
import struct
import argparse
import json
import re

# ==============================================================================
# CHECKSUM & SEED-KEY CRYPTO ENGINE
# ==============================================================================

def checksum8bit_honda(data: bytes, length: int, start: int = 0) -> int:
    """Honda K-Line 8-bit checksum: 2's complement of sum (0x100 - sum)"""
    total = sum(data[start : start + length])
    return ((total ^ 0xFF) + 1) & 0xFF

def sum8bit(data: bytes, length: int, start: int = 0) -> int:
    return sum(data[start : start + length]) & 0xFF

def sum32bit(data: bytes, length: int, start: int = 0) -> int:
    total = 0
    for i in range(start, start + length - 3, 4):
        val = struct.unpack('<I', data[i : i + 4])[0]
        total = (total + val) & 0xFFFFFFFF
    return total

def validate_packet_checksum(packet: bytes) -> bool:
    if len(packet) < 2:
        return False
    expected = checksum8bit_honda(packet, len(packet) - 1, 0)
    return expected == packet[-1]

def ar6(b: int) -> int:
    high = (b >> 4) & 0x0F
    low = b & 0x0F

    high_map = {3: 11, 4: 12, 5: 13, 6: 14, 11: 3, 12: 4, 13: 5, 14: 6}
    if high in high_map:
        high = high_map[high]

    low_map = {0: 6, 1: 5, 2: 4, 4: 10, 5: 9, 6: 8, 8: 14, 9: 13, 10: 12, 12: 2, 13: 1, 14: 0}
    if low in low_map:
        low = low_map[low]

    return ((high << 4) | low) & 0xFF

def arg_transform(data: bytearray, param0: int, param1: int) -> bytearray:
    res = bytearray(len(data))
    for i in range(len(data)):
        high = (((data[i] >> 4) + param0) << 4) & 0xF0
        low = ((data[i] % 16) + param1) % 16
        v1 = ar6((high + low) & 0xFF)
        if v1 == 15:
            v2 = 255
        elif v1 in (31, 47, 63, 79, 95, 111, 127, 143, 159, 175, 191, 207, 223, 239):
            v2 = v1 - 16
        elif v1 == 255:
            v2 = 255
        else:
            v2 = (v1 - 16) & 0xFF
        res[i] = v2
    return res

def get_shindengen_write_key(bin_bytes: bytes, seed_bytes: bytes):
    sig = bytes([0x11, 0x12, 0x13, 0x14, 0x15, 0x15, 0x17])
    pos = bin_bytes.find(sig)
    if pos == -1:
        return None
    loc2 = pos + len(sig)
    while loc2 < len(bin_bytes) - 10:
        if bin_bytes[loc2 + 8] == 0xEE and bin_bytes[loc2 + 9] in (0x00, 0xFF):
            seed_val = (seed_bytes[0] << 8) | seed_bytes[1]
            v5 = bin_bytes[loc2] | (bin_bytes[loc2 + 1] << 8)
            v6 = bin_bytes[loc2 + 2] | (bin_bytes[loc2 + 3] << 8)
            v7 = bin_bytes[loc2 + 4] | (bin_bytes[loc2 + 5] << 8)

            v8 = (v6 * (seed_val + v5)) & 0xFFFF
            if v7 > 0:
                key_val = (v8 + (seed_val % v7)) & 0xFFFF
            else:
                key_val = (v8 + seed_val) & 0xFFFF
            return bytes([(key_val >> 8) & 0xFF, key_val & 0xFF])
        loc2 += 1
    return None

def get_shindengen_recovery_key(bin_bytes: bytes, seed_bytes: bytes):
    sig = bytes([0x91, 0x91, 0x0D, 0x00, 0x9E, 0x8D])
    pos = bin_bytes.find(sig)
    if pos < 6:
        return None
    loc0 = pos - 6
    loc2 = bin_bytes[loc0] | (bin_bytes[loc0 + 1] << 8)
    loc3 = bin_bytes[loc0 + 2] | (bin_bytes[loc0 + 3] << 8)
    loc4 = bin_bytes[loc0 + 4] | (bin_bytes[loc0 + 5] << 8)

    seed_val = (seed_bytes[0] << 8) | seed_bytes[1]
    v5 = (loc3 * (seed_val + loc2)) & 0xFFFF
    if loc4 > 0:
        key_val = (v5 + (seed_val % loc4)) & 0xFFFF
    else:
        key_val = (v5 + seed_val) & 0xFFFF
    return bytes([(key_val >> 8) & 0xFF, key_val & 0xFF])

def fix_keihin_checksum(bin_bytes: bytearray, checksum_offset: int):
    if checksum_offset == 0:
        s = sum8bit(bin_bytes, len(bin_bytes) - 1, 1)
    else:
        s1 = sum8bit(bin_bytes, checksum_offset, 0)
        s2 = sum8bit(bin_bytes, len(bin_bytes) - (checksum_offset + 1), checksum_offset + 1)
        s = (s1 + s2) & 0xFF
    bin_bytes[checksum_offset] = (-s) & 0xFF

def fix_shindengen_checksum(bin_bytes: bytearray) -> int:
    sig = bytes([0x11, 0x12, 0x13, 0x14, 0x15, 0x15, 0x17])
    pos = bin_bytes.find(sig)
    if pos == -1:
        return -1
    loc2 = pos + len(sig)
    found_loc = -1
    while loc2 < len(bin_bytes) - 6:
        if bin_bytes[loc2] == 0xD0 and bin_bytes[loc2 + 1] == 0x07:
            found_loc = loc2
            if (loc2 & 3) != 0:
                found_loc = loc2 - (loc2 % 4)
            break
        loc2 += 1

    if found_loc < 4:
        return -1

    ck_offset = found_loc - 4
    s1 = sum32bit(bin_bytes, ck_offset, 0)
    s2 = sum32bit(bin_bytes, len(bin_bytes) - found_loc, found_loc)
    total_sum = (s1 + s2) & 0xFFFFFFFF
    final_ck = (0 - total_sum) & 0xFFFFFFFF
    ck_bytes = struct.pack('<I', final_ck)
    bin_bytes[ck_offset : ck_offset + 4] = ck_bytes
    return ck_offset

# ==============================================================================
# ECU DATABASE LOADER
# ==============================================================================

def load_database():
    db_path = os.path.join(os.path.dirname(__file__), 'ecm_database.json')
    if os.path.exists(db_path):
        with open(db_path, 'r', encoding='utf-8') as f:
            return json.load(f)
    return {'keihin': [], 'shindengen': []}

def detect_ecu_type(bin_bytes: bytes) -> str:
    if b'SV850' in bin_bytes:
        return 'ShindengenV850'
    if b'SH850' in bin_bytes:
        return 'ShindengenRH850'
    if bytes([0x11, 0x12, 0x13, 0x14, 0x15, 0x15, 0x17]) in bin_bytes:
        return 'ShindengenRH850'
    if bytes([0xFF, 0xFF, 0xAA, 0xAA]) in bin_bytes:
        return 'KeihinH8C'
    if bytes([0xFF, 0xFF, 0xFF, 0xAA, 0xAA, 0xAA, 0xAA]) in bin_bytes:
        return 'KeihinM32R'
    return 'KeihinOther'

# ==============================================================================
# AUXILIARY TOOLS: TUNERPRO UNLOCKER
# ==============================================================================

def rc4_crypt(key: bytes, data: bytes) -> bytes:
    S = list(range(256))
    j = 0
    out = bytearray(len(data))
    for i in range(256):
        j = (j + S[i] + key[i % len(key)]) % 256
        S[i], S[j] = S[j], S[i]
    i = j = 0
    for x in range(len(data)):
        i = (i + 1) % 256
        j = (j + S[i]) % 256
        S[i], S[j] = S[j], S[i]
        out[x] = data[x] ^ S[(S[i] + S[j]) % 256]
    return bytes(out)

def unlock_tunerpro_file(file_path: str, out_path: str = None) -> bool:
    import hashlib
    with open(file_path, 'rb') as f:
        file_bytes = f.read()

    master_key = b'$9Ad0$!#*)1QqPow^'
    key_hash = hashlib.md5(master_key).digest()
    decrypted = rc4_crypt(key_hash, file_bytes)

    try:
        text = decrypted.decode('utf-8', errors='replace').replace('\r', '')
    except:
        text = file_bytes.decode('utf-8', errors='replace').replace('\r', '')

    if 'ADXHEADER' not in text and 'XDFHEADER' not in text:
        text = file_bytes.decode('utf-8', errors='replace').replace('\r', '')
        if 'ADXHEADER' not in text and 'XDFHEADER' not in text:
            print('[ERROR] Not a recognized TunerPro XDF/ADX file.')
            return False

    # Extract passwords
    op_m = re.search(r'<openpassword>(.*?)</openpassword>', text)
    open_pass = op_m.group(1).strip() if op_m else 'None'

    mp_m = re.search(r'<modifypassword>(.*?)</modifypassword>', text)
    mod_pass = mp_m.group(1).strip() if mp_m else 'None'

    # Clean
    cleaned = re.sub(r'<openpassword>.*?</openpassword>', '', text)
    cleaned = re.sub(r'<modifypassword>.*?</modifypassword>', '', cleaned)

    if file_path.lower().endswith('.adx'):
        cleaned = re.sub(r'<flags>0x[0-9a-fA-F]+</flags>', '<flags>0x1</flags>', cleaned)
    else:
        cleaned = re.sub(r'<flags>0x[0-9a-fA-F]+</flags>', '<flags>0x10000</flags>', cleaned)

    if not out_path:
        base, ext = os.path.splitext(file_path)
        out_path = base + '_unlocked' + ext

    with open(out_path, 'w', encoding='utf-8') as f:
        f.write(cleaned)

    print('=' * 60)
    print('  [SUCCESS] TunerPro File Unlocked!')
    print('  Open Password   :', open_pass)
    print('  Modify Password :', mod_pass)
    print('  Saved Cleaned   :', out_path)
    print('=' * 60)
    return True

# ==============================================================================
# CLI DISPATCHER
# ==============================================================================

def main():
    parser = argparse.ArgumentParser(description='JOSAVE TUNER - Python K-Line Flasher & Diagnostics')
    subparsers = parser.add_subparsers(dest='command', help='Commands')

    # info
    p_info = subparsers.add_parser('info', help='Query ECM ID and info')
    p_info.add_argument('--port', default='COM1', help='Serial port')

    # read (dump)
    p_read = subparsers.add_parser('read', help='Read/Dump ROM from ECM')
    p_read.add_argument('output', help='Output .bin file path')
    p_read.add_argument('--size', type=int, default=64, help='ROM size in KB')
    p_read.add_argument('--port', default='COM1', help='Serial port')

    # clear-dtc
    p_dtc = subparsers.add_parser('clear-dtc', help='Clear DTC trouble codes')
    p_dtc.add_argument('--port', default='COM1', help='Serial port')

    # reset-ecm
    p_rec = subparsers.add_parser('reset-ecm', help='Reset ECM adaptations')
    p_rec.add_argument('--port', default='COM1', help='Serial port')

    # fix-checksum
    p_ck = subparsers.add_parser('fix-checksum', help='Fix binary checksum')
    p_ck.add_argument('file', help='Path to .bin file')

    # unlock-xdf
    p_xdf = subparsers.add_parser('unlock-xdf', help='Unlock TunerPro XDF/ADX file')
    p_xdf.add_argument('file', help='Path to .xdf or .adx file')
    p_xdf.add_argument('output', nargs='?', help='Output path')

    # list-db
    p_db = subparsers.add_parser('list-db', help='List database models')
    p_db.add_argument('--search', default='', help='Search keyword')

    args = parser.parse_args()

    if args.command == 'fix-checksum':
        with open(args.file, 'rb') as f:
            data = bytearray(f.read())
        ecu = detect_ecu_type(bytes(data))
        print(f'Detected Architecture: {ecu}')
        if 'Shindengen' in ecu:
            off = fix_shindengen_checksum(data)
            if off >= 0:
                out_p = os.path.splitext(args.file)[0] + '_cksum.bin'
                with open(out_p, 'wb') as f:
                    f.write(data)
                print(f'[SUCCESS] Shindengen 32-bit checksum corrected at 0x{off:X} -> {out_p}')
        else:
            fix_keihin_checksum(data, 0x7600 if len(data) == 0x10000 else 0x3FFF8)
            out_p = os.path.splitext(args.file)[0] + '_cksum.bin'
            with open(out_p, 'wb') as f:
                f.write(data)
            print(f'[SUCCESS] Keihin checksum corrected -> {out_p}')

    elif args.command == 'unlock-xdf':
        unlock_tunerpro_file(args.file, args.output)

    elif args.command == 'list-db':
        db = load_database()
        q = (args.search or '').lower()
        print('=' * 75)
        print(f"JOSAVE TUNER - SUPPORTED MODELS")
        print('=' * 75)

        for cat in ['shindengen', 'keihin']:
            records = db.get(cat, [])
            filtered = [r for r in records if not q or q in r.get('model_name', '').lower() or q in r.get('part_code', '').lower() or q in r.get('ecm_id', '').lower()]
            if filtered:
                print(f"\n--- {cat.upper()} ({len(filtered)} models) ---")
                for r in filtered:
                    print(f"  {r.get('model_name', 'Generic'):<35} | Part: {r['part_code']:<18} | ID: {r['ecm_id']}")

    else:
        parser.print_help()

if __name__ == '__main__':
    main()
