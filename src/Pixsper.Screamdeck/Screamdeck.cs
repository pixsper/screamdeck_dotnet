using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using ArgumentOutOfRangeException = System.ArgumentOutOfRangeException;

namespace Pixsper.Screamdeck;

public sealed class Screamdeck : IDisposable, IAsyncDisposable
{
    private const int ReadKeyTimeoutMs = 1000 / 60;

    public static IEnumerable<ScreamdeckDeviceInfo> Enumerate()
    {
        List<ScreamdeckDeviceInfo> deviceInfos = [];

        unsafe
        {
            NativeDeviceInfo* nativeDeviceInfos = NativeMethods.Enumerate();
            try
            {
                NativeDeviceInfo* d = nativeDeviceInfos;
                while (d is not null)
                {
                    string serial = Marshal.PtrToStringUni((nint)d->SerialNumber) ?? throw new InvalidOperationException("Failed to marshal device serial string buffer");

                    deviceInfos.Add(new ScreamdeckDeviceInfo(d->DeviceType, serial));

                    d = d->Next;
                }
            }
            finally
            {
                NativeMethods.FreeEnumeration(nativeDeviceInfos);
            }
        }

        return deviceInfos;
    }

    public static Screamdeck? Open(ScreamdeckDeviceInfo deviceInfo) => Open(deviceInfo.DeviceType, deviceInfo.Serial);

    public static Screamdeck? Open(ScreamdeckDeviceType deviceType, string serialNumber)
    {
        if (!Enum.IsDefined(deviceType) || deviceType == ScreamdeckDeviceType.None)
            throw new ArgumentException("Invalid device type value", nameof(deviceType));

        ArgumentException.ThrowIfNullOrWhiteSpace(serialNumber);

        IntPtr nativeHandle = IntPtr.Zero;
        if (!NativeMethods.Open(ref nativeHandle, deviceType, serialNumber))
            return null;
        
        unsafe
        {
            NativeDeviceTypeInfo* nativeDeviceTypeInfo = NativeMethods.GetDeviceTypeInfo(nativeHandle);
            if (nativeDeviceTypeInfo == null)
                return null;

            return new Screamdeck(nativeHandle, new ScreamdeckDeviceTypeInfo(nativeDeviceTypeInfo), serialNumber);
        }
    }

    public static Screamdeck? OpenFirst(ScreamdeckDeviceType deviceType = ScreamdeckDeviceType.None)
    {
        if (!Enum.IsDefined(deviceType))
            throw new ArgumentException("Invalid device type value", nameof(deviceType));

        IntPtr nativeHandle = IntPtr.Zero;
        if (!NativeMethods.OpenFirst(ref nativeHandle, deviceType))
            return null;
        
        unsafe
        {
            NativeDeviceTypeInfo* nativeDeviceTypeInfo = NativeMethods.GetDeviceTypeInfo(nativeHandle);
            if (nativeDeviceTypeInfo == null)
                return null;

            string? serialNumber;

            const int serialNumberBufferLength = 32;
            Span<char> serialNumberBuffer = stackalloc char[serialNumberBufferLength];
            fixed (char* b = serialNumberBuffer)
            {
                if (!NativeMethods.GetSerialNumber(nativeHandle, b, serialNumberBufferLength))
                    return null;

                serialNumber = Marshal.PtrToStringUni((nint)b);
                if (serialNumber is null)
                    return null;
            }

            return new Screamdeck(nativeHandle, new ScreamdeckDeviceTypeInfo(nativeDeviceTypeInfo), serialNumber);
        }
    }

    private readonly IntPtr _nativeHandle;
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private readonly Task _keyEventLoop;


    internal Screamdeck(IntPtr nativeHandle, ScreamdeckDeviceTypeInfo deviceTypeInfo, string serialNumber)
    {
        if (nativeHandle == IntPtr.Zero)
            throw new ArgumentNullException(nameof(nativeHandle));

        _nativeHandle = nativeHandle;
        DeviceTypeInfo = deviceTypeInfo;
        SerialNumber = serialNumber;
        _keyEventLoop = Task.Run(ReadKeyLoop);
    }

    
    ~Screamdeck()
    {
        ReleaseUnmanagedResourcesAsync().AsTask().Wait();
    }

    public void Dispose()
    {
        ReleaseUnmanagedResourcesAsync().AsTask().Wait();
        GC.SuppressFinalize(this);
    }

    public async ValueTask DisposeAsync()
    {
        await ReleaseUnmanagedResourcesAsync().ConfigureAwait(false);
        GC.SuppressFinalize(this);
    }

    private async ValueTask ReleaseUnmanagedResourcesAsync()
    {
        await _cancellationTokenSource.CancelAsync().ConfigureAwait(false);
        await _keyEventLoop.ConfigureAwait(false);
        _cancellationTokenSource.Dispose();

        Debug.Assert(NativeMethods.Free(_nativeHandle));
    }


    public event EventHandler<ScreamDeckKeyEventArgs>? OnKey;

    public ScreamdeckDeviceTypeInfo DeviceTypeInfo { get; }

    public string SerialNumber { get; }


