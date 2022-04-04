using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Numerics;
using System.Windows.Documents;
using System.Windows.Media.Imaging;

namespace Lab02___Dithering_and_Color_Quantization
{
    public abstract class ColorQuantization : IEffect
    {
        public abstract WriteableBitmap ApplyTo(WriteableBitmap wbm);
    }

    public class KMeans: ColorQuantization
    {
        public int K { get; set; }
        private Dictionary<Vector3, Vector3> colorMap = new();
        private int iterations = 0;

        public KMeans(int k)  
        {
            this.K = k;
        }
        
        public override WriteableBitmap ApplyTo(WriteableBitmap wbm)
        {
            var clone = wbm.Clone();
            var width = clone.PixelWidth;
            var height = clone.PixelHeight;
            var rng = new Random();

            var centroids = new List<Vector3>(K);
            //var clusters = new Dictionary<Vector3, List<Vector3>>(K);
            var clusters = new Dictionary<Vector3, (Vector3 sum, int count)>(K);

            // initialize centroids with random colors chosen from the image
            for (var i = 0; i < K; i++)
            {
                Vector3 colorVector;
                
                while(true)
                {
                    var x = rng.Next(0, width);
                    var y = rng.Next(0, height);
                    var pixelColor = clone.GetPixelColor(x, y);
                    colorVector = new Vector3(pixelColor.R, pixelColor.G, pixelColor.B);
                    if (!centroids.Contains((colorVector)))
                        break;
                }

                centroids.Add(colorVector);
            }

            // initialize clusters with the centroids
            for (var i = 0; i < K; i++)
            {
                clusters.Add(centroids[i], (new Vector3(), 0));
            }

            // run the K-means iterative algorithm
            UpdateClusters(ref centroids, ref clusters, wbm);

            // now the final clusters are formed
            // assign their new colors to the pixels
            try
            {
                wbm.Lock();
                clone.Lock();

                for (var x = 0; x < width; x++)
                {
                    for (var y = 0; y < height; y++)
                    {
                        var oldColor = clone.GetPixelColor(x, y);
                        var oldColorVector = new Vector3(oldColor.R, oldColor.G, oldColor.B);

                        
                        clone.SetPixelColor(x, y, Color.FromArgb(
                            oldColor.A,
                            (int)colorMap[oldColorVector].X,
                            (int)colorMap[oldColorVector].Y,
                            (int)colorMap[oldColorVector].Z)
                            );
                    }
                }
            }
            finally
            {
                wbm.Unlock();
                clone.Unlock();
            }

            return clone;
        }

        private void UpdateClusters(ref List<Vector3> centroids, ref Dictionary<Vector3, (Vector3 sum, int count)> clusters, in WriteableBitmap wbm)
        {
            var width = wbm.PixelWidth;
            var height = wbm.PixelHeight;

            Debug.Print("Iteration #" + ++iterations);
            
            for (var x = 0; x < width; x++)
            {
                for (var y = 0; y < height; y++)
                {
                    // get pixel color
                    var color = wbm.GetPixelColor(x, y);
                    var colorVector = new Vector3(color.R, color.G, color.B);

                    
                    // initial values
                    var closestCentroidIndex = K;
                    var closestCentroidDistance = float.MaxValue;

                    // check which centroid it's closest to
                    for (var i = 0; i < K; i++)
                    {
                        var distance = Vector3.Distance(colorVector, centroids[i]);
                        
                        if (distance < closestCentroidDistance)
                        {
                            closestCentroidIndex = i;
                            closestCentroidDistance = distance;
                        }
                    }

                    // assign the pixel color to the closest centroid's cluster
                    clusters[centroids[closestCentroidIndex]] = 
                        (Vector3.Add(
                            clusters[centroids[closestCentroidIndex]].sum, 
                            colorVector), clusters[centroids[closestCentroidIndex]].count + 1);
                    
                    if (colorMap.ContainsKey(colorVector))
                    {
                        colorMap[colorVector] = centroids[closestCentroidIndex];
                    }
                    else
                    {
                        colorMap.Add(colorVector, centroids[closestCentroidIndex]);
                    }
                }
            }

            // now all pixel colors are assigned to centroid clusters
            // take average of the clusters and check if they're the same as 
            // the original centroids

            var averages = new List<Vector3>(K) {};
            var centroidsChanged = false;
            for (var i = 0; i < K; i++)
            {
                averages.Add(Vector3.Divide(clusters[centroids[i]].sum, clusters[centroids[i]].count));
                
                // check if average is the same as the original centroid
                var error = Vector3.Distance(averages[i], centroids[i]);
                if (error > 0.1f)
                {
                    centroidsChanged = true;
                }
            }

            // if the centroids did not change more than error threshold, then return.
            // if they changed more than the error threshold assign the new centroids,
            // clear the cluster list and re-run the iteration

            if (!centroidsChanged) return;
            
            
            clusters.Clear();
            centroids.Clear();
            centroids = averages;
            
            for (var i = 0; i < K; i++)
            {
                clusters.Add(centroids[i], (new Vector3(), 0));
            }

            UpdateClusters(ref centroids, ref clusters, wbm);
        }

        public override string ToString()
        {
            return "K-Means";
        }
    }
}