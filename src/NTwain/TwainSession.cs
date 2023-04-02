﻿using NTwain.Triplets;
using System;
using System.Diagnostics;
using System.Text;
using TWAINWorkingGroup;

namespace NTwain
{
  // this file contains initialization/cleanup things.

  public partial class TwainSession
  {
    static bool __encodingRegistered;

    /// <summary>
    /// Creates TWAIN session with app info derived an executable file.
    /// </summary>
    /// <param name="exeFilePath"></param>
    /// <param name="appLanguage"></param>
    /// <param name="appCountry"></param>
    public TwainSession(string exeFilePath,
        TWLG appLanguage = TWLG.ENGLISH_USA, TWCY appCountry = TWCY.USA) :
        this(FileVersionInfo.GetVersionInfo(exeFilePath),
          appLanguage, appCountry)
    { }
    /// <summary>
    /// Creates TWAIN session with app info derived from a <see cref="FileVersionInfo"/> object.
    /// </summary>
    /// <param name="appInfo"></param>
    /// <param name="appLanguage"></param>
    /// <param name="appCountry"></param>
    public TwainSession(FileVersionInfo appInfo,
        TWLG appLanguage = TWLG.ENGLISH_USA, TWCY appCountry = TWCY.USA) :
        this(appInfo.CompanyName ?? "",
          appInfo.ProductName ?? "",
          appInfo.ProductName ?? "",
          new Version(appInfo.FileVersion ?? "1.0"),
          appInfo.FileDescription ?? "", appLanguage, appCountry)
    { }
    /// <summary>
    /// Creates TWAIN session with explicit app info.
    /// </summary>
    /// <param name="companyName"></param>
    /// <param name="productFamily"></param>
    /// <param name="productName"></param>
    /// <param name="productVersion"></param>
    /// <param name="productDescription"></param>
    /// <param name="appLanguage"></param>
    /// <param name="appCountry"></param>
    /// <param name="supportedTypes"></param>
    public TwainSession(string companyName, string productFamily, string productName,
        Version productVersion, string productDescription = "",
        TWLG appLanguage = TWLG.ENGLISH_USA, TWCY appCountry = TWCY.USA,
        DG supportedTypes = DG.IMAGE)
    {
      if (!__encodingRegistered)
      {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        __encodingRegistered = true;
      }

      _appIdentity = new()
      {
        Manufacturer = companyName,
        ProductFamily = productFamily,
        ProductName = productName,
        ProtocolMajor = (ushort)TWON_PROTOCOL.MAJOR,
        ProtocolMinor = (ushort)TWON_PROTOCOL.MINOR,
        SupportedGroups = (uint)(supportedTypes | DG.CONTROL | DG.APP2),
        Version = new TW_VERSION
        {
          Country = appCountry,
          Info = productDescription,
          Language = appLanguage,
          MajorNum = (ushort)productVersion.Major,
          MinorNum = (ushort)productVersion.Minor,
        }
      };

      DGControl = new DGControl();
      DGImage = new DGImage();
      DGAudio = new DGAudio();

      _legacyCallbackDelegate = LegacyCallbackHandler;
      _osxCallbackDelegate = OSXCallbackHandler;
    }

    internal IntPtr _hwnd;
    internal TW_USERINTERFACE _userInterface;

    /// <summary>
    /// Loads and opens the TWAIN data source manager.
    /// </summary>
    /// <param name="hwnd">Required if on Windows.</param>
    /// <returns></returns>
    public STS OpenDSM(IntPtr hwnd)
    {
      var rc = DGControl.Parent.OpenDSM(ref _appIdentity, hwnd);
      if (rc == STS.SUCCESS)
      {
        _hwnd = hwnd;
        State = STATE.S3;
        // get default source
        if (DGControl.Identity.GetDefault(ref _appIdentity, out TW_IDENTITY_LEGACY ds) == STS.SUCCESS)
        {
          _defaultDS = ds;
          DefaultSourceChanged?.Invoke(this, _defaultDS);
        }

        // determine memory mgmt routines used
        if (((DG)AppIdentity.SupportedGroups & DG.DSM2) == DG.DSM2)
        {
          DGControl.EntryPoint.Get(ref _appIdentity, out _entryPoint);
        }
      }
      return rc;
    }


    /// <summary>
    /// Closes the TWAIN data source manager.
    /// </summary>
    /// <returns></returns>
    public STS CloseDSM()
    {
      var rc = DGControl.Parent.CloseDSM(ref _appIdentity, _hwnd);
      if (rc == STS.SUCCESS)
      {
        State = STATE.S2;
        _entryPoint = default;
        _defaultDS = default;
        DefaultSourceChanged?.Invoke(this, _defaultDS);
        _hwnd = IntPtr.Zero;
      }
      return rc;
    }

    /// <summary>
    /// Gets the last status code if an operation did not return success.
    /// This can only be done once after an error.
    /// </summary>
    /// <param name="forDsmOnly">true to get status for dsm operation error, false to get status for ds operation error,</param>
    /// <returns></returns>
    public TW_STATUS GetLastStatus(bool forDsmOnly)
    {
      if (forDsmOnly)
      {
        DGControl.Status.GetForDSM(ref _appIdentity, out TW_STATUS status);
        return status;
      }
      else
      {
        DGControl.Status.GetForDS(ref _appIdentity, ref _currentDS, out TW_STATUS status);
        return status;
      }
    }

    /// <summary>
    /// Tries to get string representation of a previously gotten status 
    /// from <see cref="GetLastStatus"/> if possible.
    /// </summary>
    /// <param name="status"></param>
    /// <returns></returns>
    public string? GetStatusText(TW_STATUS status)
    {
      if (DGControl.StatusUtf8.Get(ref _appIdentity, status, out TW_STATUSUTF8 extendedStatus) == STS.SUCCESS)
      {
        return extendedStatus.ReadAndFree(this);
      }
      return null;
    }

    /// <summary>
    /// Tries to bring the TWAIN session down to some state.
    /// </summary>
    /// <param name="targetState"></param>
    /// <returns>The final state.</returns>
    public STATE TryStepdown(STATE targetState)
    {
      int tries = 0;
      while (State > targetState)
      {
        if (tries++ > 5) break;

        switch (State)
        {
          case STATE.S5:
            DisableSource();
            break;
          case STATE.S4:
            CloseSource();
            break;
          case STATE.S3:
            CloseDSM();
            break;
        }
      }
      return State;
    }
  }
}
