﻿#region "copyright"

/*
    Copyright © 2022 Christian Palm (christian@palm-family.de)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using EmbedIO;
using EmbedIO.Routing;
using EmbedIO.WebApi;
using NINA.Core.Utility;
using ninaAPI.Properties;
using System;
using System.Drawing;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace ninaAPI.WebService.V2
{
    public class ControllerV2 : WebApiController
    {

        [Route(HttpVerbs.Get, "/")]
        public string Index()
        {
            return $"ninaAPI: https://github.com/rennmaus-coder/ninaAPI/wiki/V2";
        }

        #region GET

        [Route(HttpVerbs.Get, "/version")]
        public void GetVersion()
        {
            Logger.Debug($"API call: {HttpContext.Request.Url.AbsoluteUri}");
            HttpContext.WriteToResponse(new HttpResponse() { Response = "1.0.0.0" });
        }

        [Route(HttpVerbs.Get, "/image-history")]
        public void GetHistoryCount([QueryField] bool all = false, [QueryField] int index = 0, [QueryField] bool count = false)
        {

            if (!CheckSecurity())
            {
                return;
            }

            Logger.Debug($"API call: {HttpContext.Request.Url.AbsoluteUri}");
            try
            {
                if (all)
                {
                    HttpContext.WriteToResponse(EquipmentMediatorV2.GetImageHistory(-1));
                }
                else if (count)
                {
                    HttpContext.WriteToResponse(EquipmentMediatorV2.GetImageHistoryCount());
                }
                else
                {
                    HttpContext.WriteToResponse(EquipmentMediatorV2.GetImageHistory(index));
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                HttpContext.WriteToResponse(Utility.CreateErrorTable(CommonErrors.UNKNOWN_ERROR));
            }
        }

        [Route(HttpVerbs.Get, "/event-history")]
        public void GetSocketHistoryCount()
        {
            if (!CheckSecurity())
            {
                return;
            }

            Logger.Debug($"API call: {HttpContext.Request.Url.AbsoluteUri}");
            try
            {
                HttpContext.WriteToResponse(EquipmentMediatorV2.GetSocketEventHistory());
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                HttpContext.WriteToResponse(Utility.CreateErrorTable(CommonErrors.UNKNOWN_ERROR));
            }
        }

        [Route(HttpVerbs.Get, "/sequence/{action}")]
        public void GetSequence(string action, [QueryField] bool skipValidation)
        {
            if (!CheckSecurity())
            {
                return;
            }

            Logger.Debug($"API call: {HttpContext.Request.Url.AbsoluteUri}");
            try
            {
                if (action.Equals("json"))
                {
                    HttpContext.WriteToResponse(EquipmentMediatorV2.GetSequence());
                }
                else
                {
                    HttpContext.WriteToResponse(EquipmentControllerV2.Sequence(action, skipValidation));
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                HttpContext.WriteToResponse(Utility.CreateErrorTable(CommonErrors.UNKNOWN_ERROR));
            }
        }

        [Route(HttpVerbs.Get, "/image/{index}")]
        public async Task GetImage(int index, [QueryField] bool resize = false, [QueryField] int quality = -1, [QueryField] string size = "640x480")
        {
            if (!CheckSecurity())
            {
                return;
            }

            Logger.Debug($"API call: {HttpContext.Request.Url.AbsoluteUri}");

            try
            {
                if (resize)
                {
                    string[] s = size.Split('x');
                    int width = int.Parse(s[0]);
                    int height = int.Parse(s[1]);
                    HttpContext.WriteToResponse(await EquipmentMediatorV2.GetImage(quality, index, new Size(width, height)));
                }
                else
                {
                    HttpContext.WriteToResponse(await EquipmentMediatorV2.GetImage(quality, index, Size.Empty));
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                HttpContext.WriteToResponse(Utility.CreateErrorTable(CommonErrors.UNKNOWN_ERROR));
            }
        }

        [Route(HttpVerbs.Get, "/profile/{action}")]
        public void SetProfile(string action, [QueryField] string profileid = "", [QueryField] string settingpath = "", [QueryField] object newValue = null, [QueryField] bool active = true)
        {
            if (!CheckSecurity())
            {
                return;
            }

            Logger.Debug($"API call: {HttpContext.Request.Url.AbsoluteUri}");
            try
            {
                switch (action)
                {
                    case "switch":
                        HttpContext.WriteToResponse(EquipmentControllerV2.SwitchProfile(profileid));
                        return;
                    case "change-value":
                        HttpContext.WriteToResponse(EquipmentControllerV2.ChangeProfileValue(settingpath, newValue));
                        return;
                    case "show":
                        HttpContext.WriteToResponse(EquipmentMediatorV2.GetProfile(active));
                        return;
                    default:
                        HttpContext.WriteToResponse(Utility.CreateErrorTable(CommonErrors.UNKNOWN_ACTION));
                        return;
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                HttpContext.WriteToResponse(Utility.CreateErrorTable(CommonErrors.UNKNOWN_ERROR));
            }
        }

        [Route(HttpVerbs.Get, "/application/{action}")]
        public void Application(string action, [QueryField] bool resize = false, [QueryField] int quality = -1, [QueryField] string size = "640x480", [QueryField] string tab = "")
        {
            if (!CheckSecurity())
            {
                return;
            }

            Logger.Debug($"API call: {HttpContext.Request.Url.AbsoluteUri}");
            try
            {

                switch (action)
                {
                    case "switch-tab":
                        HttpContext.WriteToResponse(EquipmentControllerV2.Application(tab));
                        return;
                    case "screenshot":
                        if (resize)
                        {
                            string[] s = size.Split('x');
                            int width = int.Parse(s[0]);
                            int height = int.Parse(s[1]);
                            HttpContext.WriteToResponse(EquipmentMediatorV2.Screenshot(quality, new Size(width, height)));
                        }
                        else
                        {
                            HttpContext.WriteToResponse(EquipmentMediatorV2.Screenshot(quality, Size.Empty));
                        }
                        return;
                    default:
                        HttpContext.WriteToResponse(Utility.CreateErrorTable(CommonErrors.UNKNOWN_ACTION));
                        return;
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                HttpContext.WriteToResponse(Utility.CreateErrorTable(CommonErrors.UNKNOWN_ERROR));
            }
        }

        #endregion

        #region Equipment

        [Route(HttpVerbs.Get, "/equipment/{device}/{action}")]
        public async Task EquipmentHandler(string device, string action, [QueryField] float position)
        {
            if (!CheckSecurity())
            {
                return;
            }

            Logger.Debug($"API call: {HttpContext.Request.Url.AbsoluteUri}");
            try
            {
                switch (device)
                {
                    case "camera":
                        if (action.Equals("info"))
                        {
                            HttpContext.WriteToResponse(EquipmentMediatorV2.GetDeviceInfo(EquipmentType.Camera));
                        }
                        else
                        {
                            HttpContext.WriteToResponse(await EquipmentControllerV2.Camera(action));
                        }
                        return;

                    case "telescope":
                        if (action.Equals("info"))
                        {
                            HttpContext.WriteToResponse(EquipmentMediatorV2.GetDeviceInfo(EquipmentType.Telescope));
                        }
                        else
                        {
                            HttpContext.WriteToResponse(await EquipmentControllerV2.Telescope(action));
                        }
                        return;

                    case "focuser":
                        if (action.Equals("info"))
                        {
                            HttpContext.WriteToResponse(EquipmentMediatorV2.GetDeviceInfo(EquipmentType.Focuser));
                        }
                        else
                        {
                            HttpContext.WriteToResponse(await EquipmentControllerV2.Focuser(action));
                        }
                        return;

                    case "filterwheel":
                        if (action.Equals("info"))
                        {
                            HttpContext.WriteToResponse(EquipmentMediatorV2.GetDeviceInfo(EquipmentType.FilterWheel));
                        }
                        else
                        {
                            HttpContext.WriteToResponse(await EquipmentControllerV2.FilterWheel(action));
                        }
                        return;

                    case "guider":
                        if (action.Equals("info"))
                        {   
                            HttpContext.WriteToResponse(EquipmentMediatorV2.GetDeviceInfo(EquipmentType.Guider));
                        }
                        else
                        {
                            HttpContext.WriteToResponse(await EquipmentControllerV2.Guider(action));
                        }
                        return;

                    case "dome":
                        if (action.Equals("info"))
                        {
                            HttpContext.WriteToResponse(EquipmentMediatorV2.GetDeviceInfo(EquipmentType.Dome));
                        }
                        else
                        {
                            HttpContext.WriteToResponse(await EquipmentControllerV2.Dome(action));
                        }
                        return;

                    case "rotator":
                        if (action.Equals("info"))
                        {
                            HttpContext.WriteToResponse(EquipmentMediatorV2.GetDeviceInfo(EquipmentType.Rotator));
                        }
                        else
                        {
                            HttpContext.WriteToResponse(await EquipmentControllerV2.Rotator(action, position));
                        }
                        return;

                    case "safetymonitor":
                        if (action.Equals("info"))
                        {
                            HttpContext.WriteToResponse(EquipmentMediatorV2.GetDeviceInfo(EquipmentType.SafetyMonitor));
                        }
                        else
                        {
                            HttpContext.WriteToResponse(await EquipmentControllerV2.SafetyMonitor(action));
                        }
                        return;

                    case "flatdevice":
                        if (action.Equals("info"))
                        {
                            HttpContext.WriteToResponse(EquipmentMediatorV2.GetDeviceInfo(EquipmentType.FlatDevice));
                        }
                        else
                        {
                            HttpContext.WriteToResponse(await EquipmentControllerV2.FlatDevice(action));
                        }
                        return;

                    case "switch":
                        if (action.Equals("info"))
                        {
                            HttpContext.WriteToResponse(EquipmentMediatorV2.GetDeviceInfo(EquipmentType.Switch));
                        }
                        else
                        {
                            HttpContext.WriteToResponse(await EquipmentControllerV2.Switch(action));
                        }
                        return;

                    case "weather":
                        if (action.Equals("info"))
                        {
                            HttpContext.WriteToResponse(EquipmentMediatorV2.GetDeviceInfo(EquipmentType.Weather));
                        }
                        else
                        {
                            HttpContext.WriteToResponse(await EquipmentControllerV2.Weather(action));
                        }
                        return;

                    default:
                        HttpContext.WriteToResponse(Utility.CreateErrorTable(new Error("Unknown Device", 400)));
                        return;
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                HttpContext.WriteToResponse(Utility.CreateErrorTable(CommonErrors.UNKNOWN_ERROR));
            }
        }

        #endregion

        private static bool checkKey(string key)
        {
            using (SHA256 sha = SHA256.Create())
            {
                return Utility.VerifyHash(sha, key, Settings.Default.ApiKey);
            }
        }

        private bool CheckSecurity()
        {
            if (Settings.Default.Secure && HttpContext.Request.Headers["apikey"] != null)
            {
                string apiKey = HttpContext.Request.Headers["apikey"];
                if (!checkKey(apiKey))
                {
                    Logger.Error(CommonErrors.INVALID_API_KEY.message);
                    Utility.CreateErrorTable(CommonErrors.INVALID_API_KEY);
                    return false;
                }
            }
            else if (Settings.Default.Secure && HttpContext.Request.Headers["apikey"] is null)
            {
                Logger.Error(CommonErrors.MISSING_API_KEY.message);
                Utility.CreateErrorTable(CommonErrors.MISSING_API_KEY);
                return false;
            }
            return true;
        }
    }
}