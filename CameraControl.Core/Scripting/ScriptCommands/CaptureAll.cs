#region Licence

// Distributed under MIT License
// ===========================================================
// 
// digiCamControl - DSLR camera remote control open source software
// Copyright (C) 2014 Duka Istvan
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, 
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF 
// MERCHANTABILITY,FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. 
// IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY 
// CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
// TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH 
// THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

#endregion

#region

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using CameraControl.Core.Classes;
using CameraControl.Devices;

#endregion

namespace CameraControl.Core.Scripting.ScriptCommands
{
    public class CaptureAll : BaseScript
    {
        public override bool Execute(ScriptObject scriptObject)
        {
            foreach (ICameraDevice cameraDevice in ServiceProvider.DeviceManager.ConnectedDevices)
            {
                ICameraDevice device = cameraDevice;
                new Thread(() => Capture(device)).Start();
            }
            return true;
        }

        private void Capture(ICameraDevice device)
        {
            try
            {
                CameraHelper.Capture(device);
            }
            catch (Exception e)
            {
                StaticHelper.Instance.SystemMessage = e.Message;
            }
        }

        public CaptureAll()
        {
            Name = "captureall";
            Description = "Trigger capture command on all connected cameras";
            DefaultValue = "captureall";
        }
    }
}