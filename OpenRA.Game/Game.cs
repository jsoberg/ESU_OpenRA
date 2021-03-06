#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using OpenRA.Chat;
using OpenRA.Graphics;
using OpenRA.Network;
using OpenRA.Primitives;
using OpenRA.Support;
using OpenRA.Widgets;
using OpenRA.Traits;
using System.Threading.Tasks;
using FS = OpenRA.FileSystem.FileSystem;

namespace OpenRA
{
	public static class Game
	{
		public const int NetTickScale = 3; // 120 ms net tick for 40 ms local tick
		public const int Timestep = 1;
		public const int TimestepJankThreshold = 250; // Don't catch up for delays larger than 250ms

		public static ModData ModData;
		public static Settings Settings;
		public static ICursor Cursor;
		static WorldRenderer worldRenderer;

		internal static OrderManager OrderManager;
		static Server.Server server;

		public static MersenneTwister CosmeticRandom = new MersenneTwister(); // not synced

		public static Renderer Renderer;
		public static Sound Sound;
		public static bool HasInputFocus = false;

		public static bool BenchmarkMode = false;

		public static GlobalChat GlobalChat;

        private const int DEFAULT_FITNESS_TICKS = 1000;
        private static int FitnessLogTickIncrement = DEFAULT_FITNESS_TICKS;

		public static OrderManager JoinServer(string host, int port, string password, bool recordReplay = true)
		{
			var connection = new NetworkConnection(host, port);
			if (recordReplay)
				connection.StartRecording(TimestampedFilename);

			var om = new OrderManager(host, port, password, connection);
			JoinInner(om);
			return om;
		}

		static string TimestampedFilename()
		{
			return DateTime.UtcNow.ToString("OpenRA-yyyy-MM-ddTHHmmssZ");
		}

		static void JoinInner(OrderManager om)
		{
			if (OrderManager != null) OrderManager.Dispose();
			OrderManager = om;
			lastConnectionState = ConnectionState.PreConnecting;
			ConnectionStateChanged(OrderManager);
		}

		public static void JoinReplay(string replayFile)
		{
			JoinInner(new OrderManager("<no server>", -1, "", new ReplayConnection(replayFile)));
		}

		static void JoinLocal()
		{
			JoinInner(new OrderManager("<no server>", -1, "", new EchoConnection()));
		}

		// More accurate replacement for Environment.TickCount
		static Stopwatch stopwatch = Stopwatch.StartNew();
		public static int RunTime { get { return (int)stopwatch.ElapsedMilliseconds; } }

		public static int RenderFrame = 0;
		public static int NetFrameNumber { get { return OrderManager.NetFrameNumber; } }
		public static int LocalTick { get { return OrderManager.LocalFrameNumber; } }

		public static event Action<string, int> OnRemoteDirectConnect = (a, b) => { };
		public static event Action<OrderManager> ConnectionStateChanged = _ => { };
		static ConnectionState lastConnectionState = ConnectionState.PreConnecting;
		public static int LocalClientId { get { return OrderManager.Connection.LocalClientId; } }

		public static void RemoteDirectConnect(string host, int port)
		{
			OnRemoteDirectConnect(host, port);
		}

		// Hacky workaround for orderManager visibility
		public static Widget OpenWindow(World world, string widget)
		{
			return Ui.OpenWindow(widget, new WidgetArgs() { { "world", world }, { "orderManager", OrderManager }, { "worldRenderer", worldRenderer } });
		}

		// Who came up with the great idea of making these things
		// impossible for the things that want them to access them directly?
		public static Widget OpenWindow(string widget, WidgetArgs args)
		{
			return Ui.OpenWindow(widget, new WidgetArgs(args)
			{
				{ "world", worldRenderer.World },
				{ "orderManager", OrderManager },
				{ "worldRenderer", worldRenderer },
			});
		}

		// Load a widget with world, orderManager, worldRenderer args, without adding it to the widget tree
		public static Widget LoadWidget(World world, string id, Widget parent, WidgetArgs args)
		{
			return ModData.WidgetLoader.LoadWidget(new WidgetArgs(args)
			{
				{ "world", world },
				{ "orderManager", OrderManager },
				{ "worldRenderer", worldRenderer },
			}, parent, id);
		}

		public static event Action LobbyInfoChanged = () => { };

		internal static void SyncLobbyInfo()
		{
			LobbyInfoChanged();
		}

