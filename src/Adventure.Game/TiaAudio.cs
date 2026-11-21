using Microsoft.Xna.Framework.Audio;

namespace Adventure.Game;

public sealed class TiaAudio : IDisposable
{
    private const int OutputRate = 44100;
    private const int TiaClock = 31440;
    private const int SamplesPerUpdate = OutputRate / 60;

    private readonly DynamicSoundEffectInstance _voice = new(OutputRate, AudioChannels.Mono);
    private readonly byte[] _buffer = new byte[SamplesPerUpdate * 2];
    private int _audc;
    private int _audf;
    private int _audv;
    private int _clockError;
    private int _divider;
    private int _div6;
    private int _div15;
    private int _div31;
    private int _div93;
    private int _poly4 = 0x0F;
    private int _poly5 = 0x1F;
    private int _poly9 = 0x1FF;
    private int _output;

    public TiaAudio()
    {
        _voice.Play();
    }

    public void Present(int audc, int audf, int audv)
    {
        _audc = audc & 0x0F;
        _audf = audf & 0x1F;
        _audv = audv & 0x0F;
        while (_voice.PendingBufferCount < 2)
        {
            Fill();
            _voice.SubmitBuffer(_buffer);
        }
    }

    public void Dispose()
    {
        _voice.Dispose();
    }

    private void Fill()
    {
        var level = (short)(_audv * 2184);
        for (var i = 0; i < SamplesPerUpdate; i++)
        {
            _clockError += TiaClock;
            if (_clockError >= OutputRate)
            {
                _clockError -= OutputRate;
                Clock();
            }

            var sample = (short)(_output * level);
            var offset = i * 2;
            _buffer[offset] = (byte)sample;
            _buffer[offset + 1] = (byte)(sample >> 8);
        }
    }

    private void Clock()
    {
        if (_divider > 0)
        {
            _divider--;
            return;
        }

        _divider = _audf;
        switch (_audc)
        {
            case 0:
            case 11:
                _output = 1;
                break;
            case 1:
                _output = Poly4();
                break;
            case 2:
                if (++_div15 >= 15)
                {
                    _div15 = 0;
                    _output = Poly4();
                }

                break;
            case 3:
                if (Poly5() != 0)
                {
                    _output = Poly4();
                }

                break;
            case 4:
            case 5:
                _output ^= 1;
                break;
            case 6:
            case 10:
                if (++_div31 >= 31)
                {
                    _div31 = 0;
                    _output ^= 1;
                }

                break;
            case 7:
                if (Poly5() != 0)
                {
                    _output ^= 1;
                }

                break;
            case 8:
                _output = Poly9();
                break;
            case 9:
                _output = Poly5();
                break;
            case 12:
            case 13:
                if (++_div6 >= 6)
                {
                    _div6 = 0;
                    _output ^= 1;
                }

                break;
            case 14:
                if (++_div93 >= 93)
                {
                    _div93 = 0;
                    _output ^= 1;
                }

                break;
            case 15:
                if (++_div6 >= 6)
                {
                    _div6 = 0;
                    _output = Poly5();
                }

                break;
        }
    }

    private int Poly4()
    {
        var feedback = ((_poly4 >> 1) ^ _poly4) & 1;
        _poly4 = ((_poly4 >> 1) | (feedback << 3)) & 0x0F;
        return _poly4 & 1;
    }

    private int Poly5()
    {
        var feedback = ((_poly5 >> 2) ^ _poly5) & 1;
        _poly5 = ((_poly5 >> 1) | (feedback << 4)) & 0x1F;
        return _poly5 & 1;
    }

    private int Poly9()
    {
        var feedback = ((_poly9 >> 4) ^ _poly9) & 1;
        _poly9 = ((_poly9 >> 1) | (feedback << 8)) & 0x1FF;
        return _poly9 & 1;
    }
}
