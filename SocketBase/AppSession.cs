﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Authentication;
using System.Text;
using System.Threading;
using SuperSocket.Common;
using SuperSocket.SocketBase.Command;
using SuperSocket.SocketBase.Config;
using AnyLog;
using SuperSocket.SocketBase.Protocol;
using SuperSocket.ProtoBase;

namespace SuperSocket.SocketBase
{
    /// <summary>
    /// AppSession base class
    /// </summary>
    /// <typeparam name="TAppSession">The type of the app session.</typeparam>
    /// <typeparam name="TPackageInfo">The type of the request info.</typeparam>
    public abstract class AppSession<TAppSession, TPackageInfo> : AppSession<TAppSession, TPackageInfo, string>
        where TAppSession : AppSession<TAppSession, TPackageInfo>, IAppSession, new()
        where TPackageInfo : class, IPackageInfo<string>
    {

    }

    /// <summary>
    /// AppSession base class
    /// </summary>
    /// <typeparam name="TAppSession">The type of the app session.</typeparam>
    /// <typeparam name="TPackageInfo">The type of the request info.</typeparam>
    /// <typeparam name="TKey">The type of the package key.</typeparam>
    public abstract class AppSession<TAppSession, TPackageInfo, TKey> : IAppSession, IAppSession<TAppSession, TPackageInfo>, IPackageHandler<TPackageInfo>, IThreadExecutingContext, ICommunicationChannel
        where TAppSession : AppSession<TAppSession, TPackageInfo, TKey>, IAppSession, new()
        where TPackageInfo : class, IPackageInfo, IPackageInfo<TKey>
    {
        #region Properties

        /// <summary>
        /// Gets the app server instance assosiated with the session.
        /// </summary>
        public virtual AppServer<TAppSession, TPackageInfo, TKey> AppServer { get; private set; }

        /// <summary>
        /// Gets the app server instance assosiated with the session.
        /// </summary>
        IAppServer IAppSession.AppServer
        {
            get { return this.AppServer; }
        }

        /// <summary>
        /// Gets or sets the charset which is used for transfering text message.
        /// </summary>
        /// <value>
        /// The charset.
        /// </value>
        public Encoding Charset { get; set; }

        private IDictionary<object, object> m_Items;

        /// <summary>
        /// Gets the items dictionary, only support 10 items maximum
        /// </summary>
        public IDictionary<object, object> Items
        {
            get
            {
                if (m_Items == null)
                    m_Items = new Dictionary<object, object>(10);

                return m_Items;
            }
        }


        private bool m_Connected = false;

        /// <summary>
        /// Gets a value indicating whether this <see cref="IAppSession"/> is connected.
        /// </summary>
        /// <value>
        ///   <c>true</c> if connected; otherwise, <c>false</c>.
        /// </value>
        public bool Connected
        {
            get { return m_Connected; }
            internal set { m_Connected = value; }
        }

        /// <summary>
        /// Gets or sets the previous command.
        /// </summary>
        /// <value>
        /// The prev command.
        /// </value>
        public TKey PrevCommand { get; set; }

        /// <summary>
        /// Gets or sets the current executing command.
        /// </summary>
        /// <value>
        /// The current command.
        /// </value>
        public TKey CurrentCommand { get; set; }


        /// <summary>
        /// Gets or sets the secure protocol of transportation layer.
        /// </summary>
        /// <value>
        /// The secure protocol.
        /// </value>
        public SslProtocols SecureProtocol
        {
            get { return SocketSession.SecureProtocol; }
            set { SocketSession.SecureProtocol = value; }
        }

        /// <summary>
        /// Gets the local listening endpoint.
        /// </summary>
        public IPEndPoint LocalEndPoint
        {
            get { return SocketSession.LocalEndPoint; }
        }

        /// <summary>
        /// Gets the remote endpoint of client.
        /// </summary>
        public IPEndPoint RemoteEndPoint
        {
            get { return SocketSession.RemoteEndPoint; }
        }

        /// <summary>
        /// Gets the logger.
        /// </summary>
        public ILog Logger
        {
            get { return AppServer.Logger; }
        }

        /// <summary>
        /// Gets or sets the last active time of the session.
        /// </summary>
        /// <value>
        /// The last active time.
        /// </value>
        public DateTime LastActiveTime { get; set; }

        /// <summary>
        /// Gets the start time of the session.
        /// </summary>
        public DateTime StartTime { get; private set; }

        /// <summary>
        /// Gets the session ID.
        /// </summary>
        public string SessionID { get; private set; }

        /// <summary>
        /// Gets the socket session of the AppSession.
        /// </summary>
        public ISocketSession SocketSession { get; private set; }

        /// <summary>
        /// Gets the proto handler.
        /// </summary>
        /// <value>
        /// The proto handler.
        /// </value>
        public IProtoHandler ProtoHandler { get; private set; }

        /// <summary>
        /// Gets the config of the server.
        /// </summary>
        public IServerConfig Config
        {
            get { return AppServer.Config; }
        }

        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="AppSession&lt;TAppSession, TPackageInfo&gt;"/> class.
        /// </summary>
        public AppSession()
        {
            this.StartTime = DateTime.Now;
            this.LastActiveTime = this.StartTime;
        }


        /// <summary>
        /// Initializes the specified app session by AppServer and SocketSession.
        /// </summary>
        /// <param name="appServer">The app server.</param>
        /// <param name="socketSession">The socket session.</param>
        public virtual void Initialize(IAppServer<TAppSession, TPackageInfo> appServer, ISocketSession socketSession)
        {
            var castedAppServer = (AppServer<TAppSession, TPackageInfo, TKey>)appServer;
            AppServer = castedAppServer;
            Charset = castedAppServer.TextEncoding;
            SocketSession = socketSession;
            SessionID = socketSession.SessionID;
            m_Connected = true;
            
            socketSession.Initialize(this);

            OnInit();
        }

        IPipelineProcessor IAppSession.CreatePipelineProcessor()
        {
            var receiveFilterFactory = AppServer.ReceiveFilterFactory;
            var receiveFilter = receiveFilterFactory.CreateFilter(AppServer, this, SocketSession.RemoteEndPoint);
            return new DefaultPipelineProcessor<TPackageInfo>(this, receiveFilter, AppServer.Config.MaxRequestLength, SocketSession as IBufferRecycler);
        }

        /// <summary>
        /// Starts the session.
        /// </summary>
        void IAppSession.StartSession()
        {
            OnSessionStarted();
        }

        /// <summary>
        /// Called when [init].
        /// </summary>
        protected virtual void OnInit()
        {

        }

        /// <summary>
        /// Called when [session started].
        /// </summary>
        protected virtual void OnSessionStarted()
        {

        }

        /// <summary>
        /// Called when [session closed].
        /// </summary>
        /// <param name="reason">The reason.</param>
        internal protected virtual void OnSessionClosed(CloseReason reason)
        {

        }


        /// <summary>
        /// Handles the exceptional error, it only handles application error.
        /// </summary>
        /// <param name="e">The exception.</param>
        protected virtual void HandleException(Exception e)
        {
            Logger.Error(e.Message, e, this);
            this.Close(CloseReason.ApplicationError);
        }

        /// <summary>
        /// Handles the unknown request.
        /// </summary>
        /// <param name="requestInfo">The request info.</param>
        protected virtual void HandleUnknownRequest(TPackageInfo requestInfo)
        {

        }

        internal void InternalHandleUnknownRequest(TPackageInfo requestInfo)
        {
            HandleUnknownRequest(requestInfo);
        }

        internal void InternalHandleExcetion(Exception e)
        {
            HandleException(e);
        }

        /// <summary>
        /// Closes the session by the specified reason.
        /// </summary>
        /// <param name="reason">The close reason.</param>
        public virtual void Close(CloseReason reason)
        {
            var protoHandler = ProtoHandler;

            if (protoHandler != null)
            {
                protoHandler.Close(this, reason);
                return;
            }                

            SocketSession.Close(reason);
        }

        /// <summary>
        /// Closes this session.
        /// </summary>
        public virtual void Close()
        {
            Close(CloseReason.ServerClosing);
        }

        #region Sending processing

        /// <summary>
        /// Try to send the data to client.
        /// </summary>
        /// <param name="data">The data which will be sent.</param>
        /// <param name="offset">The offset.</param>
        /// <param name="length">The length.</param>
        /// <returns>Indicate whether the message was pushed into the sending queue</returns>
        public virtual bool TrySend(byte[] data, int offset, int length)
        {
            return AppServer.ProtoSender.TrySend(SocketSession, ProtoHandler, new ArraySegment<byte>(data, offset, length));
        }

        /// <summary>
        /// Sends the data to client.
        /// </summary>
        /// <param name="data">The data which will be sent.</param>
        /// <param name="offset">The offset.</param>
        /// <param name="length">The length.</param>
        public virtual void Send(byte[] data, int offset, int length)
        {
            AppServer.ProtoSender.Send(SocketSession, ProtoHandler, new ArraySegment<byte>(data, offset, length));
        }

        /// <summary>
        /// Try to send the data segment to client.
        /// </summary>
        /// <param name="segment">The segment which will be sent.</param>
        /// <returns>Indicate whether the message was pushed into the sending queue</returns>
        public virtual bool TrySend(ArraySegment<byte> segment)
        {
            return AppServer.ProtoSender.TrySend(SocketSession, ProtoHandler, segment);
        }

        /// <summary>
        /// Sends the data segment to client.
        /// </summary>
        /// <param name="segment">The segment which will be sent.</param>
        public virtual void Send(ArraySegment<byte> segment)
        {
            AppServer.ProtoSender.Send(SocketSession, ProtoHandler, segment);
        }

        /// <summary>
        /// Try to send the data segments to client.
        /// </summary>
        /// <param name="segments">The segments.</param>
        /// <returns>Indicate whether the message was pushed into the sending queue; if it returns false, the sending queue may be full or the socket is not connected</returns>
        public virtual bool TrySend(IList<ArraySegment<byte>> segments)
        {
            return AppServer.ProtoSender.TrySend(SocketSession, ProtoHandler, segments);
        }

        /// <summary>
        /// Sends the data segments to client.
        /// </summary>
        /// <param name="segments">The segments.</param>
        public virtual void Send(IList<ArraySegment<byte>> segments)
        {
            AppServer.ProtoSender.Send(SocketSession, ProtoHandler, segments);
        }

        void ICommunicationChannel.Send(ArraySegment<byte> segment)
        {
            SocketSession.TrySend(segment);
        }

        void ICommunicationChannel.Close(CloseReason reason)
        {
            SocketSession.Close(reason);
        }

        #endregion

        void IPackageHandler<TPackageInfo>.Handle(TPackageInfo package)
        {
            try
            {
                AppServer.ExecuteCommand(this, package);
            }
            catch (Exception e)
            {
                HandleException(e);
            }
        }


        #region IThreadExecutingContext

        private int m_PreferedThreadId;

        /// <summary>
        /// Gets or sets the prefered executing thread's id.
        /// </summary>
        /// <value>
        /// The prefered thread id.
        /// </value>
        int IThreadExecutingContext.PreferedThreadId
        {
            get { return m_PreferedThreadId; }
            set { m_PreferedThreadId = value; }
        }

        void IThreadExecutingContext.Increment(int value)
        {
            while (true)
            {
                var oldValue = m_PreferedThreadId;
                var targetValue = oldValue + value;

                if (Interlocked.CompareExchange(ref m_PreferedThreadId, targetValue, oldValue) == oldValue)
                    return;
            }
        }

        void IThreadExecutingContext.Decrement(int value)
        {
            while (true)
            {
                var oldValue = m_PreferedThreadId;
                var targetValue = Math.Max(0, oldValue - value);

                if (Interlocked.CompareExchange(ref m_PreferedThreadId, targetValue, oldValue) == oldValue)
                    return;
            }
        }

        #endregion
    }