		public static event Action BeforeGameStart = () => { };
		internal static void StartGame(string mapUID, WorldType type)
		{
			// Dispose of the old world before creating a new one.
			if (worldRenderer != null)
				worldRenderer.Dispose();

			//Cursor.SetCursor(null);
			BeforeGameStart();

			Map map;

			using (new PerfTimer("PrepareMap"))
				map = ModData.PrepareMap(mapUID);
			using (new PerfTimer("NewWorld"))
                // JJS Issue 12, specify fitness log name in arguments
				OrderManager.World = new World(map, OrderManager, type);

		    worldRenderer = new WorldRenderer(OrderManager.World);

			using (new PerfTimer("LoadComplete"))
				OrderManager.World.LoadComplete(worldRenderer);

			if (OrderManager.GameStarted)
				return;
            
			//Ui.MouseFocusWidget = null;
			//Ui.KeyboardFocusWidget = null;

			OrderManager.LocalFrameNumber = 0;
			OrderManager.LastTickTime = RunTime;
			OrderManager.StartGame();
			//worldRenderer.RefreshPalette();
			//Cursor.SetCursor("default");

			GC.Collect();
		}

		public static void RestartGame()
		{
			var replay = OrderManager.Connection as ReplayConnection;
			var replayName = replay != null ? replay.Filename : null;
			var lobbyInfo = OrderManager.LobbyInfo;
			var orders = new[] {
					Order.Command("sync_lobby {0}".F(lobbyInfo.Serialize())),
					Order.Command("startgame")
			};

			// Disconnect from the current game
			Disconnect();
			Ui.ResetAll();

			// Restart the game with the same replay/mission
			if (replay != null)
				JoinReplay(replayName);
			else
				CreateAndStartLocalServer(lobbyInfo.GlobalSettings.Map, orders);
		}

		public static void CreateAndStartLocalServer(string mapUID, IEnumerable<Order> setupOrders, Action onStart = null)
		{
			OrderManager om = null;

			Action lobbyReady = null;
			lobbyReady = () =>
			{
				LobbyInfoChanged -= lobbyReady;
				foreach (var o in setupOrders)
					om.IssueOrder(o);

				if (onStart != null)
					onStart();
			};
			LobbyInfoChanged += lobbyReady;

			om = JoinServer(IPAddress.Loopback.ToString(), CreateLocalServer(mapUID), "");
		}

		public static bool IsHost
		{
			get
			{
				var id = OrderManager.Connection.LocalClientId;
				var client = OrderManager.LobbyInfo.ClientWithIndex(id);
				return client != null && client.IsAdmin;
			}
		}

		static Modifiers modifiers;
		public static Modifiers GetModifierKeys() { return modifiers; }
		internal static void HandleModifierKeys(Modifiers mods) { modifiers = mods; }

        // ========================================================================================================================
        // JJS Issue 21 - We don't want to read from the settings.yaml file, instead just use defaults.
        // ========================================================================================================================

		public static void InitializeSettings(Arguments args)
		{
			Settings = new Settings();
		}

        // ===========================================================================================================================
        // BEGIN No Graphics Implementation
        // ===========================================================================================================================

        internal static void InitializeNoGraphics(Arguments args)
        {
            Console.WriteLine("Platform is {0}", Platform.CurrentPlatform);

            InitializeSettings(args);
            LaunchArguments launchArgs = new LaunchArguments(args);
            InitializeLogs(launchArgs);

            if (Settings.Server.DiscoverNatDevices)
                UPnP.TryNatDiscovery();
            else
            {
                Settings.Server.NatDeviceAvailable = false;
                Settings.Server.AllowPortForward = false;
            }

            GeoIP.Initialize();

            Sound = new Sound();

            Console.WriteLine("Available mods:");
            foreach (var mod in ModMetadata.AllMods)
                Console.WriteLine("\t{0}: {1} ({2})", mod.Key, mod.Value.Title, mod.Value.Version);

            InitializeModNoGraphics(Settings.Game.Mod, args);

            if (Settings.Server.DiscoverNatDevices)
                RunAfterDelay(Settings.Server.NatDiscoveryTimeout, UPnP.StoppingNatDiscovery);
        }

        public static readonly string RED_ALERT = "ra";

