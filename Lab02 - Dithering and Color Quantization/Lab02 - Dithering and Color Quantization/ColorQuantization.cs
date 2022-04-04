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
        
        private readonly Dictionary<Vector3, Vector3> _colorMap = new();
        private int _iterations;
        private readonly Random _rng = new();
        private List<Vector3> _centroids;
        private readonly Dictionary<Vector3, (Vector3 sum, int count)> _clusters;
        public KMeans(int k)  
        {
            this.K = k;
            _centroids = new List<Vector3>(K);
            _clusters = new Dictionary<Vector3, (Vector3 sum, int count)>(K);
        }
        
        public override WriteableBitmap ApplyTo(WriteableBitmap wbm)
        {
            var clone = wbm.Clone();
            var width = clone.PixelWidth;
            var height = clone.PixelHeight;
            

            // initialize centroids with random colors chosen from the image
            for (var i = 0; i < K; i++)
            {
                Vector3 colorVector;
                
                while(true)
                {
                    var x = _rng.Next(0, width);
                    var y = _rng.Next(0, height);
                    var pixelColor = clone.GetPixelColor(x, y);
                    colorVector = new Vector3(pixelColor.R, pixelColor.G, pixelColor.B);
                    if (!_centroids.Contains((colorVector)))
                        break;
                }

                _centroids.Add(colorVector);
            }

            // initialize clusters with the centroids
            for (var i = 0; i < K; i++)
            {
                _clusters.Add(_centroids[i], (new Vector3(), 0));
            }

            // run the K-means iterative algorithm
            UpdateClusters(wbm);

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
                            (int)_colorMap[oldColorVector].X,
                            (int)_colorMap[oldColorVector].Y,
                            (int)_colorMap[oldColorVector].Z)
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

        private void UpdateClusters(in WriteableBitmap wbm)
        {
            var width = wbm.PixelWidth;
            var height = wbm.PixelHeight;
            
            while (true)
            {
                Debug.Print("Iteration #" + ++_iterations);

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
                            var distance = Vector3.Distance(colorVector, _centroids[i]);

                            if (distance < closestCentroidDistance)
                            {
                                closestCentroidIndex = i;
                                closestCentroidDistance = distance;
                            }
                        }

                        // assign the pixel color to the closest centroid's cluster
                        _clusters[_centroids[closestCentroidIndex]] = (Vector3.Add(_clusters[_centroids[closestCentroidIndex]].sum, colorVector), _clusters[_centroids[closestCentroidIndex]].count + 1);

                        if (_colorMap.ContainsKey(colorVector))
                        {
                            _colorMap[colorVector] = _centroids[closestCentroidIndex];
                        }
                        else
                        {
                            _colorMap.Add(colorVector, _centroids[closestCentroidIndex]);
                        }
                    }
                }

                // now all pixel colors are assigned to centroid clusters
                // take average of the clusters and check if they're the same as 
                // the original centroids
                var distanceThreshold = 0.1f;
                var averages = new List<Vector3>(K) { };
                var centroidsChangedAboveThreshold = false;
                for (var i = 0; i < K; i++)
                {
                    averages.Add(Vector3.Divide(_clusters[_centroids[i]].sum, _clusters[_centroids[i]].count));

                    // check if averages are within threshold of the original centroid
                    var distance = Vector3.Distance(averages[i], _centroids[i]);
                    if (distance > distanceThreshold)
                    {
                        centroidsChangedAboveThreshold = true;
                    }
                }

                // if the centroids did not change more than the threshold, then return.
                // if they changed more than the threshold assign the new centroids,
                // clear the cluster list and re-run the iteration

                if (centroidsChangedAboveThreshold == false) return;

                _clusters.Clear();
                _centroids.Clear();
                _centroids = averages;

                for (var i = 0; i < K; i++)
                {
                    _clusters.Add(_centroids[i], (new Vector3(), 0));
                }
            }
        }

        public override string ToString()
        {
            return "K-Means";
        }
    }
}