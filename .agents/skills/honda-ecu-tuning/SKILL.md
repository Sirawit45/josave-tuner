---
name: honda-ecu-tuning
description: >-
  Comprehensive guide and domain knowledge for Honda Motorcycle ECU flashing, K-Line (ISO 14230) communication,
  Keihin and Shindengen reverse-engineered protocols, Seed-Key calculations, Checksums, and memory architectures.
---

# Honda Motorcycle ECU Tuning & K-Line Flashing Skill

This skill contains deep domain knowledge for communicating with, dumping (reading), writing (flashing), and diagnostic servicing of Honda motorcycle Electronic Control Modules (ECMs) over the K-Line diagnostic interface.

---

## 1. Physical Hardware & K-Line Protocol

- **Physical Bus**: ISO 9141 / ISO 14230 K-Line, single-wire half-duplex (0V to 12V with 510 $\Omega$ pull-up).
- **Driver**: FTDI FT232R/FT232RL via native D2XX DLL (`ftd2xx.dll` / `FTD2XX_NET.dll`) or standard Serial COM Port (`System.IO.Ports.SerialPort`).
- **Half-Duplex TX Echo**: Every byte transmitted on TX is reflected back on RX. Always consume and discard $N$ bytes of echo where $N$ is the length of the transmitted command before processing the reply.
- **Fast Initialization Sequence (ISO 14230-2)**:
  1. 10,400 baud, 8N1.
  2. Bit-bang 0V (Low) pulse for **70 ms**.
  3. Release line to 12V (High) and restore UART 10,400 baud.
  4. Wait **130 ms** for ECM wakeup.
  5. Send Tester Present `FE 04 72 8C`.

---

## 2. Packet Framing & Checksum Formula

