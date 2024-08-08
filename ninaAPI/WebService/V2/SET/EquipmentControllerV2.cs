﻿#region "copyright"

/*
    Copyright © 2022 Christian Palm (christian@palm-family.de)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Equipment.Interfaces;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Profile;
using NINA.Sequencer.Interfaces.Mediator;
using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace ninaAPI.WebService.V2.SET
{
    public class EquipmentControllerV2
    {
        private static CancellationTokenSource SequenceToken;
        private static CancellationTokenSource SlewToken;
        private static CancellationTokenSource GuideToken;
        private static CancellationTokenSource AFToken;
        private static CancellationTokenSource DomeToken;

        public static async Task<HttpResponse> Camera(string action)
        {
            HttpResponse response = new HttpResponse();
            ICameraMediator cam = AdvancedAPI.Controls.Camera;

            if (action.Equals("connect"))
            {
                if (!cam.GetInfo().Connected)
                {
                    await cam.Rescan();
                    await cam.Connect();
                }
                response.Response = "Camera connected";
                return response;
            }
            if (action.Equals("disconnect"))
            {
                if (cam.GetInfo().Connected)
                {
                    await cam.Disconnect();
                }
                response.Response = "Camera disconnected";
                return response;
            }
            if (action.Equals("abort-exposure"))
            {
                cam.AbortExposure();
                response.Response = "Exposure aborted";
                return response;
            }
            else
            {
                response = Utility.CreateErrorTable(CommonErrors.UNKNOWN_ACTION);
                return response;
            }
        }

        public static async Task<HttpResponse> Telescope(string action)
        {
            HttpResponse response = new HttpResponse();
            ITelescopeMediator telescope = AdvancedAPI.Controls.Telescope;

            if (action.Equals("connect"))
            {
                if (!telescope.GetInfo().Connected)
                {
                    await telescope.Rescan();
                    await telescope.Connect();
                }
                response.Response = "Telescope connected";
                return response;
            }
            else if (action.Equals("disconnect"))
            {
                if (telescope.GetInfo().Connected)
                {
                    await telescope.Disconnect();
                }
                response.Response = "Telescope disconnected";
                return response;
            }
            else if (action.Equals("park"))
            {
                if (telescope.GetInfo().Slewing)
                {
                    telescope.StopSlew();
                }
                SlewToken?.Cancel();
                SlewToken = new CancellationTokenSource();
                telescope.ParkTelescope(AdvancedAPI.Controls.StatusMediator.GetStatus(), SlewToken.Token);
                response.Response = "Parking";
                return response;
            }
            else if (action.Equals("unpark"))
            {
                if (!telescope.GetInfo().AtPark)
                {
                    return response;
                }
                SlewToken?.Cancel();
                SlewToken = new CancellationTokenSource();
                telescope.UnparkTelescope(AdvancedAPI.Controls.StatusMediator.GetStatus(), SlewToken.Token);
                response.Response = "Unparking";
                return response;
            }            else
            {
                response = Utility.CreateErrorTable(CommonErrors.UNKNOWN_ACTION);
                return response;
            }
        }

        public static async Task<HttpResponse> Focuser(string action)
        {
            HttpResponse response = new HttpResponse();
            IFocuserMediator focuser = AdvancedAPI.Controls.Focuser;

            if (action.Equals("connect"))
            {
                if (!focuser.GetInfo().Connected)
                {
                    await focuser.Rescan();
                    await focuser.Connect();
                }
                response.Response = "Focuser connected";
                return response;
            }
            else if (action.Equals("disconnect"))
            {
                if (focuser.GetInfo().Connected)
                {
                    await focuser.Disconnect();
                }
                response.Response = "Focuser disconnected";
                return response;
            }
            else if (action.Equals("auto-focus"))
            {
                if (AFToken != null)
                {
                    AFToken.Cancel();
                }
                AFToken = new CancellationTokenSource();
                AdvancedAPI.Controls.AutoFocusFactory.Create().StartAutoFocus(AdvancedAPI.Controls.FilterWheel.GetInfo().SelectedFilter, AFToken.Token, AdvancedAPI.Controls.StatusMediator.GetStatus());
                response.Response = "AutoFocus started";
                return response;
            }
            else
            {
                response = Utility.CreateErrorTable(CommonErrors.UNKNOWN_ACTION);
                return response;
            }
        }

        public static async Task<HttpResponse> Rotator(string action)
        {
            HttpResponse response = new HttpResponse();
            IRotatorMediator rotator = AdvancedAPI.Controls.Rotator;

            if (action.Equals("connect"))
            {
                if (!rotator.GetInfo().Connected)
                {
                    await rotator.Rescan();
                    await rotator.Connect();
                }
                response.Response = "Rotator connected";
                return response;
            }
            else if (action.Equals("disconnect"))
            {
                if (rotator.GetInfo().Connected)
                {
                    await rotator.Disconnect();
                }
                response.Response = "Rotator disconnected";
                return response;
            }
            else
            {
                response = Utility.CreateErrorTable(CommonErrors.UNKNOWN_ACTION);
                return response;
            }
        }

        public static async Task<HttpResponse> FilterWheel(string action)
        {
            HttpResponse response = new HttpResponse();
            IFilterWheelMediator filterwheel = AdvancedAPI.Controls.FilterWheel;

            if (action.Equals("connect"))
            {
                if (!filterwheel.GetInfo().Connected)
                {
                    await filterwheel.Rescan();
                    await filterwheel.Connect();
                }
                response.Response = "Filterwheel connected";
                return response;
            }
            else if (action.Equals("disconnect"))
            {
                if (filterwheel.GetInfo().Connected)
                {
                    await filterwheel.Disconnect();
                }
                response.Response = "Filterwheel disconnected";
                return response;
            }
            else
            {
                response = Utility.CreateErrorTable(CommonErrors.UNKNOWN_ACTION);
                return response;
            }
        }

        public static async Task<HttpResponse> Dome(string action)
        {
            HttpResponse response = new HttpResponse();
            IDomeMediator dome = AdvancedAPI.Controls.Dome;

            if (action.Equals("connect"))
            {
                if (!dome.GetInfo().Connected)
                {
                    await dome.Rescan();
                    await dome.Connect();
                }
                response.Response = "Dome connected";
                return response;
            }
            else if (action.Equals("disconnect"))
            {
                if (dome.GetInfo().Connected)
                {
                    await dome.Disconnect();
                }
                response.Response = "Dome disconnected";
                return response;
            }
            else if (action.Equals("open"))
            {
                if (dome.GetInfo().ShutterStatus == ShutterState.ShutterOpen || dome.GetInfo().ShutterStatus == ShutterState.ShutterOpening)
                {
                    response.Response = "Shutter already open";
                    return response;
                }
                DomeToken?.Cancel();
                DomeToken = new CancellationTokenSource();
                dome.OpenShutter(DomeToken.Token);
                response.Response = "Shutter opening";
                return response;
            }
            else if (action.Equals("close"))
            {
                if (dome.GetInfo().ShutterStatus == ShutterState.ShutterClosed || dome.GetInfo().ShutterStatus == ShutterState.ShutterClosing)
                {
                    response.Response = "Shutter already closed";
                    return response;
                }
                DomeToken?.Cancel();
                DomeToken = new CancellationTokenSource();
                dome.CloseShutter(DomeToken.Token);
                response.Response = "Shutter closing";
                return response;
            }
            else if (action.Equals("stop")) // Can only stop movement that was started by the api
            {
                DomeToken?.Cancel();
                response.Response = "Movement stopped";
                return response;
            }
            else
            {
                response = Utility.CreateErrorTable(CommonErrors.UNKNOWN_ACTION);
                return response;
            }
        }

        public static async Task<HttpResponse> Switch(string action)
        {
            HttpResponse response = new HttpResponse();
            ISwitchMediator switches = AdvancedAPI.Controls.Switch;

            if (action.Equals("connect"))
            {
                if (!switches.GetInfo().Connected)
                {
                    await switches.Rescan();
                    await switches.Connect();
                }
                response.Response = "Switch connected";
                return response;
            }
            else if (action.Equals("disconnect"))
            {
                if (switches.GetInfo().Connected)
                {
                    await switches.Disconnect();
                }
                response.Response = "Switch disconnected";
                return response;
            }
            else
            {
                response = Utility.CreateErrorTable(CommonErrors.UNKNOWN_ACTION);
                return response;
            }
        }

        public static async Task<HttpResponse> Guider(string action)
        {
            HttpResponse response = new HttpResponse();
            IGuiderMediator guider = AdvancedAPI.Controls.Guider;

            if (action.Equals("connect"))
            {
                if (!guider.GetInfo().Connected)
                {
                    await guider.Rescan();
                    await guider.Connect();
                }
                response.Response = "Guider connected";
                return response;
            }
            else if (action.Equals("disconnect"))
            {
                if (guider.GetInfo().Connected)
                {
                    await guider.Disconnect();
                }
                response.Response = "Guider disconnected";
                return response;
            }
            else if (action.Equals("start"))
            {
                if (guider.GetInfo().Connected)
                {
                    GuideToken?.Cancel();
                    GuideToken = new CancellationTokenSource();
                    await guider.StartGuiding(false, AdvancedAPI.Controls.StatusMediator.GetStatus(), GuideToken.Token);
                    response.Response = "Guiding started";
                    return response;
                }
                return Utility.CreateErrorTable(new Error("Guider not connected", 409));
            }
            else if (action.Equals("stop"))
            {
                if (guider.GetInfo().Connected)
                {
                    await guider.StopGuiding(GuideToken.Token);
                    response.Response = "Guiding stopped";
                    return response;
                }
                return Utility.CreateErrorTable(new Error("Guider not connected", 409));
            }
            else
            {
                response = Utility.CreateErrorTable(CommonErrors.UNKNOWN_ACTION);
                return response;
            }
        }

        public static async Task<HttpResponse> FlatDevice(string action)
        {
            HttpResponse response = new HttpResponse();
            IFlatDeviceMediator flat = AdvancedAPI.Controls.FlatDevice;

            if (action.Equals("connect"))
            {
                if (!flat.GetInfo().Connected)
                {
                    await flat.Rescan();
                    await flat.Connect();
                }
                response.Response = "FlatDevice connected";
                return response;
            }
            else if (action.Equals("disconnect"))
            {
                if (flat.GetInfo().Connected)
                {
                    await flat.Disconnect();
                }
                response.Response = "FlatDevice disconnected";
                return response;
            }
            else
            {
                response = Utility.CreateErrorTable(CommonErrors.UNKNOWN_ACTION);
                return response;
            }
        }

        public static async Task<HttpResponse> SafetyMonitor(string action)
        {
            HttpResponse response = new HttpResponse();
            ISafetyMonitorMediator safety = AdvancedAPI.Controls.SafetyMonitor;

            if (action.Equals("connect"))
            {
                if (!safety.GetInfo().Connected)
                {
                    await safety.Rescan();
                    await safety.Connect();
                }
                response.Response = "Safetymonitor connected";
                return response;
            }
            else if (action.Equals("disconnect"))
            {
                if (safety.GetInfo().Connected)
                {
                    await safety.Disconnect();
                }
                response.Response = "Safetymonitor disconnected";
                return response;
            }
            else
            {
                response = Utility.CreateErrorTable(CommonErrors.UNKNOWN_ACTION);
                return response;
            }
        }

        public static async Task<HttpResponse> Sequence(string action)
        {
            HttpResponse response = new HttpResponse();
            ISequenceMediator sequence = AdvancedAPI.Controls.Sequence;

            if (action.Equals("start"))
            {
                SequenceToken = new CancellationTokenSource();
                sequence.GetAllTargets()[0].Parent.Parent.Run(AdvancedAPI.Controls.StatusMediator.GetStatus(), SequenceToken.Token);
                response.Response = "Sequence started";
                return response;
            }
            else if (action.Equals("stop")) // Can only stop the sequence if it was started by the api
            {
                SequenceToken?.Cancel();
                response.Response = "Sequence stopped";
                return response;
            }
            else
            {
                response = Utility.CreateErrorTable(CommonErrors.UNKNOWN_ACTION);
                return response;
            }
        }

        public static async Task<HttpResponse> Application(string applicationTab)
        {
            HttpResponse response = new HttpResponse();

            response.Response = "Switched tab";
            switch (applicationTab)
            {
                case "equipment":
                    AdvancedAPI.Controls.Application.ChangeTab(NINA.Core.Enum.ApplicationTab.EQUIPMENT);
                    return response;
                case "skyatlas":
                    AdvancedAPI.Controls.Application.ChangeTab(NINA.Core.Enum.ApplicationTab.SKYATLAS);
                    return response;
                case "framing":
                    AdvancedAPI.Controls.Application.ChangeTab(NINA.Core.Enum.ApplicationTab.FRAMINGASSISTANT);
                    return response;
                case "flatwizard":
                    AdvancedAPI.Controls.Application.ChangeTab(NINA.Core.Enum.ApplicationTab.FLATWIZARD);
                    return response;
                case "sequencer":
                    AdvancedAPI.Controls.Application.ChangeTab(NINA.Core.Enum.ApplicationTab.SEQUENCE);
                    return response;
                case "imaging":
                    AdvancedAPI.Controls.Application.ChangeTab(NINA.Core.Enum.ApplicationTab.IMAGING);
                    return response;
                case "options":
                    AdvancedAPI.Controls.Application.ChangeTab(NINA.Core.Enum.ApplicationTab.OPTIONS);
                    return response;
                default:
                    return Utility.CreateErrorTable(new Error("Invalid ApplicationTab", 400));
            }
        }

        public static HttpResponse ChangeProfileValue(string settingPath, object newValue)
        {
            HttpResponse response = new HttpResponse();
            if (string.IsNullOrEmpty(settingPath))
            {
                return Utility.CreateErrorTable(new Error("Invalid Path", 400));
            }
            if (newValue is null)
            {
                return Utility.CreateErrorTable(new Error("new value can't be null", 400));
            }

            string[] pathSplit = settingPath.Split('-'); // e.g. 'CameraSettings-PixelSize' -> CameraSettings, PixelSize
            object position = AdvancedAPI.Controls.Profile.ActiveProfile;

            if (pathSplit.Length == 1)
            {
                position.GetType().GetProperty(settingPath).SetValue(position, newValue);
                return response;
            }
            for (int i = 0; i <= pathSplit.Length - 2; i++)
            {
                position = position.GetType().GetProperty(pathSplit[i]).GetValue(position);
            }
            PropertyInfo prop = position.GetType().GetProperty(pathSplit[^1]);
            prop.SetValue(position, ((string)newValue).CastString(prop.PropertyType));
            response.Response = "Updated setting";
            return response;
        }

        public static HttpResponse SwitchProfile(string profileID)
        {
            HttpResponse response = new HttpResponse();
            Guid guid = Guid.Parse(profileID);
            ProfileMeta profile = AdvancedAPI.Controls.Profile.Profiles.Where(x => x.Id == guid).FirstOrDefault();
            AdvancedAPI.Controls.Profile.SelectProfile(profile);
            response.Response = "Successfully switched profile";
            return response;
        }
    }
}
