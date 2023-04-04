﻿using NTwain.Data;
using NTwain.DSM;

namespace NTwain.Triplets.ControlDATs
{
  /// <summary>
  /// Contains calls used with <see cref="DG.CONTROL"/> and <see cref="DAT.CALLBACK2"/>.
  /// </summary>
  public class Callback2
  {
    public TWRC RegisterCallback(ref TW_IDENTITY_LEGACY app, ref TW_IDENTITY_LEGACY ds, ref TW_CALLBACK2 data)
    {
      var rc = TWRC.FAILURE;
      if (TwainPlatform.IsWindows)
      {
        if (TwainPlatform.Is32bit && TwainPlatform.PreferLegacyDSM)
        {
          rc = WinLegacyDSM.DSM_Entry(ref app, ref ds, DG.CONTROL, DAT.CALLBACK2, MSG.REGISTER_CALLBACK, ref data);
        }
        else
        {
          rc = WinNewDSM.DSM_Entry(ref app, ref ds, DG.CONTROL, DAT.CALLBACK2, MSG.REGISTER_CALLBACK, ref data);
        }
      }
      else if (TwainPlatform.IsMacOSX)
      {
        TW_IDENTITY_MACOSX app2 = app;
        TW_IDENTITY_MACOSX ds2 = ds;
        if (TwainPlatform.PreferLegacyDSM)
        {
          rc = OSXLegacyDSM.DSM_Entry(ref app2, ref ds2, DG.CONTROL, DAT.CALLBACK2, MSG.REGISTER_CALLBACK, ref data);
        }
        else
        {
          rc = OSXNewDSM.DSM_Entry(ref app2, ref ds2, DG.CONTROL, DAT.CALLBACK2, MSG.REGISTER_CALLBACK, ref data);
        }
      }
      return rc;
    }
  }
}
