using System;
using System.IO;
using System.Linq;
using NAudio.Wave;

static class Program
{
    private static readonly string[] paths = new string[]
    {
        "people_hi.wav",
        "people_lo.wav",
        "windstorm.wav",
        "waterfall.wav",
        "mostheavy.wav"
    };

    static void Main(string[] args)
    {
        using (var waveOut = new WaveOut())
        {
            var ambientSound = new AmbientSoundProvider(paths);
            waveOut.Init(ambientSound);
            waveOut.Play();

            DisplayInfo(ambientSound);

            while (true)
            {
                switch (Console.ReadKey().Key)
                {
                    case ConsoleKey.D3:
                        ambientSound.Voices[0].Volume -= 5;
                        break;

                    case ConsoleKey.D4:
                        ambientSound.Voices[0].Volume += 5;
                        break;

                    case ConsoleKey.D1:
                        ambientSound.Voices[1].Volume -= 5;
                        break;

                    case ConsoleKey.D2:
                        ambientSound.Voices[1].Volume += 5;
                        break;

                    case ConsoleKey.Q:
                        ambientSound.Voices[2].Volume -= 5;
                        break;

                    case ConsoleKey.W:
                        ambientSound.Voices[2].Volume += 5;
                        break;

                    case ConsoleKey.A:
                        ambientSound.Voices[3].Volume -= 5;
                        break;

                    case ConsoleKey.S:
                        ambientSound.Voices[3].Volume += 5;
                        break;

                    case ConsoleKey.Z:
                        ambientSound.Voices[4].Volume -= 5;
                        break;

                    case ConsoleKey.X:
                        ambientSound.Voices[4].Volume += 5;
                        break;

                    case ConsoleKey.Escape:
                        return;
                }

                DisplayInfo(ambientSound);
            }
        }
    }

    static void DisplayInfo(AmbientSoundProvider ambientSound)
    {
        Console.Clear();
        foreach (var voice in ambientSound.Voices)
        {
            Console.WriteLine(voice.Name + " : " + voice.Volume);
        }
    }
}

class AmbientSoundProvider : ISampleProvider
{
    private static WaveFormat format = WaveFormat.CreateIeeeFloatWaveFormat(16000, 2);

    private Voice[] voices;

    public AmbientSoundProvider(string[] paths)
    {
        voices = paths.Select(path => new Voice(path)).ToArray();
    }

    public int Read(float[] buffer, int offset, int count)
    {
        for (var t = 0; t < count; t++)
        {
            buffer[offset + t] = 0;
        }

        foreach (var voice in voices)
        {
            voice.Process(buffer, offset, count);
        }

        return count;
    }

    public WaveFormat WaveFormat => format;
    public Voice[] Voices => voices;
}

class Voice
{
    private static readonly int loopFadeSampleCount = 16000;

    private string name;
    private float[] data;
    private int position;
    private int volume;

    public Voice(string path)
    {
        name = Path.GetFileNameWithoutExtension(path);
        data = ReadData(path);
        position = 0;
        volume = 0;
    }

    public void Process(float[] buffer, int offset, int count)
    {
        if (volume == 0)
        {
            return;
        }

        var a = 0.01F * volume;
        for (var t = 0; t < count; t++)
        {
            buffer[offset + t] += a * data[position];
            position++;
            if (position == data.Length)
            {
                position = 0;
            }
        }
    }

    private static float[] ReadData(string path)
    {
        using (var reader = new WaveFileReader(path))
        {
            var srcSampleCount = reader.Length / reader.BlockAlign;
            var dstSampleCount = srcSampleCount - loopFadeSampleCount;

            var data = new float[2 * dstSampleCount];
            for (var t = 0; t < loopFadeSampleCount; t++)
            {
                var fade = (float)t / loopFadeSampleCount;
                var offset = 2 * t;
                var frame = reader.ReadNextSampleFrame();
                data[offset + 0] = fade * frame[0];
                data[offset + 1] = fade * frame[1];
            }
            for (var t = loopFadeSampleCount; t < dstSampleCount; t++)
            {
                var offset = 2 * t;
                var frame = reader.ReadNextSampleFrame();
                data[offset + 0] = frame[0];
                data[offset + 1] = frame[1];
            }
            for (var t = 0; t < loopFadeSampleCount; t++)
            {
                var fade = 1F - ((float)t / loopFadeSampleCount);
                var offset = 2 * t;
                var frame = reader.ReadNextSampleFrame();
                data[offset + 0] += fade * frame[0];
                data[offset + 1] += fade * frame[1];
            }
            return data;
        }
    }

    public string Name => name;

    public int Volume
    {
        get
        {
            return volume;
        }

        set
        {
            volume = value;
            if (volume < 0) volume = 0;
            if (volume > 200) volume = 200;
        }
    }
}
