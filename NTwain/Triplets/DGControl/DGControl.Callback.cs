﻿using NTwain.Data;
using NTwain.Internals;

namespace NTwain.Triplets
{
	sealed class Callback : OpBase
	{
		internal Callback(ITwainStateInternal session) : base(session) { }
		/// <summary>
		/// This triplet is sent to the DSM by the Application to register the application’s entry point with
		/// the DSM, so that the DSM can use callbacks to inform the application of events generated by the
		/// DS.
		/// </summary>
		/// <param name="callback">The callback.</param>
		/// <returns></returns>
		public ReturnCode RegisterCallback(TWCallback callback)
		{
			Session.VerifyState(4, 4, DataGroups.Control, DataArgumentType.Callback, Message.RegisterCallback);
			return Dsm.DsmEntry(Session.AppId, Session.SourceId, Message.RegisterCallback, callback);
		}
	}
}