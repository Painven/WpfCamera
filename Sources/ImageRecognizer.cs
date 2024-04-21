using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;

namespace WpfCamera
{
    public class ImageRecognizer
    {
        public async Task<List<bool>> GetImageBoolMap(string fileName)
        {
            List<bool> lResult = new List<bool>();

            await Task.Run(() =>
            {
                //create new image with 16x16 pixel
                using var bmpSrc = Bitmap.FromFile(fileName);
                using Bitmap bmpMin = new Bitmap(bmpSrc, new Size(16, 16));

                for (int j = 0; j < bmpMin.Height; j++)
                {
                    for (int i = 0; i < bmpMin.Width; i++)
                    {
                        //reduce colors to true / false                
                        lResult.Add(bmpMin.GetPixel(i, j).GetBrightness() < 0.5f);
                    }
                }
            });

            return lResult;
        }

        public bool IsSameImage(List<bool> imageMapLeft, List<bool> imageMapright)
        {
            //determine the number of equal pixel (x of 256)
            int equalElements = imageMapLeft.Zip(imageMapright, (i, j) => i == j).Count(eq => eq);
            return equalElements >= 253;
        }

        public static List<bool> GetHash(Bitmap bmpSource)
        {
            List<bool> lResult = new List<bool>();
            //create new image with 16x16 pixel
            Bitmap bmpMin = new Bitmap(bmpSource, new Size(16, 16));
            for (int j = 0; j < bmpMin.Height; j++)
            {
                for (int i = 0; i < bmpMin.Width; i++)
                {
                    //reduce colors to true / false                
                    lResult.Add(bmpMin.GetPixel(i, j).GetBrightness() < 0.5f);
                }
            }
            return lResult;
        }
    }
}
