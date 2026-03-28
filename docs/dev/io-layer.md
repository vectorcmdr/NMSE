# IO Layer

## Overview

The IO layer (`IO/`) handles everything between a save file on disk and the in-memory
`JsonObject` tree the rest of the application works with. This includes platform detection,
LZ4 compression, TEA/XXTEA encryption, binary I/O, and multi-platform file layout management.

### Save File Pipeline

Every NMS save, regardless of platform, ultimately contains the same JSON payload. The
platforms differ only in how they wrap it:

```
Steam / GOG:
  save.hg  -->  [LZ4 magic header]  -->  LZ4 blocks  -->  JSON
  mf_save.hg  -->  TEA-encrypted metadata

Xbox Game Pass:
  containers.index  -->  blob GUIDs  -->  LZ4 blocks  -->  JSON

PlayStation 4:
  memory.dat  -->  slot table  -->  LZ4 chunks  -->  JSON
  (SaveWizard variant has a 20-byte preamble)

Nintendo Switch:
  savedata{NN}.hg  -->  LZ4 blocks  -->  JSON
  manifest{NN}.hg  -->  plaintext metadata
```

**Loading flow:** `SaveFileManager.LoadSaveFile` detects the platform, reads the file,
decompresses LZ4 blocks, decodes using Latin-1 (to preserve binary data), and parses the
resulting string into a `JsonObject`. Context transforms are then registered so that
`PlayerStateData` resolves dynamically based on `ActiveContext`.

**Saving flow:** Panels write changes directly to in-memory `JsonObject` slots.
`SaveFileManager.SaveToFile` serializes the tree back to JSON, compresses with LZ4 if
required, writes the data file, and generates a platform-appropriate meta/companion file.

---

## Classes

### SaveFileManager

| | |
|---|---|
| File | `IO/SaveFileManager.cs` |
| Purpose | High-level save file load/save operations with platform abstraction |

The main entry point for all file operations. Detects platform from file structure, loads
and decompresses data, and writes back with optional compression and metadata.

**Platform detection rules:**
- `containers.index` present -> Xbox Game Pass
- `manifest*.dat` present -> Switch
- `memory.dat` or `savedata*.hg` present -> PS4
- `save*.hg` present -> Steam or GOG (distinguished by directory name)

| Method | Description |
|--------|-------------|
| `DetectPlatform(path)` | Auto-detect save format from file structure |
| `FindDefaultSaveDirectory()` | OS-specific default NMS save location |
| `BackupSaveDirectory(path)` | Create timestamped zip backup (keeps last 10) |
| `LoadSaveFile(path)` | Load, decompress, and parse to `JsonObject` |
| `SaveToFile(path, data, compress, writeMeta, platform, slot)` | Serialize, compress, write data + meta |
| `LoadXboxSave(dir, identifier)` | Load from `containers.index` blob mapping |
| `LoadPS4MemoryDatSave(path, slotIndex)` | Load from monolithic `memory.dat` |
| `RegisterContextTransforms(root)` | Register `PlayerStateData` / `SpawnStateData` dynamic resolution |
| `DetectGameModeFast(path)` | Scan only the first block to find `PresetGameMode` |
| `FormatPlayTime(seconds)` | Format as `MM:SS` or `H:MM:SS` |

**Design choices:**

- **Latin-1 encoding** is used instead of UTF-8 to preserve bytes >= 0x80 that appear in
  NMS JSON strings. These become `BinaryData` objects in the model layer.
- **ArrayPool** is used for temporary decompression buffers to avoid long-lived allocations.
- **Two-pass decompression:** first pass scans block headers to calculate total decompressed
  size, second pass performs actual decompression. This avoids resizable buffers.

---

### SaveSlotManager

| | |
|---|---|
| File | `IO/SaveSlotManager.cs` |
| Purpose | Slot-level operations -- copy, move, swap, delete, cross-platform transfer |