        public static void InitializeModNoGraphics(string mod, Arguments args)
        {
            Game.Settings.Save();

            // Clear static state if we have switched mods
            LobbyInfoChanged = () => { };
            ConnectionStateChanged = om => { };
            BeforeGameStart = () => { };
            OnRemoteDirectConnect = (a, b) => { };
            delayedActions = new ActionQueue();

            if (server != null)
                server.Shutdown();
            if (OrderManager != null)
                OrderManager.Dispose();

            if (ModData != null)
            {
                ModData.ModFiles.UnmountAll();
                ModData.Dispose();
            }

            Console.WriteLine("Loading mod: {0}", RED_ALERT);
            Settings.Game.Mod = RED_ALERT;

            ModData = new ModData(RED_ALERT, false);

            using (new PerfTimer("LoadMaps"))
                ModData.MapCache.LoadMaps();

            JoinLocal();

            var installData = ModData.Manifest.Get<ContentInstaller>();
            var isModContentInstalled = installData.TestFiles.All(f => File.Exists(Platform.ResolvePath(f)));
            // Mod assets are missing, auto dl for Red Alert.
            if (!isModContentInstalled)
            {
                Action afterInstall =(() =>
                {
                    InitializeModNoGraphics(mod, args);
                });

                Manifest manifest = new Manifest(mod);
                FS files = new FS();
                files.LoadFromManifest(manifest);
                ObjectCreator objCreator = new ObjectCreator(manifest, files);


                Dictionary<string, object> constParams = new Dictionary<string, object>();
                constParams.Add("afterInstall", afterInstall);
                objCreator.CreateObject<object>("AutoDownloadRedAlertPackagesLogic", constParams);

                return;
            }

            ModData.InitializeLoadersNoGraphics(ModData.DefaultFileSystem);

            Game.LoadShellMap();
            Game.Settings.Save();

            // NOTE: Comment this out to run the game normally.
            AutoStartGame(new LaunchArguments(args));
        }

        internal static RunStatus LogicOnlyRun()
        {
            try
            {
                LogicOnlyLoop();
            }
            finally
            {
                // Ensure that the active replay is properly saved
                if (OrderManager != null)
                    OrderManager.Dispose();
            }

            ModData.Dispose();
            ChromeProvider.Deinitialize();

            GlobalChat.Dispose();
            OnQuit();

            return state;
        }

        static void LogicOnlyLoop()
        {
            // When the logic has fallen behind by this much, skip the pending
            // updates and start fresh.
            // For example, if we want to update logic every 10 ms but each loop
            // temporarily takes 100 ms, the 'nextLogic' timestamp will be too low
            // and the current timestamp ('now') will have moved on. Even if the
            // update time returns to normal, it will take a long time to catch up
            // (if ever).
            // This also means that the 'logicInterval' cannot be longer than this
            // value.
            const int MaxLogicTicksBehind = 250;

            // Timestamps for when the next logic and rendering should run
            var nextLogic = RunTime;

            while (state == RunStatus.Running)
            {
                // Ideal time between logic updates. Timestep = 0 means the game is paused
                // but we still call LogicTick() because it handles pausing internally.
                var logicInterval = worldRenderer != null && worldRenderer.World.Timestep != 0 ? worldRenderer.World.Timestep : Timestep;

                var now = RunTime;

                // If the logic has fallen behind too much, skip it and catch up
                if (now - nextLogic > MaxLogicTicksBehind)
                    nextLogic = now;

                if (now >= nextLogic)
                {
                    if (now >= nextLogic)
                    {
                        nextLogic += logicInterval;
                        LogicOnlyLogicTick();
                    }

                    var haveSomeTimeUntilNextLogic = now < nextLogic;
                }
                else
                    Thread.Sleep(nextLogic - now);
            }
        }

        static void LogicOnlyLogicTick()
        {
            delayedActions.PerformActions(RunTime);

            if (OrderManager.Connection.ConnectionState != lastConnectionState)
            {
                lastConnectionState = OrderManager.Connection.ConnectionState;
                ConnectionStateChanged(OrderManager);
            }

            LogicOnlyInnerLogicTick(OrderManager);

            // Check for max ticks.
            CheckMaxTicksReached(OrderManager.World);

            // Check for fitness log increment.
            CheckTicksForPeriodicFitnessLog(OrderManager.World);
    }

        static void LogicOnlyInnerLogicTick(OrderManager orderManager)
        {
            var tick = RunTime;

            var world = orderManager.World;
            var worldTimestep = world == null ? Timestep : world.Timestep;
            var worldTickDelta = tick - orderManager.LastTickTime;
            if (worldTimestep != 0 && worldTickDelta >= worldTimestep)
            {
                using (new PerfSample("tick_time"))
                {
                    // Tick the world to advance the world time to match real time:
                    //    If dt < TickJankThreshold then we should try and catch up by repeatedly ticking
                    //    If dt >= TickJankThreshold then we should accept the jank and progress at the normal rate
                    // dt is rounded down to an integer tick count in order to preserve fractional tick components.
                    var integralTickTimestep = (worldTickDelta / worldTimestep) * worldTimestep;
                    orderManager.LastTickTime += integralTickTimestep >= TimestepJankThreshold ? integralTickTimestep : worldTimestep;

                    Sync.CheckSyncUnchanged(world, orderManager.TickImmediate);

                    if (world == null)
                        return;

                    // Don't tick when the shellmap is disabled
                    if (world.ShouldTick)
                    {
                        var isNetTick = LocalTick % NetTickScale == 0;

                        if (!isNetTick || orderManager.IsReadyForNextFrame)
                        {
                            ++orderManager.LocalFrameNumber;

                            if (BenchmarkMode)
                                Log.Write("cpu", "{0};{1}".F(LocalTick, PerfHistory.Items["tick_time"].LastValue));

                            if (isNetTick)
                                orderManager.Tick();

                            Sync.CheckSyncUnchanged(world, () =>
                            {
                                world.OrderGenerator.Tick(world);
                                world.Selection.Tick(world);
                            });

                            world.Tick();

                            PerfHistory.Tick();
                        }
                        else if (orderManager.NetFrameNumber == 0)
                            orderManager.LastTickTime = RunTime;

                        //Sync.CheckSyncUnchanged(world, () => world.TickRender(worldRenderer));
                    }
                    else
                        PerfHistory.Tick();
                }
            }
        }

