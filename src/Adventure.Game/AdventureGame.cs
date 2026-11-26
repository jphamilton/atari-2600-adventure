using Adventure.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Adventure.Game;

public sealed class AdventureGame : Microsoft.Xna.Framework.Game
{
    private const int Scale = 4;

    private readonly GraphicsDeviceManager _graphics;
    private readonly GameWorld _world = GameWorld.AtTitle();
    private SpriteBatch _sprites = null!;
    private Texture2D _pixel = null!;
    private TiaAudio _audio = null!;
    private int _titledRoom = -1;
    private bool _resetDown;
    private bool _selectDown;

    public AdventureGame()
    {
        _graphics = new GraphicsDeviceManager(this);
        _graphics.PreferredBackBufferWidth = MapGeometry.PixelWidth * Scale;
        _graphics.PreferredBackBufferHeight = MapGeometry.VisibleHeight * Scale;
        IsFixedTimeStep = true;
        TargetElapsedTime = TimeSpan.FromSeconds(1.0 / 60.0);
        IsMouseVisible = true;
        Window.Title = "Adventure";
    }

    protected override void LoadContent()
    {
        _sprites = new SpriteBatch(GraphicsDevice);
        _pixel = new Texture2D(GraphicsDevice, 1, 1);
        _pixel.SetData([Color.White]);
        _audio = new TiaAudio();
    }

    protected override void UnloadContent()
    {
        _audio.Dispose();
        base.UnloadContent();
    }

    protected override void Update(GameTime gameTime)
    {
        var keyboard = Keyboard.GetState();
        if (keyboard.IsKeyDown(Keys.Escape))
        {
            Exit();
            return;
        }

        var resetDown = keyboard.IsKeyDown(Keys.Enter);
        var selectDown = keyboard.IsKeyDown(Keys.Tab);
        if (resetDown && !_resetDown)
        {
            _world.Reset();
        }

        if (selectDown && !_selectDown)
        {
            _world.Select();
        }

        _resetDown = resetDown;
        _selectDown = selectDown;

        var direction = Direction.None;
        if (keyboard.IsKeyDown(Keys.Left) || keyboard.IsKeyDown(Keys.A))
        {
            direction |= Direction.Left;
        }

        if (keyboard.IsKeyDown(Keys.Right) || keyboard.IsKeyDown(Keys.D))
        {
            direction |= Direction.Right;
        }

        if (keyboard.IsKeyDown(Keys.Up) || keyboard.IsKeyDown(Keys.W))
        {
            direction |= Direction.Up;
        }

        if (keyboard.IsKeyDown(Keys.Down) || keyboard.IsKeyDown(Keys.S))
        {
            direction |= Direction.Down;
        }

        _world.MovePlayer(direction);
        if (_world.RunsPortals)
        {
            _world.ResolveGates();
        }

        _world.ResolveCarry(keyboard.IsKeyDown(Keys.Space));
        _audio.Present(_world.Audc, _world.Audf, _world.Audv);
        if (_world.CountsInput)
        {
            _world.CountFrame();
        }
        if (_world.Player.Room != _titledRoom)
        {
            _titledRoom = _world.Player.Room;
            Window.Title = $"Adventure — {_world.CurrentRoom.Name}";
        }

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        var room = _world.CurrentRoom;
        GraphicsDevice.Clear(AtariColor(0x08));

        _sprites.Begin(samplerState: SamplerState.PointClamp);
        if (_world.HasSurround)
        {
            DrawBitmap(ObjectSprites.Surround, _world.SurroundX, _world.SurroundY, AtariColor(0x28), pixelScale: 4);
        }

        var wall = AtariColor(_world.FanfareColor ?? room.Color);
        for (var row = 0; row < MapGeometry.Rows; row++)
        {
            for (var column = 0; column < MapGeometry.Columns; column++)
            {
                if (!room.IsWall(row, column))
                {
                    continue;
                }

                var span = MapGeometry.RowSpan(row);
                _sprites.Draw(
                    _pixel,
                    new Rectangle(
                        column * MapGeometry.CellWidth * Scale,
                        span.Top * Scale,
                        MapGeometry.CellWidth * Scale,
                        span.Height * Scale),
                    wall);
            }
        }

        if (room.LeftThinWall)
        {
            DrawThinWall(MapGeometry.LeftThinX, wall);
        }

        if (room.RightThinWall)
        {
            DrawThinWall(MapGeometry.RightThinX, wall);
        }

        foreach (var item in _world.Objects)
        {
            if (item.Room != room.Id)
            {
                continue;
            }

            var graphic = item.Kind == ObjectKind.Portcullis
                ? ObjectSprites.Gate(item.State)
                : ObjectSprites.For(item.Kind, item.State);
            var pixelScale = item.Kind == ObjectKind.Bridge ? 4 : 1;
            DrawBitmap(graphic, item.X, item.Y, AtariColor(_world.DisplayColor(item.Color)), pixelScale);
        }

        if (room.Id == RoomCatalog.NumberRoom)
        {
            DrawCenteredSprite(ObjectSprites.Number(_world.GameIndex), AtariColor(0xC8));
        }

        if (room.Id == 0x1E)
        {
            DrawBitmap(ObjectSprites.Author, 0x50, 0x69, AtariColor(_world.DisplayColor(0xCB)));
        }

        DrawMarker(_world.Player.X, _world.Player.Y, AtariColor(room.Color));
        _sprites.End();

        base.Draw(gameTime);
    }

