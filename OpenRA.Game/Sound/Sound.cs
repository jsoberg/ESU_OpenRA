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
using System.IO;
using System.Linq;
using System.Reflection;
using OpenRA.FileSystem;
using OpenRA.GameRules;
using OpenRA.Primitives;

namespace OpenRA
{
	public interface ISoundLoader
	{
		bool TryParseSound(Stream stream, out ISoundFormat sound);
	}

	public interface ISoundFormat
	{
		int Channels { get; }
		int SampleBits { get; }
		int SampleRate { get; }
		float LengthInSeconds { get; }
		Stream GetPCMInputStream();
	}

	public sealed class Sound : IDisposable
	{
		readonly ISoundEngine soundEngine;
		Cache<string, ISoundSource> sounds;
		ISoundSource rawSource;
		ISound music;
		ISound video;
		MusicInfo currentMusic;

        /** No-Graphics constructor. */
        public Sound()
        {
            soundEngine = null;
        }

		public Sound(string engineName)
		{
			var enginePath = Platform.ResolvePath(".", "OpenRA.Platforms." + engineName + ".dll");
			soundEngine = CreateDevice(Assembly.LoadFile(enginePath));
		}

		static ISoundEngine CreateDevice(Assembly platformDll)
		{
			foreach (PlatformAttribute r in platformDll.GetCustomAttributes(typeof(PlatformAttribute), false))
			{
				var factory = (IDeviceFactory)r.Type.GetConstructor(Type.EmptyTypes).Invoke(null);
				return factory.CreateSound();
			}

			throw new InvalidOperationException("Platform DLL is missing PlatformAttribute to tell us what type to use!");
		}

		ISoundSource LoadSound(ISoundLoader[] loaders, IReadOnlyFileSystem fileSystem, string filename)
		{
			if (!fileSystem.Exists(filename))
			{
				Log.Write("sound", "LoadSound, file does not exist: {0}", filename);
				return null;
			}

			using (var stream = fileSystem.Open(filename))
			{
				ISoundFormat soundFormat;
				foreach (var loader in Game.ModData.SoundLoaders)
				{
					stream.Position = 0;
					if (loader.TryParseSound(stream, out soundFormat))
						return soundEngine.AddSoundSourceFromMemory(
							soundFormat.GetPCMInputStream().ReadAllBytes(), soundFormat.Channels, soundFormat.SampleBits, soundFormat.SampleRate);
				}
			}

			throw new InvalidDataException(filename + " is not a valid sound file!");
		}

		public void Initialize(ISoundLoader[] loaders, IReadOnlyFileSystem fileSystem)
		{
			sounds = new Cache<string, ISoundSource>(s => LoadSound(loaders, fileSystem, s));
			music = null;
			currentMusic = null;
			video = null;
		}

		public SoundDevice[] AvailableDevices()
		{
			var defaultDevices = new[]
			{
				new SoundDevice("Null", null, "Output Disabled")
			};

			return defaultDevices.Concat(soundEngine.AvailableDevices()).ToArray();
		}

		public void SetListenerPosition(WPos position)
		{
			soundEngine.SetListenerPosition(position);
		}

		ISound Play(Player player, string name, bool headRelative, WPos pos, float volumeModifier = 1f, bool loop = false)
		{
            // No Graphic Implementation
            return null;
		}

		public void StopAudio()
		{
			soundEngine.StopAllSounds();
		}

		public void MuteAudio()
		{
			soundEngine.Volume = 0f;
		}

		public void UnmuteAudio()
		{
			soundEngine.Volume = 1f;
		}

		public ISound Play(string name) { return Play(null, name, true, WPos.Zero, 1f); }
		public ISound Play(string name, WPos pos) { return Play(null, name, false, pos, 1f); }
		public ISound Play(string name, float volumeModifier) { return Play(null, name, true, WPos.Zero, volumeModifier); }
		public ISound Play(string name, WPos pos, float volumeModifier) { return Play(null, name, false, pos, volumeModifier); }
		public ISound PlayToPlayer(Player player, string name) { return Play(player, name, true, WPos.Zero, 1f); }
		public ISound PlayToPlayer(Player player, string name, WPos pos) { return Play(player, name, false, pos, 1f); }
		public ISound PlayLooped(string name) { return PlayLooped(name, WPos.Zero); }
		public ISound PlayLooped(string name, WPos pos) { return Play(null, name, true, pos, 1f, true); }