        // ===========================================================================================================================
        // END No Graphics Implementation
        // ===========================================================================================================================

        private static void InitializeLogs(LaunchArguments launchArgs)
        {
            // ===========================================================================================================================
            // JJS Issue 22 - Add log prepend
            // ===========================================================================================================================
            if (launchArgs.LogPrepend != null)
            {
                Log.Initialize(launchArgs.LogPrepend, launchArgs.Debug);
            }

            Log.AddChannel("perf", "perf.log");
            Log.AddChannel("debug", "debug.log");
            Log.AddChannel("sync", "syncreport.log");
            Log.AddChannel("server", "server.log");
            Log.AddChannel("sound", "sound.log");
            Log.AddChannel("graphics", "graphics.log");
            Log.AddChannel("geoip", "geoip.log");
            Log.AddChannel("irc", "irc.log");

            Log.AddChannel("scout_report", "scout_report.log");
            Log.AddChannel("order_manager", "order_manager.log");
        }

		internal static void Initialize(Arguments args)
		{
			Console.WriteLine("Platform is {0}", Platform.CurrentPlatform);

            InitializeSettings(args);
            LaunchArguments launchArgs = new LaunchArguments(args);
            InitializeLogs(launchArgs);

            if (Settings.Server.DiscoverNatDevices)
				UPnP.TryNatDiscovery();
			else
			{
				Settings.Server.NatDeviceAvailable = false;
				Settings.Server.AllowPortForward = false;
			}

			GeoIP.Initialize();

			var renderers = new[] { Settings.Graphics.Renderer, "Default", null };
			foreach (var r in renderers)
			{
				if (r == null)
					throw new InvalidOperationException("No suitable renderers were found. Check graphics.log for details.");

				Settings.Graphics.Renderer = r;
				try
				{
					Renderer = new Renderer(Settings.Graphics, Settings.Server);
					break;
				}
				catch (Exception e)
				{
					Log.Write("graphics", "{0}", e);
					Console.WriteLine("Renderer initialization failed. Fallback in place. Check graphics.log for details.");
				}
			}

			Sound = new Sound(Settings.Sound.Engine);

			GlobalChat = new GlobalChat();

			Console.WriteLine("Available mods:");
			foreach (var mod in ModMetadata.AllMods)
				Console.WriteLine("\t{0}: {1} ({2})", mod.Key, mod.Value.Title, mod.Value.Version);

			InitializeMod(Settings.Game.Mod, args);

			if (Settings.Server.DiscoverNatDevices)
				RunAfterDelay(Settings.Server.NatDiscoveryTimeout, UPnP.StoppingNatDiscovery);
		}

		public static bool IsModInstalled(string modId)
		{
			return ModMetadata.AllMods[modId].RequiresMods.All(IsModInstalled);
		}

		public static bool IsModInstalled(KeyValuePair<string, string> mod)
		{
			return ModMetadata.AllMods.ContainsKey(mod.Key)
				&& ModMetadata.AllMods[mod.Key].Version == mod.Value
				&& IsModInstalled(mod.Key);
		}

