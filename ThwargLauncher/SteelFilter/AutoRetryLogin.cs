﻿using System;

using Decal.Adapter;

namespace SteelFilter
{
	class AutoRetryLogin
	{
		readonly System.Windows.Forms.Timer loginRetryTimer = new System.Windows.Forms.Timer();
		int state;

		public AutoRetryLogin()
		{
			loginRetryTimer.Tick += new EventHandler(loginRetryTimer_Tick);
			loginRetryTimer.Interval = 200;
		}

		public void FilterCore_ClientDispatch(object sender, NetworkMessageEventArgs e)
		{
			if (e.Message.Type == 0xF7C8) // Enter Game - Big Login button clicked
				loginRetryTimer.Stop();
		}

		public void FilterCore_ServerDispatch(object sender, NetworkMessageEventArgs e)
		{
			if (e.Message.Type == 0xF659) // One of your characters is still in the world. Please try again in a few minutes.
			{
				state = 0;

				loginRetryTimer.Start();
			}
		}

		void loginRetryTimer_Tick(object sender, EventArgs e)
		{
			if (state == 0)
			{
				// Click the OK button
				Filter.Shared.PostMessageTools.ClickOK();

				state = 1;
			}
			else
			{
				// Click the Enter button
				Filter.Shared.PostMessageTools.SendMouseClick(0x015C, 0x0185);

				state = 0;
			}
		}
	}
}
