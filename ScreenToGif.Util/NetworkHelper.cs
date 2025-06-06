using System.Runtime.InteropServices;

namespace ScreenToGif.Util;

public static class NetworkHelper
{
    [ComImport, Guid("DCB00C01-570F-4A9B-8D69-199FDBA5723B"), ClassInterface(ClassInterfaceType.None)]
    private class NetworkListManager { }

    [ComImport, Guid("DCB00008-570F-4A9B-8D69-199FDBA5723B"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown), CoClass(typeof(NetworkListManager))]
    private interface INetworkCostManager
    {
        /// <summary>
        /// The GetCost method retrieves the current cost of either a machine-wide internet connection, or the first-hop of routing to a specific destination
        /// on a connection. If destinationAddress is NULL, this method instead returns the cost of the network used for machine-wide Internet connectivity.
        /// </summary>
        /// <param name="cost">
        /// A DWORD value that indicates the cost of the connection. The lowest 16 bits represent the cost level, and the highest 16 bits represent the
        /// flags. Possible values are defined by the <see cref="ConnectionCost"/> enumeration.
        /// </param>
        /// <param name="destinationAddress">
        /// An <see cref="SocketAddress"/> structure containing the destination IPv4/IPv6 address. If NULL, this method will instead return the cost associated with the
        /// preferred connection used for machine Internet connectivity.
        /// </param>
        void GetCost(out ConnectionCost cost, [In, Optional] SocketAddress? destinationAddress);
    }

    private enum ConnectionCost : uint
    {
        /// <summary>
        /// The cost is unknown.
        /// </summary>
        Unknown = 0x0,

        /// <summary>
        /// The connection is unlimited and is considered to be unrestricted of usage charges and capacity constraints.
        /// </summary>
        Unrestricted = 0x1,

        /// <summary>
        /// The use of this connection is unrestricted up to a specific data transfer limit.
        /// </summary>
        Fixed = 0x2,

        /// <summary>
        /// This connection is regulated on a per-byte basis.
        /// </summary>
        Variable = 0x4,

        /// <summary>
        /// The connection is currently in an OverDataLimit state as it has exceeded the carrier specified data transfer limit.
        /// </summary>
        OverDataLimit = 0x10000,

        /// <summary>
        /// The network is experiencing high traffic load and is congested.
        /// </summary>
        Congested = 0x20000,

        /// <summary>
        /// The connection is roaming outside the network and affiliates of the home provider.
        /// </summary>
        Roaming = 0x40000,

        /// <summary>
        /// The connection is approaching the data limit specified by the carrier.
        /// </summary>
        ApproachingDataLimit = 0x80000
    }
    
    /// <summary>
    /// The <see cref="SocketAddress"/> structure contains the IPv4/IPv6 destination address.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public sealed class SocketAddress
    {
        private const int DataSize = 128;

        /// <summary>An IPv4/IPv6 destination address.</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = DataSize)]
        public byte[] Data = new byte[DataSize];
    }

    public static bool IsNetworkMetered()
    {
        try
        {
            var cost = ConnectionCost.Unknown;

            // ReSharper disable once SuspiciousTypeConversion.Global
            var manager = new NetworkListManager() as INetworkCostManager;
            manager?.GetCost(out cost, null);

            return cost >= ConnectionCost.Fixed;
        }
        catch
        {
            return false;
        }
    }
}