		public static void InitializeMod(string mod, Arguments args)
		{
			// Clear static state if we have switched mods
			LobbyInfoChanged = () => { };
			ConnectionStateChanged = om => { };
			BeforeGameStart = () => { };
			OnRemoteDirectConnect = (a, b) => { };
			delayedActions = new ActionQueue();

			Ui.ResetAll();

			if (worldRenderer != null)
				worldRenderer.Dispose();
			worldRenderer = null;
			if (server != null)
				server.Shutdown();
			if (OrderManager != null)
				OrderManager.Dispose();

			if (ModData != null)
			{
				ModData.ModFiles.UnmountAll();
				ModData.Dispose();
			}

			ModData = null;

			// Fall back to default if the mod doesn't exist or has missing prerequisites.
			if (!ModMetadata.AllMods.ContainsKey(mod) || !IsModInstalled(mod))
				mod = new GameSettings().Mod;

			Console.WriteLine("Loading mod: {0}", mod);
			Settings.Game.Mod = mod;

			Sound.StopVideo();

			ModData = new ModData(mod, true);

			using (new PerfTimer("LoadMaps"))
				ModData.MapCache.LoadMaps();

			var installData = ModData.Manifest.Get<ContentInstaller>();
			var isModContentInstalled = installData.TestFiles.All(f => File.Exists(Platform.ResolvePath(f)));

			// Mod assets are missing!
			if (!isModContentInstalled)
			{
				InitializeMod("modchooser", new Arguments());
				return;
			}

			ModData.InitializeLoaders(ModData.DefaultFileSystem);
			Renderer.InitializeFonts(ModData);

			if (Cursor != null)
				Cursor.Dispose();

			if (Settings.Graphics.HardwareCursors)
			{
				try
				{
					Cursor = new HardwareCursor(ModData.CursorProvider);
				}
				catch (Exception e)
				{
					Log.Write("debug", "Failed to initialize hardware cursors. Falling back to software cursors.");
					Log.Write("debug", "Error was: " + e.Message);

					Console.WriteLine("Failed to initialize hardware cursors. Falling back to software cursors.");
					Console.WriteLine("Error was: " + e.Message);

					Cursor = new SoftwareCursor(ModData.CursorProvider);
				}
			}
			else
				Cursor = new SoftwareCursor(ModData.CursorProvider);

			PerfHistory.Items["render"].HasNormalTick = false;
			PerfHistory.Items["batches"].HasNormalTick = false;
			PerfHistory.Items["render_widgets"].HasNormalTick = false;
			PerfHistory.Items["render_flip"].HasNormalTick = false;

			JoinLocal();

            ModData.LoadScreen.StartGame(args);
		}

        // ==============================================================================================================
        // BEGIN Headless Auto-Start Game Methods
        // ==============================================================================================================

        private static readonly string DEFAULT_AI_NAME = "ESU AI";
		private static readonly string DEFAULT_SECONDARY_AI_NAME = "Rush AI";

		private static void AutoStartGame(LaunchArguments args)
        {  
            FitnessLogTickIncrement = args.FitnessLogTickIncrement != null ? int.Parse(args.FitnessLogTickIncrement) : DEFAULT_FITNESS_TICKS;
            var myMap = GetSpecifiedLaunchMapOrRandom(args);

            // Create "local server" for game and join it.
            var localPort = CreateLocalServer(myMap.Uid);
            JoinServer(IPAddress.Loopback.ToString(), localPort, "");
            OrderManager.TickImmediate();

            Game.RunAfterDelay(1000, () =>
            {
                // Set to spectate.
                OrderManager.IssueOrder(Order.Command("state NotReady"));
                OrderManager.IssueOrder(Order.Command("spectate"));

                // Create bots.
                string aiName = (args.Ai != null) ? args.Ai : DEFAULT_AI_NAME;
                string secondAiName = (args.SecondAi != null) ? args.SecondAi : DEFAULT_SECONDARY_AI_NAME;
                OrderManager.IssueOrder(Order.Command("slot_bot Multi0 0 {0}".F(aiName)));
                OrderManager.IssueOrder(Order.Command("slot_bot Multi1 0 {0}".F(secondAiName)));
                OrderManager.TickImmediate();

		        // Specify AI faction if argument was given.
                if (args.AiFaction != null) {
                    // Note: the index is 1 here for the specified AI. This is because index 0 is the 'bot controller' (human player that is spectating).
                    OrderManager.IssueOrder(Order.Command("faction 1 {0}".F(args.AiFaction)));
                    OrderManager.IssueOrder(Order.Command("faction 2 {0}".F(args.AiFaction)));
                }

                // Specify spawn point if argument was given.
                if (args.AiSpawnPoint != null) {
                    OrderManager.IssueOrder(Order.Command("spawn 1 {0}".F(args.AiSpawnPoint)));
                }

                // Start game and issue all immediate orders.
                OrderManager.IssueOrder(Order.Command("startgame"));
                OrderManager.TickImmediate();
            });
        }

        private static MapPreview GetSpecifiedLaunchMapOrRandom(LaunchArguments args)
        {
            MapPreview myMap = null;
            if (args.MapName != null)
            {
                myMap = ModData.MapCache.Where(m => m.Title == args.MapName).FirstOrDefault();
                if (myMap == null)
                {
                    throw new SystemException("Unkown map with name " + args.MapName);
                }
            }
            else
            {
                // We have not specified a map, so load random 2 player map.
                var usableMapList = ModData.MapCache
                                .Where(m => m.Status == MapStatus.Available && m.Visibility.HasFlag(MapVisibility.Lobby) && m.PlayerCount == 2);
                myMap = usableMapList.Random(CosmeticRandom);
            }

            Log.Write("order_manager", "Map loaded with name " + myMap.Title);
            myMap.PreloadRules();
            return myMap;
        }
        
