using System;

namespace Pixsper.Screamdeck;

public class ScreamDeckKeyEventArgs : EventArgs
{
    public int KeyIndex { get; init; }
    public int KeyX { get; init; }
    public int KeyY { get; init; }
    public bool IsDown { get; init; }

    public override string ToString() => $"ScreamDeckKeyEvent: Key {KeyIndex} ({KeyX}, {KeyY}) {(IsDown ? "Pressed" : "Released")}";
}