- **Packet Structure**: `[Header] [Length] [Data / Sub-Service...] [Checksum]`
- **Honda Checksum (Two's Complement)**:
  $$\text{Checksum} = ((\sum_{i=0}^{N-1} \text{Byte}_i \oplus 0\text{xFF}) + 1) \ \& \ 0\text{xFF}$$
  $$\text{Checksum} = (0\text{x}100 - (\sum \text{Byte}_i \ \& \ 0\text{xFF})) \ \& \ 0\text{xFF}$$

---

## 3. Core Command Set

- **Tester Present**: `FE 04 72 8C`
- **Read ECM ID**: `72 05 00 F0 99` or `72 07 72 00 00 05 10`
- **Read Flash Count**:
  - Keihin: `7D 06 01 01 03 78`
  - Shindengen: `7B 06 02 01 03 79`
- **Clear DTC**: `72 05 60 01 28`
- **Reset ECM**: `72 05 60 03 26`
- **Reset Flash Counter (Shindengen)**: `7B 08 02 02 50 19 76 9A` followed by `7C 0B 02 0B 00 00 00 FF FF FF 6F`
- **Fast Baud Rate Switching**:
  - Keihin 62,500 baud: `7E 07 01 0E 01 90 DB`
  - Shindengen 62,500 baud: `7C 07 02 0E 01 00 6C`

---

## 4. Seed-Key Security Access Algorithms

### Shindengen Write Key
1. Locate `11 12 13 14 15 15 17` in binary.
2. Find offset with `0xEE` and (`0x00` or `0xFF`) at offset + 8.
3. Extract parameters:
   - $V_5 = \text{LittleEndian}(\text{BIN}[0..1])$
   - $V_6 = \text{LittleEndian}(\text{BIN}[2..3])$
   - $V_7 = \text{LittleEndian}(\text{BIN}[4..5])$
4. Calculate:
   $$\text{SeedValue} = (S_0 \ll 8) \mid S_1$$
   $$V_8 = (V_6 \times (\text{SeedValue} + V_5)) \ \& \ 0\text{xFFFF}$$
   $$\text{KeyValue} = \begin{cases} (V_8 + (\text{SeedValue} \pmod{V_7})) \ \& \ 0\text{xFFFF} & \text{if } V_7 > 0 \\ (V_8 + \text{SeedValue}) \ \& \ 0\text{xFFFF} & \text{if } V_7 = 0 \end{cases}$$
5. Send Key: `7B 07 02 06 [Key_H] [Key_L] [Checksum]`

### Shindengen Recovery Key
- Search signature `91 91 0D 00 9E 8D` at offset + 6.
- Extract parameters $V_5, V_6, V_7$ from 6 bytes prior to signature.

---

## 5. Auto Checksum Correction

- **Keihin 8-bit Sum**:
  $$\text{Sum} = \sum_{i \neq \text{ChecksumOffset}} \text{BIN}[i] \pmod{256}$$
  $$\text{BIN}[\text{ChecksumOffset}] = (-\text{Sum}) \ \& \ 0\text{xFF}$$
- **Shindengen 32-bit Sum**:
  Locate `D0 07` after signature `11 12 13 14 15 15 17`, align to 4 bytes, calculate 32-bit sum before and after checksum slot:
  $$\text{TotalSum} = \sum \text{WordsBefore} + \sum \text{WordsAfter}$$
  $$\text{Checksum32} = (0 - \text{TotalSum}) \ \& \ 0\text{xFFFFFFFF}$$
  Store little-endian 4-byte value at `foundLoc - 4`.

---

## 6. Reading (Dumping) & Writing (Flashing) Blocks

- **Block Size**: Standard 128 bytes per chunk.
- **Write Chunk**: `[Header] [Len] [SubID] [Addr2] [Addr1] [Addr0] [128 Data Bytes...] [Checksum]`
- **Read Chunk**: `[Header] 0x07 [SubID] [Addr2] [Addr1] [Addr0] [Checksum]`
  - Keihin Read Header: `0x7D` (Reply Header `0x7E`)
  - Shindengen Read Header: `0x7B` (Reply Header `0x7C`)
- **Verification**: Always compute file hash and verify against checksum table before and after operations.

---

## 7. Real-Time Telemetry & K-Line Live Data Stream

- **Query Table `00 00`**: `72 07 72 00 00 0D [Checksum]`
- **Telemetry Packet Decoding**:
  - **Engine RPM**: `(Byte[5] << 8) | Byte[6]`
  - **Throttle Position (TPS %)**: `Byte[7] * 100.0 / 255.0`
  - **Throttle Voltage**: `Byte[8] * 5.0 / 255.0` V
  - **Engine Oil / Coolant Temp (EOT/ECT)**: `Byte[9] - 40` °C
  - **Manifold Absolute Pressure (MAP)**: `Byte[10]` kPa
  - **Intake Air Temperature (IAT)**: `Byte[11] - 40` °C
  - **Battery Voltage**: `Byte[12] * 0.1` V (Safe threshold: $\ge 12.0\text{V}$, charging: $13.8 - 14.4\text{V}$)
  - **Fuel Injector Pulse Width**: `((Byte[13] << 8) | Byte[14]) / 1000.0` ms
  - **Ignition Advance (°BTDC)**: `(Byte[15] - 60) / 2.0`
  - **O2 Sensor & Air-Fuel Ratio (AFR)**: `14.7 + (Byte[16] - 128) * 0.03`
  - **Neutral Switch**: `(Byte[17] & 0x01) != 0`
  - **Current Gear**: `(Byte[17] >> 1) & 0x07`

---

## 8. Active Actuator Component Testing

- **Command Format**: `72 05 60 [ActuatorID] [Checksum]`
  - `0x05`: Fuel Injector Pulse Test (clicks injector 5 times)
  - `0x06`: Fuel Pump Relay Test (primes pump for 3 seconds)
  - `0x07`: Tachometer Needle Sweep Test (sweeps instrument cluster)
  - `0x08`: Ignition Spark Coil Test (fires spark plug 3 times)
  - `0x04`: Throttle Position (TP) Sensor Calibration Reset

---

## 9. Thai Motorcycle Remapping Mechanics (สูตรเดินหอบ & ปลดรอบ)

### Idle Chop ("สูตรเดินหอบ" / Lope Cam Sound Mod)
- **Concept**: Emulates an aggressive high-overlap racing camshaft lope at 1,200 - 1,800 RPM closed throttle.
- **Implementation**:
  - Alternating spark retard ($-4^\circ$ to $0^\circ$) and advance ($+18^\circ$ to $+22^\circ$) on adjacent idle timing table cells.
  - Periodic fuel enrichment pulses ($+8\%$ to $+16\%$) to cause controlled combustion stumble and throatiness.
  - Requires automatic recalculation of Keihin 8-bit sum or Shindengen 32-bit checksum.

### RPM Limiter Modifier ("สูตรปลดรอบ / เปิดรอบ")
- **Stock Limit**: Factory rev limiters typically set at $9,200 - 9,800\text{ RPM}$.
- **Target Limit**: $10,000 - 13,000\text{ RPM}$ depending on valve springs and camshaft profile.
- **Search Signature**: 16-bit little-endian and big-endian words corresponding to factory thresholds (e.g. $9500 = \text{0x251C}$, $9800 = \text{0x2648}$).
- **Patch Rule**: Overwrite limiter word with target value, then perform auto checksum correction.

---

## 10. Pre-Flash Safety Checklist & Risk Prevention

1. **Battery Voltage**: Must be $\ge 12.0\text{V}$ (warn user if $< 12.0\text{V}$ to prevent ECM brownout/bricking).
2. **Throttle Closed**: Must be $0.0^\circ$ ($< 0.5\text{V}$).
3. **DTC Diagnostic Faults**: Verify 0 active faults before flashing.
4. **Hardware Cable Link**: Real-time USB Hot-Plug detection (FTDI D2XX + COM).

