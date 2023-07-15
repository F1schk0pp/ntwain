#if WINDOWS
using NTwain.Data;
using NTwain.Native;
using NTwain.Triplets;
using System;
using System.Runtime.InteropServices;
using MSG = NTwain.Data.MSG;

namespace NTwain;

// contains parts for message loop integration

partial class TwainAppSession
{
    public bool WndProc(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam)
    {
        // this handles the message from a typical WndProc message loop and checks if it's for the TWAIN source.
        var handled = false;
        if (_state < STATE.S5)
            return handled;

        WIN_MESSAGE winMsg = new() {hwnd = hWnd, message = (uint)msg, wParam = wParam, lParam = lParam};
        // no need to do another lock call when using marshal alloc
        if (_procEvent.pEvent == IntPtr.Zero)
            _procEvent.pEvent = Marshal.AllocHGlobal(Marshal.SizeOf(winMsg));
        Marshal.StructureToPtr(winMsg, _procEvent.pEvent, true);

        if (_closeDsRequested)
            return handled;

        var rc = DGControl.Event.ProcessEvent(ref _appIdentity, ref _currentDS, ref _procEvent);
        handled = rc == TWRC.DSEVENT;
        if (_procEvent.TWMessage != 0 && (handled || rc == TWRC.NOTDSEVENT))
        {
            //Debug.WriteLine($"[thread {Environment.CurrentManagedThreadId}] CheckIfTwainMessage at state {State} with MSG={_procEvent.TWMessage}.");
            HandleSourceMsg((MSG)_procEvent.TWMessage);
        }

        return handled;
    }
}
#endif