		public void PlayVideo(byte[] raw, int channels, int sampleBits, int sampleRate)
		{
			rawSource = soundEngine.AddSoundSourceFromMemory(raw, channels, sampleBits, sampleRate);
			video = soundEngine.Play2D(rawSource, false, true, WPos.Zero, InternalSoundVolume, false);
		}

		public void PlayVideo()
		{
			if (video != null)
				soundEngine.PauseSound(video, false);
		}

		public void PauseVideo()
		{
			if (video != null)
				soundEngine.PauseSound(video, true);
		}

		public void StopVideo()
		{
			if (video != null)
				soundEngine.StopSound(video);
		}

		public void Tick()
		{
			// Song finished
			if (MusicPlaying && !music.Playing)
			{
				StopMusic();
				onMusicComplete();
			}
		}

		Action onMusicComplete;
		public bool MusicPlaying { get; private set; }
		public MusicInfo CurrentMusic { get { return currentMusic; } }

		public void PlayMusic(MusicInfo m)
		{
			PlayMusicThen(m, () => { });
		}

		public void PlayMusicThen(MusicInfo m, Action then)
		{
			if (m == null || !m.Exists)
				return;

			onMusicComplete = then;

			if (m == currentMusic && music != null)
			{
				soundEngine.PauseSound(music, false);
				MusicPlaying = true;
				return;
			}

			StopMusic();

			var sound = sounds[m.Filename];
			if (sound == null)
				return;

			music = soundEngine.Play2D(sound, false, true, WPos.Zero, MusicVolume, false);
			currentMusic = m;
			MusicPlaying = true;
		}

		public void PlayMusic()
		{
			if (music == null)
				return;

			MusicPlaying = true;
			soundEngine.PauseSound(music, false);
		}

		public void StopSound(ISound sound)
		{
			if (sound != null)
				soundEngine.StopSound(sound);
		}

		public void StopMusic()
		{
			if (music != null)
				soundEngine.StopSound(music);

			MusicPlaying = false;
			currentMusic = null;
		}

		public void PauseMusic()
		{
			if (music == null)
				return;

			MusicPlaying = false;
			soundEngine.PauseSound(music, true);
		}

		public float GlobalVolume
		{
			get { return soundEngine.Volume; }
			set { soundEngine.Volume = value; }
		}

		float soundVolumeModifier = 1.0f;
		public float SoundVolumeModifier
		{
			get
			{
				return soundVolumeModifier;
			}

			set
			{
				soundVolumeModifier = value;
				soundEngine.SetSoundVolume(InternalSoundVolume, music, video);
			}
		}

		float InternalSoundVolume { get { return SoundVolume * soundVolumeModifier; } }
		public float SoundVolume
		{
			get
			{
				return Game.Settings.Sound.SoundVolume;
			}

			set
			{
				Game.Settings.Sound.SoundVolume = value;
				soundEngine.SetSoundVolume(InternalSoundVolume, music, video);
			}
		}

		public float MusicVolume
		{
			get
			{
				return Game.Settings.Sound.MusicVolume;
			}

			set
			{
				Game.Settings.Sound.MusicVolume = value;
				if (music != null)
					music.Volume = value;
			}
		}

		public float VideoVolume
		{
			get
			{
				return Game.Settings.Sound.VideoVolume;
			}

			set
			{
				Game.Settings.Sound.VideoVolume = value;
				if (video != null)
					video.Volume = value;
			}
		}

		public float MusicSeekPosition
		{
			get { return music != null ? music.SeekPosition : 0; }
		}

		public float VideoSeekPosition
		{
			get { return video != null ? video.SeekPosition : 0; }
		}

		// Returns true if played successfully
		public bool PlayPredefined(Ruleset ruleset, Player p, Actor voicedActor, string type, string definition, string variant,
			bool relative, WPos pos, float volumeModifier, bool attenuateVolume)
		{
            // NO GRPAHICS OR SOUND - Do Nothing.
            return true;
		}

		public bool PlayNotification(Ruleset rules, Player player, string type, string notification, string variant)
		{
			// NO GRPAHICS OR SOUND - Do Nothing.
            return true;
		}

		public void Dispose()
		{
			soundEngine.Dispose();
		}
	}
}