    public bool SetBrightness(int brightnessPercentage)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(brightnessPercentage, 0);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(brightnessPercentage, 100);
        return NativeMethods.SetBrightness(_nativeHandle, brightnessPercentage);
    }

    public bool SetScreensaver()
    {
        return NativeMethods.SetScreensaver(_nativeHandle);
    }

    public bool SetImage(Span<byte> imageBuffer, ScreamdeckPixelFormat pixelFormat, int qualityPercentage)
    {
        if (pixelFormat is ScreamdeckPixelFormat.Rgb or ScreamdeckPixelFormat.Bgr)
            return SetImage24(imageBuffer, pixelFormat, qualityPercentage);
        else
            return SetImage32(imageBuffer, pixelFormat, qualityPercentage);
    }

    public bool SetImage(IntPtr imageBuffer, ScreamdeckPixelFormat pixelFormat, int qualityPercentage)
    {
        if (pixelFormat is ScreamdeckPixelFormat.Rgb or ScreamdeckPixelFormat.Bgr)
            return SetImage24(imageBuffer, pixelFormat, qualityPercentage);
        else
            return SetImage32(imageBuffer, pixelFormat, qualityPercentage);
    }

    public bool SetImage24(Span<byte> imageBuffer, ScreamdeckPixelFormat pixelFormat, int qualityPercentage)
    {
        if (pixelFormat is not ScreamdeckPixelFormat.Rgb and not ScreamdeckPixelFormat.Bgr)
            throw new ArgumentOutOfRangeException(nameof(pixelFormat), "Not a 24-bit pixel format");

        unsafe
        {
            fixed (byte* ptr = imageBuffer)
                return NativeMethods.SetImage24(_nativeHandle, ptr, pixelFormat, qualityPercentage);
        }
    }

    public bool SetImage24(IntPtr imageBuffer, ScreamdeckPixelFormat pixelFormat, int qualityPercentage)
    {
        if (pixelFormat is not ScreamdeckPixelFormat.Rgb and not ScreamdeckPixelFormat.Bgr)
            throw new ArgumentOutOfRangeException(nameof(pixelFormat), "Not a 24-bit pixel format");

        unsafe
        {
            return NativeMethods.SetImage24(_nativeHandle, (byte*)imageBuffer, pixelFormat, qualityPercentage);
        }
    }

    public bool SetImage32(Span<byte> imageBuffer, ScreamdeckPixelFormat pixelFormat, int qualityPercentage)
    {
        if (pixelFormat is ScreamdeckPixelFormat.Rgb or ScreamdeckPixelFormat.Bgr)
            throw new ArgumentOutOfRangeException(nameof(pixelFormat), "Not a 32-bit pixel format");

        unsafe
        {
            fixed (byte* ptr = imageBuffer)
                return NativeMethods.SetImage32(_nativeHandle, ptr, pixelFormat, qualityPercentage);
        }
    }

    public bool SetImage32(IntPtr imageBuffer, ScreamdeckPixelFormat pixelFormat, int qualityPercentage)
    {
        if (pixelFormat is ScreamdeckPixelFormat.Rgb or ScreamdeckPixelFormat.Bgr)
            throw new ArgumentOutOfRangeException(nameof(pixelFormat), "Not a 32-bit pixel format");

        unsafe
        {
            return NativeMethods.SetImage32(_nativeHandle, (byte*)imageBuffer, pixelFormat, qualityPercentage);
        }
    }

    public bool SetKeyImage(int keyX, int keyY, Span<byte> imageBuffer, ScreamdeckPixelFormat pixelFormat,
        int qualityPercentage)
    {
        unsafe
        {
            fixed (byte* ptr = imageBuffer)
                return NativeMethods.SetKeyImage(_nativeHandle, keyX, keyY, ptr, pixelFormat, qualityPercentage);
        }
    }

    public bool SetKeyImage(int keyX, int keyY, IntPtr imageBuffer, ScreamdeckPixelFormat pixelFormat,
        int qualityPercentage)
    {
        unsafe
        {
            return NativeMethods.SetKeyImage(_nativeHandle, keyX, keyY, (byte*)imageBuffer, pixelFormat, qualityPercentage);
        }
    }



    private void ReadKeyLoop()
    {
        var keyState = new byte[DeviceTypeInfo.Columns * DeviceTypeInfo.Rows];
        var keyStateBuffer = new byte[keyState.Length];

        while (!_cancellationTokenSource.Token.IsCancellationRequested)
        {
            unsafe
            {
                fixed (byte* b = keyStateBuffer)
                {
                    var bytesRead = NativeMethods.ReadKeyTimeout(_nativeHandle, b, keyStateBuffer.Length, ReadKeyTimeoutMs);
                    if (bytesRead > keyStateBuffer.Length)
                    {
                        for (int i = 0; i < keyState.Length; ++i)
                        {
                            if (keyStateBuffer[i] != keyState[i])
                            {
                                keyState[i] = keyStateBuffer[i];
                                OnKey?.Invoke(this, new ScreamDeckKeyEventArgs
                                {
                                    KeyIndex = i,
                                    KeyX = i % DeviceTypeInfo.Columns,
                                    KeyY = i / DeviceTypeInfo.Columns,
                                    IsDown = keyState[i] > 0
                                });
                            }
                        }
                    }
                }
            }
        }
    }
}