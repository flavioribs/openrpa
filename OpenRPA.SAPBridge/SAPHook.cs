﻿using Newtonsoft.Json;
using SAPFEWSELib;
using SapROTWr;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace OpenRPA.SAPBridge
{
    public class SAPHook
    {
        private static SAPHook _instance = null;
        public static SAPHook Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new SAPHook();
                    _instance.init();
                }
                return _instance;
            }
        }
        private GuiApplication _app = null;
        public GuiApplication app
        {
            get
            {
                if (_app != null)
                {
                    try
                    {
                        int count = _app.Children.Count;
                        if (count == 0)
                        {
                            _app = null;
                        }
                    }
                    catch (Exception)
                    {
                        _app = null;
                    }
                }
                if (_app == null) _app = GetSAPGuiApp(0);
                return _app;
            }
        }
        public void init()
        {
            _ = app;
        }
        public SAPEventElement[] UIElements = new SAPEventElement[] { };
        private void GetUIElements(List<SAPEventElement> list, GuiSession session, string Parent, SAPEventElement ele, bool VisibleOnly)
        {
            list.Add(ele);
            if (!string.IsNullOrEmpty(ele.Path))
            {
                System.Diagnostics.Trace.WriteLine(ele.ToString() + " " + ele.Rectangle);
            }
            // session.FindByPosition
            if (ele.Children == null) ele.Load(session, VisibleOnly);
            if (ele.Children != null)
                foreach (var child in ele.Children)
                {
                    GetUIElements(list, session, ele.Id, child, VisibleOnly);
                }
        }
        private void GetUIElements(List<SAPEventElement> list, GuiSession session, string Parent, GuiComponent Element, bool VisibleOnly)
        {
            var ele = new SAPEventElement(session, Element, session.Info.SystemName, false, null, null, false, true, 50, VisibleOnly);
            list.Add(ele);
            foreach (var child in ele.Children)
            {
                GetUIElements(list, session, ele.Id, child, VisibleOnly);
            }

            //var msg = new SAPEventElement(Element, session.Info.SystemName, Parent, false);
            //list.Add(msg);
            //if (Element.ContainerType)
            //{
            //    if (Element is GuiVContainer vcon)
            //    {
            //        for (var i = 0; i < vcon.Children.Count; i++)
            //        {
            //            GetUIElements(list, session, Element.Id, vcon.Children.ElementAt(i));
            //        }
            //    }
            //    else if (Element is GuiContainer con)
            //    {
            //        for (var i = 0; i < con.Children.Count; i++)
            //        {
            //            GetUIElements(list, session, Element.Id, con.Children.ElementAt(i));
            //        }
            //    }
            //    else if (Element is GuiStatusbar sbar)
            //    {
            //        for (var i = 0; i < sbar.Children.Count; i++)
            //        {
            //            GetUIElements(list, session, Element.Id, sbar.Children.ElementAt(i));
            //        }
            //    }
            //}
        }
        public bool refreshingui = false;
        public void RefreshUIElements(bool VisibleOnly)
        {
            if (!Recording || !VisualizationEnabled) return;
            Program.log("RefreshUIElements:: Begin with " + UIElements.Length + " in cache");
            if (refreshingui)
            {
                Program.log("RefreshUIElements:: all ready mapping ui");
                return;
            }
            // Stop highlighing "old" UI ???
            if (System.Threading.Monitor.TryEnter(UIElements, 1000))
            {
                try
                {
                    UIElements = new SAPEventElement[] { };
                }
                finally
                {
                    System.Threading.Monitor.Exit(UIElements);
                }
            }
            try
            {
                if (app == null) UIElements = new SAPEventElement[] { };
                refreshingui = true;
                var result = new List<SAPEventElement>();
                try
                {
                    if (app != null && app.Children != null)
                        for (int x = 0; x < app.Children.Count; x++)
                        {
                            var con = app.Children.ElementAt(x) as GuiConnection;
                            if (con.Sessions.Count == 0) continue;

                            for (int j = 0; j < con.Sessions.Count; j++)
                            {
                                var session = con.Children.ElementAt(j) as GuiSession;
                                var ele = session as GuiComponent;
                                GetUIElements(result, session, session.Id, ele, VisibleOnly);
                                //for (var i = 0; i < session.Children.Count; i++)
                                //{
                                //    GetUIElements(result, session, session.Id, session.Children.ElementAt(i), VisibleOnly);
                                //}
                            }
                        }
                    Program.log("RefreshUIElements:: Refresh complete");
                }
                catch (Exception ex)
                {
                    Program.log(ex.ToString());
                }
                if (System.Threading.Monitor.TryEnter(UIElements, 1000))
                {
                    try
                    {
                        UIElements = result.ToArray();
                    }
                    finally
                    {
                        System.Threading.Monitor.Exit(UIElements);
                    }
                }
            }
            catch (Exception)
            {

                throw;
            }
            refreshingui = false;
            Program.log("RefreshUIElements:: end with " + UIElements.Length + " in cache");
        }
        public SAPSession[] Sessions { get; private set; } = new SAPSession[] { };
        public SAPConnection[] Connections { get; private set; } = new SAPConnection[] { };
        public void RefreshSessions()
        {
            if (app == null)
            {
                Sessions = new SAPSession[] { };
                Connections = new SAPConnection[] { };
                return;
            }
            var connections = new List<SAPConnection>();
            var sessions = new List<SAPSession>();
            for (int i = 0; i < app.Children.Count; i++)
            {
                var con = app.Children.ElementAt(i) as GuiConnection;
                if (con.Sessions.Count == 0) continue;
                var sapconnection = new SAPConnection();
                sapconnection.ConnectionString = con.ConnectionString;
                sapconnection.Description = con.Description;
                sapconnection.DisabledByServer = con.DisabledByServer;
                sapconnection.Id = con.Id;
                sapconnection.Name = con.Name;

                var sapsessions = new List<SAPSession>();
                for (int j = 0; j < con.Sessions.Count; j++)
                {
                    var session = con.Children.ElementAt(j) as GuiSession;
                    var sapsession = new SAPSession();
                    sapsession.Busy = session.Busy;
                    sapsession.Id = session.Id;
                    sapsession.Info = new SAPSessionInfo();
                    sapsession.Info.ApplicationServer = session.Info.ApplicationServer;
                    sapsession.Info.Client = session.Info.Client;
                    sapsession.Info.Codepage = session.Info.Codepage;
                    sapsession.Info.Flushes = session.Info.Flushes;
                    sapsession.Info.Group = session.Info.Group;
                    sapsession.Info.GuiCodepage = session.Info.GuiCodepage;
                    sapsession.Info.I18NMode = session.Info.I18NMode;
                    sapsession.Info.InterpretationTime = session.Info.InterpretationTime;
                    sapsession.Info.IsLowSpeedConnection = session.Info.IsLowSpeedConnection;
                    sapsession.Info.Language = session.Info.Language;
                    sapsession.Info.MessageServer = session.Info.MessageServer;
                    sapsession.Info.Program = session.Info.Program;
                    sapsession.Info.ResponseTime = session.Info.ResponseTime;
                    sapsession.Info.RoundTrips = session.Info.RoundTrips;
                    sapsession.Info.ScreenNumber = session.Info.ScreenNumber;
                    sapsession.Info.ScriptingModeReadOnly = session.Info.ScriptingModeReadOnly;
                    sapsession.Info.ScriptingModeRecordingDisabled = session.Info.ScriptingModeRecordingDisabled;
                    sapsession.Info.SessionNumber = session.Info.SessionNumber;
                    sapsession.Info.SystemName = session.Info.SystemName;
                    sapsession.Info.SystemNumber = session.Info.SystemNumber;
                    sapsession.Info.SystemSessionId = session.Info.SystemSessionId;
                    sapsession.Info.Transaction = session.Info.Transaction;
                    sapsession.Info.User = session.Info.User;
                    sapsession.IsActive = session.IsActive;
                    sapsession.IsListBoxActive = session.IsListBoxActive;
                    sapsession.Name = session.Name;
                    sapsession.ProgressPercent = session.ProgressPercent;
                    sapsession.ProgressText = session.ProgressText;
                    sapsession.Record = session.Record;
                    sapsession.RecordFile = session.RecordFile;
                    sapsession.SaveAsUnicode = session.SaveAsUnicode;
                    sapsession.ShowDropdownKeys = session.ShowDropdownKeys;
                    // sapsession.SuppressBackendPopups = session.SuppressBackendPopups;
                    sapsession.TestToolMode = session.TestToolMode;
                    sapsession.ActiveWindow = new SAPWindow();
                    sapsession.ActiveWindow.Changeable = session.ActiveWindow.Changeable;
                    sapsession.ActiveWindow.Handle = session.ActiveWindow.Handle;
                    sapsession.ActiveWindow.Height = session.ActiveWindow.Height;
                    sapsession.ActiveWindow.Width = session.ActiveWindow.Width;
                    sapsession.ActiveWindow.Left = session.ActiveWindow.Left;
                    sapsession.ActiveWindow.Top = session.ActiveWindow.Top;
                    sapsession.ActiveWindow.ScreenLeft = session.ActiveWindow.ScreenLeft;
                    sapsession.ActiveWindow.ScreenTop = session.ActiveWindow.ScreenTop;
                    sapsession.ActiveWindow.Iconic = session.ActiveWindow.Iconic;
                    sapsession.ActiveWindow.IconName = session.ActiveWindow.IconName;
                    sapsession.ActiveWindow.Id = session.ActiveWindow.Id;
                    sapsession.ActiveWindow.Name = session.ActiveWindow.Name;
                    sapsession.ActiveWindow.Text = session.ActiveWindow.Text;
                    sapsession.ActiveWindow.Tooltip = session.ActiveWindow.Tooltip;
                    sapsession.ActiveWindow.WorkingPaneHeight = session.ActiveWindow.WorkingPaneHeight;
                    sapsession.ActiveWindow.WorkingPaneWidth = session.ActiveWindow.WorkingPaneWidth;
                    sessions.Add(sapsession);
                    sapsessions.Add(sapsession);
                }
                sapconnection.sessions = sapsessions.ToArray();
                connections.Add(sapconnection);
            }
            Sessions = sessions.ToArray();
            Connections = connections.ToArray();
        }
        public bool Recording { get; private set; } = false;
        public bool VisualizationEnabled { get; private set; } = false;
        public void BeginRecord(bool VisualizationEnabled)
        {
            this.VisualizationEnabled = VisualizationEnabled;
            if (app == null) return;
            if (app.Connections.Count == 0) return;
            for (int i = 0; i < app.Children.Count; i++)
            {
                var con = app.Children.ElementAt(i) as GuiConnection;
                if (con.Sessions.Count == 0) continue;

                for (int j = 0; j < con.Sessions.Count; j++)
                {
                    var session = con.Children.ElementAt(j) as GuiSession;
                    session.Change -= Session_Change;
                    session.Change += Session_Change;
                    session.AbapScriptingEvent -= Session_AbapScriptingEvent;
                    session.AbapScriptingEvent += Session_AbapScriptingEvent;
                    session.Record = true;
                    Recording = true;
                }
                //if (VisualizationEnabled)
                //{
                //    for (int j = 0; j < con.Children.Count; j++)
                //    {
                //        var ses = con.Children.ElementAt(i) as GuiSession;
                //        for (int y = 0; y < ses.Children.Count; y++)
                //        {
                //            var fWin = ses.Children.ElementAt(y) as GuiFrameWindow;
                //            if (fWin != null)
                //            {
                //                fWin.ElementVisualizationMode = true;
                //            }
                //        }
                //    }
                //}
            }
        }
        internal bool Login(SAPLoginEvent message)
        {
            SAPLogon l = new SAPLogon();
            l.StartProcess();
            GuiSession session = null;
            try
            {
                if (app == null)
                {
                    _app = GetSAPGuiApp(10);
                }
                if (app == null) throw new Exception("Failed launching SAP");
                if (app.Connections.Count != 0)
                {
                    for (int i = 0; i < app.Children.Count; i++)
                    {
                        var con = app.Children.ElementAt(i) as GuiConnection;
                        if (con.Sessions.Count == 0) continue;

                        for (int j = 0; j < con.Sessions.Count; j++)
                        {
                            var ses = con.Children.ElementAt(j) as GuiSession;
                            if (ses.Info.SystemName.ToLower() == message.SystemName.ToLower())
                            {
                                session = ses;
                                return true;
                            }
                        }
                        if (session != null) break;
                    }
                }
                // SAPTestHelper.Current.CloseAllConnections();
                // SAPTestHelper.Current.SetSession();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            if (app == null || session == null)
            {
                l.OpenConnection(message.Host);
                return l.Login(message.Username, message.Password, message.Client, message.Language);
            }
            return false;
        }
        public GuiSession GetSession() { return GetSession(""); }
        public GuiSession GetSession(string SystemName)
        {
            var application = app;
            if (app == null) return null;
            if (app.Connections.Count == 0) return null;
            for (int i = 0; i < app.Children.Count; i++)
            {
                var con = application.Children.ElementAt(i) as GuiConnection;
                if (con.Sessions.Count == 0) continue;

                for (int j = 0; j < con.Sessions.Count; j++)
                {
                    var session = con.Children.ElementAt(j) as GuiSession;
                    if (string.IsNullOrEmpty(SystemName) || session.Info.SystemName.ToLower() == SystemName.ToLower()) return session;
                }
            }
            return null;
        }
        public void EndRecord()
        {
            if (app == null) return;
            if (app.Connections.Count == 0) return;
            for (int i = 0; i < app.Children.Count; i++)
            {
                var con = app.Children.ElementAt(i) as GuiConnection;
                if (con.Sessions.Count == 0) continue;

                for (int j = 0; j < con.Sessions.Count; j++)
                {
                    var session = con.Children.ElementAt(j) as GuiSession;
                    session.Change -= Session_Change;
                    session.AbapScriptingEvent -= Session_AbapScriptingEvent;
                    Recording = false;
                    session.Record = false;
                }
                for (int j = 0; j < con.Children.Count; j++)
                {
                    var ses = con.Children.ElementAt(i) as GuiSession;
                    for (int y = 0; y < ses.Children.Count; y++)
                    {
                        var fWin = ses.Children.ElementAt(y) as GuiFrameWindow;
                        if (fWin != null)
                        {
                            fWin.ElementVisualizationMode = false;
                        }
                    }
                }
            }
        }
        private void Session_AbapScriptingEvent(string param)
        {
        }
        private void Session_Change(GuiSession Session, GuiComponent Component, object CommandArray)
        {
            // if (Program.recordstarting) return;
            object[] objs = CommandArray as object[];
            objs = objs[0] as object[];
            var Action = "SetProperty";
            switch (objs[0].ToString().ToLower())
            {
                case "m":
                    Action = "InvokeMethod";
                    break;
                case "sp":
                    Action = "SetProperty";
                    break;
            }
            var ActionName = objs[1].ToString();
            upperFirstChar(ref ActionName);
            string id = Component.Id;
            var pathToRoot = new List<GuiComponent>();
            GuiComponent element = Component;
            while (element != null)
            {
                // Program.log(element.Id);
                if (element is GuiSession)
                {
                    id = id.Substring(element.Id.Length + 1);
                }
                pathToRoot.Add(element);
                element = element.Parent as GuiComponent;
            }
            Program.log(Action + " " + ActionName + " " + id);
            var e = new SAPRecordingEvent();
            e.Action = Action;
            e.ActionName = ActionName;
            e.Name = Component.Name;
            e.Type = Component.Type;
            e.TypeAsNumber = Component.TypeAsNumber;
            e.ContainerType = Component.ContainerType;
            e.Id = id;
            var idarr = id.Split('/');
            var sbarpath = idarr[0] + "/sbar/pane[0]";
            GuiStatusPane sbar = Session.GetSAPComponentById<GuiStatusPane>(sbarpath);
            if(sbar != null)
            {
                e.StatusBarText = sbar.Text;
            }
            try
            {
                if (objs.Length > 2)
                {
                    var s = objs[1];
                    var _temparr = new object[objs.Length - 2];
                    Array.Copy(objs, 2, _temparr, 0, _temparr.Length);
                    e.Parameters = JsonConvert.SerializeObject(_temparr);

                }
                Program.log(e.Action + " " + e.ActionName + " " + e.Id);
                e.SystemName = Session.Info.SystemName;
                var msg = new SAPEvent("recorderevent"); msg.Set(e);
                if(Program.log_send_message) Program.log("[send] " + msg.action);
                Program.pipe.PushMessage(msg);
            }
            catch (Exception ex)
            {
                Program.log(ex.ToString());
            }
        }
        private static void upperFirstChar(ref string s)
        {
            if (!string.IsNullOrEmpty(s))
            {
                s = char.ToUpper(s[0]) + s.Substring(1);
            }
        }
        public static GuiApplication GetSAPGuiApp(int secondsOfTimeout = 10)
        {
            GuiApplication result = null;
            try
            {
                // https://answers.sap.com/questions/12487790/what-are-the-differences-in-vba-methods-to-connect.html?childToView=12494892
                SapROTWr.CSapROTWrapper sapROTWrapper = new SapROTWr.CSapROTWrapper();
                result = getSAPGuiApp(sapROTWrapper, secondsOfTimeout);
            }
            catch (Exception ex)
            {
                Program.log(ex.ToString());
            }
            return result;
        }
        private static GuiApplication getSAPGuiApp(CSapROTWrapper sapROTWrapper, int secondsOfTimeout)
        {
            object SapGuilRot = sapROTWrapper.GetROTEntry("SAPGUI");
            if (secondsOfTimeout < 0)
            {
                throw new TimeoutException(string.Format("Can get sap script engine in {0} seconds", secondsOfTimeout));
            }
            else
            {
                if (SapGuilRot == null)
                {
                    if (secondsOfTimeout > 0)
                    {
                        System.Threading.Thread.Sleep(1000);
                        return getSAPGuiApp(sapROTWrapper, secondsOfTimeout - 1);
                    }
                    else
                    {
                        return null;
                    }
                }
                else
                {
                    object engine = SapGuilRot.GetType().InvokeMember("GetSCriptingEngine", System.Reflection.BindingFlags.InvokeMethod, null, SapGuilRot, null);
                    if (engine == null)
                        throw new NullReferenceException("No SAP GUI application found");
                    return engine as GuiApplication;
                }
            }
        }
        public void SetVisualMode(bool on)
        {
            for (int y = 0; y < app.Connections.Count; y++)
            {
                var con = app.Connections.Item(y) as GuiConnection;
                for (int j = 0; j < con.Children.Count; j++)
                {
                    var ses = con.Children.ElementAt(j) as GuiSession;
                    for (int x = 0; x < ses.Children.Count; x++)
                    {
                        var fWin = ses.Children.ElementAt(x) as GuiFrameWindow;
                        if (fWin != null)
                        {
                            fWin.ElementVisualizationMode = on;
                        }
                    }
                }
            }
        }
    }
}