    private void DrawMarker(int x, int y, Color color)
    {
        var screenX = x * Scale;
        var screenY = MapGeometry.ScreenRow(y - 1) * Scale;
        _sprites.Draw(
            _pixel,
            new Rectangle(screenX - Scale * 2, screenY, Scale * 4, Scale * 4),
            color);
    }

    private void DrawCenteredSprite(ReadOnlySpan<byte> graphic, Color color)
    {
        var minBit = 8;
        var maxBit = -1;
        var minRow = graphic.Length;
        var maxRow = -1;
        for (var row = 0; row < graphic.Length; row++)
        {
            var bits = graphic[row];
            for (var bit = 0; bit < 8; bit++)
            {
                if ((bits & (0x80 >> bit)) == 0)
                {
                    continue;
                }

                minBit = Math.Min(minBit, bit);
                maxBit = Math.Max(maxBit, bit);
                minRow = Math.Min(minRow, row);
                maxRow = Math.Max(maxRow, row);
            }
        }

        if (maxBit < 0)
        {
            return;
        }

        var width = maxBit - minBit + 1;
        var height = maxRow - minRow + 1;
        var left = (MapGeometry.PixelWidth - width) / 2;
        var top = (MapGeometry.VisibleHeight - height) / 2;
        DrawBitmap(graphic, left - minBit, MapGeometry.ScanlineTop + 1 + minRow - top, color, pixelScale: 1);
    }

    private void DrawBitmap(ReadOnlySpan<byte> graphic, int x, int y, Color color, int pixelScale = 1)
    {
        for (var row = 0; row < graphic.Length; row++)
        {
            var bits = graphic[row];
            for (var bit = 0; bit < 8; bit++)
            {
                if ((bits & (0x80 >> bit)) == 0)
                {
                    continue;
                }

                var screenX = (x + bit * pixelScale) * Scale;
                var screenY = MapGeometry.ScreenRow(y - 1 - row) * Scale;
                _sprites.Draw(_pixel, new Rectangle(screenX, screenY, Scale * pixelScale, Scale), color);
            }
        }
    }

    private void DrawThinWall(int x, Color color)
    {
        _sprites.Draw(
            _pixel,
            new Rectangle(x * Scale, 0, Scale, MapGeometry.VisibleHeight * Scale),
            color);
    }

    private static Color AtariColor(byte color)
    {
        var (red, green, blue) = AtariPalette.Rgb(color);
        return new Color(red, green, blue);
    }
}
