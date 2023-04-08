using NTwain;
using NTwain.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace WinFormSample
{
  public partial class Form1 : Form
  {
    private TwainAppSession twain;
    private readonly string saveFolder;

    public Form1()
    {
      InitializeComponent();
      var libVer = FileVersionInfo.GetVersionInfo(typeof(TwainAppSession).Assembly.Location).ProductVersion;
      Text += $"{(TwainPlatform.Is32bit ? " 32bit" : " 64bit")} on NTwain {libVer}";

      TwainPlatform.PreferLegacyDSM = false;

      twain = new TwainAppSession(new WinformMarshaller(this), Assembly.GetExecutingAssembly().Location);
      twain.StateChanged += Twain_StateChanged;
      twain.DefaultSourceChanged += Twain_DefaultSourceChanged;
      twain.CurrentSourceChanged += Twain_CurrentSourceChanged;
      twain.TransferReady += Twain_TransferReady;
      twain.DataTransferred += Twain_DataTransferred;
      twain.TransferError += Twain_TransferError;
      twain.DeviceEvent += Twain_DeviceEvent;

      capListView.SetDoubleBuffered(true);

      saveFolder = Path.Combine(Path.GetTempPath(), "ntwain-sample");
      Directory.CreateDirectory(saveFolder);

      this.Disposed += Form1_Disposed;
    }

    protected override void OnHandleCreated(EventArgs e)
    {
      base.OnHandleCreated(e);


      var hwnd = this.Handle;
      var rc = twain.OpenDSM(hwnd);
      twain.AddWinformFilter();
      Debug.WriteLine($"OpenDSM={rc}");
    }

    protected override void OnClosing(CancelEventArgs e)
    {
      var finalState = twain.TryStepdown(STATE.S2);
      Debug.WriteLine($"Stepdown result state={finalState}");
      twain.RemoveWinformFilter();
      base.OnClosing(e);
    }

    private void Form1_Disposed(object? sender, EventArgs e)
    {
      twain.Dispose();
    }


    private void Twain_DeviceEvent(TwainAppSession sender, TW_DEVICEEVENT e)
    {
      Debug.WriteLine($"[thread {Environment.CurrentManagedThreadId}] device event {e}.");

    }

    private void Twain_TransferError(TwainAppSession sender, TransferErrorEventArgs e)
    {
      if (e.Exception != null)
      {
        Debug.WriteLine($"[thread {Environment.CurrentManagedThreadId}] transfer error {e.Exception}.");
      }
      else
      {
        Debug.WriteLine($"[thread {Environment.CurrentManagedThreadId}] transfer error {e.Code}.");
      }

    }

    private void Twain_DataTransferred(TwainAppSession sender, DataTransferredEventArgs e)
    {
      Debug.WriteLine($"[thread {Environment.CurrentManagedThreadId}] data transferred with info {e.ImageInfo}");
      if (e.Data == null) return;
      using (var stream = new MemoryStream(e.Data))
      using (var img = Image.FromStream(stream))
      {
        var saveFile = Path.Combine(saveFolder, (DateTime.Now.Ticks / 1000) + ".png");
        img.Save(saveFile, ImageFormat.Png);
        Debug.WriteLine($"Saved image to {saveFile}");
      }
    }

    private void Twain_TransferReady(TwainAppSession sender, TransferReadyEventArgs e)
    {
      Debug.WriteLine($"[thread {Environment.CurrentManagedThreadId}] transfer ready.");
    }

    private void Twain_DefaultSourceChanged(TwainAppSession sender, TW_IDENTITY_LEGACY ds)
    {
      lblDefault.Text = ds.ProductName;
    }

    private void Twain_StateChanged(TwainAppSession sender, STATE state)
    {
      Invoke(() => lblState.Text = state.ToString());
    }

    private void Twain_CurrentSourceChanged(TwainAppSession sender, TW_IDENTITY_LEGACY ds)
    {
      lblCurrent.Text = ds.ProductName;
      if (twain.State == STATE.S4)
      {
        LoadCapInfoList();

        // never seen a driver support these but here it is
        var sts = twain.GetCapLabel(CAP.ICAP_SUPPORTEDSIZES, out string? test);
        var sts2 = twain.GetCapHelp(CAP.ICAP_SUPPORTEDSIZES, out string? test2);
        var sts3 = twain.GetCapLabelEnum(CAP.ICAP_SUPPORTEDSIZES, out IList<string>? test3);

        if (sts.RC == TWRC.SUCCESS || sts2.RC == TWRC.SUCCESS || sts3.RC == TWRC.SUCCESS)
        {
          Debugger.Break();
        }
      }
      else
      {
        capListView.Items.Clear();
      }
    }

    private void LoadCapInfoList()
    {
      twain.GetCapValues(CAP.CAP_SUPPORTEDCAPS, out IList<CAP> caps);
      twain.GetCapValues(CAP.CAP_EXTENDEDCAPS, out IList<CAP> extended);
      foreach (var c in caps)
      {
        ListViewItem it = new(c.ToString());

        if (twain.GetCapCurrent(c, out TW_CAPABILITY twcap).RC == TWRC.SUCCESS)
        {
          var type = twcap.DetermineValueType(twain);
          it.SubItems.Add(type.ToString());
          it.SubItems.Add(ReadTypedValue(c, type, forCurrent: true));
          it.SubItems.Add(ReadTypedValue(c, type, forCurrent: false));
        }
        else
        {
          it.SubItems.Add("");
          it.SubItems.Add("");
          it.SubItems.Add("");
        }
        it.SubItems.Add(extended.Contains(c).ToString());
        it.SubItems.Add(twain.QueryCapSupport(c).ToString());
        capListView.Items.Add(it);
      }
    }

    // there may be a better way...
    private string ReadTypedValue(CAP cap, TWTY type, bool forCurrent)
    {
      switch (type)
      {
        case TWTY.UINT8:
          var sts = forCurrent ?
            twain.GetCapCurrent(cap, out byte ubval) :
            twain.GetCapDefault(cap, out ubval);
          return ubval.ToString();
        case TWTY.INT8:
          sts = forCurrent ?
            twain.GetCapCurrent(cap, out sbyte sbval) :
            twain.GetCapDefault(cap, out sbval);
          return sbval.ToString();
        case TWTY.UINT16:
          sts = forCurrent ?
            twain.GetCapCurrent(cap, out ushort usval) :
            twain.GetCapDefault(cap, out usval);
          return usval.ToString();
        case TWTY.INT16:
          sts = forCurrent ?
            twain.GetCapCurrent(cap, out short ssval) :
            twain.GetCapDefault(cap, out ssval);
          return ssval.ToString();
        case TWTY.UINT32:
          sts = forCurrent ?
            twain.GetCapCurrent(cap, out uint uival) :
            twain.GetCapDefault(cap, out uival);
          return uival.ToString();
        case TWTY.INT32:
          sts = forCurrent ?
            twain.GetCapCurrent(cap, out int sival) :
            twain.GetCapDefault(cap, out sival);
          return sival.ToString();
        case TWTY.BOOL:
          sts = forCurrent ?
            twain.GetCapCurrent(cap, out TW_BOOL tbval) :
            twain.GetCapDefault(cap, out tbval);
          return tbval.ToString();
        case TWTY.FIX32:
          sts = forCurrent ?
            twain.GetCapCurrent(cap, out TW_FIX32 fxval) :
            twain.GetCapDefault(cap, out fxval);
          return fxval.ToString();
        case TWTY.FRAME:
          sts = forCurrent ?
            twain.GetCapCurrent(cap, out TW_FRAME frval) :
            twain.GetCapDefault(cap, out frval);
          return frval.ToString();
        case TWTY.STR32:
          sts = forCurrent ?
            twain.GetCapCurrent(cap, out TW_STR32 s32val) :
            twain.GetCapDefault(cap, out s32val);
          return s32val.ToString();
        case TWTY.STR64:
          sts = forCurrent ?
            twain.GetCapCurrent(cap, out TW_STR64 s64val) :
            twain.GetCapDefault(cap, out s64val);
          return s64val.ToString();
        case TWTY.STR128:
          sts = forCurrent ?
            twain.GetCapCurrent(cap, out TW_STR128 s128val) :
            twain.GetCapDefault(cap, out s128val);
          return s128val.ToString();
        case TWTY.STR255:
          sts = forCurrent ?
            twain.GetCapCurrent(cap, out TW_STR255 s255val) :
            twain.GetCapDefault(cap, out s255val);
          return s255val.ToString();
        case TWTY.HANDLE:
          break;
      }
      return "";
    }

    private void btnSelect_Click(object sender, EventArgs e)
    {
      twain.ShowUserSelect();
    }

    private void btnEnumSources_Click(object sender, EventArgs e)
    {
      listSources.Items.Clear();
      foreach (var ds in twain.GetSources())
      {
        listSources.Items.Add(ds);
      }
    }

    private void btnSetDef_Click(object sender, EventArgs e)
    {
      if (listSources.SelectedItem is TW_IDENTITY_LEGACY ds)
      {
        twain.SetDefaultSource(ds);
      }
    }

    private void btnOpen_Click(object sender, EventArgs e)
    {
      if (listSources.SelectedItem is TW_IDENTITY_LEGACY ds)
      {
        twain.TryStepdown(STATE.S3);

        twain.OpenSource(ds);
      }
    }

    private void btnClose_Click(object sender, EventArgs e)
    {
      twain.CloseSource();
    }

    private void btnOpenDef_Click(object sender, EventArgs e)
    {
      twain.TryStepdown(STATE.S3);

      twain.OpenSource(twain.DefaultSource);
    }

    private void btnShowSettings_Click(object sender, EventArgs e)
    {
      twain.EnableSource(true, true);
    }

    private void btnStart_Click(object sender, EventArgs e)
    {
      twain.EnableSource(true, false);
    }
  }
}