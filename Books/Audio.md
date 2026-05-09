# Audio 模块

Audio 模块提供统一音频播放服务，支持 2D 音效、3D 音效、跟随目标播放、音乐循环、分组音量控制、分组启用/禁用、播放句柄停止和查询。音频资源可以传入 `AudioClip`，也可以传入资源地址由 Resources 模块加载。

源码位置：

- `Client/Packages/com.alicizax.unity.framework/Runtime/Audio`

## 使用前提

场景中的框架根节点需要挂载：

- `ObjectPoolComponent`
- `ResourceComponent`
- `AudioComponent`

Audio 服务初始化时会依赖：

- `IResourceService`：通过资源地址加载 `AudioClip`。
- `IObjectPoolService`：复用 `AudioSourceObject`。
- `AudioMixer`：用于分组音量。
- `AudioListener`：用于全局音量和 3D 声音监听。

`AudioComponent` Inspector 中必须指定 `AudioMixer` 和 `AudioListener`。每个 `AudioType` 都需要一份 `AudioGroupConfig`，编辑器下组件会自动补齐默认配置。

## 音频分类

框架内置的音频分类：

```csharp
public enum AudioType
{
    Sound = 0,
    UISound = 1,
    Music = 2,
    Voice = 3,
    Ambient = 4,
    Max = 5
}
```

常见用法：

- `Sound`：普通 3D 或 2D 音效。
- `UISound`：UI 点击、弹窗等音效。
- `Music`：背景音乐。
- `Voice`：角色语音。
- `Ambient`：环境音。

## 获取服务

```csharp
using AlicizaX;
using AlicizaX.Audio.Runtime;

IAudioService audio = AppServices.Require<IAudioService>();
```

如果代码可能早于 Audio 初始化执行，可以使用：

```csharp
if (!AppServices.TryGet<IAudioService>(out var audio))
{
    return;
}
```

## 播放 2D 音效

通过资源地址播放：

```csharp
using AlicizaX;
using AlicizaX.Audio.Runtime;
using UnityEngine;

public sealed class UISoundExample : MonoBehaviour
{
    private void OnClick()
    {
        IAudioService audio = AppServices.Require<IAudioService>();
        audio.Play(AudioType.UISound, "Assets/Bundles/Audios/ui_click.wav");
    }
}
```

通过 `AudioClip` 播放：

```csharp
using AlicizaX;
using AlicizaX.Audio.Runtime;
using UnityEngine;

public sealed class AudioClipExample : MonoBehaviour
{
    [SerializeField] private AudioClip clickClip;

    public void Play()
    {
        IAudioService audio = AppServices.Require<IAudioService>();
        audio.Play(AudioType.UISound, clickClip, loop: false, volume: 1f);
    }
}
```

`Play` 返回 `ulong` 句柄，返回 `0UL` 表示播放失败。

## 播放和停止背景音乐

```csharp
using AlicizaX;
using AlicizaX.Audio.Runtime;
using UnityEngine;

public sealed class MusicExample : MonoBehaviour
{
    private IAudioService _audio;
    private ulong _musicHandle;

    private void Start()
    {
        _audio = AppServices.Require<IAudioService>();
        _musicHandle = _audio.Play(
            AudioType.Music,
            "Assets/Bundles/Audios/bgm_main.wav",
            loop: true,
            volume: 0.8f);
    }

    private void OnDestroy()
    {
        _audio?.Stop(_musicHandle, fadeout: true);
        _musicHandle = 0UL;
    }
}
```

查询句柄是否仍在播放：

```csharp
bool isPlaying = audio.IsPlaying(_musicHandle);
```

## 播放 3D 音效

```csharp
using AlicizaX;
using AlicizaX.Audio.Runtime;
using UnityEngine;

public sealed class ExplosionSoundExample : MonoBehaviour
{
    public void PlayExplosion(Vector3 position)
    {
        IAudioService audio = AppServices.Require<IAudioService>();
        audio.Play3D(
            AudioType.Sound,
            "Assets/Bundles/Audios/explosion.wav",
            position,
            loop: false,
            volume: 1f);
    }
}
```

3D 参数如最小距离、最大距离、衰减模式等默认来自对应 `AudioGroupConfig`。

## 跟随目标播放

```csharp
using AlicizaX;
using AlicizaX.Audio.Runtime;
using UnityEngine;

public sealed class FollowSoundExample : MonoBehaviour
{
    private ulong _engineHandle;

    private void OnEnable()
    {
        IAudioService audio = AppServices.Require<IAudioService>();
        _engineHandle = audio.PlayFollow(
            AudioType.Sound,
            "Assets/Bundles/Audios/engine_loop.wav",
            transform,
            Vector3.zero,
            loop: true,
            volume: 0.7f);
    }

    private void OnDisable()
    {
        if (AppServices.TryGet<IAudioService>(out var audio))
        {
            audio.Stop(_engineHandle, fadeout: true);
        }

        _engineHandle = 0UL;
    }
}
```

当目标对象失效或停止播放时，AudioAgent 会释放内部音频资源引用。

## 全局音量和静音

```csharp
IAudioService audio = AppServices.Require<IAudioService>();

// 全局音量，最终写到 AudioListener.volume。
audio.Volume = 0.8f;

// 全局启用/禁用。
audio.Enable = false;
audio.Enable = true;
```

`Volume` 会被限制在 `0f` 到 `1f`。

## 分组音量和开关

```csharp
IAudioService audio = AppServices.Require<IAudioService>();

audio.SetCategoryVolume(AudioType.Music, 0.5f);
audio.SetCategoryVolume(AudioType.UISound, 1f);

audio.SetCategoryEnable(AudioType.Voice, false);

float musicVolume = audio.GetCategoryVolume(AudioType.Music);
bool voiceEnabled = audio.GetCategoryEnable(AudioType.Voice);
```

