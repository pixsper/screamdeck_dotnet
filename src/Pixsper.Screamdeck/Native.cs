using System;
using System.Runtime.InteropServices;

namespace Pixsper.Screamdeck;

[StructLayout(LayoutKind.Sequential)]
internal unsafe struct NativeDeviceInfo
{
    public char* SerialNumber;
    public ScreamdeckDeviceType DeviceType;
    public NativeDeviceInfo* Next;
}

[StructLayout(LayoutKind.Sequential)]
internal struct NativeDeviceTypeInfo
{
    public ScreamdeckDeviceType DeviceType;
    public int Columns;
    public int Rows;
    public int KeyImageWidth;
    public int KeyImageHeight;
    public int KeyGapWidth;
    public int KeyGapHeight;
    public int ImageWidth;
    public int ImageHeight;

}

internal static partial class NativeMethods
{
    [LibraryImport("screamdeck", EntryPoint = "scdk_get_device_type_info_from_type")]
    public static unsafe partial NativeDeviceTypeInfo* GetDeviceTypeInfoFromType(ScreamdeckDeviceType deviceType);

    [LibraryImport("screamdeck", EntryPoint = "scdk_enumerate")]
    public static unsafe partial NativeDeviceInfo* Enumerate();

    [LibraryImport("screamdeck", EntryPoint = "scdk_free_enumeration")]
    public static unsafe partial void FreeEnumeration(NativeDeviceInfo* devices);

    [LibraryImport("screamdeck", EntryPoint = "scdk_open", StringMarshalling = StringMarshalling.Utf16)]
    [return: MarshalAs(UnmanagedType.I1)]
    public static partial bool Open(ref IntPtr device, ScreamdeckDeviceType deviceType, [MarshalAs(UnmanagedType.LPWStr)] string serialNumber);

    [LibraryImport("screamdeck", EntryPoint = "scdk_open_first", StringMarshalling = StringMarshalling.Utf16)]
    [return: MarshalAs(UnmanagedType.I1)]
    public static partial bool OpenFirst(ref IntPtr device, ScreamdeckDeviceType deviceType);

    [LibraryImport("screamdeck", EntryPoint = "scdk_free")]
    [return: MarshalAs(UnmanagedType.I1)]
    public static partial bool Free(IntPtr device);

    [LibraryImport("screamdeck", EntryPoint = "scdk_get_device_type_info")]
    public static unsafe partial NativeDeviceTypeInfo* GetDeviceTypeInfo(IntPtr device);

    [LibraryImport("screamdeck", EntryPoint = "scdk_get_serial_number")]
    [return: MarshalAs(UnmanagedType.I1)]
    public static unsafe partial bool GetSerialNumber(IntPtr device, char* serialNumberBuffer, nuint serialNumberLength);

    [LibraryImport("screamdeck", EntryPoint = "scdk_read_key")]
    public static unsafe partial int ReadKey(IntPtr device, byte* keyStateBuffer, int keyStateBufferLength);

    [LibraryImport("screamdeck", EntryPoint = "scdk_read_key_timeout")]
    public static unsafe partial int ReadKeyTimeout(IntPtr device, byte* keyStateBuffer, int keyStateBufferLength,
        int timeoutMs);

    [LibraryImport("screamdeck", EntryPoint = "scdk_set_image")]
    [return: MarshalAs(UnmanagedType.I1)]
    public static unsafe partial bool SetImage(IntPtr device, byte* imageBuffer, ScreamdeckPixelFormat pixelFormat,
        int qualityPercentage);

    [LibraryImport("screamdeck", EntryPoint = "scdk_set_image_24")]
    [return: MarshalAs(UnmanagedType.I1)]
    public static unsafe partial bool SetImage24(IntPtr device, byte* imageBuffer, ScreamdeckPixelFormat pixelFormat,
        int qualityPercentage);

    [LibraryImport("screamdeck", EntryPoint = "scdk_set_image_32")]
    [return: MarshalAs(UnmanagedType.I1)]
    public static unsafe partial bool SetImage32(IntPtr device, byte* imageBuffer, ScreamdeckPixelFormat pixelFormat,
        int qualityPercentage);

    [LibraryImport("screamdeck", EntryPoint = "scdk_set_key_image")]
    [return: MarshalAs(UnmanagedType.I1)]
    public static unsafe partial bool SetKeyImage(IntPtr device, int keyX, int keyY, byte* imageBuffer,
        ScreamdeckPixelFormat pixelFormat, int qualityPercentage);

    [LibraryImport("screamdeck", EntryPoint = "scdk_set_brightness")]
    [return: MarshalAs(UnmanagedType.I1)]
    public static partial bool SetBrightness(IntPtr device, int brightnessPercentage);

    [LibraryImport("screamdeck", EntryPoint = "scdk_set_screensaver")]
    [return: MarshalAs(UnmanagedType.I1)]
    public static partial bool SetScreensaver(IntPtr device);
}