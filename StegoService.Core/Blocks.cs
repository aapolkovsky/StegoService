using System;

using StegoService.Core.Helpers;

namespace StegoService.Core.Blocks
{
    public class Block<T>
    {
        public static readonly int Size = 8;

        protected T[,] m_matrix = new T[Size, Size];

        public T this[int y, int x]
        {
            get { return m_matrix[y, x]; }
            set { m_matrix[y, x] = value; }
        }

        public Block()
        {
        }

        public Block(Block<T> block)
        {
            for (int i = 0; i < Block<T>.Size; i++)
            {
                for (int j = 0; j < Block<T>.Size; j++)
                {
                    m_matrix[i, j] = block.m_matrix[i, j];
                }
            }
        }

        public static U FromMatrix<U>(T[,] matrix, int x, int y) where U : Block<T>, new()
        {
            var result = new U();
            for (int i = 0; i < Size; i++)
            {
                for (int j = 0; j < Size; j++)
                {
                    result.m_matrix[i, j] = matrix[i + y, j + x];
                }
            }
            return result;
        }

        public void PrintBlock(string formatString = "{0,-5:0.000} ")
        {
            for (int i = 0; i < Block<T>.Size; i++)
            {
                for (int j = 0; j < Block<T>.Size; j++)
                {
                    Console.Write(formatString, m_matrix[i, j]);
                }
                Console.WriteLine();
            }
        }
    }

    public sealed class ByteBlock : Block<byte>
    {
        public ByteBlock() : base()
        {
        }

        public ByteBlock(ByteBlock block) : base(block)
        {
        }

        public static ByteBlock FromMatrix(byte[,] matrix, int x, int y)
        {
            return ByteBlock.FromMatrix<ByteBlock>(matrix, x, y);
        }

        public DctBlock DCT()
        {
            double sum;
            var transformed = new DctBlock();
            for (int v = 0; v < DctBlock.Size; v++)
            {
                for (int u = 0; u < DctBlock.Size; u++)
                {
                    sum = 0;
                    for (int i = 0; i < ByteBlock.Size; i++)
                    {
                        for (int j = 0; j < ByteBlock.Size; j++)
                        {
                            sum += m_matrix[i, j] *
                                Math.Cos((Math.PI * v * (2.0 * i + 1.0)) / (2.0 * ByteBlock.Size)) *
                                Math.Cos((Math.PI * u * (2.0 * j + 1.0)) / (2.0 * ByteBlock.Size));
                        }
                    }                    
                    transformed[v, u] = (DctHelpers.FourierCoefficient(v) * DctHelpers.FourierCoefficient(u) * sum) /
                        Math.Sqrt(2.0 * ByteBlock.Size);
                }
            }
            return transformed;
        }
    }

    public sealed class DctBlock : Block<double>
    {
        private const int point1X = 6, point1Y = 2;
        private const int point2X = 4, point2Y = 4;
        private const int point3X = 2, point3Y = 6;

        public double Point1
        {
            get { return m_matrix[point1Y, point1X]; }
            set { m_matrix[point1Y, point1X] = value; }
        }

        public double Point2
        {
            get { return m_matrix[point2Y, point2X]; }
            set { m_matrix[point2Y, point2X] = value; }
        }

        public double Point3
        {
            get { return m_matrix[point3Y, point3X]; }
            set { m_matrix[point3Y, point3X] = value; }
        }

        public DctBlock() : base()
        {
        }

        public DctBlock(DctBlock block) : base(block)
        {
        }

        public static DctBlock FromMatrix(double[,] matrix, int x, int y)
        {
            return DctBlock.FromMatrix<DctBlock>(matrix, x, y);
        }

