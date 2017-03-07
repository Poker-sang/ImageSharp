// ReSharper disable ObjectCreationAsStatement
// ReSharper disable InconsistentNaming
namespace ImageSharp.Tests.Common
{
    using System;
    using System.Runtime.CompilerServices;

    using Xunit;

    public unsafe class BufferPointerTests
    {
        public struct Foo
        {
            public int A;

            public double B;

            public Foo(int a, double b)
            {
                this.A = a;
                this.B = b;
            }

            internal static Foo[] CreateArray(int size)
            {
                Foo[] result = new Foo[size];
                for (int i = 0; i < size; i++)
                {
                    result[i] = new Foo(i, i);
                }
                return result;
            }
        }
        
        [Fact]
        public void ConstructWithoutOffset()
        {
            Foo[] array = Foo.CreateArray(3);
            fixed (Foo* p = array)
            {
                // Act:
                BufferPointer<Foo> ap = new BufferPointer<Foo>(array, p);

                // Assert:
                Assert.Equal(array, ap.Array);
                Assert.Equal((IntPtr)p, ap.PointerAtOffset);
            }
        }

        [Fact]
        public void ConstructWithOffset()
        {
            Foo[] array = Foo.CreateArray(3);
            int offset = 2;
            fixed (Foo* p = array)
            {
                // Act:
                BufferPointer<Foo> ap = new BufferPointer<Foo>(array, p, offset);

                // Assert:
                Assert.Equal(array, ap.Array);
                Assert.Equal(offset, ap.Offset);
                Assert.Equal((IntPtr)(p+offset), ap.PointerAtOffset);
            }
        }

        [Fact]
        public void Slice()
        {
            Foo[] array = Foo.CreateArray(5);
            int offset0 = 2;
            int offset1 = 2;
            int totalOffset = offset0 + offset1;
            fixed (Foo* p = array)
            {
                BufferPointer<Foo> ap = new BufferPointer<Foo>(array, p, offset0);

                // Act:
                ap = ap.Slice(offset1);

                // Assert:
                Assert.Equal(array, ap.Array);
                Assert.Equal(totalOffset, ap.Offset);
                Assert.Equal((IntPtr)(p + totalOffset), ap.PointerAtOffset);
            }
        }

        public class Copy
        {
            private static void AssertNotDefault<T>(T[] data, int idx)
                where T : struct
            {
                Assert.NotEqual(default(T), data[idx]);
            }

            [Theory]
            [InlineData(4)]
            [InlineData(1500)]
            public void GenericToOwnType(int count)
            {
                Foo[] source = Foo.CreateArray(count + 2);
                Foo[] dest = new Foo[count + 5];

                fixed (Foo* pSource = source)
                fixed (Foo* pDest = dest)
                {
                    BufferPointer<Foo> apSource = new BufferPointer<Foo>(source, pSource);
                    BufferPointer<Foo> apDest = new BufferPointer<Foo>(dest, pDest);

                    BufferPointer.Copy(apSource, apDest, count);
                }

                AssertNotDefault(source, 1);
                AssertNotDefault(dest, 1);

                Assert.Equal(source[0], dest[0]);
                Assert.Equal(source[1], dest[1]);
                Assert.Equal(source[count-1], dest[count-1]);
                Assert.NotEqual(source[count], dest[count]);
            }
            
            [Theory]
            [InlineData(4)]
            [InlineData(1500)]
            public void GenericToBytes(int count)
            {
                int destCount = count * sizeof(Foo);
                Foo[] source = Foo.CreateArray(count + 2);
                byte[] dest = new byte[destCount + sizeof(Foo) + 1];

                fixed (Foo* pSource = source)
                fixed (byte* pDest = dest)
                {
                    BufferPointer<Foo> apSource = new BufferPointer<Foo>(source, pSource);
                    BufferPointer<byte> apDest = new BufferPointer<byte>(dest, pDest);

                    BufferPointer.Copy(apSource, apDest, count);
                }

                AssertNotDefault(source, 1);

                Assert.True(ElementsAreEqual(source, dest, 0));
                Assert.True(ElementsAreEqual(source, dest, count - 1));
                Assert.False(ElementsAreEqual(source, dest, count));
            }

            private static byte[] CreateTestBytes(int count)
            {
                byte[] result = new byte[count];
                for (int i = 0; i < result.Length; i++)
                {
                    result[i] = (byte)(i % 255);
                }
                return result;
            }

            [Theory]
            [InlineData(4)]
            [InlineData(1500)]
            public void BytesToGeneric(int count)
            {
                int srcCount = count * sizeof(Foo);
                byte[] source = CreateTestBytes(srcCount);
                Foo[] dest = new Foo[count + 2];
                
                fixed(byte* pSource = source)
                fixed (Foo* pDest = dest)
                {
                    BufferPointer<byte> apSource = new BufferPointer<byte>(source, pSource);
                    BufferPointer<Foo> apDest = new BufferPointer<Foo>(dest, pDest);

                    BufferPointer.Copy(apSource, apDest, count);
                }

                AssertNotDefault(source, sizeof(Foo) + 1);
                AssertNotDefault(dest, 1);

                Assert.True(ElementsAreEqual(dest, source, 0));
                Assert.True(ElementsAreEqual(dest, source, 1));
                Assert.True(ElementsAreEqual(dest, source, count - 1));
                Assert.False(ElementsAreEqual(dest, source, count));
            }

            [Fact]
            public void ColorToBytes()
            {
                Color[] colors = { new Color(0, 1, 2, 3), new Color(4, 5, 6, 7), new Color(8, 9, 10, 11), };

                using (PinnedBuffer<Color> colorBuf = new PinnedBuffer<Color>(colors))
                using (PinnedBuffer<byte> byteBuf = new PinnedBuffer<byte>(colors.Length*4))
                {
                    BufferPointer.Copy<Color>(colorBuf, byteBuf, colorBuf.Count);

                    byte[] a = byteBuf.Array;

                    for (int i = 0; i < byteBuf.Count; i++)
                    {
                        Assert.Equal((byte)i, a[i]);
                    }
                }
            }
            
            private static bool ElementsAreEqual(Foo[] array, byte[] rawArray, int index)
            {
                fixed (Foo* pArray = array)
                fixed (byte* pRaw = rawArray)
                {
                    Foo* pCasted = (Foo*)pRaw;

                    Foo val1 = pArray[index];
                    Foo val2 = pCasted[index];

                    return val1.Equals(val2);
                }
            }
        }
    }
}