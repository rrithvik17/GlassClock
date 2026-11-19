using System;
using System.Runtime.InteropServices;

namespace GlassClock;

internal enum ProjectionTopology
{
    Internal,
    Clone,
    Extend,
    ExternalOnly,
    Unknown
}

internal static class ProjectionManager
{
    private const uint QDC_DATABASE_CURRENT = 0x00000004;

    private const uint DISPLAYCONFIG_TOPOLOGY_INTERNAL = 0x00000001;
    private const uint DISPLAYCONFIG_TOPOLOGY_CLONE = 0x00000002;
    private const uint DISPLAYCONFIG_TOPOLOGY_EXTEND = 0x00000004;
    private const uint DISPLAYCONFIG_TOPOLOGY_EXTERNAL = 0x00000008;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int GetDisplayConfigBufferSizes(
        uint flags,
        out uint numPathArrayElements,
        out uint numModeInfoArrayElements);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int QueryDisplayConfig(
        uint flags,
        ref uint numPathArrayElements,
        [Out] DISPLAYCONFIG_PATH_INFO[] pathArray,
        ref uint numModeInfoArrayElements,
        [Out] DISPLAYCONFIG_MODE_INFO[] modeInfoArray,
        out uint currentTopologyId);

    public static ProjectionTopology GetTopology()
    {
        try
        {
            // QDC_DATABASE_CURRENT is the Windows CCD database query.
            // Microsoft specifies that this mode returns the active topology
            // through currentTopologyId.
            int result = GetDisplayConfigBufferSizes(
                QDC_DATABASE_CURRENT,
                out uint pathCount,
                out uint modeCount);

            if (result != 0)
                return ProjectionTopology.Unknown;

            for (int attempt = 0; attempt < 3; attempt++)
            {
                if (pathCount == 0)
                    pathCount = 1;

                if (modeCount == 0)
                    modeCount = 1;

                var paths = new DISPLAYCONFIG_PATH_INFO[pathCount];
                var modes = new DISPLAYCONFIG_MODE_INFO[modeCount];

                uint actualPathCount = pathCount;
                uint actualModeCount = modeCount;

                result = QueryDisplayConfig(
                    QDC_DATABASE_CURRENT,
                    ref actualPathCount,
                    paths,
                    ref actualModeCount,
                    modes,
                    out uint topologyId);

                if (result == 0)
                {
                    return topologyId switch
                    {
                        DISPLAYCONFIG_TOPOLOGY_INTERNAL => ProjectionTopology.Internal,
                        DISPLAYCONFIG_TOPOLOGY_CLONE => ProjectionTopology.Clone,
                        DISPLAYCONFIG_TOPOLOGY_EXTEND => ProjectionTopology.Extend,
                        DISPLAYCONFIG_TOPOLOGY_EXTERNAL => ProjectionTopology.ExternalOnly,
                        _ => ProjectionTopology.Unknown
                    };
                }

                // Display configuration can change between buffer-size and query
                // calls. Re-read the sizes and retry.
                if (result == 122) // ERROR_INSUFFICIENT_BUFFER
                {
                    result = GetDisplayConfigBufferSizes(
                        QDC_DATABASE_CURRENT,
                        out pathCount,
                        out modeCount);

                    if (result != 0)
                        return ProjectionTopology.Unknown;

                    continue;
                }

                return ProjectionTopology.Unknown;
            }

            return ProjectionTopology.Unknown;
        }
        catch
        {
            // Never let display detection prevent the clock from starting.
            // If detection fails, keep the normal laptop clock visible.
            return ProjectionTopology.Unknown;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct LUID
    {
        public uint LowPart;
        public int HighPart;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct DISPLAYCONFIG_PATH_INFO
    {
        public DISPLAYCONFIG_PATH_SOURCE_INFO sourceInfo;
        public DISPLAYCONFIG_PATH_TARGET_INFO targetInfo;
        public uint flags;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct DISPLAYCONFIG_PATH_SOURCE_INFO
    {
        public LUID adapterId;
        public uint id;
        public uint modeInfoIdx;
        public uint statusFlags;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct DISPLAYCONFIG_PATH_TARGET_INFO
    {
        public LUID adapterId;
        public uint id;
        public uint modeInfoIdx;
        public uint outputTechnology;
        public uint rotation;
        public uint scaling;
        public DISPLAYCONFIG_RATIONAL refreshRate;
        public uint scanLineOrdering;

        // Native Windows BOOL is 4 bytes.
        public int targetAvailable;

        public uint statusFlags;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct DISPLAYCONFIG_RATIONAL
    {
        public uint Numerator;
        public uint Denominator;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct DISPLAYCONFIG_MODE_INFO
    {
        public uint infoType;
        public uint id;
        public LUID adapterId;
        public DISPLAYCONFIG_MODE_INFO_UNION modeInfo;
    }

    // DISPLAYCONFIG_MODE_INFO contains a native union.
    // The largest member on Windows is DISPLAYCONFIG_TARGET_MODE.
    [StructLayout(LayoutKind.Explicit)]
    private struct DISPLAYCONFIG_MODE_INFO_UNION
    {
        [FieldOffset(0)]
        public DISPLAYCONFIG_TARGET_MODE targetMode;

        [FieldOffset(0)]
        public DISPLAYCONFIG_SOURCE_MODE sourceMode;

        [FieldOffset(0)]
        public DISPLAYCONFIG_DESKTOP_IMAGE_INFO desktopImageInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct DISPLAYCONFIG_TARGET_MODE
    {
        public DISPLAYCONFIG_VIDEO_SIGNAL_INFO targetVideoSignalInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct DISPLAYCONFIG_SOURCE_MODE
    {
        public uint width;
        public uint height;
        public uint pixelFormat;
        public POINTL position;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct DISPLAYCONFIG_DESKTOP_IMAGE_INFO
    {
        public POINTL PathSourceSize;
        public RECT DesktopImageRegion;
        public RECT DesktopImageClip;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct DISPLAYCONFIG_VIDEO_SIGNAL_INFO
    {
        public ulong pixelRate;
        public DISPLAYCONFIG_RATIONAL hSyncFreq;
        public DISPLAYCONFIG_RATIONAL vSyncFreq;
        public DISPLAYCONFIG_2DREGION activeSize;
        public DISPLAYCONFIG_2DREGION totalSize;

        // This is a 32-bit native union. Do NOT add another rational here.
        public uint videoStandard;

        public uint scanLineOrdering;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct DISPLAYCONFIG_2DREGION
    {
        public uint cx;
        public uint cy;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct POINTL
    {
        public int x;
        public int y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int left;
        public int top;
        public int right;
        public int bottom;
    }
}