        public void InsertBit(bool bit)
        {
            double modifier = 50;
            double point1 = m_matrix[point1Y, point1X];
            double point2 = m_matrix[point2Y, point2X];
            double point3 = m_matrix[point3Y, point3X];
            if (bit)
            {
                if (point3.LessThanOrEqualWithPrecision(Math.Max(point1, point2), Precision.OneE3))
                {
                    point3 = Math.Max(point1, point2) + modifier / 2;
                    if (point1 > point2)
                    {
                        point1 -= modifier / 2;
                    }
                    else
                    {
                        point2 -= modifier / 2;
                    }
                }
            }
            else
            {
                if (point3.GreaterThanOrEqualWithPrecision(Math.Min(point1, point2), Precision.OneE3))
                {
                    point3 = Math.Min(point1, point2) - modifier / 2;
                    if (point1 < point2)
                    {
                        point1 += modifier / 2;
                    }
                    else
                    {
                        point2 += modifier / 2;
                    }
                }
            }
            m_matrix[point1Y, point1X] = point1;
            m_matrix[point2Y, point2X] = point2;
            m_matrix[point3Y, point3X] = point3;
        }

        public bool ExtractBit()
        {
            bool result;
            if (TryExtractBit(out result))
            {
                return result;
            }
            else
            {
                throw new InvalidOperationException("Bit is unextractable.");
            }
        }

        public bool TryExtractBit(out bool bit)
        {
            double point1 = m_matrix[point1Y, point1X];
            double point2 = m_matrix[point2Y, point2X];
            double point3 = m_matrix[point3Y, point3X];
            if (point3.LessThanWithPrecision(Math.Min(point1, point2), Precision.OneE3))
            {
                bit = false;
                return true;
            }
            else if (point3.GreaterThanWithPrecision(Math.Max(point1, point2), Precision.OneE3))
            {
                bit = true;
                return true;
            }
            else
            {
                bit = false;
                return false;
            }
        }

        public ByteBlock InverseDCT()
        {
            double sum;
            var block = new ByteBlock();
            for (int v = 0; v < ByteBlock.Size; v++)
            {
                for (int u = 0; u < ByteBlock.Size; u++)
                {
                    sum = 0;
                    for (int i = 0; i < DctBlock.Size; i++)
                    {
                        for (int j = 0; j < DctBlock.Size; j++)
                        {
                            sum += (DctHelpers.FourierCoefficient(i) * DctHelpers.FourierCoefficient(j) * m_matrix[i, j]) *
                                Math.Cos((Math.PI * i * (2.0 * v + 1.0)) / (2.0 * DctBlock.Size)) *
                                Math.Cos((Math.PI * j * (2.0 * u + 1.0)) / (2.0 * DctBlock.Size));
                        }
                    }
                    double value = sum / Math.Sqrt(2.0 * DctBlock.Size);
                    if (value > 255)
                    {
                        block[v, u] = 255;
                    }
                    else if (value < 0)
                    {
                        block[v, u] = 0;
                    }
                    else
                    {
                        block[v, u] = Convert.ToByte(value);
                    }
                }
            }
            return block;
        }

        public bool TestSmoothness()
        {
            double highFrequency = 0;
            double minHighFrequency = 40.0;
            int lastCell = DctBlock.Size;
            for (int i = 2; i < DctBlock.Size; i++)
            {
                for (int j = lastCell - 1; j < DctBlock.Size; j++)
                {
                    highFrequency += Math.Abs(m_matrix[i, j]);
                }
                lastCell--;
            }
            if (highFrequency.GreaterThanWithPrecision(minHighFrequency, Precision.OneE3))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool TestSharpness()
        {
            double lowFrequency = 0;
            double maxLowFrequency = 2600.0;
            int lastCell = DctBlock.Size - 1;
            for (int i = 0; i < DctBlock.Size - 1; i++)
            {
                for (int j = 0; j < lastCell; j++)
                {
                    if (i != 0 || j != 0)
                    {
                        lowFrequency += Math.Abs(m_matrix[i, j]);
                    }
                }
                lastCell--;
            }            
            if (lowFrequency.LessThanWithPrecision(maxLowFrequency, Precision.OneE3))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool TestInsertableness(bool bit)
        {
            bool result;
            var testBlock = new DctBlock(this);
            testBlock.InsertBit(bit);
            testBlock = testBlock.InverseDCT().DCT();
            if (testBlock.TestSharpness() && testBlock.TestSmoothness() && testBlock.TryExtractBit(out result))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool IsSuitable()
        {
            return TestSharpness() && TestSmoothness();
        }
    }
}
