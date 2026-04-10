namespace NMSE.IO;

/// <summary>
/// Chunked LZ4 compression stream with 1MB blocks (HGSAVEV2 format).
/// Each block has 8-byte header: uncompressedLen(4) + compressedLen(4).
/// Used by post-Omega Xbox/Microsoft saves between versions 4.52 and 5.00.
/// Note: The caller must prepend the "HGSAVEV2\0" header before the first block.
/// Note: Apparently the trailing null terminator byte should be compressed as
/// a separate 1-byte chunk. This class does not handle that automatically; the
/// caller must split the data accordingly if strict compatibility is required.
/// For Worlds Part I 5.00+, Xbox uses standard NMS LZ4 streaming (Lz4CompressorStream).
/// </summary>
public class Lz4ChunkedCompressorStream : Stream
{
    private const int BufferSize = 1048576; // 1MB
    private readonly Stream _inner;
    private readonly byte[] _buffer = new byte[BufferSize];
    private int _bufferPos;
    private int _totalUncompressed;
    private int _totalCompressed;

    /// <summary>Gets the total number of uncompressed bytes written.</summary>
    public int UncompressedSize => _totalUncompressed;
    /// <summary>Gets the total number of compressed bytes produced (including headers).</summary>
    public int CompressedSize => _totalCompressed;

    /// <summary>
    /// Initializes a new instance wrapping the specified output stream.
    /// </summary>
    /// <param name="innerStream">The stream to write chunked compressed data to.</param>
    public Lz4ChunkedCompressorStream(Stream innerStream)
    {
        _inner = innerStream;
        _bufferPos = 0;
    }

    private void CompressAndFlushBlock()
    {
        if (_bufferPos == 0) return;

        int maxLen = Lz4Compressor.MaxCompressedLength(_bufferPos);
        byte[] compressed = new byte[maxLen];
        int compressedLen = Lz4Compressor.Compress(_buffer, 0, _bufferPos, compressed, 0, maxLen);

        // Write 8-byte header: uncompressed + compressed lengths
        BinaryIO.WriteInt32LE(_inner, _bufferPos);
        BinaryIO.WriteInt32LE(_inner, compressedLen);
        _inner.Write(compressed, 0, compressedLen);

        _totalUncompressed += _bufferPos;
        _totalCompressed += compressedLen + 8;
        _bufferPos = 0;
    }

    /// <inheritdoc />
    public override void Write(byte[] buffer, int offset, int count)
    {
        while (count > 0)
        {
            if (_bufferPos == BufferSize) CompressAndFlushBlock();
            int toCopy = Math.Min(count, BufferSize - _bufferPos);
            Buffer.BlockCopy(buffer, offset, _buffer, _bufferPos, toCopy);
            _bufferPos += toCopy;
            offset += toCopy;
            count -= toCopy;
        }
    }

    /// <inheritdoc />
    public override void WriteByte(byte value)
    {
        if (_bufferPos == BufferSize) CompressAndFlushBlock();
        _buffer[_bufferPos++] = value;
    }

    /// <inheritdoc />
    public override void Flush()
    {
        if (_bufferPos > 0) CompressAndFlushBlock();
        _inner.Flush();
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (_bufferPos > 0) CompressAndFlushBlock();
            _inner.Dispose();
        }
        base.Dispose(disposing);
    }

    /// <inheritdoc />
    public override bool CanRead => false;
    /// <inheritdoc />
    public override bool CanSeek => false;
    /// <inheritdoc />
    public override bool CanWrite => true;
    /// <inheritdoc />
    public override long Length => throw new NotSupportedException();
    /// <inheritdoc />
    public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
    /// <inheritdoc />
    public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    /// <inheritdoc />
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    /// <inheritdoc />
    public override void SetLength(long value) => throw new NotSupportedException();
}
