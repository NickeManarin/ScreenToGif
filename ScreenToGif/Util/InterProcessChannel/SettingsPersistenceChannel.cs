using System;
using System.Runtime.Remoting.Channels.Ipc;

namespace ScreenToGif.Util.InterProcessChannel
{
    /// <summary>
    /// Interprocess channel that is responsible for passing to another instance (with administrative rights) in order to be saved.
    /// </summary>
    public static class SettingsPersistenceChannel
    {
        internal static IpcChannel ServerChannel { get; private set; }

        
        internal static void RegisterServer()
        {
            try
            {
                if (ServerChannel != null)
                    return;

                ServerChannel = new IpcChannel("localhost:9193");
                System.Runtime.Remoting.Channels.ChannelServices.RegisterChannel(ServerChannel, true);

                //Expose an object for remote calls.
                System.Runtime.Remoting.RemotingConfiguration.RegisterWellKnownServiceType(typeof(SettingsRemoteObject), "ScreenToGifSettingsRemoteObject.rem", System.Runtime.Remoting.WellKnownObjectMode.Singleton);
            }
            catch (Exception e)
            {
                LogWriter.Log(e, "It was not possible to register the IPC server.");
            }
        }

        internal static void UnregisterServer()
        {
            try
            {
                if (ServerChannel == null)
                    return;

                System.Runtime.Remoting.Channels.ChannelServices.UnregisterChannel(ServerChannel);

                ServerChannel = null;
            }
            catch (Exception e)
            {
                LogWriter.Log(e, "It was not possible to unregister the IPC server.");
            }
        }

        internal static void SendMessage(string serialized, bool isLocal)
        {
            try
            {
                var channel = new IpcChannel();

                System.Runtime.Remoting.Channels.ChannelServices.RegisterChannel(channel, true);

                //Register as client for remote object.
                var remoteType = new System.Runtime.Remoting.WellKnownClientTypeEntry(typeof(SettingsRemoteObject), "ipc://localhost:9193/ScreenToGifSettingsRemoteObject.rem");
                System.Runtime.Remoting.RemotingConfiguration.RegisterWellKnownClientType(remoteType);

                var service = new SettingsRemoteObject();
                service.SendSettings(serialized, isLocal);

                System.Runtime.Remoting.Channels.ChannelServices.UnregisterChannel(channel);
            }
            catch (Exception e)
            {
                LogWriter.Log(e, "It was not possible to send a message via the IPC server.");
            }
        }
        
        
        /// <summary>
        /// Object that is used as the inter process bag.
        /// </summary>
        public class SettingsRemoteObject : MarshalByRefObject
        {
            /// <summary>
            /// Method used to receive arguments from another instance.
            /// </summary>
            /// <param name="serialized">The serialized dictionary to be passed to the other instance.</param>
            /// <param name="isLocal">True if the settings is local.</param>
            public bool SendSettings(string serialized, bool isLocal)
            {
                try
                {
                    Settings.UserSettings.All.SaveFromAnotherInstance(serialized, isLocal);

                    return true;
                }
                catch (Exception e)
                {
                    LogWriter.Log(e, "Impossible to send/receive arguments via IPC.");
                    return false;
                }
                finally
                {
                    UnregisterServer();
                }
            }
        }
    }
}