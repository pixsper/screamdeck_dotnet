using System;

namespace Pixsper.Screamdeck;

public record ScreamdeckDeviceTypeInfo
{
    public static ScreamdeckDeviceTypeInfo FromDeviceType(ScreamdeckDeviceType deviceType)
    {
        if (!Enum.IsDefined(deviceType) || deviceType == ScreamdeckDeviceType.None)
            throw new ArgumentException("Invalid device type value", nameof(deviceType));

        unsafe
        {
            NativeDeviceTypeInfo* nativeDeviceTypeInfo = NativeMethods.GetDeviceTypeInfoFromType(deviceType);
            if (nativeDeviceTypeInfo == null)
                throw new InvalidOperationException($"Failed to find device type info for '{deviceType}'");

            return new ScreamdeckDeviceTypeInfo(nativeDeviceTypeInfo);
        }
    }

    internal unsafe ScreamdeckDeviceTypeInfo(NativeDeviceTypeInfo* nativeDeviceTypeInfo)
    {
        DeviceType = nativeDeviceTypeInfo->DeviceType;
        Columns = nativeDeviceTypeInfo->Columns;
        Rows = nativeDeviceTypeInfo->Rows;
        KeyImageWidth = nativeDeviceTypeInfo->KeyImageWidth;
        KeyImageHeight = nativeDeviceTypeInfo->KeyImageHeight;
        KeyGapWidth = nativeDeviceTypeInfo->KeyGapWidth;
        KeyGapHeight = nativeDeviceTypeInfo->KeyGapHeight;
        ImageWidth = nativeDeviceTypeInfo->ImageWidth;
        ImageHeight = nativeDeviceTypeInfo->ImageHeight;
    }

    public ScreamdeckDeviceTypeInfo()
    {

    }

    public ScreamdeckDeviceType DeviceType { get; init; }
    public int Columns { get; init;}
    public int Rows { get; init; }
    public int KeyImageWidth { get; init; }
    public int KeyImageHeight { get; init; }
    public int KeyGapWidth { get; init; }
    public int KeyGapHeight { get; init; }
    public int ImageWidth { get; init; }
    public int ImageHeight { get; init; }
}