        // ==============================================================================================================
        // END Headless Auto-Start Game Methods
        // ==============================================================================================================

        // ===========================================================================================================================
        // BEGIN JJS - Issue 9 - End game after pre-determined amount of ticks
        // ===========================================================================================================================

        private const int MAX_TICKS_BEFORE_END_GAME = 200000;

        private static void CheckMaxTicksReached(World world)
        {
            if (LocalTick >= MAX_TICKS_BEFORE_END_GAME)
            {
                Log.Write("order_manager", "Maximum Ticks Reached: {0}".F(MAX_TICKS_BEFORE_END_GAME));
                world.EndGame();
            }
        }

        // ===========================================================================================================================
        // END JJS - Issue 9 - End game after pre-determined amount of ticks
        // ===========================================================================================================================

        // ===========================================================================================================================
        // BEGIN JJS - Issue 30 - Intermittent tick log
        // ===========================================================================================================================

        private static bool WasFitnessLogIncrementSet = false;

        private static void CheckTicksForPeriodicFitnessLog(World world)
        {
            if (world != null && !WasFitnessLogIncrementSet) {
                world.SetFitnessLogTickIncrement(FitnessLogTickIncrement);
                WasFitnessLogIncrementSet = true;
            }
        }

        // ===========================================================================================================================
        // END JJS - Issue 30 - Intermittent tick log
        // ===========================================================================================================================

        	public static void LoadEditor(string mapUid)
		{
			StartGame(mapUid, WorldType.Editor);
		}

		public static void LoadShellMap()
		{
			var shellmap = ChooseShellmap();

			using (new PerfTimer("StartGame"))
				StartGame(shellmap, WorldType.Shellmap);
		}

		static string ChooseShellmap()
		{
			var shellmaps = ModData.MapCache
				.Where(m => m.Status == MapStatus.Available && m.Visibility.HasFlag(MapVisibility.Shellmap))
				.Select(m => m.Uid);

			if (!shellmaps.Any())
				throw new InvalidDataException("No valid shellmaps available");

			return shellmaps.Random(CosmeticRandom);
		}

		static RunStatus state = RunStatus.Running;
		public static event Action OnQuit = () => { };

		// Note: These delayed actions should only be used by widgets or disposing objects
		// - things that depend on a particular world should be queuing them on the world actor.
		static volatile ActionQueue delayedActions = new ActionQueue();
		public static void RunAfterTick(Action a) { delayedActions.Add(a, RunTime); }
		public static void RunAfterDelay(int delayMilliseconds, Action a) { delayedActions.Add(a, RunTime + delayMilliseconds); }

		static void TakeScreenshotInner()
		{
			Log.Write("debug", "Taking screenshot");

			Bitmap bitmap;
			using (new PerfTimer("Renderer.TakeScreenshot"))
				bitmap = Renderer.Device.TakeScreenshot();

			ThreadPool.QueueUserWorkItem(_ =>
			{
				var mod = ModData.Manifest.Mod;
				var directory = Platform.ResolvePath("^", "Screenshots", mod.Id, mod.Version);
				Directory.CreateDirectory(directory);

				var filename = TimestampedFilename();
				var format = Settings.Graphics.ScreenshotFormat;
				var extension = ImageCodecInfo.GetImageEncoders().FirstOrDefault(x => x.FormatID == format.Guid)
					.FilenameExtension.Split(';').First().ToLowerInvariant().Substring(1);
				var destination = Path.Combine(directory, string.Concat(filename, extension));

				using (new PerfTimer("Save Screenshot ({0})".F(format)))
					bitmap.Save(destination, format);

				bitmap.Dispose();

				RunAfterTick(() => Debug("Saved screenshot " + filename));
			});
		}

