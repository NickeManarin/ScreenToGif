using System;
using System.Runtime.Remoting.Channels.Ipc;
using System.Windows;

namespace ScreenToGif.Util
{
    public static class InterProcess
    {
        /// <summary>
        /// Object that is used a the inter process bag.
        /// </summary>
        public class InterProcessRemoteObject : MarshalByRefObject
        {
            /// <summary>
            /// Method used to receive arguments from another instance.
            /// </summary>
            /// <param name="args">The arguments to be passed to the other instance.</param>
            public bool SendArguments(string[] args)
            {
                try
                {
                    if (args?.Length > 0)
                        Argument.Prepare(args);

                    Application.Current.Dispatcher.BeginInvoke(new Action(() => App.MainViewModel.OpenEditor.Execute(args)));

                    return true;
                }
                catch (Exception e)
                {
                    LogWriter.Log(e, "Impossible to send/receive arguments via IPC.");
                    return false;
                }
            }
        }

        internal static IpcChannel ServerChannel { get; private set; }


        internal static void RegisterServer()
        {
            try
            {
                if (ServerChannel != null)
                    return;

                ServerChannel = new IpcChannel("localhost:9192");
                System.Runtime.Remoting.Channels.ChannelServices.RegisterChannel(ServerChannel, true);

                //Expose an object for remote calls.
                System.Runtime.Remoting.RemotingConfiguration.RegisterWellKnownServiceType(typeof(InterProcessRemoteObject), "ScreenToGifRemoteObject.rem", System.Runtime.Remoting.WellKnownObjectMode.Singleton);
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

        internal static void SendMessage(string[] args)
        {
            try
            {
                var channel = new IpcChannel();

                System.Runtime.Remoting.Channels.ChannelServices.RegisterChannel(channel, true);

                //Register as client for remote object.
                var remoteType = new System.Runtime.Remoting.WellKnownClientTypeEntry(typeof(InterProcessRemoteObject), "ipc://localhost:9192/ScreenToGifRemoteObject.rem");
                System.Runtime.Remoting.RemotingConfiguration.RegisterWellKnownClientType(remoteType);

                var service = new InterProcessRemoteObject();
                service.SendArguments(args);
            }
            catch (Exception e)
            {
                LogWriter.Log(e, "It was not possible to send a message via the IPC server.");
            }
        }
    }
}