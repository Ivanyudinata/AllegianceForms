﻿using System.Drawing;

namespace AllegianceForms.Engine
{
    public class Animation
    {
        public bool Enabled { get; private set; }
        public PointF TopLeft;
        public int SectorId { get; set; }

        public Image[] Frames { get; set; }
        private int _currentFrame = 0;
        private bool _loop;
        private int _ticksBetweenFrames;
        private int _ticksToNextFrame;

        public Animation(string[] frames, float x, float y, int width, int height, int ticksBetweenFrames, bool loop)
        {
            TopLeft.X = x;
            TopLeft.Y = x;
            _loop = loop;
            _ticksBetweenFrames = ticksBetweenFrames;
            Enabled = false;

            Frames = new Image[frames.Length];
            for (var f = 0; f < frames.Length; f++)
            {
                var i = Image.FromFile(frames[f]);
                Frames[f] = new Bitmap(i, width, height);
            }
        }

        public void Resize(float width, float height)
        {
            var frames2 = new Image[Frames.Length];
            for (var f = 0; f < Frames.Length; f++)
            {
                frames2[f] = new Bitmap(Frames[f], (int)width, (int)height);
            }

            Frames = frames2;
        }

        public void Start()
        {
            Enabled = true;
            _ticksToNextFrame = _ticksBetweenFrames;
        }

        public void Stop()
        {
            Enabled = false;
            _currentFrame = 0;
        }

        public void Pause()
        {
            Enabled = false;
        }

        public void Draw(Graphics g, bool sameSector)
        {
            if (!Enabled) return;

            // Dont draw but still update frames/time if not viewing sector!
            if (sameSector) g.DrawImage(Frames[_currentFrame], TopLeft);

            if (_ticksToNextFrame <= 0)
            {
                _ticksToNextFrame = _ticksBetweenFrames;
                if (_currentFrame < Frames.Length - 1)
                {
                    _currentFrame++;
                }
                else
                {
                    _currentFrame = 0;
                    if (!_loop) Enabled = false;
                }
            }

            _ticksToNextFrame--;
        }
    }
}