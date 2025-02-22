﻿using System;
using System.Collections.Generic;

using Filter.Shared;

using Decal.Adapter;

namespace SteelFilter
{
	class LoginCompleteMessageQueueManager
	{
		bool freshLogin;

		readonly Queue<string> loginMessageQueue = new Queue<string>();
		bool sendingLastEnter;

		public void FilterCore_ClientDispatch(object sender, NetworkMessageEventArgs e)
		{
            if (e.Message.Type == 0xF7C8) // Enter Game
            {
                freshLogin = true;
                try
                {
                    LaunchControl.RecordLaunchResponse(DateTime.UtcNow);
                }
                catch
                {
                    log.WriteInfo("FilterCore_ClientDispatch: Exception trying to record launch response");
                }
                Heartbeat.LaunchHeartbeat();
            }

			if (freshLogin && e.Message.Type == 0xF7B1 && Convert.ToInt32(e.Message["action"]) == 0xA1) // Character Materialize (Any time is done portalling in, login or portal)
			{
                freshLogin = false;

				if (loginMessageQueue.Count > 0)
				{
                    sendingLastEnter = false;
					CoreManager.Current.RenderFrame += new EventHandler<EventArgs>(Current_RenderFrame);
				}
			}
		}

        private void DebugMessage(NetworkMessageEventArgs e)
        {
            bool dbg = true;
            for (int i = 0; i < e.Message.Count; ++i)
            {
                object oval = e.Message[i];
                if (oval != null)
                {
                    string sval = oval.ToString();
                }
            }
        }

		void Current_RenderFrame(object sender, EventArgs e)
		{
			try
			{
				if (loginMessageQueue.Count == 0 && sendingLastEnter == false)
				{
					CoreManager.Current.RenderFrame -= new EventHandler<EventArgs>(Current_RenderFrame);
					return;
				}

				if (sendingLastEnter)
				{
					PostMessageTools.SendEnter();
					sendingLastEnter = false;
				}
				else
				{
					PostMessageTools.SendEnter();
                    string cmd = loginMessageQueue.Dequeue();
                    PostMessageTools.SendMsg(cmd);
                    sendingLastEnter = true;
				}
			}
			catch (Exception ex) { Debug.LogException(ex); }
		}

		public void FilterCore_CommandLineText(object sender, ChatParserInterceptEventArgs e)
		{
			if (e.Text.StartsWith("/tf lcmq add "))
			{
				loginMessageQueue.Enqueue(e.Text.Substring(13, e.Text.Length - 13));
				Debug.WriteToChat("Login Complete Message Queue added: " + e.Text);

				e.Eat = true;
			}
			else if (e.Text.StartsWith("/tf lmq add ")) // Backwards Compatability
			{
				loginMessageQueue.Enqueue(e.Text.Substring(12, e.Text.Length - 12));
				Debug.WriteToChat("Login Complete Message Queue added: " + e.Text);

				e.Eat = true;
			}
			else if (e.Text == "/tf lcmq clear" || e.Text == "/tf lmq clear")
			{
				loginMessageQueue.Clear();
				Debug.WriteToChat("Login Complete Message Queue cleared");

				e.Eat = true;
			}
		}
	}
}
