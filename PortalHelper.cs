using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;

using log4net;

using Loki;
using Loki.Bot;
using Loki.Bot.Logic.Behaviors;
using Loki.Bot.Logic.Bots.Grind;
using Loki.Bot.Pathfinding;
using Loki.Game;
using Loki.Game.Inventory;
using Loki.Game.NativeWrappers;
using Loki.Game.Objects;
using Loki.TreeSharp;
using Loki.Utilities;
using Loki.Utilities.Plugins;
using Action = System.Action;

namespace ExileBot
{
    class PortalHelper : IPlugin
    {
        private static readonly ILog Log = Logger.GetLoggerInstanceForType();
        private static readonly int _minimumPortalDistance = 20000;
        private static readonly int _minimumHealthPercent = 30;
        private string _lastArea = "3_3_14_2"; //null;
        private WaitTimer _slowPulseTimer = new WaitTimer(TimeSpan.FromSeconds(1));
        private bool _botNeedsStart = false;

        #region Implementation of IEquatable<IPlugin>

        public bool Equals(IPlugin other) { return Name.Equals(other.Name); }

        #endregion

        #region Implementation of IPlugin

        public string Author { get { return "Ben"; } }
        public Version Version { get { return new Version(1, 0, 0, 0); } }
        public string Name { get { return "Portal Helper"; } }
        public string Description { get { return "Creates a portal when you are near death and uses it if you die."; } }


        public void OnInitialize() { }
        public void OnStop() { }
        public void OnShutdown() { }
        public void OnEnabled() { }
        public void OnDisabled() { }
        public void OnConfig() { }
        public void OnStart() { }

        #endregion

        public void OnPulse()
        {
            if (!_slowPulseTimer.IsFinished) return;
            _slowPulseTimer.Reset();

            if (LokiPoe.ObjectManager.Me.IsInTown)
            {
                Portal portal = portalFromTown();
                //Log.Debug("===\nFOUND PORTAL\n===");

                if (portal != null)
                {
                    if (portal.Distance > 30)
                    {
                        //Log.Debug("===\nMOVING TO PORTAL\n===");
                        BotManager.CurrentBot.Stop();
                        CommonBehaviors.MoveTo(ret => portal.Position, ret => "moving to Portal", 13);
                    }
                    else
                    {
                        //Log.Debug("===\nINTERACTING WITH PORTAL\n===");
                        portal.Interact(false, false, LokiPoe.InputTargetType.MustMatchTarget);
                        _botNeedsStart = true;
                    }
                }
            }
            else
            {
                if (_botNeedsStart)
                {
                    BotManager.CurrentBot.Start();
                    _botNeedsStart = false;
                }

                if (LokiPoe.LocalData.WorldAreaId != null)
                {
                    _lastArea = LokiPoe.LocalData.WorldAreaId;
                }

                if (portalFromArea() == null && LokiPoe.ObjectManager.Me.HealthPercent < _minimumHealthPercent)
                {
                    //Log.Debug("====\nMAKING PORTAL\n====");
                    makeTP();
                }
            }
        }

        #region Portal Helpers

        private static void makeTP()
        {
            LokiPoe.ObjectManager.Me.Inventory.Main.FindItem("Portal Scroll").Use();
        }

        public static Portal portalFromArea()
        {
            LokiPoe.ObjectManager.ClearCache();

            if (LokiPoe.ObjectManager.Me.IsInTown)
                return null;

            try
            {
                if (LokiPoe.ObjectManager.Portals.Count(p => p.Distance < _minimumPortalDistance) < 1)
                    return null;

                return LokiPoe.ObjectManager.Portals.First(p => p.Distance < _minimumPortalDistance);
            }
            catch (Exception e)
            {
                return null;
            }
        }

        public Portal portalFromTown()
        {
            LokiPoe.ObjectManager.ClearCache();

            if (!LokiPoe.ObjectManager.Me.IsInTown)
                return null;

            try
            {
                PortalObject po = LokiPoe.LocalData.TownPortals.First(o => o.AreaId == _lastArea);

                if (po == null)
                    return null;

                Portal p = LokiPoe.ObjectManager.Portals.First(o => o.Name.Contains(po.OwnerName));

                if (p == null)
                    return null;

                return p;
            }
            catch (Exception e)
            {
                return null;
            }
        }

        #endregion
    }
}