分组音量会写入 `AudioMixer` 暴露参数。默认参数名：

- `SoundVolume`
- `UISoundVolume`
- `MusicVolume`
- `VoiceVolume`
- `AmbientVolume`

如果 `AudioGroupConfig.ExposedVolumeParameter` 不为空，会优先使用配置中的参数名。

## 停止音频

```csharp
// 停止指定句柄。
audio.Stop(handle, fadeout: true);

// 停止某一分类的所有音频。
audio.Stop(AudioType.Sound, fadeout: false);

// 停止所有音频。
audio.StopAll(fadeout: true);
```

禁用某个分类时，该分类当前播放中的音频会停止：

```csharp
audio.SetCategoryEnable(AudioType.Ambient, false);
```

## AudioEmitter 组件

`AudioEmitter` 是挂在场景物体上的播放组件，适合环境音、机关音、场景循环声等。

常用字段：

- `Audio Type`：音频分类。
- `Clip Mode`：使用资源地址或直接引用 `AudioClip`。
- `Address` / `Clip`：播放资源。
- `Play On Enable`：启用时自动播放。
- `Loop`：循环播放。
- `Follow Self`：声音是否跟随自身 Transform。
- `Use Trigger Range`：根据 Listener 距离自动播放/停止。
- `Min Distance` / `Max Distance`：3D 衰减距离。

代码控制：

```csharp
using AlicizaX.Audio.Runtime;
using UnityEngine;

public sealed class AudioEmitterExample : MonoBehaviour
{
    [SerializeField] private AudioEmitter emitter;

    public void Play()
    {
        emitter.Play();
    }

    public void Stop()
    {
        emitter.Stop();
    }
}
```

## 音频地址播放的加载行为

通过地址播放音频时，AudioService 会使用 `IResourceService.LoadAssetSyncHandle<AudioClip>` 或 `LoadAssetAsyncHandle<AudioClip>` 加载资源，并维护内部 Clip 缓存。

公开 `IAudioService` 的地址播放接口当前走同步加载路径：

```csharp
audio.Play(AudioType.Sound, "Assets/Bundles/Audios/hit.wav");
audio.Play3D(AudioType.Sound, "Assets/Bundles/Audios/hit.wav", position);
audio.PlayFollow(AudioType.Sound, "Assets/Bundles/Audios/engine.wav", target, Vector3.zero);
```

`AudioEmitter` 中的地址模式可以配置 `Async` 和 `Cache Clip`，适合场景环境音等组件化使用。

## 完整示例：音频设置面板

```csharp
using AlicizaX;
using AlicizaX.Audio.Runtime;
using UnityEngine;
using UnityEngine.UI;

public sealed class AudioSettingsPanel : MonoBehaviour
{
    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Toggle voiceToggle;

    private IAudioService _audio;

    private void OnEnable()
    {
        _audio = AppServices.Require<IAudioService>();

        masterSlider.SetValueWithoutNotify(_audio.Volume);
        musicSlider.SetValueWithoutNotify(_audio.GetCategoryVolume(AudioType.Music));
        voiceToggle.SetIsOnWithoutNotify(_audio.GetCategoryEnable(AudioType.Voice));

        masterSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
        musicSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
        voiceToggle.onValueChanged.AddListener(OnVoiceEnableChanged);
    }

    private void OnDisable()
    {
        masterSlider.onValueChanged.RemoveListener(OnMasterVolumeChanged);
        musicSlider.onValueChanged.RemoveListener(OnMusicVolumeChanged);
        voiceToggle.onValueChanged.RemoveListener(OnVoiceEnableChanged);
    }

    private void OnMasterVolumeChanged(float value)
    {
        _audio.Volume = value;
    }

    private void OnMusicVolumeChanged(float value)
    {
        _audio.SetCategoryVolume(AudioType.Music, value);
    }

    private void OnVoiceEnableChanged(bool enabled)
    {
        _audio.SetCategoryEnable(AudioType.Voice, enabled);
    }
}
```

## API 速查

```csharp
float Volume { get; set; }
bool Enable { get; set; }

float GetCategoryVolume(AudioType type);
void SetCategoryVolume(AudioType type, float value);
bool GetCategoryEnable(AudioType type);
void SetCategoryEnable(AudioType type, bool value);

ulong Play(AudioType type, string path, bool loop = false, float volume = 1f);
ulong Play(AudioType type, AudioClip clip, bool loop = false, float volume = 1f);

ulong Play3D(AudioType type, string path, in Vector3 position, bool loop = false, float volume = 1f);
ulong Play3D(AudioType type, AudioClip clip, in Vector3 position, bool loop = false, float volume = 1f);

ulong PlayFollow(AudioType type, string path, Transform target, in Vector3 localOffset, bool loop = false, float volume = 1f);
ulong PlayFollow(AudioType type, AudioClip clip, Transform target, in Vector3 localOffset, bool loop = false, float volume = 1f);

bool Stop(ulong handle, bool fadeout = false);
bool IsPlaying(ulong handle);
void Stop(AudioType type, bool fadeout);
void StopAll(bool fadeout);
```

## 注意事项

- `AudioComponent` 必须指定 `AudioMixer` 和 `AudioListener`。
- 每个 `AudioType` 都要有对应 `AudioGroupConfig`，且 MixerGroup 不能为空。
- 地址播放依赖 Resources 模块，资源包必须已初始化。
- 背景音乐、循环环境音要保存播放句柄，并在生命周期结束时 Stop。
- `Play` 返回 `0UL` 表示播放失败，不要继续用这个句柄做状态判断。
- 分组音量最小会被限制到 `0.0001f`，禁用分组时才会写入静音值。
