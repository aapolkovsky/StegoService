using System;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Collections;
using System.Collections.Generic;

using StegoService.Core.Helpers;

namespace StegoService.Core.BitmapContainer
{
    public enum RgbChannels
    {
        Red,
        Green,
        Blue
    }

    public sealed class BitmapContainer
    {
        private readonly Bitmap m_bitmap;

        private readonly int m_height;
        private readonly int m_width;

        private byte[,] m_redChannel, m_greenChannel, m_blueChannel;

        public Bitmap Bitmap
        {
            get { return m_bitmap; }
        }

        public int Height
        {
            get { return m_height; }
        }

        public int Width
        {
            get { return m_width; }
        }

        public BitmapContainer(Bitmap bitmap)
        {
            m_bitmap = new Bitmap(bitmap);

            m_height = m_bitmap.Height;
            m_width = m_bitmap.Width;

            m_redChannel = new byte[m_height, m_width];
            m_greenChannel = new byte[m_height, m_width];
            m_blueChannel = new byte[m_height, m_width];

            SplitColors();
        }

        public void ResetColor(RgbChannels channel)
        {
            SplitColor(channel);
        }

        public void ResetColors()
        {
            SplitColors();
        }

        private void SplitColor(RgbChannels channel)
        {
            for (int i = 0; i < m_height; i++)
            {
                for (int j = 0; j < m_width; j++)
                {
                    var color = m_bitmap.GetPixel(j, i);
                    switch (channel)
                    {
                        case RgbChannels.Red:
                            m_redChannel[i, j] = color.R;
                            break;
                        case RgbChannels.Green:
                            m_greenChannel[i, j] = color.G;
                            break;
                        case RgbChannels.Blue:
                            m_blueChannel[i, j] = color.B;
                            break;
                    }
                }
            }
        }

        private void SplitColors()
        {
            for (int i = 0; i < m_height; i++)
            {
                for (int j = 0; j < m_width; j++)
                {
                    var color = m_bitmap.GetPixel(j, i);
                    m_redChannel[i, j] = color.R;
                    m_greenChannel[i, j] = color.G;
                    m_blueChannel[i, j] = color.B;
                }
            }
        }

        public void BuildFromColors()
        {
            for (int i = 0; i < m_height; i++)
            {
                for (int j = 0; j < m_width; j++)
                {
                    var color = Color.FromArgb(m_redChannel[i, j], m_greenChannel[i, j], m_blueChannel[i, j]);
                    m_bitmap.SetPixel(j, i, color);
                }
            }
        }


        public void InsertStego(string text)
        {
            if (!TryInsertStego(text))
            {
                throw new InvalidOperationException("Not enough space.");
            }
        }

        public bool TryInsertStego(string text)
        {
            var bitArray = text.GetBits();
            var blocks = MatrixHelpers.ToBlocks(m_blueChannel);
            var transformedBlocks = blocks
                .Select(block => block.DCT())
                .ToArray();
            var suitableBlocks = transformedBlocks
                .Where(block => block.IsSuitable())
                .ToArray();
            if (suitableBlocks.Length < bitArray.Count)
            {
                return false;
            }
            int i = 0;
            foreach (var block in suitableBlocks)
            {
                if (i == bitArray.Count)
                {
                    break;
                }
                else if (block.TestInsertableness(bitArray[i]))
                {
                    block.InsertBit(bitArray[i]);
                    i++;
                }
                else
                {
                    block.InsertBit(bitArray[i]);
                }
            }
            if (i != bitArray.Count)
            {
                return false;
            }
            var blocks2 = transformedBlocks
                .Select(block => block.InverseDCT())
                .ToArray();
            MatrixHelpers.FillMatrix(m_blueChannel, blocks2);
            BuildFromColors();
            return true;
        }

        public string ExtractStego()
        {
            var blocks = MatrixHelpers.ToBlocks(m_blueChannel);
            var transformedBlocks = blocks
                .Select(block => block.DCT())
                .ToArray();
            var suitableBlocks = transformedBlocks
                .Where(block => block.IsSuitable());            
            var byteList = new List<byte>();
            var bitArray = new BitArray(8);
            int bitCount = 0;
            foreach (var block in suitableBlocks)
            {
                bool bit;
                if (block.TryExtractBit(out bit))
                {
                    bitArray.Set(bitCount, bit);
                    bitCount++;
                    if (bitCount == 8)
                    {
                        var byteArray = bitArray.ToByteArray();
                        if (byteArray[0] == 0)
                            break;
                        bitCount = 0;
                        byteList.Add(byteArray[0]);
                    }
                }
            }
            return Encoding.UTF8.GetString(byteList.ToArray());
        }
    }
}
