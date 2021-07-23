using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace FacesToCubemap
{
    class Program
    {
        static void Main(string[] args)
        {
            // Example on how to use
            Bitmap[] faces = new Bitmap[6];
            faces[0] = new Bitmap("NegativeX.png"); // Left
            faces[1] = new Bitmap("PositiveY.png"); // Top
            faces[2] = new Bitmap("PositiveZ.png"); // Front 
            faces[3] = new Bitmap("NegativeY.png"); // Bottom
            faces[4] = new Bitmap("PositiveX.png"); // Right
            faces[5] = new Bitmap("NegativeZ.png"); // Back

            Bitmap cubeMap = CreateCubemap(faces);
            cubeMap.Save("Cubemap.png", ImageFormat.Png);
        }

        private static Bitmap CreateCubemap(Bitmap[] faces)
        {
            VerifyFaces(faces);

            int numberOfFaces = faces.Length;
            int faceSize = faces[0].Width;
            int panoramaWidth = faceSize * 4;
            int panoramaHeight = faceSize * 3;
            Bitmap panorama = new Bitmap(panoramaWidth, panoramaHeight, PixelFormat.Format32bppArgb);

            BitmapData panoramaData = panorama.LockBits(new Rectangle(0, 0, panoramaWidth, panoramaHeight), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
            byte[] panoramaBytes = new byte[Math.Abs(panoramaData.Stride * panoramaHeight)];
            IntPtr scan0Panorama = panoramaData.Scan0;
            Marshal.Copy(scan0Panorama, panoramaBytes, 0, panoramaBytes.Length);

            BitmapData[] facesData = new BitmapData[numberOfFaces];
            byte[][] facesBytes = new byte[numberOfFaces][];
            IntPtr[] scan0Faces = new IntPtr[numberOfFaces];
            for (int i = 0; i < numberOfFaces; i++)
            {
                facesData[i] = faces[i].LockBits(new Rectangle(0, 0, faceSize, faceSize),
                    ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);

                facesBytes[i] = new byte[Math.Abs(facesData[i].Stride * faceSize)];
                scan0Faces[i] = facesData[i].Scan0;
                Marshal.Copy(scan0Faces[i], facesBytes[i], 0, facesBytes[i].Length);
            }

            Task[] tasks = new Task[6];
            int size = panoramaBytes.Length / 6;
            for (int j = 0; j < tasks.Length; j++)
            {
                int jj = j;
                tasks[j] = Task.Factory.StartNew(() =>
                {
                    int iMin = size * jj;
                    int iMax = iMin + size;

                    for (int i = iMin; i < iMax; i += 4)
                    {
                        int ii = i / 4;

                        int column = ii % panoramaWidth;
                        int row = (int)Math.Floor(((double)ii / panoramaWidth));

                        int sColumn = (int)Math.Floor((double)column / faceSize);
                        int sRow = (int)Math.Floor((double)row / faceSize);

                        byte pixelB = 0;
                        byte pixelR = 0;
                        byte pixelG = 0;
                        byte pixelA = 0;

                        if (!IsEmpty(sRow, sColumn))
                        {
                            int index = GetIndex(sRow, sColumn);
                            int index2 = ((column % faceSize) + (((row % faceSize) * faceSize))) * 4;

                            byte _b = facesBytes[index][index2];
                            byte _r = facesBytes[index][index2 + 1];
                            byte _g = facesBytes[index][index2 + 2];
                            byte _a = facesBytes[index][index2 + 3];

                            pixelB = _b;
                            pixelR = _r;
                            pixelG = _g;
                            pixelA = _a;

                        }

                        panoramaBytes[i] = pixelB;
                        panoramaBytes[i + 1] = pixelR;
                        panoramaBytes[i + 2] = pixelG;
                        panoramaBytes[i + 3] = pixelA;
                    }
                });
            }
            Task.WaitAll(tasks);

            for (int i = 0; i < numberOfFaces; i++)
            {
                faces[i].UnlockBits(facesData[i]);
            }

            Marshal.Copy(panoramaBytes, 0, scan0Panorama, panoramaBytes.Length);
            panorama.UnlockBits(panoramaData);

            return panorama;
        }

        private static void VerifyFaces(Bitmap[] faces)
        {
            int faceSize = -1;
            foreach (Bitmap face in faces)
            {
                if (face == null)
                    throw new ArgumentNullException("Some faces are missing");

                if (faceSize == -1)
                    faceSize = face.Width;

                if (face.Width != faceSize || face.Height != faceSize)
                    throw new ArgumentException("All faces must be square and same size");
            }
        }

        private static bool IsEmpty(int row, int column)
        {
            if (row == 0 && column == 0)
                return true;
            if (row == 0 && column == 2)
                return true;
            if (row == 0 && column == 3)
                return true;
            if (row == 2 && column == 0)
                return true;
            if (row == 2 && column == 2)
                return true;
            if (row == 2 && column == 3)
                return true;

            return false;
        }

        private static int GetIndex(int row, int column)
        {
            if (row == 1 && column == 0)
                return 0;
            if (row == 0 && column == 1)
                return 1;
            if (row == 1 && column == 1)
                return 2;
            if (row == 2 && column == 1)
                return 3;
            if (row == 1 && column == 2)
                return 4;
            if (row == 1 && column == 3)
                return 5;

            return -1;
        }
    }
}
