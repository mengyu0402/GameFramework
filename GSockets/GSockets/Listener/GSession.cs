﻿using System;
using System.Net.Sockets;
using GSockets;

namespace GSockets.Listener.Session
{
	/// <summary>
	/// socket session
	/// </summary>
	public class GSession
	{

		/// <summary>
		/// The listener.
		/// </summary>
		internal GSocketBase listener;

		/// <summary>
		/// session socket
		/// </summary>
		internal Socket socket;

		/// <summary>
		/// session ID
		/// </summary>
		public uint sid;

		/// <summary>
		/// The buffer stream.
		/// </summary>
		IBuffStream stream = null;

		/// <summary>
		/// Initializes
		/// </summary>
		/// <returns>The initializes.</returns>
		/// <param name="lenth">Lenth.</param>
		/// <typeparam name="TBuff">The 1st type parameter.</typeparam>
		internal void Initializes<TBuff>(int lenth) where TBuff : IBuffStream
		{
			stream = Activator.CreateInstance<TBuff>();
			stream.Resize(lenth);
		}

		/// <summary>
		/// Release this instance.
		/// </summary>
		public virtual void Release()
		{
			socket = null;
			stream.Zero();
		}

		/// <summary>
		/// disconect
		/// </summary>
		internal void Disconnect()
		{
			if (socket == null) return;

			if (socket.Connected) 
			{
				socket.Shutdown(SocketShutdown.Both);
				socket.Disconnect(false);
				socket.Close();
			}

			Release();
		}

		/// <summary>
		/// send mesage
		/// </summary>
		/// <param name="msgId">Message identifier.</param>
		/// <param name="message">Message.</param>
		public void SendMessage(uint msgId, object message)
		{
			SendBegin(listener.ToBytes(msgId, 0, SocketDefine.PACKET_STREAM, message));
		}

		/// <summary>
		/// begin recv message
		/// </summary>
		internal void ReceiveBegin()
		{
			try
			{
				if (socket == null) return;
				if (!socket.Connected) return;

				socket.BeginReceive(
					stream.buff, 
					stream.position, 
					stream.buff.Length - (stream.position + stream.length), 
					SocketFlags.None,
					new AsyncCallback(ReceiveEnd),
					this);
			}
			catch (Exception ex)
			{
				listener.PrintLog("Session - Receive Begin : sid:{0} message : {1} stack : {2}", sid, ex.Message, ex.StackTrace);
			}
		}

		/// <summary>
		/// end recv message
		/// </summary>
		/// <param name="ar">Ar.</param>
		void ReceiveEnd(IAsyncResult ar)
		{
			try
			{
				if (socket == null) return;

				int length = socket.EndReceive(ar);

				stream.length += length;

				//make packet
				PacketProcess();
			}
			catch (Exception ex)
			{
				listener.Disconnect(sid);
				listener.PrintLog("Session - Receive End : sid:{0} message : {1} stack : {2}", sid, ex.Message, ex.StackTrace);
			}
			finally
			{
				ReceiveBegin();
			}
		}

		/// <summary>
		/// make packet
		/// </summary>
		void PacketProcess()
		{
			int offset = stream.position;
			int length = stream.position + stream.length;

			while (offset < length)
			{
				GNetPacket netPacket = listener.ToNetPacket(stream.buff, offset, length);

				if (netPacket == null) break;

				offset += netPacket.body.Length + 9;

				listener.OnMessageEvent(this, netPacket);
			}

			//finish
			if (offset >= length) { stream.Zero(); return; }

			//封包内容过长
			if (offset == 0) return;

			//未处理完成，挪动数据
			Buffer.BlockCopy(stream.buff, offset, stream.buff, 0, length - offset);
			stream.position = 0;
			stream.length = length - offset;
		}

		/// <summary>
		/// send buff
		/// </summary>
		/// <param name="body">Body.</param>
		void SendBegin(byte[] body)
		{
			try
			{
				if (socket == null) return;

				socket.BeginSend(body, 0, body.Length, SocketFlags.None, new AsyncCallback(SendEnd), this);
			}
			catch (Exception ex)
			{ 
				listener.PrintLog("Session - Send Begin : sid:{0} message : {1} stack : {2}", sid, ex.Message, ex.StackTrace);
			}
		}

		/// <summary>
		/// send buff end
		/// </summary>
		/// <param name="ar">Ar.</param>
		void SendEnd(IAsyncResult ar)
		{
			try
			{
				int length = socket.EndSend(ar);
			}
			catch (Exception ex)
			{ 
				listener.PrintLog("Session - Send End : sid:{0} message : {1} stack : {2}", sid, ex.Message, ex.StackTrace);
			}
		}
	}
}