		static void InnerLogicTick(OrderManager orderManager)
		{
			var tick = RunTime;

			var world = orderManager.World;

			var uiTickDelta = tick - Ui.LastTickTime;
			if (uiTickDelta >= Timestep)
			{
				// Explained below for the world tick calculation
				var integralTickTimestep = (uiTickDelta / Timestep) * Timestep;
				Ui.LastTickTime += integralTickTimestep >= TimestepJankThreshold ? integralTickTimestep : Timestep;

				Viewport.TicksSinceLastMove += uiTickDelta / Timestep;

				Sync.CheckSyncUnchanged(world, Ui.Tick);
				Cursor.Tick();
			}

			var worldTimestep = world == null ? Timestep : world.Timestep;
			var worldTickDelta = tick - orderManager.LastTickTime;
			if (worldTimestep != 0 && worldTickDelta >= worldTimestep)
			{
				using (new PerfSample("tick_time"))
				{
					// Tick the world to advance the world time to match real time:
					//    If dt < TickJankThreshold then we should try and catch up by repeatedly ticking
					//    If dt >= TickJankThreshold then we should accept the jank and progress at the normal rate
					// dt is rounded down to an integer tick count in order to preserve fractional tick components.
					var integralTickTimestep = (worldTickDelta / worldTimestep) * worldTimestep;
					orderManager.LastTickTime += integralTickTimestep >= TimestepJankThreshold ? integralTickTimestep : worldTimestep;

					Sound.Tick();
					Sync.CheckSyncUnchanged(world, orderManager.TickImmediate);

					if (world == null)
						return;

					// Don't tick when the shellmap is disabled
					if (world.ShouldTick)
					{
						var isNetTick = LocalTick % NetTickScale == 0;

						if (!isNetTick || orderManager.IsReadyForNextFrame)
						{
							++orderManager.LocalFrameNumber;

							if (BenchmarkMode)
								Log.Write("cpu", "{0};{1}".F(LocalTick, PerfHistory.Items["tick_time"].LastValue));

							if (isNetTick)
								orderManager.Tick();

							Sync.CheckSyncUnchanged(world, () =>
							{
								world.OrderGenerator.Tick(world);
								world.Selection.Tick(world);
							});

							world.Tick();

							PerfHistory.Tick();
						}
						else if (orderManager.NetFrameNumber == 0)
							orderManager.LastTickTime = RunTime;

						Sync.CheckSyncUnchanged(world, () => world.TickRender(worldRenderer));
					}
					else
						PerfHistory.Tick();
				}
			}
		}

		static void LogicTick()
		{
			delayedActions.PerformActions(RunTime);

			if (OrderManager.Connection.ConnectionState != lastConnectionState)
			{
				lastConnectionState = OrderManager.Connection.ConnectionState;
				ConnectionStateChanged(OrderManager);
			}

			InnerLogicTick(OrderManager);
			if (worldRenderer != null && OrderManager.World != worldRenderer.World)
				InnerLogicTick(worldRenderer.World.OrderManager);
		}

		public static bool TakeScreenshot = false;

		static void RenderTick()
		{
			using (new PerfSample("render"))
			{
				++RenderFrame;

				// worldRenderer is null during the initial install/download screen
				if (worldRenderer != null)
				{
					Renderer.BeginFrame(worldRenderer.Viewport.TopLeft, worldRenderer.Viewport.Zoom);
					Sound.SetListenerPosition(worldRenderer.Viewport.CenterPosition);
					worldRenderer.Draw();
				}
				else
					Renderer.BeginFrame(int2.Zero, 1f);

				using (new PerfSample("render_widgets"))
				{
					Renderer.WorldVoxelRenderer.BeginFrame();
					Ui.PrepareRenderables();
					Renderer.WorldVoxelRenderer.EndFrame();

					Ui.Draw();

					if (ModData != null && ModData.CursorProvider != null)
					{
						Cursor.SetCursor(Ui.Root.GetCursorOuter(Viewport.LastMousePos) ?? "default");
						Cursor.Render(Renderer);
					}
				}

				using (new PerfSample("render_flip"))
					Renderer.EndFrame(new DefaultInputHandler(OrderManager.World));

				if (TakeScreenshot)
				{
					TakeScreenshot = false;
					TakeScreenshotInner();
				}
			}

			PerfHistory.Items["render"].Tick();
			PerfHistory.Items["batches"].Tick();
			PerfHistory.Items["render_widgets"].Tick();
			PerfHistory.Items["render_flip"].Tick();

			if (BenchmarkMode)
				Log.Write("render", "{0};{1}".F(RenderFrame, PerfHistory.Items["render"].LastValue));
		}

