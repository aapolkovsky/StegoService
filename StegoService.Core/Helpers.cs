using System;
using System.Text;
using System.Collections;

using StegoService.Core.Blocks;

namespace StegoService.Core.Helpers
{
    public static class DctHelpers
    {
        public static double FourierCoefficient(int x)
        {
            return x > 0 ? 1.0 : (1.0 / Math.Sqrt(2));
        }
    }

    public static class BitArrayHelpers
    {
        public static BitArray GetBits(this string text)
        {
            var strBytes = Encoding.UTF8.GetBytes(text + '\0');
            var bitArray = new BitArray(strBytes);
            return bitArray;
        }

        public static byte[] ToByteArray(this BitArray bitArray)
        {
            int length = (bitArray.Count - 1) / 8 + 1;
            var byteArray = new byte[length];
            bitArray.CopyTo(byteArray, 0);
            return byteArray;
        }
    }

    public static class Precision
    {
        public const double OneE1   = 0.1;
        public const double OneE2   = 0.01;
        public const double OneE3   = 0.001;
        public const double OneE4   = 0.0001;
        public const double OneE5   = 0.00001;
        public const double OneE6   = 0.000001;
        public const double OneE7   = 0.0000001;
        public const double OneE8   = 0.00000001;
        public const double OneE9   = 0.000000001;
        public const double OneE10  = 0.0000000001;

        public static bool EqualsWithPrecision(this double num1, double num2, double precision = OneE5)
        {
            return Math.Abs(num1 - num2) < precision;
        }

        public static bool NotEqualsWithPrecision(this double num1, double num2, double precision = OneE5)
        {
            return Math.Abs(num1 - num2) >= precision;
        }

        public static bool LessThanWithPrecision(this double num1, double num2, double precision = OneE5)
        {
            return (num2 - num1) >= precision;
        }

        public static bool LessThanOrEqualWithPrecision(this double num1, double num2, double precision = OneE5)
        {
            return (num2 - num1) > precision || Math.Abs(num1 - num2) < precision;
        }

        public static bool GreaterThanWithPrecision(this double num1, double num2, double precision = OneE5)
        {
            return (num1 - num2) >= precision;
        }

        public static bool GreaterThanOrEqualWithPrecision(this double num1, double num2, double precision = OneE5)
        {
            return (num1 - num2) > precision || Math.Abs(num1 - num2) < precision;
        }
    }

    public static class MatrixHelpers
    {
        public static ByteBlock[] ToBlocks(byte[,] matrix)
        {
            int x = (matrix.GetLength(1) / ByteBlock.Size) * ByteBlock.Size;
            int y = (matrix.GetLength(0) / ByteBlock.Size) * ByteBlock.Size;
            int blocksCount = (x * y) / (ByteBlock.Size * ByteBlock.Size);
            var blocks = new ByteBlock[blocksCount];
            int x1 = 0;
            int y1 = 0;
            for (int i = 0; i < blocksCount; i++)
            {
                x1 = (ByteBlock.Size * i) % x;
                blocks[i] = ByteBlock.FromMatrix<ByteBlock>(matrix, x1, y1);
                if (x1 + ByteBlock.Size == x)
                {
                    y1 += ByteBlock.Size;
                }
            }
            return blocks;
        }

        public static byte[,] ToMatrix(ByteBlock[] blocks, int width, int height)
        {
            var matrix = new byte[height, width];
            int x = (width / ByteBlock.Size) * ByteBlock.Size;
            int y = (height / ByteBlock.Size) * ByteBlock.Size;
            int blocksCount = (x * y) / (ByteBlock.Size * ByteBlock.Size);
            int x1 = 0;
            int y1 = 0;
            for (int k = 0; k < blocksCount; k++)
            {
                x1 = (ByteBlock.Size * k) % x;
                for (int i = 0; i < ByteBlock.Size; i++)
                {
                    for (int j = 0; j < ByteBlock.Size; j++)
                    {
                        matrix[i + y1, j + x1] = blocks[k][i, j];
                    }
                }
                if (x1 + ByteBlock.Size == x)
                {
                    y1 += ByteBlock.Size;
                }
            }
            return matrix;
        }

        public static void FillMatrix(byte[,] matrix, ByteBlock[] blocks)
        {
            int x = (matrix.GetLength(1) / ByteBlock.Size) * ByteBlock.Size;
            int y = (matrix.GetLength(0) / ByteBlock.Size) * ByteBlock.Size;
            int blocksCount = (x * y) / (ByteBlock.Size * ByteBlock.Size);
            int x1 = 0;
            int y1 = 0;
            for (int k = 0; k < blocksCount; k++)
            {
                x1 = (ByteBlock.Size * k) % x;
                for (int i = 0; i < ByteBlock.Size; i++)
                {
                    for (int j = 0; j < ByteBlock.Size; j++)
                    {
                        matrix[i + y1, j + x1] = blocks[k][i, j];
                    }
                }
                if (x1 + ByteBlock.Size == x)
                {
                    y1 += ByteBlock.Size;
                }
            }
        }
    }
}