All methods are `static`. Provides platform-aware file path resolution and atomic slot
operations.

| Method | Description |
|--------|-------------|
| `GetSlotFiles(dir, index, platform)` | Returns data + meta file paths for a slot |
| `CopySlot(dir, src, dest, platform)` | Copy slot A to B (backs up existing B) |
| `MoveSlot(dir, src, dest, platform)` | Copy then delete source |
| `SwapSlots(dir, a, b, platform)` | Atomic swap via temp directory |
| `DeleteSlot(dir, index, platform)` | Delete data + meta files |
| `TransferCrossPlatform(src, dest, srcSlot, platform, options)` | Transfer with UID/platform rewriting |
| `SlotIndexToStorageSlot(index)` | Convert array index to TEA encryption key slot |
| `StorageSlotFromFileName(name)` | Derive storage slot from filename |

**File naming conventions:**
- Steam/GOG: `save.hg`, `save2.hg`, `save3.hg` with `mf_save.hg` meta
- Switch/PS4: `savedata{NN:D2}.hg` with `manifest{NN:D2}.hg` meta

`TransferOptions` allows selective transfer of bases, discoveries, settlements, and ByteBeat
data, plus UID/LID/USN/PTK rewriting for cross-platform compatibility.

---

### ContainersIndexManager

| | |
|---|---|
| File | `IO/ContainersIndexManager.cs` |
| Purpose | Parse and write Xbox Game Pass `containers.index` files and blob directories |

Xbox saves use an indirection layer: `containers.index` maps slot identifiers to blob
directories containing `container.{N}` files. Each blob directory holds data and meta blobs
addressed by GUID.

Blob container files are exactly **328 bytes** (128-byte UTF-16LE identifiers per entry).
Xbox saves can use three compression formats:
- **HGSAVEV2**: `"HGSAVEV2\0"` header + multi-frame LZ4 chunks (post-Omega)
- **NMS LZ4 streaming**: `0xE5A1EDFE` magic per chunk (multi-block)
- **Plain/single-block**: uncompressed or single-block LZ4

`XboxSlotInfo` holds: `Identifier`, `SecondIdentifier`, `SyncTime`, `SyncState`,
`DirectoryGuid`, `BlobDirectoryPath`, `DataFilePath`, `MetaFilePath`, `LastModified`.

| Method | Description |
|--------|-------------|
| `IsXboxSaveDirectory(path)` | Check for `containers.index` |
| `ParseContainersIndex(path)` | Parse index into `Dictionary<string, XboxSlotInfo>` |
| `LoadXboxSave(slotInfo)` | Load and decompress JSON from blob |
| `LoadXboxMeta(slotInfo)` | Load raw metadata blob |
| `WriteXboxSave(slotInfo, data, meta)` | Write data + meta blobs |
| `WriteContainersIndex(path, slots, ...)` | Rewrite the index file |

---

### MemoryDatManager

| | |
|---|---|
| File | `IO/MemoryDatManager.cs` |
| Purpose | Read and write PlayStation monolithic `memory.dat` files |

PS4 stores all 31 save slots (account + 15 saves x 2) in a single `memory.dat` file.
Metadata sits at offset `0x20`, data region starts at `0x4020`. Each slot has a fixed
allocation: 256 KB for account data, 3 MB per save. SaveWizard-modified files have a
20-byte preamble.

`MemoryDatSlot` holds: `Index`, `Exists`, `MetaFormat`, `CompressedSize`, `ChunkOffset`,
`ChunkSize`, `MetaIndex`, `Timestamp`, `DecompressedSize`, `IsSaveWizard`.

| Method | Description |
|--------|-------------|
| `IsMemoryDat(path)` | Check if file is `memory.dat` |
| `IsSaveWizardFormat(path)` | Detect SaveWizard header |
| `ReadSlots(path)` | Parse all 31 slot metadata entries |
| `ExtractSlotData(path, index)` | Decompress JSON for one slot |
| `WriteMemoryDat(path, dataMap, slotMap)` | Write complete file with all slots |

