using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace OpenALPR_CS
{
    class FpsCounter
    {
        private long[] elapsedArray;
        private int currentIndex = 0;
        private Stopwatch sw = new Stopwatch(); 
        
        public FpsCounter()
        {
            elapsedArray = new long[5];
        }

        public FpsCounter(int bufferSize)
        {
            elapsedArray = new long[bufferSize];
        }

        public void Restart()
        {
            sw.Restart();
        }

        public void Stop()
        {
            sw.Stop();
            elapsedArray[GetNextIndex()] = sw.ElapsedMilliseconds;
        }

        private int GetNextIndex()
        {
            currentIndex++;
            if(currentIndex >= elapsedArray.Length)
            {
                currentIndex = 0;
            }
            return currentIndex;
        }

        public double GetFPS()
        {
            int len = elapsedArray.Length;
            long accum = 0;
            for(int i=0; i < len; i++)
            {
                accum += elapsedArray[i];
            }
            double mspf = ((double)accum) / ((double)len);
            double fps = 1000.0 / mspf;
            return fps;
        }

    }
}