    /// <summary>
    /// AppServer basic class for whose request infoe type is StringPackageInfo
    /// </summary>
    /// <typeparam name="TAppSession">The type of the app session.</typeparam>
    public abstract class AppSession<TAppSession> : AppSession<TAppSession, StringPackageInfo>
        where TAppSession : AppSession<TAppSession, StringPackageInfo>, IAppSession, new()
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AppSession&lt;TAppSession&gt;"/> class.
        /// </summary>
        public AppSession()
        {

        }


        /// <summary>
        /// Handles the unknown request.
        /// </summary>
        /// <param name="requestInfo">The request info.</param>
        protected override void HandleUnknownRequest(StringPackageInfo requestInfo)
        {
            Send("Unknown request: " + requestInfo.Key);
        }



        /// <summary>
        /// Sends the response.
        /// </summary>
        /// <param name="message">The message which will be sent.</param>
        /// <param name="paramValues">The parameter values.</param>
        public virtual void Send(string message, params object[] paramValues)
        {
            Send(string.Format(message, paramValues));
        }

        /// <summary>
        /// Try to send the message to client.
        /// </summary>
        /// <param name="message">The message which will be sent.</param>
        /// <returns>Indicate whether the message was pushed into the sending queue</returns>
        public virtual bool TrySend(string message)
        {
            var textEncoder = AppServer.TextEncoder;

            if (textEncoder != null)
            {
                return TrySend(textEncoder.EncodeText(message));
            }

            var data = this.Charset.GetBytes(message);
            return TrySend(new ArraySegment<byte>(data, 0, data.Length));
        }

        /// <summary>
        /// Sends the message to client.
        /// </summary>
        /// <param name="message">The message which will be sent.</param>
        public virtual void Send(string message)
        {
            var textEncoder = AppServer.TextEncoder;

            if (textEncoder != null)
            {
                Send(textEncoder.EncodeText(message));
                return;
            }

            var data = this.Charset.GetBytes(message);
            Send(new ArraySegment<byte>(data, 0, data.Length));
        }
    }

    /// <summary>
    /// AppServer basic class for whose request infoe type is StringPackageInfo
    /// </summary>
    public class AppSession : AppSession<AppSession>
    {

    }
}