		static void Loop()
		{
			// The game loop mainly does two things: logic updates and
			// drawing on the screen.
			// ---
			// We ideally want the logic to run every 'Timestep' ms and
			// rendering to be done at 'MaxFramerate', so 1000 / MaxFramerate ms.
			// Any additional free time is used in 'Sleep' so we don't
			// consume more CPU/GPU resources than necessary.
			// ---
			// In case logic or rendering takes more time than the ideal
			// and we're getting behind, we can skip rendering some frames
			// but there's a fail-safe minimum FPS to make sure the screen
			// gets updated at least that often.
			// ---
			// TODO: Separate world/UI rendering
			// It would be nice to separate the world rendering from the UI rendering
			// so that we can update the UI more often than the world. This would
			// help make the game playable (mouse/controls) even in low world
			// framerates.
			// It's not possible at the moment because the render buffer is cleared
			// before rendering and we don't keep the last rendered world buffer.

			// When the logic has fallen behind by this much, skip the pending
			// updates and start fresh.
			// For example, if we want to update logic every 10 ms but each loop
			// temporarily takes 100 ms, the 'nextLogic' timestamp will be too low
			// and the current timestamp ('now') will have moved on. Even if the
			// update time returns to normal, it will take a long time to catch up
			// (if ever).
			// This also means that the 'logicInterval' cannot be longer than this
			// value.
			const int MaxLogicTicksBehind = 250;

			// Try to maintain at least this many FPS during replays, even if it slows down logic.
			// However, if the user has enabled a framerate limit that is even lower
			// than this, then that limit will be used.
			const int MinReplayFps = 10;

			// Timestamps for when the next logic and rendering should run
			var nextLogic = RunTime;
			var nextRender = RunTime;
			var forcedNextRender = RunTime;

			while (state == RunStatus.Running)
			{
				// Ideal time between logic updates. Timestep = 0 means the game is paused
				// but we still call LogicTick() because it handles pausing internally.
				var logicInterval = worldRenderer != null && worldRenderer.World.Timestep != 0 ? worldRenderer.World.Timestep : Timestep;

				// Ideal time between screen updates
				var maxFramerate = Settings.Graphics.CapFramerate ? Settings.Graphics.MaxFramerate.Clamp(1, 1000) : 1000;
				var renderInterval = 1000 / maxFramerate;

				var now = RunTime;

				// If the logic has fallen behind too much, skip it and catch up
				if (now - nextLogic > MaxLogicTicksBehind)
					nextLogic = now;

				// When's the next update (logic or render)
				var nextUpdate = Math.Min(nextLogic, nextRender);
				if (now >= nextUpdate)
				{
					var forceRender = now >= forcedNextRender;

					if (now >= nextLogic)
					{
						nextLogic += logicInterval;

						LogicTick();

						// Force at least one render per tick during regular gameplay
						if (OrderManager.World != null && !OrderManager.World.IsReplay)
							forceRender = true;
					}

					var haveSomeTimeUntilNextLogic = now < nextLogic;
					var isTimeToRender = now >= nextRender;

					if ((isTimeToRender && haveSomeTimeUntilNextLogic) || forceRender)
					{
						nextRender = now + renderInterval;

						// Pick the minimum allowed FPS (the lower between 'minReplayFPS'
						// and the user's max frame rate) and convert it to maximum time
						// allowed between screen updates.
						// We do this before rendering to include the time rendering takes
						// in this interval.
						var maxRenderInterval = Math.Max(1000 / MinReplayFps, renderInterval);
						forcedNextRender = now + maxRenderInterval;

						RenderTick();
					}
				}
				else
					Thread.Sleep(nextUpdate - now);
			}
		}

		internal static RunStatus Run()
		{
			if (Settings.Graphics.MaxFramerate < 1)
			{
				Settings.Graphics.MaxFramerate = new GraphicSettings().MaxFramerate;
				Settings.Graphics.CapFramerate = false;
			}

			try
			{
				Loop();
			}
			finally
			{
				// Ensure that the active replay is properly saved
				if (OrderManager != null)
					OrderManager.Dispose();
			}

			if (worldRenderer != null)
				worldRenderer.Dispose();
			ModData.Dispose();
			ChromeProvider.Deinitialize();

			GlobalChat.Dispose();
			Sound.Dispose();
			Renderer.Dispose();

			OnQuit();

			return state;
		}

		public static void Exit()
		{
			state = RunStatus.Success;
		}

		public static void Restart()
		{
			state = RunStatus.Restart;
		}

		public static void AddChatLine(Color color, string name, string text)
		{
			OrderManager.AddChatLine(color, name, text);
		}

		public static void Debug(string s, params object[] args)
		{
			AddChatLine(Color.White, "Debug", string.Format(s, args));
		}

		public static void Disconnect()
		{
			if (OrderManager.World != null)
				OrderManager.World.TraitDict.PrintReport();

			OrderManager.Dispose();
			CloseServer();
			JoinLocal();
		}

		public static void CloseServer()
		{
			if (server != null)
				server.Shutdown();
		}

		public static T CreateObject<T>(string name)
		{
			return ModData.ObjectCreator.CreateObject<T>(name);
		}

		public static void CreateServer(ServerSettings settings)
		{
			server = new Server.Server(new IPEndPoint(IPAddress.Any, settings.ListenPort), settings, ModData, false);
		}

		public static int CreateLocalServer(string map)
		{
			var settings = new ServerSettings()
			{
				Name = "Skirmish Game",
				Map = map,
				AdvertiseOnline = false,
				AllowPortForward = false
			};

			server = new Server.Server(new IPEndPoint(IPAddress.Loopback, 0), settings, ModData, false);

			return server.Port;
		}

		public static bool IsCurrentWorld(World world)
		{
			return OrderManager != null && OrderManager.World == world && !world.Disposing;
		}
	}
}