---

### MetaCrypto

| | |
|---|---|
| File | `IO/MetaCrypto.cs` |
| Purpose | TEA/XXTEA encryption for Steam/GOG meta files, plus SpookyHash integrity hashing |

Steam and GOG meta files are encrypted with XXTEA using a 4-uint32 key derived from the
storage slot index. The base key bytes spell "SEAN", "DAVE", "RYAN", "GRNT" (yep, you guessed it!) --
`DeriveKey0(storageSlot)` varies the first uint by slot.

| Method | Description |
|--------|-------------|
| `DeriveKey0(storageSlot)` | Compute slot-dependent first key word |
| `Encrypt(data, slot, iterations)` | XXTEA encrypt (6 or 8 rounds) |
| `Decrypt(data, slot, iterations)` | Try primary slot, then brute-force all others |
| `ComputeMetaHashes(data)` | SpookyHash (16 bytes) + SHA256 (32 bytes) = 48 bytes |

`SpookyHashV2` is an internal implementation of Bob Jenkins' SpookyHash V2 used for the
first 16 bytes of the integrity hash.

**Design choice:** `Decrypt` tries all possible slot keys when the primary fails, which
handles files that were copied between slots manually.

---

### MetaFileWriter

| | |
|---|---|
| File | `IO/MetaFileWriter.cs` |
| Purpose | Write platform-specific meta/companion files alongside save data |

Each platform has a different meta file format and header:

| Platform | Header | Encryption |
|----------|--------|------------|
| Steam/GOG | `0xEEEEEEBE` | XXTEA (slot-keyed) |
| Switch/PS4 | `0x000007D0` | None (hardware protection) |

Meta format versions: VANILLA (104 bytes), WAYPOINT (360), WORLDS_I (384), WORLDS_II (432).

`SaveMetaInfo` holds: `BaseVersion`, `GameMode`, `Season`, `TotalPlayTime`, `SaveName`,
`SaveSummary`, `DifficultyPreset`, `DifficultyPresetTag`.

| Method | Description |
|--------|-------------|
| `WriteSteamMeta(path, data, format, info, slot)` | Write encrypted Steam meta |
| `WriteSwitchMeta(path, format, info, slot)` | Write plaintext Switch meta |
| `WritePlaystationStreamingMeta(path, format, info, slot)` | Write PS4 meta |
| `ReadSteamMeta(path, slot)` | Read and decrypt Steam meta |
| `ExtractMetaInfo(saveData)` | Extract metadata fields from the save JSON |
| `GetSteamMetaPath(savePath)` | Convert `save.hg` to `mf_save.hg` |

---

### BinaryIO

| | |
|---|---|
| File | `IO/BinaryIO.cs` |
| Purpose | Low-level binary I/O utilities for little-endian integers and Base64 |

A static helper class used throughout the IO layer.

| Method | Description |
|--------|-------------|
| `ReadInt32LE(stream)` | Read 32-bit little-endian integer |
| `WriteInt32LE(stream, value)` | Write 32-bit little-endian integer |
| `ReadInt64LE(stream)` | Read 64-bit little-endian integer |
| `WriteInt64LE(stream, value)` | Write 64-bit little-endian integer |
| `ReadAllBytes(stream)` | Read all remaining bytes |
| `ReadFileBytes(path)` | Read entire file |
| `ReadFully(stream, span)` | Read exactly N bytes; throws on short read |
| `Base64Encode(bytes)` / `Base64Decode(string)` | Base64 codec |

---

### LZ4 Compression Classes

NMS uses LZ4 fast compression for save data. The project includes a native C# LZ4
implementation (no external dependencies) with multiple stream wrappers for different use
cases.

#### Lz4Compressor

