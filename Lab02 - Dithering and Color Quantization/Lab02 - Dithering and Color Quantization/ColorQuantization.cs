using System;
using System.Collections.Generic;
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
            var clusters = new Dictionary<Vector3, List<Vector3>>(K);

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
                clusters.Add(centroids[i], new List<Vector3>());
            }

            // run the K-means iterative algorithm
            UpdateClusters(ref centroids, ref clusters, wbm);

            // now the final clusters are formed
            // assign pixels to their new colors

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

                        for (var i = 0; i < K; i++)
                        {
                            if (clusters[centroids[i]].Contains(oldColorVector))
                            {
                                clone.SetPixelColor(x, y, Color.FromArgb(
                                    oldColor.A,
                                    (int)centroids[i].X,
                                    (int)centroids[i].Y,
                                    (int)centroids[i].Z)
                                    );
                                break;
                            }
                        }

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

        private void UpdateClusters(ref List<Vector3> centroids, ref Dictionary<Vector3, List<Vector3>> clusters, in WriteableBitmap wbm)
        {
            var width = wbm.PixelWidth;
            var height = wbm.PixelHeight;
            
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
                    clusters[centroids[closestCentroidIndex]].Add(colorVector);
                }
            }

            // now all pixel colors are assigned to centroid clusters
            // take average of the clusters and check if they're the same as 
            // the original centroids

            var averages = new List<Vector3>(K) {};
            var centroidsChanged = false;
            float error = 0.0f;
            for (var i = 0; i < K; i++)
            {
                var sum = new Vector3(0, 0, 0);
                for (var j = 0; j < clusters[centroids[i]].Count; j++)
                {
                    sum = Vector3.Add(sum, clusters[centroids[i]][j]);
                }

                averages.Add(Vector3.Divide(sum, clusters[centroids[i]].Count));
                
                // check if average is the same as the original centroid
                error = Vector3.Distance(averages[i], centroids[i]);
                if (error > 0.1f)
                {
                    centroidsChanged = true;
                }
            }

            // if the centroids changed assign the new centroids, clear the cluster list
            // and re-run the iteration

            if (centroidsChanged)
            {
                centroids.Clear();
                centroids = averages;
                
                clusters.Clear();
                for (var i = 0; i < K; i++)
                {
                    clusters.Add(centroids[i], new List<Vector3>());
                }

                UpdateClusters(ref centroids, ref clusters, wbm);
            }
        }



        public override string ToString()
        {
            return "K-Means";
        }
    }
}