| | |
|---|---|
| File | `IO/Lz4Compressor.cs` |
| Purpose | Core LZ4 compress/decompress algorithm |

Static class with the raw compression engine. Constants: `MinMatch = 4`,
`HashLog = 16`, `HashTableSize = 65536`, `MaxInputSize = 0x7E000000`.

| Method | Description |
|--------|-------------|
| `MaxCompressedLength(inputLen)` | Calculate worst-case output buffer size |
| `Compress(src, srcOff, srcLen, dst, dstOff, dstLen)` | Compress; returns bytes written |
| `Decompress(src, srcOff, srcLen, dst, dstOff, dstLen)` | Decompress; returns bytes written |

#### Lz4CompressorStream

| | |
|---|---|
| File | `IO/Lz4CompressorStream.cs` |
| Purpose | Write-only stream that outputs LZ4-compressed chunks with 16-byte headers |

Header format: `magic(4) + compressedLen(4) + uncompressedLen(4) + padding(4)`. Uses a
512 KB internal buffer. Flushes a compressed block whenever the buffer fills. Exposes
`UncompressedSize` and `CompressedSize` properties.

#### Lz4BufferedCompressorStream

| | |
|---|---|
| File | `IO/Lz4BufferedCompressorStream.cs` |
| Purpose | Buffered compression -- accumulates all data, compresses in one block on dispose |

Starts with a 64 KB buffer that grows on demand. All data is held in memory until
`Dispose()` is called, at which point it compresses everything as a single LZ4 block. Useful
when the total size is small or when single-block output is required.

#### Lz4ChunkedCompressorStream

| | |
|---|---|
| File | `IO/Lz4ChunkedCompressorStream.cs` |
| Purpose | Chunked LZ4 compression with 1 MB blocks and 8-byte headers |

Header format: `uncompressedLen(4) + compressedLen(4)`. Uses 1 MB blocks. This format
matches the chunked layout used by certain NMS platform variants.

#### Lz4DecompressorStream

| | |
|---|---|
| File | `IO/Lz4DecompressorStream.cs` |
| Purpose | Read-only stream that decompresses LZ4 blocks on the fly |

Constructor takes an inner stream and optional expected uncompressed size (0 = dynamic
sizing). Has a safety limit of 256 MB maximum decompressed output. Reads and decompresses
blocks incrementally as callers read from the stream.

---

## Platform Abstraction Summary

| Concern | Steam/GOG | Xbox Game Pass | PlayStation 4 | Switch |
|---------|-----------|---------------|---------------|--------|
| File layout | Individual `.hg` files | `containers.index` + blob dirs | `memory.dat` monolith | Individual `.hg` files |
| Compression | LZ4 (16-byte header blocks) | LZ4 (16-byte header blocks) | LZ4 (chunked, 8-byte headers) | LZ4 (16-byte header blocks) |
| Encryption | XXTEA meta files | None (Xbox handles it) | None (hardware) | None |
| Meta format | `mf_save.hg` (encrypted) | Blob metadata | Embedded in `memory.dat` | `manifest{NN}.hg` |
| Manager class | `SaveFileManager` | `ContainersIndexManager` | `MemoryDatManager` | `SaveFileManager` |

---

## Context Transform System

After loading a save, `SaveFileManager.RegisterContextTransforms` registers dynamic path
transforms on the root `JsonObject`. When application code accesses
`root.GetValue("PlayerStateData")`, the transform checks the `ActiveContext` field:

- If `ActiveContext` is `"BaseContext"` -> resolves to `BaseContext.PlayerStateData`
- If `ActiveContext` is `"ExpeditionContext"` -> resolves to `ExpeditionContext.PlayerStateData`

The same pattern applies to `SpawnStateData`. This means all Logic and Panel code can use
simple paths like `"PlayerStateData.Health"` without knowing which context is active. The
transform is transparent and applied automatically by `JsonObject.GetValue`.
